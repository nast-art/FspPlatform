using FspPlatformApi.DTOs.Fsp;

namespace FspPlatformApi.DTOs.Candidates;

/// <summary>
/// Карточка кандидата, видимая работодателю ДО принятия приглашения или отклика (ТЗ 2.2 п.2).
/// Контакты сюда намеренно не входят — они выдаются отдельным эндпоинтом после accept.
/// </summary>
public record CandidatePublicCardDto
{
    /// <summary>ID кандидата (= users.id).</summary>
    public required Guid? CandidateId { get; init; }

    /// <summary>Фамилия.</summary>
    public required string LastName { get; init; }

    /// <summary>Имя.</summary>
    public required string FirstName { get; init; }

    /// <summary>Отчество.</summary>
    public string? MiddleName { get; init; }

    /// <summary>Город.</summary>
    public string? City { get; init; }

    /// <summary>Описание / самопрезентация.</summary>
    public string? About { get; init; }

    /// <summary>Стаж, лет.</summary>
    public required decimal ExperienceYears { get; init; }

    /// <summary>Софт-скиллы.</summary>
    public string[]? SoftSkills { get; init; }

    /// <summary>ID участника ФСП (null — истории ФСП нет; обрабатывается корректно).</summary>
    public Guid? FspId { get; init; }

    /// <summary>Есть ли история ФСП (удобный флаг для UI).</summary>
    public required bool HasFspHistory { get; init; }

    /// <summary>Основная специализация.</summary>
    public string? SpecializationName { get; init; }

    /// <summary>Текущий подтверждённый грейд (не заявленный!).</summary>
    public string? GradeName { get; init; }

    /// <summary>
    /// Подтверждён ли грейд тестом (Q&A менторов: неподтверждённый грейд показываем
    /// со статусом и опускаем в выдаче, не скрываем). False — показывать бейдж
    /// «грейд не подтверждён».
    /// </summary>
    public required bool Confirmed { get; init; }

    /// <summary>Категория («Backend-разработка (Миддл)»).</summary>
    public string? CategoryName { get; init; }

    /// <summary>Лучший процент по пройденным тестам (подтверждённый грейд).</summary>
    public decimal? BestTestPercent { get; init; }
    // Лучшая попытка среди всех завершённых тестов (включая проваленные) — только для
    // показа; в ранжировании участвует BestTestPercent (подтверждённые тесты).
    public decimal? BestAttemptPercent { get; init; }

    /// <summary>Итоговый скор ранжирования (70% тест + до 30 ФСП + до 10 полнота профиля).</summary>
    public required decimal RankScore { get; init; }

    /// <summary>Позиция внутри категории.</summary>
    public required int PositionInCategory { get; init; }

    /// <summary>Видны ли этому работодателю контакты (принятое приглашение/отклик).</summary>
    public required bool ContactsVisible { get; init; }

    /// <summary>Стек (названия навыков).</summary>
    public required List<string> Skills { get; init; }

    /// <summary>Достижения ФСП (пустой список, если истории нет).</summary>
    public required List<FspAchievementDto> FspAchievements { get; init; }
}