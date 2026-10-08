namespace FspPlatformApi.DTOs.Dictionaries;

/// <summary>Грейд из строгой линейки (rank 1–6: стажер → архитектор).</summary>
public record GradeDto
{
    /// <summary>ID.</summary>
    public required short Id { get; init; }

    /// <summary>Код («стажер», «джуниор», ...).</summary>
    public required string Code { get; init; }

    /// <summary>Название для UI.</summary>
    public required string Name { get; init; }

    /// <summary>Ранг 1–6 (для сравнения «выше/ниже»).</summary>
    public required short Rank { get; init; }

    /// <summary>Рекомендуемый стаж, лет (справочно).</summary>
    public decimal? MinExpYears { get; init; }
}