# Asset Management System

ASP.NET Core Web API (.NET 8 LTS) + EF Core 8 + SQL Server, with a simple web UI. Four modules: Assets, Employees, Allocation, Returns.

```
AssetManagement.sln
├── AssetManagement.API/   backend:  Controllers → Services → AppDbContext → SQL Server   (http://localhost:5025)
└── AssetManagement.UI/    frontend: ASP.NET Core MVC (Razor .cshtml views), calls the API server-side   (http://localhost:5003)
```

Allocation and Returns share one table (`AssetAllocations`). A return closes an open allocation by setting `ReturnedDate`, so the full history is kept.

## Prerequisites

- .NET 8 SDK
- SQL Server Express running as `localhost\SQLEXPRESS` (Windows login)

## Setup

1. The connection string is in `AssetManagement.API/appsettings.json`. It uses Windows login, so no password is needed:
   ```json
   "ConnectionStrings": {
     "Default": "Server=localhost\\SQLEXPRESS;Database=AssetManagement;Trusted_Connection=True;TrustServerCertificate=True"
   }
   ```
   For a different instance, change `Server=` (e.g. `localhost\MSSQLSERVER01`).
2. Restore the EF CLI tool (pinned in `dotnet-tools.json`) and create the database:
   ```
   dotnet tool restore
   cd AssetManagement.API
   dotnet ef database update
   ```
   The `InitialCreate` migration is already in `Migrations/`, so `database update` creates the `AssetManagement` database and all 3 tables.
