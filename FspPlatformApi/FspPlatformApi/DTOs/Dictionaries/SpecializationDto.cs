namespace FspPlatformApi.DTOs.Dictionaries;

/// <summary>Специализация (Backend, Frontend, ...).</summary>
public record SpecializationDto
{
    /// <summary>ID.</summary>
    public required short Id { get; init; }

    /// <summary>Код («Backend», «тестирование», ...).</summary>
    public required string Code { get; init; }

    /// <summary>Название для UI.</summary>
    public required string Name { get; init; }

    /// <summary>ID родительской специализации (иерархия, null — корень).</summary>
    public short? ParentId { get; init; }
}