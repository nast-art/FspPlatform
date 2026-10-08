using System;
using System.Collections.Generic;

namespace FspPlatformApi.Data.Entities;

public partial class Application
{
    public Guid Id { get; set; }

    public Guid VacancyId { get; set; }

    public Guid CandidateId { get; set; }

    public string? CoverLetter { get; set; }

    public string Status { get; set; } = null!;

    public DateTime SubmittedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual CandidateProfile Candidate { get; set; } = null!;

    public virtual Status StatusNavigation { get; set; } = null!;

    public virtual Vacancy Vacancy { get; set; } = null!;
}
