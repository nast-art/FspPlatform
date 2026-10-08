using FspPlatformApi.DTOs.Tasks;
using FspPlatformApi.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FspPlatformApi.Controllers;

[Authorize(Roles = "работодатель")]
[ApiController]
[Route("api/employer-tasks")]
public class EmployerTasksController : ControllerBase
{
    private readonly IEmployerTaskService _tasks;
    private readonly ICurrentUserAccessor _current;

    public EmployerTasksController(IEmployerTaskService tasks, ICurrentUserAccessor current)
        => (_tasks, _current) = (tasks, current);

    /// <summary>Мои задачи со статистикой назначений (вкладка «Мои задачи»).</summary>
    [HttpGet]
    public async Task<ActionResult<List<EmployerTaskListItemDto>>> Get()
        => Ok(await _tasks.GetCompanyTasksAsync(_current.UserId));

    /// <summary>
    /// Все ответы кандидатов по задачам компании, свежие сверху.
    /// Архив задачи НЕ скрывает уже выданные задания — только новые назначения закрыты.
    /// </summary>
    [HttpGet("completions")]
    public async Task<ActionResult<List<TaskCompletionDto>>> Completions()
        => Ok(await _tasks.GetCompletionsAsync(_current.UserId));

    [HttpPost]
    public async Task<ActionResult<Guid>> Create(EmployerTaskUpsertDto dto)
        => Ok(await _tasks.CreateTaskAsync(_current.UserId, dto));

    /// <summary>Назначение одному кандидату (совместимость со старым UI).</summary>
    [HttpPost("{taskId:guid}/assign")]
    public async Task<IActionResult> Assign(Guid taskId, AssignTaskDto dto)
    {
        await _tasks.AssignAsync(_current.UserId, taskId, dto);
        return NoContent();
    }

    /// <summary>Массовое назначение выбранным кандидатам из подборки. Возвращает число назначенных.</summary>
    [HttpPost("{taskId:guid}/assign-bulk")]
    public async Task<ActionResult<int>> AssignBulk(Guid taskId, AssignTaskBulkDto dto)
        => Ok(await _tasks.AssignManyAsync(_current.UserId, taskId, dto));

    /// <summary>Архивация задачи: новые назначения закрыты, уже выданные задания продолжают жить.</summary>
    [HttpPost("{taskId:guid}/archive")]
    public async Task<IActionResult> Archive(Guid taskId)
    {
        await _tasks.ArchiveAsync(_current.UserId, taskId);
        return NoContent();
    }

    /// <summary>Возврат задачи из архива.</summary>
    [HttpPost("{taskId:guid}/restore")]
    public async Task<IActionResult> Restore(Guid taskId)
    {
        await _tasks.RestoreAsync(_current.UserId, taskId);
        return NoContent();
    }

    /// <summary>Оценка выполненного ответа (0..maxScore). Кандидату уходит уведомление.</summary>
    [HttpPost("completions/{candidateTaskId:guid}/score")]
    public async Task<IActionResult> Score(Guid candidateTaskId, TaskScoreDto dto)
    {
        await _tasks.ScoreAsync(_current.UserId, candidateTaskId, dto);
        return NoContent();
    }

    /// <summary>Отзыв назначения до начала работы кандидатом (статус, не удаление).</summary>
    [HttpPost("completions/{candidateTaskId:guid}/unassign")]
    public async Task<IActionResult> Unassign(Guid candidateTaskId)
    {
        await _tasks.UnassignAsync(_current.UserId, candidateTaskId);
        return NoContent();
    }
}

[Authorize(Roles = "кандидат")]
[ApiController]
[Route("api/my-tasks")]
public class MyTasksController : ControllerBase
{
    private readonly IEmployerTaskService _tasks;
    private readonly ICurrentUserAccessor _current;

    public MyTasksController(IEmployerTaskService tasks, ICurrentUserAccessor current)
        => (_tasks, _current) = (tasks, current);

    /// <summary>Регулярные короткие задания: решить задачу или предложить подход (ТЗ п.2).</summary>
    [HttpGet]
    public async Task<ActionResult<List<CandidateTaskDto>>> Get()
        => Ok(await _tasks.GetMyTasksAsync(_current.UserId));

    [HttpPost("{id:guid}/answer")]
    public async Task<IActionResult> Answer(Guid id, TaskAnswerDto dto)
    {
        await _tasks.SubmitAnswerAsync(_current.UserId, id, dto);
        return NoContent();
    }
}