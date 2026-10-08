using System;
using System.Collections.Generic;

namespace FspPlatformApi.Data.Entities;

public partial class EmployerSearchRequest
{
    public Guid Id { get; set; }

    public Guid CompanyId { get; set; }

    public string? Title { get; set; }

    public string Filters { get; set; } = null!;

    public Guid CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Company Company { get; set; } = null!;

    public virtual User CreatedByNavigation { get; set; } = null!;

    public virtual ICollection<EmployerSearchResult> EmployerSearchResults { get; set; } = new List<EmployerSearchResult>();
}
