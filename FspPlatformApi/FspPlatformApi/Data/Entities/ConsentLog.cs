using System;
using System.Collections.Generic;
using System.Net;

namespace FspPlatformApi.Data.Entities;

public partial class ConsentLog
{
    public long Id { get; set; }

    public Guid UserId { get; set; }

    public string ConsentType { get; set; } = null!;

    public bool Granted { get; set; }

    public string TermsVersion { get; set; } = null!;

    public DateTime GrantedAt { get; set; }

    public IPAddress? IpAddress { get; set; }

    public DateTime? RevokedAt { get; set; }

    public virtual Status ConsentTypeNavigation { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
