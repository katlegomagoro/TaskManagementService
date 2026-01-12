# TaskManagementService

A cleanly architected **Task Management system** built with **.NET**, **CQRS**, **MediatR**, **Entity Framework Core**, and **PostgreSQL**, with a **Blazor Server UI** and **Firebase Authentication**.

This project is designed to demonstrate modern backend engineering practices, **SOLID principles**, and a clear separation of concerns.

---

## Overview

TaskManagementService provides a backend system for managing tasks, including **creating, updating, retrieving, and completing tasks**.

The solution follows **Clean Architecture** principles and exposes a REST API documented via **OpenAPI (Swagger)**.

A Blazor Server application is included as a lightweight client to demonstrate authentication and API consumption, while keeping the core focus on backend design and engineering quality.

---

## Key Features

- Task creation, update, retrieval, and completion
- CQRS with MediatR (commands and queries clearly separated)
- Clean Architecture with strict layer boundaries
- Entity Framework Core with PostgreSQL
- REST API with OpenAPI / Swagger documentation
- Firebase Authentication (email/password)
- Global error handling and validation
- Ready for unit testing and extension

---

## Architecture Overview

The solution is structured using **Clean Architecture** to ensure maintainability, testability, and scalability.



TaskManagementService.sln

/src
├── TaskManagementService.Domain
│ ├── Entities
│ ├── Enums
│ └── Common
│
├── TaskManagementService.Application
│ ├── Commands
│ ├── Queries
│ ├── Handlers
│ ├── Validators
│ ├── DTOs
│ └── Interfaces
│
├── TaskManagementService.Infrastructure
│ ├── Persistence (EF Core, DbContext, Configurations)
│ ├── Repositories
│ ├── Authentication (Firebase token validation)
│ └── DependencyInjection
│
├── TaskManagementService.Api
│ ├── Controllers
│ ├── Middleware
│ └── Program.cs
│
└── TaskManagementService.Blazor
├── Pages (Landing, Login, Register)
├── Components
├── Services
└── wwwroot


---

## CQRS & MediatR

- **Commands** handle state-changing operations  
  - `CreateTask`
  - `UpdateTask`
  - `CompleteTask`

- **Queries** handle read-only operations  
  - `GetTaskById`
  - `GetTasks`

MediatR acts as the request pipeline, enforcing separation of concerns and decoupling the application layer from controllers and infrastructure.

---

## SOLID Principles

- Single Responsibility per layer and class
- Dependency inversion via interfaces
- No UI or infrastructure leakage into the domain layer
- Clear boundaries between Domain, Application, Infrastructure, and Presentation

---

## Domain Model

### Task Entity

- Title
- Description
- Status (Draft, Active, Completed)
- CreatedAt
- CompletedAt (nullable)

The domain model is **persistence-agnostic** and contains no Entity Framework Core attributes.

---

## Data Layer

- Entity Framework Core with PostgreSQL
- Fluent configuration using `IEntityTypeConfiguration<T>`
- Migrations managed via the EF Core CLI
- No business logic inside the `DbContext`

---

## API Layer

The API exposes REST endpoints for all task operations.

- Documented using Swagger / OpenAPI
- Clear request and response models
- Standard HTTP status codes
- Error handling aligned with **RFC 7807 (Problem Details)**

### Example Endpoints



POST /api/tasks
GET /api/tasks
GET /api/tasks/{id}
PUT /api/tasks/{id}
POST /api/tasks/{id}/complete


---

## Authentication

Authentication is handled using **Firebase Authentication**.

- Email/password sign-in
- Firebase ID tokens (JWT) validated server-side
- Authentication isolated from domain and application logic
- Backend maps Firebase identity to internal user context where required

Firebase is used strictly for **identity**, not as a data store.

---

## Blazor Server UI

The Blazor Server project provides:

- Landing page
- Login and registration using Firebase Authentication
- A simple UI to demonstrate authenticated access to the API

The UI contains no business logic and communicates with the backend via HTTP.

---

## Tech Stack

- .NET (Blazor Server + Web API)
- C#
- Entity Framework Core
- PostgreSQL
- MediatR
- FluentValidation
- Swagger / OpenAPI
- MudBlazor
- Firebase Authentication
- Visual Studio 2022

---

## How to Run the Project

### Prerequisites

- .NET SDK installed
- PostgreSQL running locally
- Firebase project configured for authentication

## Set Up the Project

1. Clone the repository
2. Ensure `.NET 9 SDK` is installed
3. Open the solution in Visual Studio 2022 or run from terminal:

```bash
dotnet restore
dotnet ef database update
dotnet run --project ProductAdminPanel
```

Ensure your `appsettings.json` contains the following connection string:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Database=ProductAdminDb;Trusted_Connection=True;MultipleActiveResultSets=True;TrustServerCertificate=true"
}
```

For feedback or collaboration inquiries, reach out via:

**Name**: Katlego Magoro  
**Email**: katlegomagoro98@gmail.com  

**LinkedIn**: [linkedin.com/in/katlego-magoro-288b08236](https://www.linkedin.com/in/katlego-magoro-288b08236)

