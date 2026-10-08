namespace FspPlatformApi.DTOs.Dictionaries;

/// <summary>Навык/технология из стека.</summary>
public record SkillDto
{
    /// <summary>ID.</summary>
    public required short Id { get; init; }

    /// <summary>Название («C#», «PostgreSQL»).</summary>
    public required string Name { get; init; }

    /// <summary>ID категории навыка (Язык/Фреймворк/БД/...).</summary>
    public required short CategoryId { get; init; }
}