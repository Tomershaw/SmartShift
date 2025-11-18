# SmartShift Codebase Architecture Guide

## Overview
SmartShift is a full-stack shift management and employee scheduling application with AI-powered shift assignment capabilities. The architecture follows **Clean Architecture** principles with clear separation of concerns across multiple layers.

---

## Backend Architecture Pattern: Clean Architecture + CQRS (MediatR)

The backend is organized into distinct layers:

1. **SmartShift.Domain** - Core business logic and entities
2. **SmartShift.Application** - Use cases and command/query handlers
3. **SmartShift.Infrastructure** - Data access, external services, AI integration
4. **SmartShift.Api** - HTTP endpoints and request handling
5. **SmartShift.Contracts** - Shared DTOs (minimal usage)
6. **SmartShift.AppHost** - Aspire orchestration for local development

---

## Backend Architecture Details

### 1. Domain Layer (`SmartShift.Domain`)

**Purpose**: Contains business entities and core domain logic with no external dependencies.

**Key Entities**:
- `ApplicationUser` - Extends IdentityUser with TenantId, FullName, CreatedAt/UpdatedAt
- `Shift` - Represents scheduled work with validation (StartTime, RequiredEmployeeCount, SkillLevelRequired, MinimumEarlyEmployees)
- `Employee` - Worker profile linked to ApplicationUser
- `Tenant` - Multi-tenant support (one tenant per organization)
- `ShiftRegistration` - Join table tracking employee registrations for shifts with status (Pending/Approved/Rejected)
- `RefreshToken` - JWT token refresh mechanism
- `EmployeeShiftAvailability` - Enum for shift arrival types (Early/Regular)
- `ShiftStatus` - Enum (Open/Assigned/Cancelled)

**Key Domain Services**:
- `EmployeeShiftMatchingService` - Matches employees to shifts based on criteria
- `ShiftScoringService` - Scores employee-shift compatibility

**Database Relationships**:
- ApplicationUser -> Tenant (many-to-one)
- Shift -> Tenant (many-to-one)
- ShiftRegistration -> Shift + Employee + Tenant (composite foreign keys with NoAction delete)
- Unique constraint on ShiftRegistration: (TenantId, EmployeeId, ShiftId) when Status is Pending/Approved

### 2. Application Layer (`SmartShift.Application`)

**Purpose**: Orchestrates use cases using MediatR CQRS pattern. Each feature has:
- **Command** - IRequest<TResponse> (state-changing operation)
- **CommandHandler** - IRequestHandler<TCommand, TResponse>
- **Validator** - FluentValidation for input validation
- **Result/Response** - Simple DTO with Success flag and Message

**Feature Organization** (by domain):
```
Features/
├── Employees/
├── ProcessShifts/
├── Scheduling/
│   ├── RegisterForShift/
│   ├── ApproveShiftEmployees/
│   ├── CancelRegistration/
│   ├── CancelShift/
│   ├── CreateShift/
│   ├── DeleteShift/
│   ├── DayRegistrants/
│   ├── GetEmployeeShifts/
│   ├── GetShifts/
│   └── RegistrationsSnapshot/
└── UserManagement/
```

**Key Pattern - Example: RegisterForShift**:
```csharp
// Command (IRequest<RegisterForShiftResult>)
public class RegisterForShiftCommand : IRequest<RegisterForShiftResult>
{
    public Guid ShiftId { get; set; }
    public EmployeeShiftAvailability ShiftArrivalType { get; set; }
    public Guid UserId { get; set; }
}

// Handler (implements IRequestHandler<T, TResult>)
public class RegisterForShiftCommandHandler : IRequestHandler<RegisterForShiftCommand, RegisterForShiftResult>
{
    public async Task<RegisterForShiftResult> Handle(RegisterForShiftCommand request, CancellationToken cancellationToken)
    {
        // Business logic
    }
}
```

**Pipeline Behaviors**:
- `ValidationBehavior<TRequest, TResponse>` - Runs FluentValidation before each handler

**DI Registration** (`DependencyInjection.cs`):
- Registers all MediatR handlers via assembly scanning
- Services wired up in SmartShift.Api Program.cs

### 3. Infrastructure Layer (`SmartShift.Infrastructure`)

