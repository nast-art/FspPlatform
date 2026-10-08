using System;
using System.Collections.Generic;

namespace FspPlatformApi.Data.Entities;

public partial class TestQuestion
{
    public Guid Id { get; set; }

    public short SpecializationId { get; set; }

    public short GradeId { get; set; }

    public string QuestionType { get; set; } = null!;

    public Guid QuestionGroup { get; set; }

    public short Difficulty { get; set; }

    public string Prompt { get; set; } = null!;

    public string Payload { get; set; } = null!;

    public string CorrectPayload { get; set; } = null!;

    public decimal MaxScore { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Grade Grade { get; set; } = null!;

    public virtual Status QuestionTypeNavigation { get; set; } = null!;

    public virtual Specialization Specialization { get; set; } = null!;

    public virtual ICollection<TestSessionAnswer> TestSessionAnswers { get; set; } = new List<TestSessionAnswer>();
}