3. Set up Google sign-in (see [Sign-in setup](#sign-in-setup) below) and store the values as user-secrets:
   ```
   dotnet user-secrets set "Authentication:Google:ClientId" "<your client id>" --project AssetManagement.UI
   dotnet user-secrets set "Authentication:Google:ClientSecret" "<your client secret>" --project AssetManagement.UI
   dotnet user-secrets set "Auth:AdminEmails" "you@gmail.com" --project AssetManagement.UI
   ```
4. Run the API and the UI (two terminals, from the repo root):
   ```
   dotnet run --project AssetManagement.API --launch-profile http
   dotnet run --project AssetManagement.UI --launch-profile http
   ```
   - UI: http://localhost:5003
   - Swagger: http://localhost:5025/swagger (click **Authorize** and enter the API key, `dev-only-api-key-change-me` in Development)

## Sign-in setup

Users sign in with Google (OAuth). The UI only trusts what Google says about who the user is.

**1. Create the Google credentials** (once):
1. Open https://console.cloud.google.com/apis/credentials and pick or create a project.
2. If asked, configure the *OAuth consent screen* (External, add your email as a test user).
3. **Create credentials → OAuth client ID → Web application.**
4. Under **Authorized redirect URIs** add `http://localhost:5003/signin-google`.
5. Copy the **Client ID** and **Client secret**.

**2. Give them to the app:** as user-secrets for local runs (step 3 above), or in `.env` for Docker (`GOOGLE_CLIENT_ID`, `GOOGLE_CLIENT_SECRET`, `ADMIN_EMAILS`).

**Who can do what**

| Role | Who | Can |
|---|---|---|
| Admin | emails listed in `Auth:AdminEmails` / `ADMIN_EMAILS` (comma separated) | view everything, and add, edit, delete, allocate and return |
| Viewer | any other Google account that signs in | view only (the buttons and forms are hidden, and the server refuses changes anyway) |
| not signed in | | nothing, redirected to the sign-in page |

**How the API is protected:** the API is not open to users. Every `/api` call must send the shared secret in the `X-Api-Key` header, and only the UI server knows it (`ApiKey` setting; `API_KEY` in `.env`). In Development both projects use `dev-only-api-key-change-me` from `appsettings.Development.json`; never use that value anywhere real.

## Run with Docker

Needs only Docker Desktop (no .NET SDK or SQL Server on the machine). From the repo root:
```
cp .env.example .env        # then open .env and set SA_PASSWORD, API_KEY, GOOGLE_CLIENT_ID/SECRET, ADMIN_EMAILS
docker compose up --build
```
All Docker settings and secrets (SQL `sa` password, API key, Google credentials, admin emails, ports, environment) live in `.env`, which is git-ignored and never committed. `.env.example` is the committed template. If a required value is missing, `docker compose` stops with an error instead of using a default. The Google redirect URI for Docker is the same, `http://localhost:5003/signin-google`.
- UI: http://localhost:5003
- Swagger: http://localhost:5025/swagger

This builds two images, `assetmanagement-api` and `assetmanagement-ui`, and also starts SQL Server 2022 (`mcr.microsoft.com/mssql/server`). The API applies the migrations on startup, so the database is created on first run. Data is kept in the `sqldata` volume; `docker compose down -v` deletes it.

Stop the locally running API/UI first, since Docker uses the same ports (5025, 5003, 1433).

## UI

ASP.NET Core MVC with four pages (one controller + one Razor view each):

- **Assets**: add, edit (including status), delete. The table shows a status badge.
- **Employees**: add, edit (including active), delete. The "Assets" button shows what an employee currently holds.
- **Allocation**: pick an available asset and an active employee. The list can be filtered to Active / Returned / All, and each open row has a "Return" button.
- **Returns**: pick an open allocation and a condition (Good / Damaged). The list shows returned assets.

API errors (e.g. "Asset LAP-0012 is already allocated.") appear in the message bar at the top.

How it works: the browser only talks to the UI. Each controller calls the API with `HttpClient` (helpers in `BaseController`), passes the data to its `.cshtml` view and shows it. Forms post back to the UI controller (with an anti-forgery token), which calls the API and then redirects (Post/Redirect/Get) so refreshing never re-submits.

```
AssetManagement.UI/
├── Controllers/   BaseController (GetAsync / SendAsync helpers), AccountController (sign-in/out) + one controller per page
├── Models/        Asset, Employee, Allocation, AssetReturn - used for both the lists and the forms
├── Views/         Shared/_Layout.cshtml, Shared/_Messages.cshtml, <Controller>/Index.cshtml
└── wwwroot/css/   site.css (static files are still served from wwwroot)
```

The API address is `ApiBaseUrl` in `AssetManagement.UI/appsettings.json`. Because calls are server-to-server, CORS is not involved.

### How the project was created

```
dotnet new webapi -n AssetManagement.Api --use-controllers -f net8.0
cd AssetManagement.Api
dotnet add package Microsoft.EntityFrameworkCore.SqlServer --version 8.0.13
dotnet add package Microsoft.EntityFrameworkCore.Design --version 8.0.13
dotnet ef migrations add InitialCreate
dotnet ef database update
```

## Endpoints

| Module | Method | Route | Notes |
|---|---|---|---|
| Assets | GET | `/api/assets` | |
| | GET | `/api/assets/{id}` | |
| | POST | `/api/assets` | 409 if `AssetTag` exists |
| | PUT | `/api/assets/{id}` | `status` optional; `Allocated` only via allocations |
| | DELETE | `/api/assets/{id}` | 400 if allocated; retired if it has history |
| Employees | GET | `/api/employees` | |
| | GET | `/api/employees/{id}` | |
| | POST | `/api/employees` | 409 if `EmployeeCode` / `Email` exists |
| | PUT | `/api/employees/{id}` | can't set `isActive=false` while holding assets |
| | DELETE | `/api/employees/{id}` | 400 if holding assets; deactivated if it has history |
| | GET | `/api/employees/{id}/assets` | assets currently held (includes `allocationId`) |
| Allocation | POST | `/api/allocations` | asset must be `Available`, employee active |
| | GET | `/api/allocations?active=true` | `true` = open, `false` = returned, omitted = all |
| Returns | POST | `/api/returns` | `returnCondition`: `Good` → Available, `Damaged` → UnderRepair |
| | GET | `/api/returns` | all returned allocations |

Errors come back as `{ "message": "Asset LAP-0012 is already allocated." }` with 400 / 404 / 409.

## Sample request bodies

**POST /api/assets**
```json
{ "assetTag": "LAP-0012", "name": "Dell Latitude 5440", "category": "Laptop", "serialNumber": "DL5440-XYZ123", "purchaseDate": "2026-01-15" }
```

**PUT /api/assets/1**
```json
{ "assetTag": "LAP-0012", "name": "Dell Latitude 5440 (16GB)", "category": "Laptop", "serialNumber": "DL5440-XYZ123", "purchaseDate": "2026-01-15", "status": "Available" }
```

**POST /api/employees**
```json
{ "employeeCode": "EMP-001", "fullName": "Ravi Kumar", "email": "ravi.kumar@company.com", "department": "Engineering" }
```

**PUT /api/employees/1**
```json
{ "employeeCode": "EMP-001", "fullName": "Ravi Kumar", "email": "ravi.kumar@company.com", "department": "Platform Engineering", "isActive": true }
```

**POST /api/allocations**
```json
{ "assetId": 1, "employeeId": 1, "remarks": "Issued for onboarding" }
```

**POST /api/returns**
```json
{ "allocationId": 1, "returnCondition": "Good", "remarks": "Returned with charger" }
```

All of these are also in `AssetManagement.API/AssetManagement.Api.http` (runnable from VS / VS Code REST Client).

## Test flow

1. `POST /api/assets` → 201, `status: "Available"`, `id: 1`
2. `POST /api/employees` → 201, `id: 1`
3. `POST /api/allocations` `{ "assetId": 1, "employeeId": 1 }` → 201, note the allocation `id`
4. `GET /api/assets/1` → `status: "Allocated"`. Allocating again → 400 `Asset LAP-0012 is already allocated.`
5. `GET /api/employees/1/assets` → one item with `allocationId: 1`
6. `POST /api/returns` `{ "allocationId": 1, "returnCondition": "Good" }` → 200, `assetStatus: "Available"`
7. `GET /api/assets/1` → `status: "Available"` again; `GET /api/returns` shows the closed allocation
