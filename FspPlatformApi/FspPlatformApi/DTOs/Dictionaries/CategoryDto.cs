namespace FspPlatformApi.DTOs.Dictionaries;

/// <summary>Категория = специализация + грейд (основа распределения кандидатов, ТЗ).</summary>
public record CategoryDto
{
    /// <summary>ID категории.</summary>
    public required short Id { get; init; }

    /// <summary>ID специализации.</summary>
    public required short SpecializationId { get; init; }

    /// <summary>ID грейда.</summary>
    public required short GradeId { get; init; }

    /// <summary>Отображаемое имя («Backend-разработка (Миддл)»).</summary>
    public required string DisplayName { get; init; }
}