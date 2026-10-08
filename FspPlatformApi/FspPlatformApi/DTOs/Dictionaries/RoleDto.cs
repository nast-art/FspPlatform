namespace FspPlatformApi.DTOs.Dictionaries;

/// <summary>Роль в команде.</summary>
public record RoleDto
{
    /// <summary>ID.</summary>
    public required short Id { get; init; }

    /// <summary>Название («Разработчик», «Team Lead», ...).</summary>
    public required string Name { get; init; }
}