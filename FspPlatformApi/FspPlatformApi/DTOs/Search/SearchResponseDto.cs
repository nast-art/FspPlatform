namespace FspPlatformApi.DTOs.Search;

/// <summary>Результат подборки. request_id позволяет вернуться к подборке позже (история).</summary>
public record SearchResponseDto
{
    /// <summary>ID сохранённого поискового запроса.</summary>
    public required Guid RequestId { get; init; }

    /// <summary>Общее количество подходящих кандидатов.</summary>
    public required int Total { get; init; }

    /// <summary>Страница результатов с ранжированием и объяснимостью.</summary>
    public required List<SearchCandidateItemDto> Items { get; init; }

    /// <summary>
    /// Фильтры, которыми собрана подборка (снапшот запроса). Возвращаются для
    /// сохранённых подборок — чтобы кабинет мог восстановить панель фильтров
    /// в том виде, в каком подборка делалась.
    /// </summary>
    public Dictionary<string, object>? Filters { get; init; }

    /// <summary>
    /// Когда подборка создана. Нужна в истории запросов: ТЗ требует возможности
    /// вернуться к ранее полученной подборке — без даты подборки в списке не
    /// отличить друг от друга.
    /// </summary>
    public DateTime? CreatedAt { get; init; }
}