**Database**:
- **Type**: SQL Server (EF Core 9.0.4)
- **Context**: `ApplicationDbContext` extends IdentityDbContext<ApplicationUser>
- **Migrations**: Located in Migrations/ folder, applied on startup via `context.Database.Migrate()`

**Key Components**:

#### Data Access
- `ApplicationDbContext` - DbContext with DbSet for Shifts, Employees, ShiftRegistrations, Tenants, RefreshTokens
- Configured relationships and constraints in OnModelCreating

#### Repositories (Interface-based)
- `IEmployeeRepository` / `EmployeeRepository`
  - GetByIdAsync, AddAsync, UpdateAsync, GetAllAsync, DeleteAsync
  
- `IShiftRepository` / `ShiftRepository`
  - GetByIdAsync, GetAllAsync, RegisterEmployeeForShiftAsync, ApproveShiftRegistrationAsync
  - GetApprovedEmployeesForShiftAsync, GetWeekAssignmentsSnapshotAsync (complex business logic)
  - CancelRegistrationAsync, GetShiftsInDateRangeAsync

#### Authentication
- `IJwtTokenGenerator` / `JwtTokenGenerator` - Creates JWT tokens
- `RefreshTokenService` - Manages refresh token lifecycle
- `IUserRegistrationService` - Handles user sign-up
- `ICurrentUserService` / `CurrentUserService` - Extracts user info from JWT claims

#### AI Integration
- `IShiftAssignmentAIService` / `ShiftAssignmentAIService`
  - Uses Microsoft Semantic Kernel with OpenAI
  - Methods:
    - `AnalyzeShiftRequirementsAsync` - AI analysis of shift needs
    - `GenerateShiftSummaryAsync` - Creates shift summary
    - `GetRecommendedEmployeesAsync` - AI-based employee recommendations

#### Seeding
- `SeedData` - Initializes database with:
  - Default Tenant ("אריא")
  - Sample Employees
  - Sample Shifts
  - Identity Roles (Admin, Manager, Employee)
  - Default Admin user

### 4. API Layer (`SmartShift.Api`)

**Framework**: Carter (minimal API routing library) + ASP.NET Core

**Request/Response Pattern**:
- Endpoints use Carter's `ICarterModule` for route grouping
- Endpoints invoke MediatR commands/queries

**Endpoint Structure**:
```
Features/
├── AI/
│   └── Endpoints/
├── Admin/
│   └── Endpoints/
├── Employees/
│   └── Endpoints/
└── Scheduling/
    └── Endpoints/
        ├── SchedulingModule.cs - RegisterForShift (POST /api/shifts/register)
        ├── ApproveShiftEmployeesModule.cs
        ├── CancelRegistrationModule.cs
        ├── GetEmployeeShiftsModule.cs
        └── ShiftsModule.cs

Root Endpoints/
├── LoginEndpoint.cs (POST /api/account/login)
├── LogoutEndpoint.cs (POST /api/account/logout)
├── RefreshTokenEndpoint.cs (POST /api/account/refresh-token)
├── RegisterEndpoint.cs (POST /api/account/register)
└── TokenValidationEndpoint.cs
```

**Example Endpoint (Carter Module)**:
```csharp
public class SchedulingModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/shifts/register", RegisterForShift)
            .WithName("RegisterForShift")
            .WithTags("Scheduling");
    }
    
    private static async Task<IResult> RegisterForShift(
        RegisterForShiftCommand command,
        IMediator mediator,
        ICurrentUserService currentUserService)
    {
        var userId = currentUserService.GetUserId();
        command.UserId = Guid.Parse(userId);
        
        var result = await mediator.Send(command);
        return result.Success ? Results.Ok(result) : Results.BadRequest(result);
    }
}
```

**Authentication**:
- JWT Bearer tokens (configured in Program.cs)
- Token validation: Issuer, Audience, Lifetime, Signing Key
- Claims: NameClaimType = "nameid", RoleClaimType = ClaimTypes.Role
- Custom events for OnTokenValidated, OnAuthenticationFailed, OnChallenge, OnForbidden

