using System.Text.Json;
using FspPlatformApi.Data.Entities;
using FspPlatformApi.DTOs.Tasks;
using FspPlatformApi.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FspPlatformApi.Services.Platform;

/// <summary>
/// Регулярные короткие задания (ТЗ п.2, дополнительный функционал):
/// работодатель создаёт задачу → назначает кандидатам из подборки (массово) →
/// кандидат решает или предлагает подход → работодатель оценивает (свежий сигнал,
/// обновляет last_activity_at профиля).
///
/// Жизненный цикл назначения (candidate_tasks): назначено → в_работе → выполнено,
/// либо пропущено / срок_истек / отменено (отменено — только до начала работы,
/// инициатива работодателя, строка не удаляется — статистика и история сохраняются).
///
/// Повторное назначение после отмены: строка РЕАКТИВИРУЕТСЯ (возврат в "назначено"
/// с очисткой ответа, оценки и дат), а не создаётся заново — в таблице уникальный
/// ключ (task_id, candidate_id), вторая строка тому же кандидату невозможна.
///
/// Отменённые назначения:
/// - исключаются из «Ответов кандидатов» (это рабочая очередь, ответа от них не будет);
/// - исключаются из счётчика «Назначено» в статистике задачи (не активная воронка);
/// - НЕ исключаются из picker'а назначения — их можно назначить повторно;
/// - строка в БД со статусом "отменено" сохраняется — история и аудит целы.
/// </summary>
public class EmployerTaskService : IEmployerTaskService
{
    private readonly ApplicationDbContext _db;
    private readonly INotificationService _notify;

    public EmployerTaskService(ApplicationDbContext db, INotificationService notify)
        => (_db, _notify) = (db, notify);

    /// <summary>Компания текущего работодателя. Без неё — понятная ошибка, а не "Sequence contains no elements".</summary>
    private async Task<Company> MyCompanyAsync(Guid ownerUserId) =>
        await _db.Companies.FirstOrDefaultAsync(c => c.OwnerUserId == ownerUserId && c.Status == "активен")
        ?? throw new InvalidOperationException("Сначала создайте профиль компании.");

    public async Task<Guid> CreateTaskAsync(Guid ownerUserId, EmployerTaskUpsertDto dto)
    {
        var company = await MyCompanyAsync(ownerUserId);
        var t = new EmployerTask
        {
            CompanyId = company.Id,
            SpecializationId = (short)dto.SpecializationId,
            GradeId = dto.GradeId,
            Title = dto.Title,
            Description = dto.Description,
            Payload = dto.Payload.HasValue ? dto.Payload.Value.GetRawText() : null, // jsonb как string
            MaxScore = dto.MaxScore ?? 10,
            // ВАЖНО: задаём явно. CLR-дефолт bool — false, и если не задать значение,
            // EF вставляет false, игнорируя DEFAULT TRUE в БД — задача сразу "в архиве".
            IsActive = true
        };
        _db.EmployerTasks.Add(t);
        await _db.SaveChangesAsync();
        return t.Id;
    }

    public async Task AssignAsync(Guid ownerUserId, Guid taskId, AssignTaskDto dto)
    {
        var company = await MyCompanyAsync(ownerUserId);
        var task = await _db.EmployerTasks
            .FirstOrDefaultAsync(t => t.Id == taskId && t.CompanyId == company.Id)
            ?? throw new KeyNotFoundException("Задача не найдена.");
        if (!task.IsActive)
            throw new InvalidOperationException("Задача в архиве — сначала создайте новую.");

        var existing = await _db.CandidateTasks
            .FirstOrDefaultAsync(x => x.TaskId == taskId && x.CandidateId == dto.CandidateId);

        if (existing is not null && existing.Status != "отменено")
            throw new InvalidOperationException("Задание уже назначено этому кандидату.");

        if (existing is not null)
        {
            // Реактивация ранее отменённого назначения: одна строка на пару
            // (задача, кандидат) — уникальный ключ не даст создать вторую.
            Reactivate(existing);
        }
        else
        {
            _db.CandidateTasks.Add(new CandidateTask { TaskId = taskId, CandidateId = dto.CandidateId });
        }

        await _db.SaveChangesAsync();

        await _notify.CreateAsync(dto.CandidateId, "новое_задание",
            new { taskId, title = task.Title, company = company.Name });
    }

