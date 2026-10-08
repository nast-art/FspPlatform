using System;
using System.Collections.Generic;

namespace FspPlatformApi.Data.Entities;

public partial class FspAchievement
{
    public Guid Id { get; set; }

    public Guid FspId { get; set; }

    public string EventType { get; set; } = null!;

    public string EventName { get; set; } = null!;

    public DateOnly EventDate { get; set; }

    public string? Role { get; set; }

    public short? ResultPlace { get; set; }

    public decimal? ResultScore { get; set; }

    public string? Direction { get; set; }

    public string SourcePayload { get; set; } = null!;

    public decimal Weight { get; set; }

    public bool Verified { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Status EventTypeNavigation { get; set; } = null!;

    public virtual FspProfile Fsp { get; set; } = null!;
}
