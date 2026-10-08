using FluentValidation;
using FspPlatformApi.DTOs.Auth;
using FspPlatformApi.DTOs.Employers;
using FspPlatformApi.DTOs.Invitations;
using FspPlatformApi.DTOs.Tests;

namespace FspPlatformApi.Validators;

public class RegisterDtoValidator : AbstractValidator<RegisterDto>
{
    public RegisterDtoValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(255);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8).MaximumLength(100);
        RuleFor(x => x.Role).Must(r => r is "кандидат" or "работодатель")
            .WithMessage("Роль должна быть 'кандидат' или 'работодатель'");
    }
}

public class VacancyUpsertDtoValidator : AbstractValidator<VacancyUpsertDto>
{
    public VacancyUpsertDtoValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Description).NotEmpty();
        RuleFor(x => x.SalaryFrom).GreaterThan(0);                          // ТЗ 2.2 п.1
        RuleFor(x => x.SalaryTo).GreaterThanOrEqualTo(x => x.SalaryFrom);
    }
}

public class SendInvitationDtoValidator : AbstractValidator<SendInvitationDto>
{
    public SendInvitationDtoValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Message).NotEmpty();                                 // ТЗ 2.2 п.2
        RuleFor(x => x.SalaryFrom).GreaterThan(0);                          // ТЗ 2.2 п.1
        RuleFor(x => x.SalaryTo).GreaterThanOrEqualTo(x => x.SalaryFrom);
        RuleFor(x => x.ContactMethod).Must(c => c is "почта" or "телефон" or "телеграм" or "через_приложение");
    }
}

public class StartTestDtoValidator : AbstractValidator<StartTestDto>
{
    public StartTestDtoValidator()
    {
        RuleFor(x => x.SpecializationId).GreaterThan(0);
        RuleFor(x => x.TargetGradeId).GreaterThan((short)0);
    }
}