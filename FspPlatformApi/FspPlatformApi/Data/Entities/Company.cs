using System;
using System.Collections.Generic;

namespace FspPlatformApi.Data.Entities;

public partial class Company
{
    public Guid Id { get; set; }

    public Guid OwnerUserId { get; set; }

    public string Name { get; set; } = null!;

    public string? Industry { get; set; }

    public string? Description { get; set; }

    public string? Website { get; set; }

    public string? ContactEmail { get; set; }

    public string? ContactPhone { get; set; }

    public string? LogoPath { get; set; }

    public string Status { get; set; } = null!;

    public DateTime? VerifiedAt { get; set; }

    public short VerificationLevel { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<AtsIntegration> AtsIntegrations { get; set; } = new List<AtsIntegration>();

    public virtual ICollection<EmployerSearchRequest> EmployerSearchRequests { get; set; } = new List<EmployerSearchRequest>();

    public virtual ICollection<EmployerTask> EmployerTasks { get; set; } = new List<EmployerTask>();

    public virtual ICollection<Invitation> Invitations { get; set; } = new List<Invitation>();

    public virtual User OwnerUser { get; set; } = null!;

    public virtual Status StatusNavigation { get; set; } = null!;

    public virtual ICollection<Vacancy> Vacancies { get; set; } = new List<Vacancy>();

    public virtual ICollection<VacancyReport> VacancyReports { get; set; } = new List<VacancyReport>();
}
