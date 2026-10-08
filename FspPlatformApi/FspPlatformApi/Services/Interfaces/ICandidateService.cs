using FspPlatformApi.DTOs.Candidates;

namespace FspPlatformApi.Services.Interfaces;

public interface ICandidateService
{
    Task<CandidateProfileUpsertDto?> GetMyProfileAsync(Guid userId);
    Task UpsertMyProfileAsync(Guid userId, CandidateProfileUpsertDto dto);
    Task<Guid> StartSurveyAsync(Guid userId, StartSurveyDto dto);
    Task SubmitSurveyAsync(Guid userId, SurveySubmitDto dto);
    Task<MyStatusDto> GetMyStatusAsync(Guid userId);
    Task<List<MySpecializationDto>> GetMySpecializationsAsync(Guid userId);
    Task LinkFspAsync(Guid userId, LinkFspDto dto);
    Task<CandidatePublicCardDto> GetPublicCardAsync(Guid candidateId, Guid? employerUserId);
    Task<CandidateContactsDto> GetContactsAsync(Guid candidateId, Guid employerUserId);
    Task<ResumeUploadResult> UploadResumeAsync(Guid userId, Stream fileStream, string fileName);

    Task<byte[]> GetProfilePdfAsync(Guid userId);
}