using System;
using System.Collections.Generic;

namespace FspPlatformApi.Data.Entities;

public partial class Vacancy
{
    public Guid Id { get; set; }

    public Guid CompanyId { get; set; }

    public string Title { get; set; } = null!;

    public string Description { get; set; } = null!;

    public short SpecializationId { get; set; }

    public short GradeFromId { get; set; }

    public short GradeToId { get; set; }

    public int SalaryFrom { get; set; }

    public int SalaryTo { get; set; }

    public string Currency { get; set; } = null!;

    public string? EmploymentType { get; set; }

    public string? Location { get; set; }

    public string Status { get; set; } = null!;

    public DateTime? PublishedAt { get; set; }

    public DateTime? ExpiresAt { get; set; }

    public int ViewsCount { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<Application> Applications { get; set; } = new List<Application>();

    public virtual Company Company { get; set; } = null!;

    public virtual Grade GradeFrom { get; set; } = null!;

    public virtual Grade GradeTo { get; set; } = null!;

    public virtual ICollection<Invitation> Invitations { get; set; } = new List<Invitation>();

    public virtual Specialization Specialization { get; set; } = null!;

    public virtual Status StatusNavigation { get; set; } = null!;

    public virtual ICollection<VacancyReport> VacancyReports { get; set; } = new List<VacancyReport>();

    public virtual ICollection<Skill> Skills { get; set; } = new List<Skill>();
}
