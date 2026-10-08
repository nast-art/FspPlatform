namespace FspPlatformApi.DTOs.Search;

/// <summary>Параметры списка опубликованных вакансий (доступен всем, включая кандидатов).</summary>
public record SearchVacancyQuery
{
    /// <summary>Фильтр по специализации.</summary>
    public int? SpecializationId { get; init; }

    /// <summary>Текстовый поиск по названию/описанию.</summary>
    public string? Query { get; init; }

    /// <summary>Страница (с 1).</summary>
    public required int Page { get; init; }

    /// <summary>Размер страницы.</summary>
    public required int PageSize { get; init; }
}