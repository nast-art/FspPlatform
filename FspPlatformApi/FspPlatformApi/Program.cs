using System.Text;
using FluentValidation;
using FluentValidation.AspNetCore;
using Mapster;
using FspPlatformApi.Data.Entities;
using FspPlatformApi.Middleware;
using FspPlatformApi.Services.Auth;
using FspPlatformApi.Services.Candidates;
using FspPlatformApi.Services.Employers;
using FspPlatformApi.Services.Infrastructure;
using FspPlatformApi.Services.Matching;
using FspPlatformApi.Services.Platform;
using FspPlatformApi.Services.Testing;
using FspPlatformApi.Services.Interfaces;
using FspPlatformApi.Services.Interfaces.Matching;
using FspPlatformApi.Services.Interfaces.Testing;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

// ---------- Хранение данных ----------
builder.Services.AddDbContext<ApplicationDbContext>(opt =>
    opt.UseNpgsql(config.GetConnectionString("Default")));

// ---------- Аутентификация JWT (совместимость с будущим Keycloak/ФСП ID: users.id = UUID) ----------
var jwtKey = Encoding.UTF8.GetBytes(config["Jwt:Key"]!);
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true, ValidIssuer = config["Jwt:Issuer"],
            ValidateAudience = true, ValidAudience = config["Jwt:Audience"],
            ValidateIssuerSigningKey = true, IssuerSigningKey = new SymmetricSecurityKey(jwtKey),
            ValidateLifetime = true, ClockSkew = TimeSpan.FromMinutes(2)
        };
        // ДИАГНОСТИКА: точная причина 401 печатается в консоль приложения
        o.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = ctx =>
            {
                Console.WriteLine($"JWT FAIL: {ctx.Exception.GetType().Name}: {ctx.Exception.Message}");
                return Task.CompletedTask;
            },
            OnTokenValidated = _ =>
            {
                Console.WriteLine("JWT OK");
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddControllers().AddJsonOptions(o =>
{
    o.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddEndpointsApiExplorer();

// GigaChat (SberCloud): OpenAI-совместимый API, обычный DNS, ключ в Bearer.
// Используют OpenAnswerGrader и QuestionGeneratorService через CreateClient("ai").
builder.Services.AddHttpClient("ai", client =>
{
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.Timeout = TimeSpan.FromSeconds(120);
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
});

// ---------- OpenAPI (ТЗ 3.5) ----------
// ---------- OpenAPI ----------
builder.Services.AddSwaggerGen(o =>
{
    o.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "ФСП Платформа — API подбора ИТ-специалистов",
        Version = "v1",
        Description = "API платформы подбора ИТ-специалистов"
    });

    // JWT Bearer
    o.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Введите JWT-токен без слова Bearer"
    });

    // Требуем Bearer для всех endpoints
    o.AddSecurityRequirement(document =>
        new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", document)] = []
        });
});

// ---------- Mapster + FluentValidation ----------
TypeAdapterConfig.GlobalSettings.Scan(typeof(Program).Assembly);
builder.Services.AddSingleton(TypeAdapterConfig.GlobalSettings);
builder.Services.AddFluentValidationAutoValidation()
    .AddFluentValidationClientsideAdapters()
    .AddValidatorsFromAssemblyContaining<Program>();

// ---------- Регистрация сервисов ----------
builder.Services.AddScoped<ICurrentUserAccessor, CurrentUserAccessor>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IDictionaryService, DictionaryService>();
builder.Services.AddScoped<ICandidateService, CandidateService>();

builder.Services.AddScoped<ICodeRunnerService, CodeRunnerService>();
builder.Services.AddSingleton<GigaChatTokenProvider>();
builder.Services.AddScoped<IOpenAnswerGrader, OpenAnswerGrader>();
builder.Services.AddScoped<IQuestionGeneratorService, QuestionGeneratorService>();
builder.Services.AddScoped<IQuestionValidatorService, QuestionValidatorService>();
builder.Services.AddScoped<IQuestionCalibrationService, QuestionCalibrationService>();
builder.Services.AddScoped<ITestService, TestService>();

builder.Services.AddScoped<ICompanyService, CompanyService>();
builder.Services.AddScoped<IVacancyService, VacancyService>();
builder.Services.AddScoped<IApplicationService, ApplicationService>();
builder.Services.AddScoped<IInvitationService, InvitationService>();
builder.Services.AddScoped<ISearchService, SearchService>();
builder.Services.AddScoped<IFspAchievementService, FspAchievementService>();
builder.Services.AddScoped<IEmployerTaskService, EmployerTaskService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IAtsIntegrationService, AtsIntegrationService>();

// ---------- CORS ----------
builder.Services.AddCors(o => o.AddPolicy("frontend", p => p
    .WithOrigins(config.GetSection("Cors:Origins").Get<string[]>() ?? ["http://localhost:5173"])
    .AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("frontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();
}

app.Run();
