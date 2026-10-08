namespace FspPlatformApi.Services.Interfaces;

/// <summary>
/// Результат загрузки резюме: путь к сохранённому PDF + распознанные данные.
/// Все поля nullable — распознавание эвристическое (MVP), что-то может отсутствовать.
/// </summary>
public record ResumeUploadResult(
    string StoredPath,
    string? RecognizedEmail,
    string? RecognizedPhone,
    string? RecognizedTelegram,
    string? RecognizedCity,
    string? RecognizedLastName,
    string? RecognizedFirstName,
    string? RecognizedMiddleName,
    decimal? RecognizedExperienceYears,
    string? RecognizedAbout,
    string[]? RecognizedSoftSkills,
    List<string> RecognizedSkills);