# TaskManagementService

A **.NET 9 Blazor Server** task management system with:

- **Firebase Authentication** for login/registration (via JS in the browser + JS interop to C#)
- **SQL Server + EF Core 9** as the system of record
- **Role/permission management** stored in SQL
- A **task CRUD** experience for normal users + an **admin master view** for Admin/SuperAdmin
- **MudBlazor** UI throughout

This repo focuses on solid backend fundamentals (data modeling, services, auth state, and permissions) while keeping the UI clean and maintainable.

---

## What this app does

- Users can register/login using Firebase email/password auth
- Users can create and manage their own tasks
- Admin/SuperAdmin can view all tasks in a master list
- SuperAdmin can manage user permissions (and admins can view)

---

## Solution structure (2 projects)

### 1) `TaskManagementService` (Blazor Server app)

This is the main app: UI, server runtime, DI wiring, controllers, and service layer that talks to the DAL.

**Key responsibilities**
- Render MudBlazor pages and components
- Maintain authentication state (claims + role)
- Call Firebase via JS interop (login/register/logout)
- Use `DbContextFactory` (DAL) through services to query/update SQL
- Enforce permission-driven UI and service rules

#### `wwwroot/`
Static assets.
- `firebase-auth.js`: browser-side auth wrapper (login/register/token handling)
- `firebase-admin.js`: JS helper for admin-related Firebase actions  
  > Important: no secrets or service-account keys should ever live in `wwwroot`.
- `css/`, `favicon.png`

#### `Pages/`
Routed screens.
- `Index.razor`: landing/root entry behavior
- `Authentication.razor`: `/auth` and `/auth/{Mode?}` wrapper for login/register + breadcrumbs
- `Profile.razor`: user profile page (DisplayName + task stats)
- `ManageUserPermissions.razor`: permission management page
- `Tasks/`
  - `MyTasks.razor`: user’s task list + filters + dialogs
  - `AllTasks.razor`: admin/master task list + stats + bulk operations

Most pages have code-behind `*.razor.cs` for logic.

#### `Components/`
Reusable UI components.
- `Authentication/`
  - `Login.razor` (+ `.razor.cs`)
  - `Register.razor` (+ `.razor.cs`)
- `Dialogs/`
  - `AddUserPermissionDialog.razor` (+ `.razor.cs`)
  - `ConfirmDialog.razor`
- `Tasks/`
  - `TaskFilters.razor` (+ `.razor.cs`) (search/status/date range)
  - `TaskForm.razor` (+ `.razor.cs`) (create/edit task dialog + validation)

#### `Services/`
Service layer used by pages/components.
- `AuthenticationService`: orchestrates sign-in/out + bridges Firebase auth results into app state
- `FirebaseAuthClient`: JS interop client calling `firebase-auth.js`
- `FirebaseUserSearchService`: looks up/searches users in Firebase (used in permissions UI)
- `PermissionService`: reads/writes `UserPermission` in SQL + applies role rules
- `TaskService`: reads/writes tasks in SQL + applies ownership/visibility rules
- `UserProfileService`: updates profile fields like DisplayName
- `CustomAuthenticationStateProvider`: Blazor auth state provider based on stored token + SQL permissions
- `AuthLocalStorageService`: stores auth token/state in browser storage
- `AuthStatePersistor`: keeps auth state consistent across refresh/navigation

#### `Interfaces/`
Contracts used by DI.
- `IAuthenticationService`
- `IFirebaseAuthClient`
- `IFirebaseUserSearchService`
- `ITaskService`
- `IUserProfileService`
(plus any others you’ve added)

#### `Controllers/`
Server endpoints.
- `FirebaseAdminController.cs`: server-side admin operations for Firebase  
  (admin logic should always stay server-side)

#### `Models/` and `ViewModels/`
UI-side models for binding and grids.
- `AuthenticationModel`
- `TaskViewModel`
- `UserPermissionViewModel`

#### `Shared/`
Layout and navigation.
- `MainLayout.razor` (+ `.razor.cs` + `.razor.css`)
- `NavMenu.razor` (+ `.razor.cs` + `.razor.css`)

#### `Extensions/`
- `EnumExtensions.cs` (enum display helpers)

#### Root files
- `Program.cs`: DI registrations (MudBlazor, EF Core factory, auth, services, controllers, Serilog, etc.)
- `appsettings.json`, `appsettings.Development.json`

---

### 2) `TaskManagementServiceDAL` (EF Core Data Access Layer)

This project owns the schema: entities, configurations, DbContext, factory, migrations.

**Key responsibilities**
- EF Core models
- Fluent configurations (`IEntityTypeConfiguration<T>`)
- `DbContext` + `DbContextFactory` (Blazor-friendly)
- EF migrations

#### `Models/`
Database entities.
- `AppUser` (maps Firebase UID to your local user record)
- `TaskItem` (task table)
- `UserPermission` (role/permission table)

#### `Enums/`
- `PermissionType`
- `TaskStatus`

#### `Configurations/`
Fluent mappings for each entity.
- `AppUserConfiguration`
- `TaskItemConfiguration`
- `UserPermissionConfiguration`

Includes property constraints, relationships, indexes, and enum conversions (stored as string).

#### `TaskManagementServiceDbContext.cs`
- DbSets, relationship setup, applies configurations

#### `TaskManagementServiceDbContextFactory.cs`
- Factory for safe DbContext creation in Blazor Server

#### `Migrations/`
- Initial migration + model snapshot

#### `appsettings.json`
- DAL-side connection string placeholder/defaults

---

## Core data + rules

### Users (Firebase + SQL)
- Firebase handles **authentication**
- SQL stores the app’s user profile and permissions

`AppUser` (SQL) typically contains:
- `FirebaseUid` (primary link to Firebase identity)
- `Email`
- `DisplayName`
- other profile fields you add later

### Permissions
Permissions are stored in SQL via `UserPermission`.

High-level behavior:
- **SuperAdmin**: full control (manage permissions, bulk actions, etc.)
- **Admin**: view-only permissions, broader task visibility
- **User**: manages own tasks only

### Tasks
- `TaskItem` is the database record
- `/tasks` shows the current user’s tasks
- `/all-tasks` shows all tasks for Admin/SuperAdmin
- Create/Edit flows happen in `TaskForm` dialog
- Filtering is done via `TaskFilters`

---

## App flow (end-to-end)

1. User visits `/auth` and logs in/registers via `firebase-auth.js`
2. JS returns token/uid → C# receives it through `FirebaseAuthClient` (JS interop)
3. `AuthenticationService` ensures the user exists in SQL (`AppUser`)
4. `CustomAuthenticationStateProvider` builds claims + resolves role from SQL permissions
5. User navigates:
   - `/tasks` for personal task management
   - `/all-tasks` for admin master view
   - `/ManageUserPermissions` for permission management
   - `/profile` to update DisplayName + view stats

---

## Tech stack

- .NET 9 (Blazor Server)
- C#
- MudBlazor
- EF Core 9
- SQL Server
- Firebase Authentication (email/password)
- Serilog (logging)

---

````md
## How to run locally

### Prerequisites
- .NET 9 SDK
- SQL Server (LocalDB or full instance)
- A Firebase project configured for Email/Password auth

### Configure settings
Update your connection string (typically in `TaskManagementService/appsettings.Development.json`):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=TaskManagementServiceDb;Trusted_Connection=True;MultipleActiveResultSets=True;TrustServerCertificate=true"
  }
}
````