    /// <summary>
    /// Массовое назначение задачи выбранным кандидатам (из модального окна подборки).
    /// Активные назначения пропускаются, отменённые — реактивируются.
    /// Возвращает число реально назначенных (новых + реактивированных).
    /// </summary>
    public async Task<int> AssignManyAsync(Guid ownerUserId, Guid taskId, AssignTaskBulkDto dto)
    {
        var company = await MyCompanyAsync(ownerUserId);
        var task = await _db.EmployerTasks
            .FirstOrDefaultAsync(t => t.Id == taskId && t.CompanyId == company.Id)
            ?? throw new KeyNotFoundException("Задача не найдена.");
        if (!task.IsActive)
            throw new InvalidOperationException("Задача в архиве — сначала создайте новую.");

        var ids = dto.CandidateIds.Distinct().ToList();
        if (ids.Count == 0)
            throw new InvalidOperationException("Выберите хотя бы одного кандидата.");
        if (ids.Count > 50)
            throw new InvalidOperationException("Не более 50 кандидатов за одно назначение.");

        var existingRows = await _db.CandidateTasks
            .Where(x => x.TaskId == taskId && ids.Contains(x.CandidateId))
            .ToListAsync();

        // уже активное назначение — пропускаем молча; отменённое — реактивируем
        var toReactivate = existingRows.Where(x => x.Status == "отменено").ToList();
        var toCreate = ids.Except(existingRows.Select(x => x.CandidateId)).ToList();

        foreach (var row in toReactivate)
            Reactivate(row);

        foreach (var candidateId in toCreate)
            _db.CandidateTasks.Add(new CandidateTask { TaskId = taskId, CandidateId = candidateId });

        var assignedCount = toReactivate.Count + toCreate.Count;

        if (assignedCount > 0)
            await _db.SaveChangesAsync();

        var notifiedIds = toReactivate.Select(x => x.CandidateId).Concat(toCreate).ToList();
        foreach (var candidateId in notifiedIds)
            await _notify.CreateAsync(candidateId, "новое_задание",
                new { taskId, title = task.Title, company = company.Name });

        return assignedCount;
    }

    /// <summary>
    /// Возврат отменённого назначения в работу: статус "назначено",
    /// сброс ответа, оценки и дат — кандидат начинает задание заново,
    /// но строка (и её история) остаётся одна.
    /// </summary>
    private static void Reactivate(CandidateTask row)
    {
        row.Status = "назначено";
        row.AssignedAt = DateTime.UtcNow;
        row.CompletedAt = null;
        row.AnswerText = null;
        row.AnswerPayload = null;
        row.Score = null;
    }

    /// <summary>Архивация задачи: новые назначения закрыты, уже выданные задания продолжают жить.</summary>
    public async Task ArchiveAsync(Guid ownerUserId, Guid taskId)
    {
        var company = await MyCompanyAsync(ownerUserId);
        var task = await _db.EmployerTasks
            .FirstOrDefaultAsync(t => t.Id == taskId && t.CompanyId == company.Id)
            ?? throw new KeyNotFoundException("Задача не найдена.");
        task.IsActive = false;
        await _db.SaveChangesAsync();
    }

    /// <summary>Возврат задачи из архива.</summary>
    public async Task RestoreAsync(Guid ownerUserId, Guid taskId)
    {
        var company = await MyCompanyAsync(ownerUserId);
        var task = await _db.EmployerTasks
            .FirstOrDefaultAsync(t => t.Id == taskId && t.CompanyId == company.Id)
            ?? throw new KeyNotFoundException("Задача не найдена.");
        task.IsActive = true;
        await _db.SaveChangesAsync();
    }

