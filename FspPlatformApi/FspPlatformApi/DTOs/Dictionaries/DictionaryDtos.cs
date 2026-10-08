namespace FspPlatformApi.DTOs.Dictionaries;

/// <summary>Агрегат справочников (один запрос для инициализации форм на фронте).</summary>
public record DictionaryDtos
{
    /// <summary>Специализации.</summary>
    public required List<SpecializationDto> Specializations { get; init; }

    /// <summary>Грейды.</summary>
    public required List<GradeDto> Grades { get; init; }

    /// <summary>Навыки.</summary>
    public required List<SkillDto> Skills { get; init; }

    /// <summary>Роли в команде.</summary>
    public required List<RoleDto> Roles { get; init; }

    /// <summary>Категории (специализация + грейд).</summary>
    public required List<CategoryDto> Categories { get; init; }
}