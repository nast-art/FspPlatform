using System;
using System.Collections.Generic;

namespace FspPlatformApi.Data.Entities;

public partial class Invitation
{
    public Guid Id { get; set; }

    public Guid CompanyId { get; set; }

    public Guid CandidateId { get; set; }

    public Guid? VacancyId { get; set; }

    public string Title { get; set; } = null!;

    public string Message { get; set; } = null!;

    public int SalaryFrom { get; set; }

    public int SalaryTo { get; set; }

    public string Currency { get; set; } = null!;

    public string ContactMethod { get; set; } = null!;

    public string Status { get; set; } = null!;

    public DateTime SentAt { get; set; }

    public DateTime? ViewedAt { get; set; }

    public DateTime? RespondedAt { get; set; }

    public DateTime? ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual CandidateProfile Candidate { get; set; } = null!;

    public virtual Company Company { get; set; } = null!;

    public virtual Status ContactMethodNavigation { get; set; } = null!;

    public virtual Status StatusNavigation { get; set; } = null!;

    public virtual Vacancy? Vacancy { get; set; }
}
