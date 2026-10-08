using FspPlatformApi.Data.Entities;
using FspPlatformApi.DTOs.Dictionaries;
using FspPlatformApi.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FspPlatformApi.Services.Infrastructure;

public class DictionaryService : IDictionaryService
{
    private readonly ApplicationDbContext _db;
    public DictionaryService(ApplicationDbContext db) => _db = db;

    public async Task<DictionaryDtos> GetAllAsync()
    {
        var specs = await _db.Specializations.Where(s => s.IsActive).OrderBy(s => s.SortOrder)
            .Select(s => new SpecializationDto { Id = s.Id, Code = s.Code, Name = s.Name, ParentId = s.ParentId })
            .ToListAsync();
        var grades = await _db.Grades.Where(g => g.IsActive).OrderBy(g => g.Rank)
            .Select(g => new GradeDto { Id = g.Id, Code = g.Code, Name = g.Name, Rank = g.Rank, MinExpYears = g.MinExpYears })
            .ToListAsync();
        var skills = await _db.Skills.Where(s => s.IsActive)
            .Select(s => new SkillDto { Id = s.Id, Name = s.Name, CategoryId = s.CategoryId }).ToListAsync();
        var roles = await _db.Roles.Where(r => r.IsActive)
            .Select(r => new RoleDto { Id = r.Id, Name = r.Name }).ToListAsync();
        var categories = await _db.Categories.Where(c => c.IsActive)
            .Select(c => new CategoryDto { Id = c.Id, SpecializationId = c.SpecializationId, GradeId = c.GradeId, DisplayName = c.DisplayName })
            .ToListAsync();
        return new DictionaryDtos
        {
            Specializations = specs, Grades = grades, Skills = skills, Roles = roles, Categories = categories
        };
    }
}