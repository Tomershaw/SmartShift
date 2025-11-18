# ======================================
# STAGE 1 - Build
# ======================================
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy the entire repo into the build container (includes wwwroot)
COPY . .

# Restore dependencies for the API project
RUN dotnet restore SmartShift.Api/SmartShift.Api.csproj

# Build the API (Release)
RUN dotnet build SmartShift.Api/SmartShift.Api.csproj -c Release -o /app/build

# Publish the API
RUN dotnet publish SmartShift.Api/SmartShift.Api.csproj -c Release -o /app/publish

# ======================================
# STAGE 2 - Runtime
# ======================================
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Copy output from the build stage (includes wwwroot with frontend)
COPY --from=build /app/publish .

# Expose port 8080 (Container Apps default)
EXPOSE 8080

# Set ASP.NET Core to listen on port 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "SmartShift.Api.dll"]
