using System;
using System.Collections.Generic;

namespace FspPlatformApi.Data.Entities;

public partial class VCandidatePublicCard
{
    public Guid? CandidateId { get; set; }

    public string? LastName { get; set; }

    public string? FirstName { get; set; }

    public string? MiddleName { get; set; }

    public string? City { get; set; }

    public string? About { get; set; }

    public decimal? ExperienceYears { get; set; }

    public List<string>? SoftSkills { get; set; }

    public string? ProfilePdfPath { get; set; }

    public Guid? FspId { get; set; }

    public bool? HasFspHistory { get; set; }

    public short? SpecializationId { get; set; }

    public short? CurrentGradeId { get; set; }

    public bool? IsProfilePublic { get; set; }

    public DateTime? LastActivityAt { get; set; }
}
