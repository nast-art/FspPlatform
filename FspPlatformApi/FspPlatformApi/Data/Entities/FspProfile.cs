using System;
using System.Collections.Generic;

namespace FspPlatformApi.Data.Entities;

public partial class FspProfile
{
    public Guid FspId { get; set; }

    public DateTime? LinkedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<FspAchievement> FspAchievements { get; set; } = new List<FspAchievement>();

    public virtual User? User { get; set; }
}