**Middleware**:
- `ExceptionMiddleware` - Global error handling
- CORS - Allows localhost in dev, configured origins in prod via CORS_ORIGINS env var
- Static file serving - Frontend served from wwwroot/

**Program.cs Setup**:
1. Swagger/OpenAPI configuration with JWT security
2. Application & Infrastructure DI
3. Entity Framework DbContext (SQL Server)
4. Identity Core (ApplicationUser + IdentityRole)
5. JWT Bearer authentication
6. Carter for minimal APIs
7. MediatR for command/query dispatch
8. FluentValidation for input validation
9. Semantic Kernel for AI services
10. Database seeding on startup

---

## Frontend Architecture

**Framework Stack**:
- **Build Tool**: Vite 6.3.5 with TypeScript
- **Frontend Framework**: React 19.1.0 with React Router DOM 7.6.0
- **Styling**: Tailwind CSS 3.4.3 + PostCSS
- **HTTP Client**: Axios 1.9.0 with interceptors
- **UI Components**: Lucide React icons
- **Drag & Drop**: @dnd-kit (for shift management)

**Project Structure**:
```
frontend/src/
├── main.tsx - App initialization
├── App.tsx - Route definitions (React Router)
├── App.css - Global styles
├── index.css - Tailwind + global CSS
├── features/
│   ├── auth/
│   │   ├── api/authService.ts - Auth API calls
│   │   ├── components/
│   │   │   ├── AuthPage.tsx
│   │   │   ├── ProtectedRoute.tsx - Route guard
│   │   │   └── LoginForm
│   │   └── context/
│   │       ├── AuthContext.tsx - Type definitions
│   │       ├── AuthProvider.tsx - Auth state management
│   │       └── useAuth.ts - Hook for auth context
│   ├── appLoading/ - Loading states
│   └── scheduling/
│       ├── components/EmployeeSignupPage.tsx
│       └── admin/pages/
│           ├── AdminDashboardPage.tsx
│           ├── AdminShiftsPage.tsx
│           ├── AdminShiftSummaryPage.tsx
│           ├── AdminCreateShiftPage.tsx
│           ├── AdminCreateWeekPage.tsx
│           ├── AdminRegistrationsPage.tsx
│           └── ratings/pages/AdminEmployeesPage.tsx
└── services/
    └── api.ts - Axios instance with interceptors
```

### Frontend API Integration

**HTTP Client Setup** (`api.ts`):
```typescript
const api = axios.create({
    baseURL: "https://localhost:7002/api",
});

// Request Interceptor: Adds JWT to Authorization header
// Response Interceptor: 
//   - 401 status triggers automatic token refresh
//   - Retries failed request with new token
//   - Falls back to logout on refresh failure
```

**Token Management**:
- Access token & Refresh token stored in localStorage
- Automatic token refresh on 401 response
- Refresh endpoint excluded from Authorization header (to avoid infinite loops)

### State Management & Authentication

**React Context Pattern** (`AuthContext.tsx`):
```typescript
export interface AuthContextType {
  user: { email: string; role: string; exp: number } | null;
  isAuthenticated: boolean;
  logout: () => void;
}
```

**ProtectedRoute Component**:
- Checks user authentication and roles (Admin, Manager, Employee)
- Redirects to /auth if not authenticated
- Uses allowedRoles prop to restrict access

### Routing

Key routes defined in `App.tsx`:
- `/auth` - LoginForm + RegisterForm
- `/employee/signup` - Employee shift registration page
- `/admin` - Admin dashboard (Admin/Manager only)
- `/admin/shifts` - Shift management grid (Admin/Manager)
- `/admin/shifts/:shiftId/summary` - Individual shift details (Admin/Manager)
- `/admin/employees/register` - Register new employee (Admin/Manager)
- `/admin/ratings` - Employee ratings (Admin/Manager)
- `/admin/shifts/create` - Create single shift (Admin/Manager)
- `/admin/shifts/create-week` - Create week of shifts (Admin/Manager)
- `/admin/registrations` - View shift registrations (Admin/Manager)

### Build Output

- Vite build output directory: `../SmartShift.Api/wwwroot`
- Built frontend files served as static content by ASP.NET Core
- Dev server proxies `/api/*` requests to backend at `https://localhost:7001`

---

