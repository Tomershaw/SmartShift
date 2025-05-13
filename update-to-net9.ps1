# Update TargetFramework to net9.0 for all projects
$projects = @(
    "SmartShift.Api/SmartShift.Api.csproj",
    "SmartShift.Application/SmartShift.Application.csproj",
    "SmartShift.Domain/SmartShift.Domain.csproj",
    "SmartShift.Infrastructure/SmartShift.Infrastructure.csproj",
    "SmartShift.Tests/SmartShift.Tests.csproj",
    "SmartShift.Contracts/SmartShift.Contracts.csproj",
    "SmartShift.AppHost/SmartShift.AppHost.csproj"
)

foreach ($project in $projects) {
    (Get-Content $project) -replace '<TargetFramework>net8\.0</TargetFramework>', '<TargetFramework>net9.0</TargetFramework>' | Set-Content $project
}

# NuGet Package Updates

# --- SmartShift.Api ---
dotnet add SmartShift.Api package Microsoft.EntityFrameworkCore --version 9.0.4
dotnet add SmartShift.Api package Microsoft.EntityFrameworkCore.SqlServer --version 9.0.4
dotnet add SmartShift.Api package Microsoft.EntityFrameworkCore.Design --version 9.0.4
dotnet add SmartShift.Api package Microsoft.EntityFrameworkCore.Relational --version 9.0.4
dotnet add SmartShift.Api package Carter --version 9.0.0
dotnet add SmartShift.Api package FluentValidation --version 12.0.0
dotnet add SmartShift.Api package FluentValidation.DependencyInjectionExtensions --version 12.0.0
dotnet add SmartShift.Api package Microsoft.AspNetCore.OpenApi --version 9.0.4
dotnet add SmartShift.Api package Swashbuckle.AspNetCore --version 8.1.1

# --- SmartShift.Infrastructure ---
dotnet add SmartShift.Infrastructure package Microsoft.EntityFrameworkCore --version 9.0.4
dotnet add SmartShift.Infrastructure package Microsoft.EntityFrameworkCore.SqlServer --version 9.0.4
dotnet add SmartShift.Infrastructure package Microsoft.EntityFrameworkCore.Design --version 9.0.4
dotnet add SmartShift.Infrastructure package Microsoft.EntityFrameworkCore.Relational --version 9.0.4

# --- SmartShift.Application ---
dotnet add SmartShift.Application package Microsoft.Extensions.DependencyInjection.Abstractions --version 9.0.4

# --- SmartShift.Tests (Test Packages) ---
dotnet add SmartShift.Tests package Microsoft.NET.Test.Sdk --version 17.13.0
dotnet add SmartShift.Tests package xunit --version 2.9.3
dotnet add SmartShift.Tests package xunit.runner.visualstudio --version 3.1.0
dotnet add SmartShift.Tests package coverlet.collector --version 6.0.4

# --- SmartShift.AppHost (Aspire Packages) ---
dotnet add SmartShift.AppHost package Aspire.Hosting.AppHost --version 9.2.1
dotnet add SmartShift.AppHost package Aspire.Dashboard.Sdk.win-x64 --version 9.2.1
dotnet add SmartShift.AppHost package Aspire.Hosting.Orchestration.win-x64 --version 9.2.1

# Final restore and build to ensure everything is good
dotnet restore
dotnet build