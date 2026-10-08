namespace FspPlatformApi.DTOs.Tasks;

/// <summary>Короткая задача от работодателя (регулярные задания, ТЗ п.2 — желательно для MVP).</summary>
public record EmployerTaskUpsertDto
{
    /// <summary>ID специализации.</summary>
    public required int SpecializationId { get; init; }

    /// <summary>Грейд (опционально).</summary>
    public short? GradeId { get; init; }

    /// <summary>Название задачи.</summary>
    public required string Title { get; init; }

    /// <summary>Описание задачи/кейса.</summary>
    public required string Description { get; init; }

    /// <summary>Доп. данные (JSON).</summary>
    public System.Text.Json.JsonElement? Payload { get; init; }

    /// <summary>Максимальный балл (по умолчанию 10).</summary>
    public decimal? MaxScore { get; init; }
}