    /// <summary>
    /// Список задач компании со статистикой: сколько назначено, сколько ответили,
    /// средний балл. Одним запросом — агрегаты отдельным GROUP BY, без N+1.
    ///
    /// "Назначено" = активные назначения; отменённые из воронки исключаем.
    /// </summary>
    public async Task<List<EmployerTaskListItemDto>> GetCompanyTasksAsync(Guid ownerUserId)
    {
        var company = await MyCompanyAsync(ownerUserId);

        var tasks = await _db.EmployerTasks
            .AsNoTracking()
            .Include(t => t.Specialization)
            .Include(t => t.Grade)
            .Where(t => t.CompanyId == company.Id)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        var taskIds = tasks.Select(t => t.Id).ToList();

        var stats = await _db.CandidateTasks
            .AsNoTracking()
            .Where(x => taskIds.Contains(x.TaskId))
            .GroupBy(x => x.TaskId)
            .Select(g => new
            {
                TaskId = g.Key,
                // "Назначено" = активные назначения; отменённые из воронки исключаем
                Assigned = g.Count(x => x.Status != "отменено"),
                Answered = g.Count(x => x.Status == "выполнено"),
                AvgScore = g.Where(x => x.Status == "выполнено" && x.Score != null)
                            .Average(x => (decimal?)x.Score)
            })
            .ToListAsync();

        return tasks.Select(t =>
        {
            var s = stats.FirstOrDefault(x => x.TaskId == t.Id);
            return new EmployerTaskListItemDto
            {
                Id = t.Id,
                Title = t.Title,
                Description = t.Description,
                SpecializationName = t.Specialization?.Name,
                GradeName = t.Grade?.Name,
                MaxScore = t.MaxScore ?? 10m,
                IsActive = t.IsActive,
                CreatedAt = t.CreatedAt,
                AssignedCount = s?.Assigned ?? 0,
                AnsweredCount = s?.Answered ?? 0,
                AvgScore = s?.AvgScore
            };
        }).ToList();
    }

    /// <summary>
    /// Все ответы кандидатов по всем задачам компании, свежие сверху.
    /// ВАЖНО: не фильтруем по is_active задачи — архив закрывает только НОВЫЕ назначения,
    /// уже выданные задания кандидаты решают, а работодатель оценивает.
    /// Отменённые назначения из очереди исключаем: ответа от них не будет.
    /// Категория кандидата подтягивается отдельно (основная специализация + текущий грейд).
    /// </summary>
    public async Task<List<TaskCompletionDto>> GetCompletionsAsync(Guid ownerUserId)
    {
        var company = await MyCompanyAsync(ownerUserId);

        var rows = await _db.CandidateTasks
            .AsNoTracking()
            .Include(x => x.Task)
            .Include(x => x.Candidate)
            .Include(x => x.StatusNavigation)
            .Where(x => x.Task.CompanyId == company.Id)
            // отменённые назначения из рабочей очереди исключаем:
            // ответа от них не будет, показывать нечего
            .Where(x => x.Status != "отменено")
            .OrderByDescending(x => x.CompletedAt ?? x.AssignedAt)
            .ToListAsync();

        var result = new List<TaskCompletionDto>();
        foreach (var x in rows)
        {
            var categoryName = await _db.CandidateSpecializations
                .Where(cs => cs.CandidateId == x.CandidateId && cs.IsPrimary)
                .Join(_db.Categories,
                    cs => new { cs.SpecializationId, GradeId = cs.CurrentGradeId },
                    cat => new { cat.SpecializationId, cat.GradeId },
                    (cs, cat) => cat.DisplayName)
                .FirstOrDefaultAsync();

            result.Add(new TaskCompletionDto
            {
                CandidateTaskId = x.Id,
                TaskId = x.TaskId,
                TaskTitle = x.Task?.Title ?? "Задача",
                CandidateId = x.CandidateId,
                CandidateName = ((x.Candidate?.LastName ?? "") + " " + (x.Candidate?.FirstName ?? "")).Trim(),
                CategoryName = categoryName,
                Status = x.StatusNavigation?.Name ?? x.Status,
                StatusCode = x.Status,
                AnswerText = x.AnswerText,
                AssignedAt = x.AssignedAt,
                CompletedAt = x.CompletedAt,
                Score = x.Score,
                MaxScore = x.Task?.MaxScore ?? 10m
            });
        }
        return result;
    }

