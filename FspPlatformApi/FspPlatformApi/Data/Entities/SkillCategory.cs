using System;
using System.Collections.Generic;

namespace FspPlatformApi.Data.Entities;

public partial class SkillCategory
{
    public short Id { get; set; }

    public string Name { get; set; } = null!;

    public short SortOrder { get; set; }

    public virtual ICollection<Skill> Skills { get; set; } = new List<Skill>();
}
