using System;
using System.Collections.Generic;

namespace FspPlatformApi.Data.Entities;

public partial class User
{
    public Guid Id { get; set; }

    public string Email { get; set; } = null!;

    public string? PasswordHash { get; set; }

    public string Role { get; set; } = null!;

    public bool EmailConfirmed { get; set; }

    public bool IsActive { get; set; }

    public Guid? FspId { get; set; }

    public DateTime? FspLinkedAt { get; set; }

    public DateTime? LastLoginAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();

    public virtual ICollection<AuthToken> AuthTokens { get; set; } = new List<AuthToken>();

    public virtual CandidateProfile? CandidateProfile { get; set; }

    public virtual ICollection<Company> Companies { get; set; } = new List<Company>();

    public virtual ICollection<ConsentLog> ConsentLogs { get; set; } = new List<ConsentLog>();

    public virtual ICollection<EmployerSearchRequest> EmployerSearchRequests { get; set; } = new List<EmployerSearchRequest>();

    public virtual FspProfile? Fsp { get; set; }

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public virtual Status RoleNavigation { get; set; } = null!;

    public virtual ICollection<VacancyReport> VacancyReports { get; set; } = new List<VacancyReport>();
}