    public async Task<List<CandidateTaskDto>> GetMyTasksAsync(Guid candidateId) =>
        await _db.CandidateTasks
            .Where(t => t.CandidateId == candidateId)
            .OrderByDescending(t => t.AssignedAt)
            .Select(t => new CandidateTaskDto
            {
                Id = t.Id,
                Title = t.Task.Title,
                Description = t.Task.Description,
                CompanyName = t.Task.Company.Name,
                Status = t.Status,
                AssignedAt = t.AssignedAt,
                CompletedAt = t.CompletedAt,
                Score = t.Score,
                AnswerText = t.AnswerText
            })
            .ToListAsync();

    public async Task SubmitAnswerAsync(Guid candidateId, Guid candidateTaskId, TaskAnswerDto dto)
    {
        var t = await _db.CandidateTasks
            .Include(x => x.Task)
            .FirstOrDefaultAsync(x => x.Id == candidateTaskId && x.CandidateId == candidateId)
            ?? throw new KeyNotFoundException("Задание не найдено.");

        // отменённое работодателем задание кандидат решать не может —
        // иначе его ответ попадёт в вакуум и останется без оценки
        if (t.Status is "выполнено" or "пропущено" or "отменено")
            throw new InvalidOperationException("Задание закрыто.");

        t.AnswerText = dto.AnswerText;
        t.AnswerPayload = dto.AnswerPayload.HasValue ? dto.AnswerPayload.Value.GetRawText() : null;
        t.Status = "выполнено";
        t.CompletedAt = DateTime.UtcNow;

        // свежий сигнал о кандидате — актуальность профиля (ТЗ п.2)
        var profile = await _db.CandidateProfiles.FindAsync(candidateId);
        if (profile is not null)
            profile.LastActivityAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
    }

    public async Task ScoreAsync(Guid ownerUserId, Guid candidateTaskId, TaskScoreDto dto)
    {
        var company = await MyCompanyAsync(ownerUserId);
        var t = await _db.CandidateTasks
            .Include(x => x.Task)
            .FirstOrDefaultAsync(x => x.Id == candidateTaskId && x.Task.CompanyId == company.Id)
            ?? throw new KeyNotFoundException("Ответ не найден или не принадлежит вашей компании.");

        if (t.Status != "выполнено")
            throw new InvalidOperationException("Оценивать можно только выполненные задания.");

        var maxScore = t.Task.MaxScore ?? 10m;
        t.Score = Math.Clamp(dto.Score, 0m, maxScore);
        await _db.SaveChangesAsync();

        await _notify.CreateAsync(t.CandidateId, "оценка_задания",
            new { candidateTaskId, title = t.Task.Title, score = t.Score });
    }

    /// <summary>
    /// Отзыв назначения до начала работы кандидатом. Это статус ("отменено"),
    /// а не удаление строки: статистика задачи (Назначено/Ответили) и история сохраняются,
    /// кандидат получает уведомление вместо молчаливого исчезновения задания.
    /// Отменённое назначение дальше исключается из очереди ответов и из счётчика «Назначено»,
    /// но остаётся в picker'е и при повторном назначении реактивируется (см. AssignAsync).
    /// </summary>
    public async Task UnassignAsync(Guid ownerUserId, Guid candidateTaskId)
    {
        var company = await MyCompanyAsync(ownerUserId);
        var t = await _db.CandidateTasks
            .Include(x => x.Task)
            .FirstOrDefaultAsync(x => x.Id == candidateTaskId && x.Task.CompanyId == company.Id)
            ?? throw new KeyNotFoundException("Назначение не найдено.");

        if (t.Status != "назначено")
            throw new InvalidOperationException(
                "Отозвать можно только задание, которое кандидат ещё не начал.");

        t.Status = "отменено";
        await _db.SaveChangesAsync();

        await _notify.CreateAsync(t.CandidateId, "задание_отменено",
            new { candidateTaskId, title = t.Task.Title, company = company.Name });
    }
}