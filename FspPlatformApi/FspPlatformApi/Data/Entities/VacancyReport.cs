using System;
using System.Collections.Generic;

namespace FspPlatformApi.Data.Entities;

public partial class VacancyReport
{
    public Guid Id { get; set; }

    public Guid? VacancyId { get; set; }

    public Guid? CompanyId { get; set; }

    public Guid? ReporterId { get; set; }

    public string Reason { get; set; } = null!;

    public string? Details { get; set; }

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual Company? Company { get; set; }

    public virtual User? Reporter { get; set; }

    public virtual Status StatusNavigation { get; set; } = null!;

    public virtual Vacancy? Vacancy { get; set; }
}
