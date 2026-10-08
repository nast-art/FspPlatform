using System;
using System.Collections.Generic;

namespace FspPlatformApi.Data.Entities;

public partial class Status
{
    public short Id { get; set; }

    public string Code { get; set; } = null!;

    public string Entity { get; set; } = null!;

    public string Name { get; set; } = null!;

    public short SortOrder { get; set; }

    public bool IsActive { get; set; }

    public virtual ICollection<Application> Applications { get; set; } = new List<Application>();

    public virtual ICollection<AuthToken> AuthTokens { get; set; } = new List<AuthToken>();

    public virtual ICollection<CandidateSpecialization> CandidateSpecializations { get; set; } = new List<CandidateSpecialization>();

    public virtual ICollection<CandidateTask> CandidateTasks { get; set; } = new List<CandidateTask>();

    public virtual ICollection<Company> Companies { get; set; } = new List<Company>();

    public virtual ICollection<ConsentLog> ConsentLogs { get; set; } = new List<ConsentLog>();

    public virtual ICollection<FspAchievement> FspAchievements { get; set; } = new List<FspAchievement>();

    public virtual ICollection<GradeChangeHistory> GradeChangeHistories { get; set; } = new List<GradeChangeHistory>();

    public virtual ICollection<Invitation> InvitationContactMethodNavigations { get; set; } = new List<Invitation>();

    public virtual ICollection<Invitation> InvitationStatusNavigations { get; set; } = new List<Invitation>();

    public virtual ICollection<TestQuestion> TestQuestions { get; set; } = new List<TestQuestion>();

    public virtual ICollection<TestSession> TestSessions { get; set; } = new List<TestSession>();

    public virtual ICollection<User> Users { get; set; } = new List<User>();

    public virtual ICollection<Vacancy> Vacancies { get; set; } = new List<Vacancy>();

    public virtual ICollection<VacancyReport> VacancyReports { get; set; } = new List<VacancyReport>();
}
