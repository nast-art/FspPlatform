using System;
using System.Collections.Generic;

namespace FspPlatformApi.Data.Entities;

public partial class TestAssignment
{
    public Guid Id { get; set; }

    public short SpecializationId { get; set; }

    public short GradeId { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public short DurationMinutes { get; set; }

    public decimal PassThreshold { get; set; }

    public short QuestionsCount { get; set; }

    public string DifficultyMix { get; set; } = null!;

    public int Version { get; set; }

    public bool IsActive { get; set; }

    public virtual Grade Grade { get; set; } = null!;

    public virtual Specialization Specialization { get; set; } = null!;

    public virtual ICollection<TestSession> TestSessions { get; set; } = new List<TestSession>();
}
