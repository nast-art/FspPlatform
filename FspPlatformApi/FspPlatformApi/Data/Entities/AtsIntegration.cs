using System;
using System.Collections.Generic;

namespace FspPlatformApi.Data.Entities;

public partial class AtsIntegration
{
    public Guid Id { get; set; }

    public Guid CompanyId { get; set; }

    public string Provider { get; set; } = null!;

    public string Config { get; set; } = null!;

    public bool IsActive { get; set; }

    public DateTime? LastSyncAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Company Company { get; set; } = null!;
}