Make sure Firebase settings used by your `firebase-auth.js` are correct (Firebase config values are safe to be client-side, but **service account secrets are not**).

### Apply migrations

Run migrations (choose the correct startup project):

```bash
dotnet ef database update --project TaskManagementServiceDAL --startup-project TaskManagementService
```

### Run the app

```bash
dotnet run --project TaskManagementService
```

---

## Sample SQL data (optional)

If you want a small, clean dataset for local testing, run the script below in SSMS or Azure Data Studio.
It inserts **5 users** and **10 tasks** (2 tasks per user), and sets `CompletedAtUtc` for completed items.

### Notes

* Adjust enum values if your `PermissionType` or `TaskStatus` enums differ.
* Assumes tables are named `AppUsers` and `TaskItems`.
* Uses UTC (`GETUTCDATE()`).
* This script is safe to run multiple times (it checks by email before inserting).

### One-time seed script (users + tasks)

```sql
SET NOCOUNT ON;

BEGIN TRAN;

-- 1) Insert 5 users (only if they don't exist yet)
IF NOT EXISTS (SELECT 1 FROM AppUsers WHERE Email = 'john.smith@example.com')
BEGIN
    INSERT INTO AppUsers (DisplayName, Email, FirebaseUid, PermissionType, CreatedAtUtc, ModifiedAtUtc)
    VALUES ('John Smith', 'john.smith@example.com', 'seed_uid_john', 0, GETUTCDATE(), GETUTCDATE());
END;

IF NOT EXISTS (SELECT 1 FROM AppUsers WHERE Email = 'sarah.j@example.com')
BEGIN
    INSERT INTO AppUsers (DisplayName, Email, FirebaseUid, PermissionType, CreatedAtUtc, ModifiedAtUtc)
    VALUES ('Sarah Johnson', 'sarah.j@example.com', 'seed_uid_sarah', 1, GETUTCDATE(), GETUTCDATE());
END;

IF NOT EXISTS (SELECT 1 FROM AppUsers WHERE Email = 'mike.wilson@example.com')
BEGIN
    INSERT INTO AppUsers (DisplayName, Email, FirebaseUid, PermissionType, CreatedAtUtc, ModifiedAtUtc)
    VALUES ('Mike Wilson', 'mike.wilson@example.com', 'seed_uid_mike', 2, GETUTCDATE(), GETUTCDATE());
END;

IF NOT EXISTS (SELECT 1 FROM AppUsers WHERE Email = 'emma.davis@example.com')
BEGIN
    INSERT INTO AppUsers (DisplayName, Email, FirebaseUid, PermissionType, CreatedAtUtc, ModifiedAtUtc)
    VALUES ('Emma Davis', 'emma.davis@example.com', 'seed_uid_emma', 0, GETUTCDATE(), GETUTCDATE());
END;

IF NOT EXISTS (SELECT 1 FROM AppUsers WHERE Email = 'admin@example.com')
BEGIN
    INSERT INTO AppUsers (DisplayName, Email, FirebaseUid, PermissionType, CreatedAtUtc, ModifiedAtUtc)
    VALUES ('Admin User', 'admin@example.com', 'seed_uid_admin', 3, GETUTCDATE(), GETUTCDATE());
END;

-- 2) Resolve user IDs
DECLARE @johnId  INT = (SELECT Id FROM AppUsers WHERE Email = 'john.smith@example.com');
DECLARE @sarahId INT = (SELECT Id FROM AppUsers WHERE Email = 'sarah.j@example.com');
DECLARE @mikeId  INT = (SELECT Id FROM AppUsers WHERE Email = 'mike.wilson@example.com');
DECLARE @emmaId  INT = (SELECT Id FROM AppUsers WHERE Email = 'emma.davis@example.com');
DECLARE @adminId INT = (SELECT Id FROM AppUsers WHERE Email = 'admin@example.com');

-- 3) Insert 10 tasks (2 per user) - only if each task doesn't already exist for that user
-- Status examples (adjust to your enum):
-- 0 = Open, 1 = InProgress, 2 = Completed, 3 = OnHold

-- John (2)
IF NOT EXISTS (SELECT 1 FROM TaskItems WHERE OwnerUserId = @johnId AND Title = 'Draft sprint plan')
BEGIN
    INSERT INTO TaskItems (Title, Description, Status, OwnerUserId, CreatedAtUtc, ModifiedAtUtc, CompletedAtUtc)
    VALUES ('Draft sprint plan', 'Create a simple sprint plan for the week', 1, @johnId,
            DATEADD(DAY, -6, GETUTCDATE()), DATEADD(DAY, -2, GETUTCDATE()), NULL);
END;

IF NOT EXISTS (SELECT 1 FROM TaskItems WHERE OwnerUserId = @johnId AND Title = 'Submit project update')
BEGIN
    INSERT INTO TaskItems (Title, Description, Status, OwnerUserId, CreatedAtUtc, ModifiedAtUtc, CompletedAtUtc)
    VALUES ('Submit project update', 'Send weekly progress update to the team', 2, @johnId,
            DATEADD(DAY, -5, GETUTCDATE()), DATEADD(DAY, -1, GETUTCDATE()), DATEADD(DAY, -1, GETUTCDATE()));
END;

-- Sarah (2)
IF NOT EXISTS (SELECT 1 FROM TaskItems WHERE OwnerUserId = @sarahId AND Title = 'Review backlog')
BEGIN
    INSERT INTO TaskItems (Title, Description, Status, OwnerUserId, CreatedAtUtc, ModifiedAtUtc, CompletedAtUtc)
    VALUES ('Review backlog', 'Triage and reorder backlog items', 0, @sarahId,
            DATEADD(DAY, -7, GETUTCDATE()), DATEADD(DAY, -7, GETUTCDATE()), NULL);
END;

IF NOT EXISTS (SELECT 1 FROM TaskItems WHERE OwnerUserId = @sarahId AND Title = 'Approve timesheets')
BEGIN
    INSERT INTO TaskItems (Title, Description, Status, OwnerUserId, CreatedAtUtc, ModifiedAtUtc, CompletedAtUtc)
    VALUES ('Approve timesheets', 'Approve submitted timesheets for the month', 2, @sarahId,
            DATEADD(DAY, -4, GETUTCDATE()), DATEADD(DAY, -2, GETUTCDATE()), DATEADD(DAY, -2, GETUTCDATE()));
END;

-- Mike (2)
IF NOT EXISTS (SELECT 1 FROM TaskItems WHERE OwnerUserId = @mikeId AND Title = 'Fix validation bug')
BEGIN
    INSERT INTO TaskItems (Title, Description, Status, OwnerUserId, CreatedAtUtc, ModifiedAtUtc, CompletedAtUtc)
    VALUES ('Fix validation bug', 'Resolve required-field validation on task form', 1, @mikeId,
            DATEADD(DAY, -3, GETUTCDATE()), DATEADD(DAY, -1, GETUTCDATE()), NULL);
END;

IF NOT EXISTS (SELECT 1 FROM TaskItems WHERE OwnerUserId = @mikeId AND Title = 'Refactor task query')
BEGIN
    INSERT INTO TaskItems (Title, Description, Status, OwnerUserId, CreatedAtUtc, ModifiedAtUtc, CompletedAtUtc)
    VALUES ('Refactor task query', 'Improve filtering query performance', 3, @mikeId,
            DATEADD(DAY, -6, GETUTCDATE()), DATEADD(DAY, -3, GETUTCDATE()), NULL);
END;

-- Emma (2)
IF NOT EXISTS (SELECT 1 FROM TaskItems WHERE OwnerUserId = @emmaId AND Title = 'Update dashboard layout')
BEGIN
    INSERT INTO TaskItems (Title, Description, Status, OwnerUserId, CreatedAtUtc, ModifiedAtUtc, CompletedAtUtc)
    VALUES ('Update dashboard layout', 'Adjust spacing and grid layout for tasks page', 0, @emmaId,
            DATEADD(DAY, -5, GETUTCDATE()), DATEADD(DAY, -5, GETUTCDATE()), NULL);
END;

IF NOT EXISTS (SELECT 1 FROM TaskItems WHERE OwnerUserId = @emmaId AND Title = 'Polish status chips')
BEGIN
    INSERT INTO TaskItems (Title, Description, Status, OwnerUserId, CreatedAtUtc, ModifiedAtUtc, CompletedAtUtc)
    VALUES ('Polish status chips', 'Improve chip labels and consistency', 2, @emmaId,
            DATEADD(DAY, -4, GETUTCDATE()), DATEADD(DAY, -1, GETUTCDATE()), DATEADD(DAY, -1, GETUTCDATE()));
END;

-- Admin (2)
IF NOT EXISTS (SELECT 1 FROM TaskItems WHERE OwnerUserId = @adminId AND Title = 'Audit permissions')
BEGIN
    INSERT INTO TaskItems (Title, Description, Status, OwnerUserId, CreatedAtUtc, ModifiedAtUtc, CompletedAtUtc)
    VALUES ('Audit permissions', 'Verify Admin and SuperAdmin access rules', 1, @adminId,
            DATEADD(DAY, -2, GETUTCDATE()), GETUTCDATE(), NULL);
END;

IF NOT EXISTS (SELECT 1 FROM TaskItems WHERE OwnerUserId = @adminId AND Title = 'Verify backups')
BEGIN
    INSERT INTO TaskItems (Title, Description, Status, OwnerUserId, CreatedAtUtc, ModifiedAtUtc, CompletedAtUtc)
    VALUES ('Verify backups', 'Confirm nightly backups are running', 2, @adminId,
            DATEADD(DAY, -3, GETUTCDATE()), DATEADD(DAY, -1, GETUTCDATE()), DATEADD(DAY, -1, GETUTCDATE()));
END;

COMMIT TRAN;
```

---

## What’s done vs what’s next

**Done foundation**

* Firebase auth wired (login/register)
* Auth state persisted (local storage + custom auth state provider)
* DAL with EF Core models/configurations and migrations
* Core pages: auth, profile, my tasks, all tasks, manage permissions
* MudBlazor UI structure in place

**Typical next steps**

* Tighten service-layer authorization checks (not only UI gating)
* Server-side paging everywhere + more efficient queries
* Add audit fields (Created/Modified) + soft delete if needed
* Optional: introduce CQRS/MediatR once the core feature set is stable

---

## Contact

**Name**: Katlego Magoro
**Email**: [katlegomagoro98@gmail.com](mailto:katlegomagoro98@gmail.com)
**LinkedIn**: [https://www.linkedin.com/in/katlego-magoro-288b08236](https://www.linkedin.com/in/katlego-magoro-288b08236)




