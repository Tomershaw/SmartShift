# Update EF Core packages in Infrastructure project
dotnet add SmartShift.Infrastructure/SmartShift.Infrastructure.csproj package Microsoft.EntityFrameworkCore --version 9.0.4
dotnet add SmartShift.Infrastructure/SmartShift.Infrastructure.csproj package Microsoft.EntityFrameworkCore.SqlServer --version 9.0.4
dotnet add SmartShift.Infrastructure/SmartShift.Infrastructure.csproj package Microsoft.EntityFrameworkCore.Design --version 9.0.4
dotnet add SmartShift.Infrastructure/SmartShift.Infrastructure.csproj package Microsoft.EntityFrameworkCore.Relational --version 9.0.4

# Update EF Core packages in API project
dotnet add SmartShift.Api/SmartShift.Api.csproj package Microsoft.EntityFrameworkCore --version 9.0.4
dotnet add SmartShift.Api/SmartShift.Api.csproj package Microsoft.EntityFrameworkCore.SqlServer --version 9.0.4
dotnet add SmartShift.Api/SmartShift.Api.csproj package Microsoft.EntityFrameworkCore.Design --version 9.0.4
dotnet add SmartShift.Api/SmartShift.Api.csproj package Microsoft.EntityFrameworkCore.Relational --version 9.0.4

# Restore and build
dotnet restore
dotnet build 