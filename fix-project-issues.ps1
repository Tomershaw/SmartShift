# Paths to the project files
$projects = @(
    "SmartShift.Api/SmartShift.Api.csproj",
    "SmartShift.Application/SmartShift.Application.csproj",
    "SmartShift.Domain/SmartShift.Domain.csproj",
    "SmartShift.Infrastructure/SmartShift.Infrastructure.csproj",
    "SmartShift.Tests/SmartShift.Tests.csproj",
    "SmartShift.Contracts/SmartShift.Contracts.csproj",
    "SmartShift.AppHost/SmartShift.AppHost.csproj"
)

# Step 1: Remove unnecessary Aspire references
foreach ($project in $projects) {
    (Get-Content $project) -replace '.Aspire\.Dashboard\.Sdk\.win-x64.', '' | Set-Content $project
    (Get-Content $project) -replace '.Aspire\.Hosting\.Orchestration\.win-x64.', '' | Set-Content $project
}

# Step 2: Ensure Microsoft.CodeAnalysis dependencies are aligned to 4.11.0
dotnet add SmartShift.Api/SmartShift.Api.csproj package Microsoft.CodeAnalysis.Common --version 4.11.0
dotnet add SmartShift.Api/SmartShift.Api.csproj package Microsoft.CodeAnalysis.CSharp --version 4.11.0

# Step 3: Restore and Build the solution
dotnet restore
dotnet build

Write-Host "Cleanup and updates completed successfully!" -ForegroundColor Green
