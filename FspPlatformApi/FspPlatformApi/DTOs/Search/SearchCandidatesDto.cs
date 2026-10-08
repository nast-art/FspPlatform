namespace FspPlatformApi.DTOs.Search;

/// <summary>
/// Поисковый запрос работодателя по банку кандидатов.
/// Фильтры можно уточнять повторными запросами — каждая подборка сохраняется в истории (ТЗ).
/// </summary>
public record SearchCandidatesDto
{
    /// <summary>Фильтр по специализации.</summary>
    public int? SpecializationId { get; init; }

    /// <summary>Фильтр по грейду.</summary>
    public short? GradeId { get; init; }

    /// <summary>Фильтр по категории (специализация + грейд).</summary>
    public int? CategoryId { get; init; }

    /// <summary>Фильтр по стеку (ID навыков).</summary>
    public List<short>? SkillIds { get; init; }

    /// <summary>Минимальный итоговый скор ранжирования.</summary>
    public decimal? MinScore { get; init; }

    /// <summary>Только с подтверждёнными достижениями ФСП.</summary>
    public bool? HasFsp { get; init; }

    /// <summary>Фильтр по городу.</summary>
    public string? City { get; init; }

    /// <summary>Текстовый поиск по ФИО/о себе.</summary>
    public string? Query { get; init; }

    /// <summary>
    /// ID задачи (employer_tasks) для назначения: кандидаты, которым она уже АКТИВНО
    /// назначена, исключаются из выдачи. Отменённые назначения не исключаются —
    /// таких кандидатов можно назначить повторно.
    /// </summary>
    public Guid? ExcludeTaskId { get; init; }

    /// <summary>Страница (с 1).</summary>
    public int Page { get; init; } = 1;

    /// <summary>Размер страницы.</summary>
    public int PageSize { get; init; } = 20;

      public bool? Save { get; init; }
}