## Frontend-Backend Integration

1. **API Communication**:
   - Frontend calls backend REST API at `/api/*` paths
   - Backend responds with JSON (command results with Success/Message/Data)

2. **Authentication Flow**:
   - User logs in via LoginEndpoint → receives JWT + RefreshToken
   - Frontend stores tokens in localStorage
   - Axios interceptor adds Authorization header to all requests
   - Token refresh happens automatically on 401

3. **Static File Serving**:
   - Frontend Vite build outputs to SmartShift.Api/wwwroot
   - ASP.NET Core serves index.html for all unmatched routes (SPA fallback)
   - API routes remain under `/api/*` and are handled by Carter endpoints

4. **Development Mode**:
   - Frontend dev server runs on configured port (via Vite)
   - Proxies /api calls to backend HTTPS endpoint
   - Backend CORS allows localhost origins

---

## Orchestration: SmartShift.AppHost

**Framework**: .NET Aspire (Microsoft's distributed app orchestration)

**Purpose**: 
- Local development environment orchestration
- Manages container startup and service discovery
- Simplifies running multi-service applications

**Configuration** (Program.cs):
```csharp
var builder = DistributedApplication.CreateBuilder(args);
var api = builder.AddProject<Projects.SmartShift_Api>("api");
builder.Build().Run();
```

**Node.js Support**: Aspire.Hosting.NodeJs package enables frontend service integration

---

## Database Architecture

### Technology
- **Engine**: SQL Server (EF Core 9.0.4)
- **Migration Strategy**: EF Core migrations with automatic application on startup
- **Multi-tenancy**: TenantId column on Tenant-dependent entities

### Key Tables
1. **AspNetUsers** - ApplicationUser (from Identity)
2. **AspNetRoles** - IdentityRole
3. **AspNetUserRoles** - Identity mapping
4. **Tenants** - Organization container
5. **Employees** - Worker records (linked to AspNetUsers via UserId)
6. **Shifts** - Scheduled work slots with requirements
7. **ShiftRegistrations** - Employee-to-Shift join with status tracking
8. **RefreshTokens** - JWT token refresh persistence
9. **AspNetUserClaims**, **AspNetUserLogins**, etc. - Identity tables

### Constraints
- ShiftRegistration has unique index on (TenantId, EmployeeId, ShiftId) filtered for Status IN (Pending, Approved)
- Prevents duplicate active registrations for same employee on same shift

---

## Key Libraries & Frameworks Summary

### Backend
| Layer | Library | Version | Purpose |
|-------|---------|---------|---------|
| API | Carter | 9.0.0 | Minimal API routing |
| CQRS | MediatR | 12.5.0 | Command/Query dispatch pattern |
| Validation | FluentValidation | 12.0.0 | Input validation |
| ORM | Entity Framework Core | 9.0.4 | Data access |
| Database | SQL Server | - | Primary data store |
| Auth | JWT Bearer | 9.0.0 | Token authentication |
| Auth | ASP.NET Identity | 9.0.4 | User/Role management |
| AI | Microsoft Semantic Kernel | 1.4.0 | LLM integration (OpenAI) |
| API Docs | Swagger/Swashbuckle | 8.1.1 | OpenAPI documentation |
| Orchestration | .NET Aspire | 9.2.1 | Local dev orchestration |

### Frontend
| Category | Library | Version | Purpose |
|----------|---------|---------|---------|
| Framework | React | 19.1.0 | UI components |
| Router | React Router DOM | 7.6.0 | Client-side routing |
| HTTP | Axios | 1.9.0 | API communication |
| Styling | Tailwind CSS | 3.4.3 | Utility-first CSS |
| Build | Vite | 6.3.5 | Fast build tool |
| Language | TypeScript | ~5.8.3 | Type safety |
| Icons | Lucide React | 0.543.0 | Icon library |
| Drag & Drop | @dnd-kit | 6.3.1 | Shift drag-and-drop |

---

## Key Design Patterns

### 1. CQRS (Command Query Responsibility Segregation)
- **Commands**: State-changing operations (RegisterForShift, CreateShift, ApproveRegistration)
- **Queries**: Read-only operations (GetShifts, GetEmployeeShifts)
- All routed through MediatR

### 2. Repository Pattern
- IEmployeeRepository, IShiftRepository abstractions
- Implementations in Infrastructure layer
- DI allows swapping implementations

### 3. Pipeline Behavior Pattern (MediatR)
- ValidationBehavior intercepts all commands
- Runs FluentValidation before handler execution
- Throws ValidationException if validation fails

### 4. Multi-Tenancy
- TenantId column on data entities
- ICurrentUserService extracts TenantId from JWT claims
- All queries filtered by tenant

### 5. Clean Separation of Concerns
- Domain: Pure business logic, no dependencies
- Application: Use case orchestration, no ASP.NET references
- Infrastructure: External concerns (DB, AI, Auth)
- API: HTTP request handling only

### 6. Dependency Injection (Microsoft.Extensions.DependencyInjection)
- Interface-based registration
- Scoped services for request lifetime
- Transient for stateless behaviors

---

## Development Workflow

### Backend Development
1. Define domain entity in `SmartShift.Domain`
2. Create EF Core migration if schema changes
3. Create Command/Handler/Validator in `SmartShift.Application/Features/*`
4. Create Carter endpoint in `SmartShift.Api/Features/*/Endpoints`
5. DI automatically registers via assembly scanning
6. Test via Swagger UI

### Frontend Development
1. Create React component in `frontend/src/features/`
2. Add route in `App.tsx`
3. Call backend API via `api` instance from `frontend/src/services/api.ts`
4. Vite dev server hot-reloads on save
5. Axios interceptors handle authentication

### Local Testing
- Run via SmartShift.AppHost (Aspire)
- Or run individual projects (backend on 7001/7002, frontend on dev port)
- Database initialized and seeded on first startup

---

## Important Implementation Notes

### Token & Authentication
- JWT key, issuer, audience configured in appsettings.json
- Refresh tokens stored in database for revocation capability
- Frontend must exclude Authorization header from refresh-token requests

### Multi-Tenancy
- All queries must filter by TenantId
- ICurrentUserService provides tenant context from JWT
- Database constraints enforce tenant isolation

### AI Integration (Semantic Kernel)
- Configured in `SemanticKernelServiceExtensions`
- Analyzes shifts and generates summaries using OpenAI
- Optional/graceful degradation if API unavailable

### Error Handling
- MediatR pipeline validation throws FluentValidation.ValidationException
- ExceptionMiddleware catches and formats error responses
- Frontend expects { Message, Success } response format

### CORS & Security
- Dev: Allows all localhost origins
- Prod: Configured via CORS_ORIGINS environment variable
- HTTPS enforced in production

---

## Configuration Files

### Backend
- `SmartShift.Api/appsettings.json` - Default settings (localhost)
- `SmartShift.Api/appsettings.Development.json` - Dev-specific overrides
- Environment variables:
  - `ConnectionStrings:DefaultConnection` - SQL Server
  - `Jwt:Key`, `Jwt:Issuer`, `Jwt:Audience` - JWT config
  - `CORS_ORIGINS` - Production CORS origins
  - `SemanticKernel:*` - OpenAI API configuration

### Frontend
- `frontend/vite.config.ts` - Build output to wwwroot, dev proxy configuration
- `frontend/tsconfig.json` - TypeScript strict mode enabled
- `frontend/tailwind.config.js` - Tailwind CSS configuration

---

## Next Steps for Development

1. **Adding a New Feature**:
   - Create domain entity if needed
   - Create application command + handler + validator
   - Create Carter endpoint
   - Create frontend React component + API service call

2. **Database Changes**:
   - Modify domain entity or DbContext
   - Run `dotnet ef migrations add MigrationName`
   - Migrations auto-apply on startup

3. **API Testing**:
   - Use Swagger UI at /swagger when app runs
   - Use Postman with Bearer token authentication
   - Frontend can also be tested directly

4. **Debugging**:
   - Backend: Break in handler, use ILogger for logging
   - Frontend: React DevTools, Axios interceptor logs
   - Database: SQL Server Management Studio or Azure Data Studio

---

**Last Updated**: November 2025
**Project Status**: Active Development
**Target Framework**: .NET 9, React 19, TypeScript 5.8
