using System;
using System.Collections.Generic;

namespace FspPlatformApi.Data.Entities;

public partial class CandidateProfile
{
    public Guid UserId { get; set; }

    public string? LastName { get; set; }

    public string? FirstName { get; set; }

    public string? MiddleName { get; set; }

    public string? Phone { get; set; }

    public string? Telegram { get; set; }

    public string? City { get; set; }

    public string? About { get; set; }

    public decimal ExperienceYears { get; set; }

    public List<string>? SoftSkills { get; set; }

    public string? ResumePdfPath { get; set; }

    public string? ProfilePdfPath { get; set; }

    public bool IsProfilePublic { get; set; }

    public bool ShowContacts { get; set; }

    public short ProfileCompleteness { get; set; }

    public DateTime? LastActivityAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<Application> Applications { get; set; } = new List<Application>();

    public virtual ICollection<CandidateCategory> CandidateCategories { get; set; } = new List<CandidateCategory>();

    public virtual ICollection<CandidateSkill> CandidateSkills { get; set; } = new List<CandidateSkill>();

    public virtual ICollection<CandidateSpecialization> CandidateSpecializations { get; set; } = new List<CandidateSpecialization>();

    public virtual ICollection<CandidateTask> CandidateTasks { get; set; } = new List<CandidateTask>();

    public virtual ICollection<EmployerSearchResult> EmployerSearchResults { get; set; } = new List<EmployerSearchResult>();

    public virtual ICollection<GradeChangeHistory> GradeChangeHistories { get; set; } = new List<GradeChangeHistory>();

    public virtual ICollection<Invitation> Invitations { get; set; } = new List<Invitation>();

    public virtual ICollection<SurveySession> SurveySessions { get; set; } = new List<SurveySession>();

    public virtual ICollection<TestSession> TestSessions { get; set; } = new List<TestSession>();

    public virtual User User { get; set; } = null!;

    public virtual ICollection<Role> Roles { get; set; } = new List<Role>();
}
