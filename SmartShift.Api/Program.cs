using Carter;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SmartShift.Api.Middleware;
using SmartShift.Api.Services;
using SmartShift.Application;
using SmartShift.Application.Common.Behaviors;
using SmartShift.Application.Common.Interfaces;
using SmartShift.Application.Features.UserManagement.CreateUser;
using SmartShift.Domain.Data;
using SmartShift.Domain.Services;
using SmartShift.Infrastructure.AI;
using SmartShift.Infrastructure.Authentication;
using SmartShift.Infrastructure.Data;
using SmartShift.Infrastructure.Repositories;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using SmartShift.Infrastructure.Vault;

using SmartShift.Infrastructure.Email;


using SmartShift.Infrastructure.Interfaces;



var builder = WebApplication.CreateBuilder(args);

// ✅ OCI Vault — load secrets in production via Instance Principal
if (builder.Environment.IsProduction())
{
    var vaultOcid = builder.Configuration["OciVault:VaultOcid"]
        ?? throw new InvalidOperationException("OciVault:VaultOcid is not configured in appsettings.json");

    builder.Configuration.AddOciVault(vaultOcid,
    [
        "ConnectionStrings--DefaultConnection",
        "Jwt--Key",
        "Jwt--Issuer",
        "Jwt--Audience",
        "CORS--ORIGINS",
        "SemanticKernel--OpenAI--ApiKey",
        "Brevo--SmtpLogin",
        "Brevo--SmtpKey",
        "Brevo--FromEmail"
    ]);
}


// ✅ Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "SmartShift API", Version = "v1" });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer' followed by your JWT token."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// ✅ Application & Infra
builder.Services.AddApplication();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentityCore<ApplicationUser>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 8;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// ✅ JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            RoleClaimType = ClaimTypes.Role,
            NameClaimType = "nameid",
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)
            )
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogInformation("📨 Raw Token Received: {Token}", context.Token);
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogInformation("✅ Token Successfully Validated");

                foreach (var claim in context.Principal!.Claims)
                {
                    logger.LogInformation("🔑 Claim Type: {Type}, Value: {Value}", claim.Type, claim.Value);
                }

                return Task.CompletedTask;
            },
            OnAuthenticationFailed = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogError(context.Exception, "❌ Authentication Failed");
                return Task.CompletedTask;
            },
            OnChallenge = context =>
            {
                context.HandleResponse();
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/json";
                return context.Response.WriteAsync("{\"error\": \"Unauthorized\"}");
            },
            OnForbidden = context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json";
                return context.Response.WriteAsync("{\"error\": \"Forbidden\"}");
            }
        };
    });

builder.Services.AddAuthorization();

// ✅ Carter, CORS, DI
builder.Services.AddCarter();
builder.Services.AddScoped<Carter.IValidatorLocator, Carter.DefaultValidatorLocator>();
var originsFromEnv = Environment
    .GetEnvironmentVariable("CORS_ORIGINS");

// מפצל לרשימה
string[] allowedOrigins = Array.Empty<string>();

if (!string.IsNullOrWhiteSpace(originsFromEnv))
{
    allowedOrigins = originsFromEnv
        .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            // Dev - localhost מאושר בשביל Aspire
            policy
                .SetIsOriginAllowed(origin =>
                {
                    if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri))
                        return false;

                    return uri.Host == "localhost";
                })
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials();
        }
        else
        {
            // Prod - משתמשים ב origins מה env
            if (allowedOrigins.Length > 0)
            {
                policy
                    .WithOrigins(allowedOrigins)
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials();
            }
            else
            {
                // אם לא הוגדר env, אפשר או לחסום הכל או לזרוק שגיאה לוגית
                // כאן לדוגמה אני חוסם הכל במודע
                policy
                    .DisallowCredentials(); // סתם משהו "ריק"
            }
        }
    });
});

// ✅ Application Services
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
builder.Services.AddScoped<IUserRegistrationService, UserRegistrationService>();
builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();
builder.Services.AddScoped<IShiftRepository, ShiftRepository>();
builder.Services.AddScoped<RefreshTokenService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<EmployeeShiftMatchingService>();
builder.Services.AddScoped<ShiftScoringService>();
builder.Services.AddScoped<SmartShift.Infrastructure.Interfaces.IEmailSender,
    SmartShift.Infrastructure.Email.BrevoSmtpEmailSender>();





// ✅ AI Services
builder.Services.AddSemanticKernel(builder.Configuration);
builder.Services.AddScoped<IShiftAssignmentAIService, ShiftAssignmentAIService>();

// ✅ MediatR
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(SmartShift.Application.DependencyInjection).Assembly);
    cfg.RegisterServicesFromAssembly(typeof(RegisterForShiftCommandHandler).Assembly);
});

// ✅ FluentValidation
builder.Services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly(), ServiceLifetime.Scoped);
builder.Services.AddValidatorsFromAssembly(typeof(SmartShift.Application.DependencyInjection).Assembly, ServiceLifetime.Scoped);

// ✅ Pipeline Behaviors
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

var app = builder.Build();

// ✅ Database Migrations (fast, blocking - must complete before app starts)
if (app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        //context.Database.Migrate();
    }


    // ✅ Seed Data (non-blocking - runs in background after app starts)
    _ = Task.Run(async () =>
{
    await Task.Delay(2000); // Wait 2 seconds for app to fully start
    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<ApplicationDbContext>();
    var logger = services.GetRequiredService<ILogger<Program>>();

    try
    {
        logger.LogInformation("🌱 Starting database seeding...");

        await SeedData.SeedRolesAsync(services);
        await SeedData.SeedTenantAsync(context);

        var tenant = await context.Tenants.FirstOrDefaultAsync(t => t.Name == "אריא");
        if (tenant != null)
        {
            await SeedData.SeedEmployeesAsync(context, tenant.Id);
            await SeedData.SeedShiftsAsync(context, tenant.Id);
            await SeedData.SeedAdminUserAsync(services, tenant.Id);
        }

        logger.LogInformation("✅ Database seeding completed successfully.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "❌ Error during database seeding. App will continue running.");
    }
});
}

// ✅ Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionMiddleware();
app.UseHttpsRedirection();
app.UseRouting();

app.UseDefaultFiles();   // 🔵 מחפש index.html ב-wwwroot
app.UseStaticFiles();    // 🔵 מגיש קבצים סטטיים

app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapCarter();

// 🔵 SPA Fallback - serve index.html for non-API routes (React Router)
app.MapFallbackToFile("index.html");

app.Run();
