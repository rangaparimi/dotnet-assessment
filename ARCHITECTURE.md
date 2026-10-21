# Asset Management System: Architecture and Data Flow

This document explains how the application is built and how data travels through every part of it, from a click in the browser down to a row in SQL Server and back again.

Contents

1. [Big picture](#1-big-picture)
2. [Project structure](#2-project-structure)
3. [Layers and their responsibilities](#3-layers-and-their-responsibilities)
4. [What happens at startup](#4-what-happens-at-startup)
5. [The life of one request, node by node](#5-the-life-of-one-request-node-by-node)
6. [Feature flows](#6-feature-flows)
7. [How the data changes shape](#7-how-the-data-changes-shape)
8. [Error handling paths](#8-error-handling-paths)
9. [Database design](#9-database-design)
10. [Dependency injection and lifetimes](#10-dependency-injection-and-lifetimes)
11. [Configuration, environments and Docker](#11-configuration-environments-and-docker)
12. [Reference: routes and endpoints](#12-reference-routes-and-endpoints)
13. [Business rules](#13-business-rules)
14. [Glossary](#14-glossary)
15. [Authentication and authorization](#15-authentication-and-authorization)
16. [Pagination](#16-pagination)

---

## 1. Big picture

The system is two ASP.NET Core applications and one database:

```
┌──────────────┐   HTML pages    ┌─────────────────────┐   HTTP + JSON   ┌──────────────────────┐   SQL    ┌──────────────┐
│   Browser    │ ◀────────────▶ │  AssetManagement.UI │ ◀────────────▶ │ AssetManagement.Api  │ ◀──────▶ │  SQL Server  │
│ (the user)   │  GET / POST     │  ASP.NET Core MVC   │  GET/POST/PUT/  │ ASP.NET Core Web API │ EF Core  │ AssetManage- │
│              │  form posts     │  Razor .cshtml      │  DELETE         │ + EF Core            │          │ ment DB      │
└──────────────┘                 └─────────────────────┘                 └──────────────────────┘          └──────────────┘
                                  localhost:5003                          localhost:5025
```

- **The browser only talks to the UI.** It never calls the API or the database directly.
- **The UI has no database.** It gets and changes all data by calling the API over HTTP with JSON.
- **The API is the only part that touches the database.** It holds all the business rules.

| Part | Technology | Port (local) | Job |
|---|---|---|---|
| `AssetManagement.UI` | ASP.NET Core MVC, Razor views | 5003 | Renders HTML pages, handles form posts, calls the API |
| `AssetManagement.Api` | ASP.NET Core Web API, EF Core 8 | 5025 | REST endpoints, business rules, data access |
| SQL Server | SQL Server Express (local) / SQL Server 2022 (Docker) | 1433 | Stores assets, employees and allocations |

---

## 2. Project structure

```
dotnet-assignment/
├── AssetManagement.sln
├── docker-compose.yml          runs db + api + ui together
├── .env.example                template for Docker settings and secrets (.env itself is git-ignored)
├── dotnet-tools.json           pins the dotnet-ef CLI tool
│
├── AssetManagement.Api/        BACKEND
│   ├── Program.cs              startup: DI registrations + middleware pipeline
│   ├── appsettings.json        connection string, logging, UiOrigins (CORS)
│   ├── Controllers/            HTTP endpoints (thin)
│   │   ├── AssetsController.cs
│   │   ├── EmployeesController.cs
│   │   ├── AllocationsController.cs
│   │   └── ReturnsController.cs
│   ├── DTOs/                   request/response shapes (the API contract)
│   ├── Services/               business rules
│   │   ├── Interfaces/         IAssetService, IEmployeeService, IAllocationService, IReturnService
│   │   ├── AssetService.cs, EmployeeService.cs, AllocationService.cs, ReturnService.cs
│   │   └── ServiceResult.cs    success / NotFound / BadRequest / Conflict wrapper
│   ├── Repositories/           data access (EF Core queries)
│   │   ├── Interfaces/         IAssetRepository, IEmployeeRepository, IAllocationRepository, IUnitOfWork
│   │   ├── AssetRepository.cs, EmployeeRepository.cs, AllocationRepository.cs
│   │   └── UnitOfWork.cs       the single SaveChangesAsync
│   ├── Data/AppDbContext.cs    EF Core DbContext + table configuration
│   ├── Models/                 entities = database tables
│   │   ├── Asset.cs, Employee.cs, AssetAllocation.cs
│   │   └── Enums/AssetStatus.cs
│   ├── Middleware/GlobalExceptionHandler.cs   unexpected errors -> JSON { message }
│   ├── Migrations/             generated database schema history
│   └── Dockerfile
│
└── AssetManagement.UI/         FRONTEND
    ├── Program.cs              startup: MVC, anti-forgery, HttpClient for the API
    ├── appsettings.json        ApiBaseUrl
    ├── Controllers/
    │   ├── BaseController.cs   GetAsync / SendAsync helpers (all API calls go through here)
    │   ├── AssetsController.cs, EmployeesController.cs, AllocationsController.cs, ReturnsController.cs
    ├── Models/                 Asset, Employee, Allocation, AssetReturn (DTO-shaped, used for lists and forms)
    ├── Views/
    │   ├── _ViewImports.cshtml, _ViewStart.cshtml
    │   ├── Shared/_Layout.cshtml      header + tabs + message bar
    │   ├── Shared/_Messages.cshtml    green/red message bar (TempData)
    │   └── Assets|Employees|Allocations|Returns/Index.cshtml
    ├── wwwroot/css/site.css    the only static file
    └── Dockerfile
```

---

## 3. Layers and their responsibilities

```
UI  (AssetManagement.UI)                         API  (AssetManagement.Api)
┌──────────────────────────────┐                 ┌───────────────────────────────────────────────┐
│ View (.cshtml)               │                 │ Middleware  (CORS, exception handler, ...)     │
│   renders HTML, posts forms  │                 │ Controller  HTTP in/out, status codes         │
│ UI Controller                │   HTTP + JSON   │ Service     business rules, entity <-> DTO     │
│   reads form, calls API,     │ ──────────────▶ │ Repository  LINQ queries, Add/Remove          │
│   picks view or redirect     │                 │ Unit of Work  one SaveChanges = 1 transaction │
│ BaseController               │                 │ AppDbContext  EF Core: LINQ -> SQL            │
│   HttpClient + error text    │                 │ Entities    C# classes mapped to tables       │
│ Models  (DTO-shaped)         │                 └───────────────────────────────────────────────┘
└──────────────────────────────┘                                     │ SQL
                                                                     ▼
                                                               SQL Server
```

| Layer | Knows about | Does NOT know about |
|---|---|---|
| View | its model, ViewBag lists, tag helpers | HTTP calls, API, database |
| UI controller | UI models, BaseController helpers, TempData, views | database, entities, business rules |
| API controller | DTOs, service interfaces, HTTP status codes | EF Core, SQL, entities |
| Service | repository interfaces, entities, DTOs, `ServiceResult` | HTTP, EF Core, SQL |
| Repository | `AppDbContext`, entities, LINQ | business rules, DTOs, HTTP |
| Unit of Work | `AppDbContext` | everything else |
| AppDbContext | entities, table configuration | business rules |

**Rule of thumb:** each layer only talks to the layer directly below it, through an interface (API side) or through HTTP (UI → API).

---

## 4. What happens at startup

### 4.1 API startup ([AssetManagement.Api/Program.cs](AssetManagement.Api/Program.cs))

**Phase 1: register services** (`builder.Services...`). Nothing runs yet; this only tells the dependency-injection container *how* to build objects later.

| Registration | Lifetime | Purpose |
|---|---|---|
| `AddDbContext<AppDbContext>(UseSqlServer(cs))` | Scoped | EF Core context; `cs` = `ConnectionStrings:Default` |
| `IUnitOfWork → UnitOfWork` | Scoped | single `SaveChangesAsync` |
| `IAssetRepository`, `IEmployeeRepository`, `IAllocationRepository` | Scoped | data access |
| `IAssetService`, `IEmployeeService`, `IAllocationService`, `IReturnService` | Scoped | business logic |
| `AddControllers().AddJsonOptions(JsonStringEnumConverter)` | n/a | controllers; enums serialized as text (`"Available"`) |
| `AddEndpointsApiExplorer()` + `AddSwaggerGen()` | n/a | Swagger UI |
| `AddCors("ui")` with `UiOrigins` | n/a | CORS policy (only needed if a browser calls the API directly) |
| `AddExceptionHandler<GlobalExceptionHandler>()` + `AddProblemDetails()` | n/a | global error handling |

**Phase 2: build** (`var app = builder.Build();`). The container is locked and the `WebApplication` is created.

**Phase 3: optional migrations.** If `ApplyMigrationsOnStartup` is `true` (Docker sets it), the API creates a scope, gets `AppDbContext` and runs `Database.Migrate()`. That creates the database and tables if they don't exist.

**Phase 4: middleware pipeline**, in this exact order. Every request passes through it top to bottom:

```
request ─▶ UseCors("ui")          adds CORS headers (first, so error responses get them too)
        ─▶ UseExceptionHandler()  catches exceptions thrown further down -> GlobalExceptionHandler
        ─▶ ApiKeyMiddleware       /api calls without the right X-Api-Key header -> 401 { message }
        ─▶ UseSwagger/UI          Development only: /swagger
        ─▶ UseHttpsRedirection()  redirects to https if an https port is configured
        ─▶ UseAuthorization()     (no auth rules defined yet)
        ─▶ MapControllers()       routing to the controller action
```

**Phase 5: `app.Run()`.** Kestrel (the built-in web server) starts listening on port 5025 (or 8080 inside Docker).

### 4.2 UI startup ([AssetManagement.UI/Program.cs](AssetManagement.UI/Program.cs))

| Registration | Purpose |
|---|---|
| `AddControllersWithViews(...)` | MVC controllers + Razor views |
| `AutoValidateAntiforgeryTokenAttribute` (global filter) | every POST must carry the hidden anti-forgery token |
| `SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true` | UI models are shared by lists and forms; only fields with explicit `[Required]` are required |
| `AddHttpClient("Api", BaseAddress = ApiBaseUrl, header X-Api-Key)` | named `HttpClient` used for all API calls, created through `IHttpClientFactory`; sends the shared API key on every call |
| `AddAuthentication().AddCookie().AddGoogle()` | cookie sign-in; Google (OAuth) proves who the user is; role claim added after sign-in |
| `AddAuthorization(FallbackPolicy = RequireAuthenticatedUser)` | every page needs a signed-in user unless marked `[AllowAnonymous]` |

Pipeline: `UseStaticFiles()` (serves `wwwroot/css/site.css`) → `UseRouting()` → `UseAuthentication()` → `UseAuthorization()` → `MapControllerRoute("{controller=Assets}/{action=Index}/{id?}")`. So `/` opens `AssetsController.Index` for a signed-in user, and the sign-in page for everyone else. See [section 15](#15-authentication-and-authorization).

---

## 5. The life of one request, node by node

This is the full journey for **"Add Asset" → Save**. Every other feature follows the same path; only the controller, service and query differ.

### Step 1: Browser → UI (form post)

The user fills the form in [Views/Assets/Index.cshtml](AssetManagement.UI/Views/Assets/Index.cshtml) and clicks **Save**. The `<form asp-action="Save" method="post">` was rendered with a hidden `__RequestVerificationToken`. The browser sends:

```http
POST http://localhost:5003/Assets/Save
Content-Type: application/x-www-form-urlencoded

Id=0&AssetTag=LAP-001&Name=Dell+XPS&Category=Laptop&SerialNumber=&PurchaseDate=2026-10-01&__RequestVerificationToken=...
```

### Step 2: UI pipeline

1. **Kestrel** receives the request.
2. **Routing** matches `{controller=Assets}/{action=Save}` → `AssetsController.Save`.
3. **Anti-forgery filter** checks the token. If it's missing or wrong, the response is **400** and nothing else runs.
4. **Model binding** builds an `Asset` object (from [UI Models/Asset.cs](AssetManagement.UI/Models/Asset.cs)) out of the form fields.
5. **Validation** checks the attributes (`[Required]`, `[MaxLength]`) and fills `ModelState`.

### Step 3: UI controller ([UI Controllers/AssetsController.cs](AssetManagement.UI/Controllers/AssetsController.cs))

```csharp
if (ModelState.IsValid)
{
    var saved = asset.Id == 0
        ? await SendAsync(HttpMethod.Post, "api/assets", asset)          // new
        : await SendAsync(HttpMethod.Put, $"api/assets/{asset.Id}", asset); // edit
    if (saved) { TempData["Success"] = "..."; return RedirectToAction(nameof(Index)); }
}
ViewBag.Assets = await GetAsync<List<Asset>>("api/assets");
return View(nameof(Index), asset);   // show the form again with the user's input
```

### Step 4: BaseController → API ([BaseController.cs](AssetManagement.UI/Controllers/BaseController.cs))

`SendAsync` creates the request with the named `"Api"` `HttpClient` (base address `http://localhost:5025/`), turns the `Asset` object into JSON with `JsonContent.Create`, and sends it:

```http
POST http://localhost:5025/api/assets
Content-Type: application/json

{"id":0,"assetTag":"LAP-001","name":"Dell XPS","category":"Laptop","serialNumber":null,"purchaseDate":"2026-10-01T00:00:00","status":null}
```

This is a **server-to-server** call (UI server → API server), so CORS is not involved.

### Step 5: API pipeline

1. **Kestrel** (port 5025) receives the request.
2. **CORS middleware** does nothing here (no browser `Origin` header).
3. **Exception handler middleware** wraps everything below it.
4. **Routing** matches `[Route("api/assets")]` + `[HttpPost]` → `AssetsController.Create`.
5. **Dependency injection** builds the object graph for this request (all Scoped):
   `AssetsController` ← `IAssetService` ← `IAssetRepository`, `IAllocationRepository`, `IUnitOfWork` ← one shared `AppDbContext`.
6. **Model binding** turns the JSON into a `CreateAssetDto` (extra fields like `id` and `status` are ignored).
7. **`[ApiController]` validation** checks `[Required]`, `[MaxLength]` on the DTO. If invalid, it **returns 400 automatically** with `{ title, errors: {...} }` and the controller code never runs.

### Step 6: API controller ([API Controllers/AssetsController.cs](AssetManagement.Api/Controllers/AssetsController.cs))

```csharp
var result = await _assetService.CreateAsync(dto);
if (!result.Success)
    return StatusCode(result.StatusCode, new { message = result.Message });
return CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result.Data);
```

The controller only translates between HTTP and the service.

### Step 7: Service ([AssetService.cs](AssetManagement.Api/Services/AssetService.cs))

1. Trims the input.
2. Business rule "tag must be unique": `await _assets.TagExistsAsync(tag)`. If true it returns `ServiceResult.Conflict(...)`, which becomes **409**.
3. Creates the entity: `new Asset { ..., Status = AssetStatus.Available }`.
4. `_assets.Add(asset)` stages the insert.
5. `await _unitOfWork.SaveChangesAsync()` commits.
6. Maps entity → DTO with `ToDto(asset)` and returns `ServiceResult.Ok(dto)`.

### Step 8: Repository and Unit of Work

- [AssetRepository.TagExistsAsync](AssetManagement.Api/Repositories/AssetRepository.cs): `_db.Assets.AnyAsync(a => a.AssetTag == tag && a.Id != excludeId)`
- `AssetRepository.Add`: `_db.Assets.Add(asset)` marks the entity as **Added** in EF Core's change tracker (no SQL yet).
- [UnitOfWork.SaveChangesAsync](AssetManagement.Api/Repositories/UnitOfWork.cs): `_db.SaveChangesAsync()` sends all staged changes in **one transaction**.

### Step 9: EF Core → SQL Server

EF Core translates the LINQ and the tracked changes into parameterized SQL:

```sql
-- TagExistsAsync
SELECT CASE WHEN EXISTS (SELECT 1 FROM [Assets] AS [a] WHERE [a].[AssetTag] = @tag ...) THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END

-- SaveChangesAsync
INSERT INTO [Assets] ([AssetTag], [Category], [Name], [PurchaseDate], [SerialNumber], [Status])
OUTPUT INSERTED.[Id]
VALUES (@p0, @p1, @p2, @p3, @p4, @p5);
```

SQL Server checks the unique index on `AssetTag`, generates the new `Id` (identity column), and EF Core writes that `Id` back into `asset.Id`. The `AssetStatus` enum is saved as the text `'Available'` because of `HasConversion<string>()`.

### Step 10: Back up through the API

```
SQL row ─▶ entity (asset.Id = 7) ─▶ ToDto ─▶ ServiceResult.Ok(AssetDto)
        ─▶ controller: CreatedAtAction ─▶ System.Text.Json ─▶ HTTP 201 Created
```

```http
HTTP/1.1 201 Created
Location: http://localhost:5025/api/assets/7
Content-Type: application/json

{"id":7,"assetTag":"LAP-001","name":"Dell XPS","category":"Laptop","serialNumber":null,"purchaseDate":"2026-10-01T00:00:00","status":"Available"}
```

### Step 11: Back in the UI

1. `SendAsync` sees a success status. The body has no `message` field, so it returns `true`.
2. The controller sets `TempData["Success"] = "Asset LAP-001 saved."`. TempData is stored in a cookie and survives one redirect.
3. `RedirectToAction(nameof(Index))` returns **302 → /Assets**. This is the **Post/Redirect/Get** pattern: refreshing the page won't submit the form again.

### Step 12: The browser follows the redirect

```
GET /Assets ─▶ AssetsController.Index ─▶ GetAsync<List<Asset>>("api/assets")
            ─▶ API GET /api/assets ─▶ AssetService.GetAllAsync ─▶ AssetRepository.GetAllAsync
            ─▶ SELECT ... FROM [Assets] ORDER BY [AssetTag] ─▶ entities ─▶ DTOs ─▶ JSON
            ─▶ UI deserializes into List<Asset> ─▶ ViewBag.Assets
            ─▶ View(new Asset()) renders Views/Assets/Index.cshtml inside _Layout.cshtml
            ─▶ _Messages.cshtml shows TempData["Success"] in the green bar
            ─▶ HTML sent to the browser
```

### The whole round trip on one screen

```
[Browser] Save
   │  POST /Assets/Save (form fields + anti-forgery token)
   ▼
[UI] Kestrel → Routing → Anti-forgery → Model binding (Asset) → Validation
   ▼
[UI] AssetsController.Save → BaseController.SendAsync
   │  POST /api/assets (JSON)                                    HttpClient from IHttpClientFactory
   ▼
[API] Kestrel → CORS → ExceptionHandler → Routing → DI → Model binding (CreateAssetDto) → [ApiController] validation
   ▼
[API] AssetsController.Create
   ▼
[API] AssetService.CreateAsync          business rule: unique tag
   ▼
[API] AssetRepository (AnyAsync, Add) → UnitOfWork.SaveChangesAsync
   ▼
[EF Core] LINQ / change tracker → SQL (parameterized, one transaction)
   ▼
[SQL Server] INSERT, unique index check, identity Id
   ▲
[API] entity → AssetDto → ServiceResult → 201 Created JSON
   ▲
[UI] TempData["Success"] → 302 Redirect /Assets
   ▲
[Browser] GET /Assets → fresh page with the new row and a green message
```

---

## 6. Feature flows

Notation: `UI:` = UI controller action, `API:` = API endpoint, `SVC:` = service method, `REPO:` = repository method, `SQL:` = what EF Core runs.

### 6.1 Assets

**List assets (page load)**
```
Browser GET /Assets
UI:   AssetsController.Index(editId = null)
      GetAsync<List<Asset>>("api/assets")
API:  GET /api/assets → AssetsController.GetAll
SVC:  AssetService.GetAllAsync → assets.Select(ToDto)
REPO: AssetRepository.GetAllAsync → _db.Assets.AsNoTracking().OrderBy(a => a.AssetTag).ToListAsync()
SQL:  SELECT [Id],[AssetTag],[Category],[Name],[PurchaseDate],[SerialNumber],[Status] FROM [Assets] ORDER BY [AssetTag]
UI:   ViewBag.Assets = list; View(new Asset()) → table + empty "Add Asset" form
```

**Open the edit form**
```
Browser GET /Assets?editId=5   (the "Edit" link in a table row)
UI:   Index loads the list (same calls as above), then
      form = assets.FirstOrDefault(a => a.Id == 5)   → the form is pre-filled, Status dropdown shown
```
No extra API call: the asset is taken from the list that was already loaded.

**Save an edit**
```
Browser POST /Assets/Save  (Id=5, ..., Status=UnderRepair)
UI:   SendAsync(PUT, "api/assets/5", asset)
API:  PUT /api/assets/5 → AssetsController.Update(id, UpdateAssetDto)
SVC:  AssetService.UpdateAsync
        REPO GetByIdAsync(5)                    → 404 if missing    (tracked entity)
        REPO TagExistsAsync(tag, excludeId: 5)  → 409 if taken by another asset
        rule: Status cannot be set to Allocated here           → 400
        rule: an Allocated asset's status cannot be changed    → 400
        copy fields onto the tracked entity
        UnitOfWork.SaveChangesAsync
SQL:  UPDATE [Assets] SET [Name] = @p0, [Status] = @p1, ... WHERE [Id] = @p2   (only changed columns)
UI:   TempData["Success"], redirect to /Assets
```

**Delete**
```
Browser POST /Assets/Delete/5   (after the JavaScript confirm() dialog)
UI:   SendAsync(DELETE, "api/assets/5")
API:  DELETE /api/assets/5 → AssetsController.Delete
SVC:  AssetService.DeleteAsync
        REPO GetByIdAsync(5)                          → 404 if missing
        rule: currently Allocated                     → 400 "cannot be deleted"
        REPO AllocationRepository.AnyForAssetAsync(5)
          ├─ has history → Status = Retired, save     → 200 { message: "...marked as Retired instead of deleted." }
          └─ no history  → AssetRepository.Remove, save → 200 { message: "Asset X was deleted." }
UI:   SendAsync reads { message } and puts it in TempData["Success"]; redirect to /Assets
```

### 6.2 Employees

**List** — same pattern as assets: `GET /api/employees` → `EmployeeService.GetAllAsync` → `_db.Employees.AsNoTracking().OrderBy(e => e.EmployeeCode)`.

**Create / update**
```
Browser POST /Employees/Save
UI:   Id == 0 ? POST api/employees : PUT api/employees/{id}
SVC:  EmployeeService.CreateAsync / UpdateAsync
        FindDuplicateAsync → REPO CodeExistsAsync, EmailExistsAsync  → 409 if taken
        (update) rule: an employee who still holds assets cannot be deactivated
              REPO AllocationRepository.EmployeeHoldsAssetsAsync      → 400
        Add (create, IsActive = true) or copy fields (update)
        UnitOfWork.SaveChangesAsync
SQL:  INSERT INTO [Employees] ... / UPDATE [Employees] SET ...
```

**Delete**
```
SVC:  EmployeeService.DeleteAsync
        GetByIdAsync                                  → 404
        EmployeeHoldsAssetsAsync                      → 400 "still holds assets"
        AnyForEmployeeAsync
          ├─ has history → IsActive = false (deactivated), save
          └─ no history  → Remove, save
```

**"Assets held by" an employee**
```
Browser GET /Employees?heldById=3
UI:   loads employees, then GetAsync<List<Allocation>>("api/allocations?active=true")
      and keeps the rows where EmployeeId == 3   → ViewBag.HeldAssets
```
The API also has `GET /api/employees/{id}/assets` (`EmployeeService.GetCurrentAssetsAsync` → `AllocationRepository.GetOpenByEmployeeWithAssetAsync`). The UI doesn't use it; it reuses the allocation list instead so it needs no extra model.

### 6.3 Allocation

**Page load**
```
Browser GET /Allocations?show=active|returned|all
UI:   AllocationsController.Index → LoadPageDataAsync(show)
        GetAsync<List<Asset>>("api/assets")         → keep Status == "Available"  → asset dropdown
        GetAsync<List<Employee>>("api/employees")   → keep IsActive              → employee dropdown
        GetAsync<List<Allocation>>(url)             → table
          show=active   → api/allocations?active=true
          show=returned → api/allocations?active=false
          show=all      → api/allocations
API:  GET /api/allocations?active=... → AllocationService.GetAllAsync(active)
REPO: AllocationRepository.GetAllWithDetailsAsync(active)
        _db.AssetAllocations.AsNoTracking().Include(Asset).Include(Employee)
           .Where(ReturnedDate == null | != null).OrderByDescending(AllocatedDate)
SQL:  SELECT ... FROM [AssetAllocations] a
      INNER JOIN [Assets] ... INNER JOIN [Employees] ...
      WHERE a.[ReturnedDate] IS NULL ORDER BY a.[AllocatedDate] DESC
```
The filter dropdown is a small GET form: changing it reloads the page with `?show=...`.

**Allocate an asset**
```
Browser POST /Allocations/Create  (AssetId=3, EmployeeId=5, Remarks=...)
UI:   validation ("Choose an asset." / "Choose an employee.")
      SendAsync(POST, "api/allocations", allocation)
API:  POST /api/allocations → AllocationsController.Allocate(CreateAllocationDto)
SVC:  AllocationService.AllocateAsync
        REPO AssetRepository.GetByIdAsync          → 404 if missing
        rule: asset already Allocated               → 400
        rule: asset not Available (UnderRepair/Retired) → 400
        REPO EmployeeRepository.GetByIdAsync       → 404 if missing
        rule: employee not active                   → 400
        new AssetAllocation { AssetId, EmployeeId, AllocatedDate = DateTime.UtcNow, Remarks }
        REPO AllocationRepository.Add(allocation)
        asset.Status = Allocated                    (tracked entity, just a property change)
        UnitOfWork.SaveChangesAsync                 ← ONE transaction for both changes
SQL:  BEGIN TRAN
        INSERT INTO [AssetAllocations] (...) VALUES (...)
        UPDATE [Assets] SET [Status] = 'Allocated' WHERE [Id] = 3
      COMMIT
DB:   unique filtered index IX_AssetAllocations_AssetId_Open (AssetId WHERE ReturnedDate IS NULL)
      blocks a second open allocation of the same asset, even if two requests race
API:  201 Created + AllocationDto
UI:   TempData["Success"] = "Asset allocated."; redirect /Allocations
```

### 6.4 Returns

**Page load**
```
Browser GET /Returns   or   /Returns?allocationId=7   (the "Return" button on the Allocation page)
UI:   ReturnsController.Index → LoadPageDataAsync
        GetAsync<List<Allocation>>("api/allocations?active=true")  → dropdown of open allocations
        GetAsync<List<AssetReturn>>("api/returns")                → returned-assets table
      View(new AssetReturn { AllocationId = 7 })  → allocation 7 is pre-selected
API:  GET /api/returns → ReturnService.GetAllAsync
REPO: AllocationRepository.GetReturnedWithDetailsAsync
        .Include(Asset).Include(Employee).Where(ReturnedDate != null).OrderByDescending(ReturnedDate)
```

**Record a return**
```
Browser POST /Returns/Create  (AllocationId=7, ReturnCondition=Damaged, Remarks=...)
UI:   SendAsync(POST, "api/returns", assetReturn)
API:  POST /api/returns → ReturnsController.Return(CreateReturnDto)
        DTO validation: ReturnCondition must match ^(Good|Damaged)$
SVC:  ReturnService.ReturnAsync
        REPO GetByIdWithDetailsAsync(7)   (tracked, Asset + Employee loaded)  → 404 if missing
        rule: already returned                                              → 400
        allocation.ReturnedDate    = DateTime.UtcNow
        allocation.ReturnCondition = "Damaged"
        allocation.Remarks         = MergeRemarks(old, new)   → "old | Return: new"
        allocation.Asset.Status    = Damaged ? UnderRepair : Available
        UnitOfWork.SaveChangesAsync   ← one transaction: allocation row + asset row
SQL:  UPDATE [AssetAllocations] SET [ReturnedDate]=..., [ReturnCondition]=..., [Remarks]=... WHERE [Id]=7
      UPDATE [Assets] SET [Status] = 'UnderRepair' WHERE [Id] = ...
API:  200 OK + ReturnDto (includes the asset's new status)
UI:   TempData["Success"] = "Return recorded."; redirect /Returns
```

A return does not create a new row. It **closes** the open allocation row by setting `ReturnedDate`, so the full history stays in one table.

---

## 7. How the data changes shape

One asset, followed through every representation:

| Where | Type | Example |
|---|---|---|
| Browser form | form fields (text) | `AssetTag=LAP-001&PurchaseDate=2026-10-01` |
| UI model binding | `AssetManagement.UI.Models.Asset` | `Id = 0, AssetTag = "LAP-001", Status = null` |
| UI → API | JSON text | `{"assetTag":"LAP-001","purchaseDate":"2026-10-01T00:00:00",...}` |
| API model binding | `CreateAssetDto` | validated with `[Required]`, `[MaxLength]` |
| Service | `Asset` **entity** | trimmed values, `Status = AssetStatus.Available` |
| EF Core | tracked entity, state `Added` | change tracker |
| Database | row in `Assets` | `Status` stored as `'Available'` (nvarchar) |
| Service (return trip) | `AssetDto` | via `ToDto(asset)` |
| API → UI | JSON | `"status":"Available"` (enum as text via `JsonStringEnumConverter`) |
| UI | `List<Asset>` | `ReadFromJsonAsync<List<Asset>>()` |
| View | HTML | `<span class="badge Available">Available</span>` |

### Three kinds of "model"

| Kind | Where | Purpose | Touches the database? |
|---|---|---|---|
| **Entity** | `Api/Models` (`Asset`, `Employee`, `AssetAllocation`) | a table row, with relationships (`Allocations`) | yes, through EF Core |
| **DTO** | `Api/DTOs` (`AssetDto`, `CreateAssetDto`, ...) | the API contract: what goes over HTTP | no |
| **UI model** | `UI/Models` (`Asset`, `Employee`, `Allocation`, `AssetReturn`) | DTO-shaped copy used for lists **and** forms | no |

Entities never leave the API. The service layer converts them to DTOs (`ToDto`) before anything is returned. The UI only ever sees JSON shaped like the DTOs.

### Mapping points

| From → To | Where | How |
|---|---|---|
| Form fields → UI model | UI MVC model binding | automatic, by field name |
| UI model → JSON | `BaseController.SendAsync` | `JsonContent.Create(body)` |
| JSON → DTO | API model binding | System.Text.Json |
| DTO → entity | service (`CreateAsync`, `UpdateAsync`) | manual `new Asset { ... }` / property copy |
| Entity ↔ table row | EF Core | conventions + Fluent API in `AppDbContext.OnModelCreating` |
| Entity → DTO | service | `private static ToDto(...)` |
| DTO → JSON | API controller | System.Text.Json + `JsonStringEnumConverter` |
| JSON → UI model | `BaseController.GetAsync` | `ReadFromJsonAsync<T>()` |
| UI model → HTML | Razor view | `@a.AssetTag`, tag helpers (`asp-for`) |

---

## 8. Error handling paths

Every failure ends up as a message in the red bar ([_Messages.cshtml](AssetManagement.UI/Views/Shared/_Messages.cshtml)) or under a form field.

| # | What goes wrong | Caught by | HTTP | What the user sees |
|---|---|---|---|---|
| 1 | Missing anti-forgery token | UI global filter | 400 | a bare "400 Bad Request" (a normal form post always includes the token) |
| 2 | Form field invalid (empty name, bad email, nothing chosen) | UI validation (`ModelState`) | none, API is not called | message under the field; typed values kept |
| 3 | DTO invalid at the API | `[ApiController]` automatic validation | 400 `{ errors }` | red bar with the validation messages |
| 4 | Business rule broken (not found, duplicate, asset already allocated, employee inactive, ...) | service returns `ServiceResult.NotFound/BadRequest/Conflict` | 404 / 400 / 409 `{ message }` | red bar with the API's message; typed values kept |
| 5 | Two requests race past a check (duplicate tag, same asset allocated twice) | database unique index → `SqlException 2601/2627` → `GlobalExceptionHandler` | 409 | "This change conflicts with existing data..." |
| 6 | Foreign key violation | `SqlException 547` → `GlobalExceptionHandler` | 400 | "This change references a record that does not exist or is still in use." |
| 7 | Database unreachable / other SQL error | `GlobalExceptionHandler` | 503 | "The database is not available..." |
| 8 | Concurrency conflict | `DbUpdateConcurrencyException` → `GlobalExceptionHandler` | 409 | "The record was changed by another user..." |
| 9 | Any other bug | `GlobalExceptionHandler` (logged as Error) | 500 | "An unexpected error occurred..." |
| 10 | API not running | `BaseController` catches `HttpRequestException` | none | "Cannot reach the API. Is AssetManagement.Api running?" |

How the message reaches the screen:

```
API: { "message": "Asset LAP-001 is already allocated." }   (or { "errors": {...} })
 └▶ BaseController.ReadMessageAsync extracts the text
     └▶ TempData["Error"] = text
         └▶ controller shows the view again (or redirects)
             └▶ _Layout.cshtml → _Messages.cshtml renders <div class="message error">
```

**Expected** failures (rules 2–4) are normal return values (`ServiceResult`), not exceptions. **Unexpected** failures (5–9) are exceptions turned into JSON in one place (`GlobalExceptionHandler`), so the UI always gets `{ "message": "..." }`.

---

## 9. Database design

Created by EF Core migrations from the entities and [AppDbContext.cs](AssetManagement.Api/Data/AppDbContext.cs) (code-first).

```
┌──────────────────────────┐          ┌───────────────────────────────┐          ┌──────────────────────────┐
│ Assets                   │          │ AssetAllocations              │          │ Employees                │
├──────────────────────────┤          ├───────────────────────────────┤          ├──────────────────────────┤
│ Id            int PK id  │ 1      * │ Id              int PK id     │ *      1 │ Id           int PK id   │
│ AssetTag      nvarchar50 │◀─────────│ AssetId         int FK        │─────────▶│ EmployeeCode nvarchar20  │
│   UNIQUE                 │          │ EmployeeId      int FK        │          │   UNIQUE                 │
│ Name          nvarchar100│          │ AllocatedDate   datetime2     │          │ FullName     nvarchar100 │
│ Category      nvarchar50 │          │ ReturnedDate    datetime2 NULL│          │ Email        nvarchar100 │
│ SerialNumber  nvarchar100│          │ ReturnCondition nvarchar20 NULL          │   UNIQUE                 │
│   NULL                   │          │ Remarks         nvarchar500 NULL         │ Department   nvarchar50  │
│ PurchaseDate  datetime2  │          └───────────────────────────────┘          │ IsActive     bit         │
│ Status        nvarchar20 │                                                     └──────────────────────────┘
└──────────────────────────┘
```

| Item | Details |
|---|---|
| `Assets.AssetTag` | unique index `IX_Assets_AssetTag` |
| `Employees.EmployeeCode`, `Employees.Email` | unique indexes |
| `Assets.Status` | enum `AssetStatus` stored as text (`HasConversion<string>()`): Available, Allocated, UnderRepair, Retired |
| `AssetAllocations.AssetId` | index `IX_AssetAllocations_AssetId` (history lookups) **and** unique filtered index `IX_AssetAllocations_AssetId_Open` `WHERE [ReturnedDate] IS NULL` (at most one open allocation per asset) |
| Foreign keys | `AssetId → Assets.Id`, `EmployeeId → Employees.Id`, both `ON DELETE NO ACTION` (Restrict): deleting an asset or employee can never wipe allocation history |
| Open vs returned | `ReturnedDate IS NULL` = still with the employee; set = returned |

Migrations in [Migrations/](AssetManagement.Api/Migrations/):

1. `20261006090819_InitialCreate`: the three tables, keys, foreign keys and indexes.
2. `20261006092405_AddOpenAllocationIndex`: the filtered unique index.

Applied with `dotnet ef database update` (local) or automatically at startup when `ApplyMigrationsOnStartup=true` (Docker). Applied migrations are recorded in the `__EFMigrationsHistory` table.

---

## 10. Dependency injection and lifetimes

### API object graph for one request

```
AssetsController
  └─ IAssetService → AssetService
       ├─ IAssetRepository      → AssetRepository      ─┐
       ├─ IAllocationRepository → AllocationRepository ─┼─ all receive the SAME AppDbContext
       └─ IUnitOfWork           → UnitOfWork           ─┘   (Scoped = one per HTTP request)
```

Everything is **Scoped**. That matters because:

- `AppDbContext` is not thread-safe and should live for one unit of work, which here is one request.
- Repositories and the Unit of Work must share **the same** context. That's how "add allocation + change asset status" are saved together in one `SaveChangesAsync`, as one transaction.
- At the end of the request the container disposes the context and returns its database connection to the pool.

### UI

- `IHttpClientFactory` (registered by `AddHttpClient("Api", ...)`) is injected into every UI controller through `BaseController`. It hands out `HttpClient`s with the API base address already set, and manages the underlying connections (no socket exhaustion, DNS refreshes).
- Controllers are created per request.

---

## 11. Configuration, environments and Docker

### Settings

| Setting | Project | Local value | Docker value | Source |
|---|---|---|---|---|
| `ConnectionStrings:Default` | API | `Server=localhost\SQLEXPRESS;...Trusted_Connection=True` (Windows login, no password) | `Server=db,1433;User Id=sa;Password=${SA_PASSWORD}...` | `appsettings.json` / `docker-compose.yml` + `.env` |
| `ApplyMigrationsOnStartup` | API | not set (false) | `true` | `.env` → compose |
| `ASPNETCORE_ENVIRONMENT` | API | `Development` (launchSettings) | from `.env` | Development enables Swagger |
| `UiOrigins` | API | `["http://localhost:5003"]` | same | CORS policy (unused by the MVC UI, which calls the API server-side) |
| `ApiBaseUrl` | UI | `http://localhost:5025` | `http://api:8080` | `appsettings.json` / compose |

Configuration is layered: `appsettings.json` → `appsettings.{Environment}.json` → environment variables (Docker). In environment variables, `__` means a nested key (`ConnectionStrings__Default`).

### Secrets

- **Local:** no secrets (Windows authentication).
- **Docker:** the SQL `sa` password lives only in `.env` (git-ignored, also excluded from the Docker build context). `.env.example` is the committed template. `docker-compose.yml` refuses to start if `SA_PASSWORD` is missing.

### Docker runtime

```
                       docker compose network
host:5003 ─▶ ┌──────────┐  http://api:8080  ┌──────────┐   db:1433   ┌──────────────────────┐
             │ ui :8080 │ ────────────────▶ │ api :8080│ ──────────▶ │ db  SQL Server 2022  │
host:5025 ─▶ └──────────┘                   └──────────┘             │ volume: sqldata      │
host:1433 ─────────────────────────────────────────────────────────▶ └──────────────────────┘
```

Start order: `db` starts → its healthcheck (`sqlcmd SELECT 1`) passes → `api` starts and runs the migrations → `ui` starts. Inside the network the containers reach each other by service name (`api`, `db`). `localhost` inside a container means that container itself.

Both Dockerfiles are two-stage builds: the .NET SDK image compiles and publishes, then only the published output is copied into the small ASP.NET runtime image, which runs as the non-root `app` user.

---

## 12. Reference: routes and endpoints

### UI routes (what the browser calls)

| Method | URL | Controller action | Calls the API |
|---|---|---|---|
| GET | `/` or `/Assets` | `AssetsController.Index` | GET `api/assets` |
| GET | `/Assets?editId={id}` | `AssetsController.Index` | GET `api/assets` |
| POST | `/Assets/Save` | `AssetsController.Save` | POST `api/assets` or PUT `api/assets/{id}` |
| POST | `/Assets/Delete/{id}` | `AssetsController.Delete` | DELETE `api/assets/{id}` |
| GET | `/Employees` | `EmployeesController.Index` | GET `api/employees` |
| GET | `/Employees?editId={id}` | `EmployeesController.Index` | GET `api/employees` |
| GET | `/Employees?heldById={id}` | `EmployeesController.Index` | GET `api/employees` + GET `api/allocations?active=true` |
| POST | `/Employees/Save` | `EmployeesController.Save` | POST `api/employees` or PUT `api/employees/{id}` |
| POST | `/Employees/Delete/{id}` | `EmployeesController.Delete` | DELETE `api/employees/{id}` |
| GET | `/Allocations?show=active\|returned\|all` | `AllocationsController.Index` | GET `api/assets`, `api/employees`, `api/allocations?...` |
| POST | `/Allocations/Create` | `AllocationsController.Create` | POST `api/allocations` |
| GET | `/Returns?allocationId={id}` | `ReturnsController.Index` | GET `api/allocations?active=true`, `api/returns` |
| POST | `/Returns/Create` | `ReturnsController.Create` | POST `api/returns` |

### API endpoints

| Method | URL | Body | Success | Errors |
|---|---|---|---|---|
| GET | `/api/assets` | | 200 `AssetDto[]` | |
| GET | `/api/assets/paged?page&pageSize` | | 200 `PagedResult<AssetDto>` | |
| GET | `/api/assets/{id}` | | 200 `AssetDto` | 404 |
| POST | `/api/assets` | `CreateAssetDto` | 201 `AssetDto` | 400, 409 |
| PUT | `/api/assets/{id}` | `UpdateAssetDto` | 200 `AssetDto` | 400, 404, 409 |
| DELETE | `/api/assets/{id}` | | 200 `{ message }` | 400, 404 |
| GET | `/api/employees` | | 200 `EmployeeDto[]` | |
| GET | `/api/employees/paged?page&pageSize` | | 200 `PagedResult<EmployeeDto>` | |
| GET | `/api/employees/{id}` | | 200 `EmployeeDto` | 404 |
| POST | `/api/employees` | `CreateEmployeeDto` | 201 `EmployeeDto` | 400, 409 |
| PUT | `/api/employees/{id}` | `UpdateEmployeeDto` | 200 `EmployeeDto` | 400, 404, 409 |
| DELETE | `/api/employees/{id}` | | 200 `{ message }` | 400, 404 |
| GET | `/api/employees/{id}/assets` | | 200 `HeldAssetDto[]` | 404 |
| GET | `/api/allocations?active=true\|false` | | 200 `AllocationDto[]` | |
| GET | `/api/allocations/paged?active&page&pageSize` | | 200 `PagedResult<AllocationDto>` | |
| POST | `/api/allocations` | `CreateAllocationDto` | 201 `AllocationDto` | 400, 404 |
| GET | `/api/returns` | | 200 `ReturnDto[]` | |
| GET | `/api/returns/paged?page&pageSize` | | 200 `PagedResult<ReturnDto>` | |
| POST | `/api/returns` | `CreateReturnDto` | 200 `ReturnDto` | 400, 404 |

Swagger (Development): http://localhost:5025/swagger. Sample requests: [AssetManagement.Api.http](AssetManagement.Api/AssetManagement.Api.http).

---

## 13. Business rules

All of these live in the API services; the UI only mirrors a few of them in its dropdowns for convenience.

**Assets** ([AssetService.cs](AssetManagement.Api/Services/AssetService.cs))
- Asset tag must be unique.
- New assets start as `Available`.
- Status can't be set to `Allocated` by editing; only the allocation flow does that.
- An `Allocated` asset's status can't be changed until it is returned.
- An `Allocated` asset can't be deleted.
- An asset with allocation history is **retired** instead of deleted.

**Employees** ([EmployeeService.cs](AssetManagement.Api/Services/EmployeeService.cs))
- Employee code and email must each be unique.
- New employees are active.
- An employee who still holds assets can't be deactivated or deleted.
- An employee with allocation history is **deactivated** instead of deleted.

**Allocation** ([AllocationService.cs](AssetManagement.Api/Services/AllocationService.cs))
- The asset must exist and be `Available`.
- The employee must exist and be active.
- Allocating creates an allocation row **and** sets the asset to `Allocated`, in one transaction.
- The database allows only one open allocation per asset (filtered unique index).

**Returns** ([ReturnService.cs](AssetManagement.Api/Services/ReturnService.cs))
- The allocation must exist and still be open.
- Condition is `Good` or `Damaged`.
- `Good` → asset becomes `Available`; `Damaged` → asset becomes `UnderRepair`.
- Return remarks are appended to the allocation remarks (`old | Return: new`).
- Allocation update + asset status change are saved in one transaction.

---

## 14. Glossary

| Term | Meaning in this project |
|---|---|
| **MVC** | Model-View-Controller. The UI's pattern: controllers prepare data, views render HTML |
| **Razor view (.cshtml)** | HTML with C# (`@model`, `@foreach`, tag helpers like `asp-for`) rendered on the server |
| **Tag helper** | `asp-for`, `asp-action`, ... attributes that generate the right HTML names, URLs and validation markup |
| **ViewBag / TempData** | ViewBag passes extra data (lists) to a view in the same request; TempData survives one redirect (used for messages) |
| **Post/Redirect/Get** | after a successful POST, redirect to a GET so refreshing doesn't resubmit |
| **Anti-forgery token** | hidden form field that proves a POST came from our own page (CSRF protection) |
| **IHttpClientFactory** | creates configured `HttpClient`s and manages their connections |
| **Middleware pipeline** | the ordered chain (`app.Use...`) every request passes through |
| **Model binding / validation** | turning form fields or JSON into C# objects, then checking their attributes |
| **DTO** | Data Transfer Object: the shape of data sent over HTTP |
| **Entity** | a C# class mapped to a database table by EF Core |
| **Service / ServiceResult** | business logic layer; returns success or an expected failure with a status code and message |
| **Repository** | class that holds the database queries for one area |
| **Unit of Work** | commits everything changed in a request with one `SaveChangesAsync` (one transaction) |
| **DbContext** | EF Core's session with the database: tracks entities, translates LINQ to SQL |
| **Change tracking** | EF Core remembers loaded entities and generates UPDATEs only for changed columns |
| **AsNoTracking** | read-only query; faster because EF doesn't track the results |
| **Include** | loads related entities with a SQL JOIN |
| **Migration** | generated C# that creates or changes the database schema |
| **Dependency injection (Scoped)** | the container builds and disposes objects; Scoped = one instance per HTTP request |
| **CORS** | browser rule for cross-origin calls; configured on the API, not needed by the server-side MVC UI |
| **Docker Compose** | starts db, api and ui together with settings from `.env` |

---

## 15. Authentication and authorization

Two separate protections, each guarding a different door:

```
 user ──(1) Google sign-in + role──▶ UI ──(2) X-Api-Key header──▶ API ──▶ database
```

| | Protects | Mechanism | Where |
|---|---|---|---|
| **(1) User sign-in and roles** | the UI pages | Google OAuth sign-in, then a cookie; role = Admin or Viewer | UI |
| **(2) API key** | the API | shared secret in the `X-Api-Key` header | UI sends it, API checks it |

### 15.1 Sign-in with Google (authentication)

```
Browser GET /Assets            not signed in
UI  fallback policy ──▶ 302 /Account/Login?ReturnUrl=/Assets
Browser POST /Account/GoogleLogin       ("Sign in with Google" button, anti-forgery token included)
UI  Challenge(Google) ──▶ 302 https://accounts.google.com/o/oauth2/v2/auth?client_id=...&redirect_uri=http://localhost:5003/signin-google&scope=openid profile email
Google  user signs in and agrees ──▶ 302 /signin-google?code=...
UI  Google handler (built into ASP.NET Core, route /signin-google)
      exchanges the code for the user's profile (server to server, using ClientSecret)
      OnCreatingTicket: email in Auth:AdminEmails ? role Admin : role Viewer
      writes the sign-in cookie (name, email, role)
    ──▶ 302 back to /Assets
```

- The app never sees or stores a Google password. Google only tells it who the user is.
- The user stays signed in through the cookie until they click **Sign out** (`POST /Account/Logout`).
- Roles are not stored anywhere. They are worked out at sign-in from `Auth:AdminEmails` and kept inside the cookie.
- In Docker the cookie-encryption keys live inside the container, so a container restart signs everyone out.

### 15.2 Roles (authorization)

| Where | Rule |
|---|---|
| global fallback policy | every page requires a signed-in user, unless marked `[AllowAnonymous]` (only `Account/Login`, `GoogleLogin` and `AccessDenied`) |
| every POST action (`Save`, `Delete`, `Create`) | `[Authorize(Roles = "Admin")]`, so a Viewer's request is refused (redirect to `/Account/AccessDenied`) |
| views | `User.IsInRole("Admin")` hides the forms and the Edit, Delete and Return buttons from Viewers |

The views only hide things for convenience. **The real enforcement is the attribute on the actions**, so a Viewer can't change data even by sending the request by hand.

### 15.3 API key (protecting the API)

- The API's `ApiKeyMiddleware` requires the `X-Api-Key` header on every path under `/api`. A missing or wrong key gets `401 { "message": "Missing or invalid API key." }`. The key is compared in fixed time.
- The UI adds the header to every call through the named `HttpClient`, so users never see it.
- Swagger's pages are not under `/api`, so they stay reachable. Its **Authorize** button sends the key for "Try it out".
- The API does not know which user is calling. Per-user roles are enforced in the UI, and the API trusts the UI because it holds the key.
- Both apps refuse to start if their key setting is missing.

### 15.4 Settings

| Setting | Where it comes from |
|---|---|
| `Authentication:Google:ClientId`, `Authentication:Google:ClientSecret` | user-secrets (local) / `.env` → `GOOGLE_CLIENT_ID`, `GOOGLE_CLIENT_SECRET` (Docker) |
| `Auth:AdminEmails` (comma separated) | user-secrets (local) / `.env` → `ADMIN_EMAILS` (Docker) |
| `ApiKey` (UI and API, same value) | `appsettings.Development.json` (dev-only value) / `.env` → `API_KEY` (Docker) |

### 15.5 Limits to be aware of

- If the API's port is reachable and someone obtains the key, they can do anything through the API. Keep the key secret and the API port private.
- Role changes (editing `ADMIN_EMAILS`) apply the next time the user signs in.
- Any Google account can sign in as a Viewer. To restrict who may sign in at all, add an allow-list check in `OnCreatingTicket`.

---

## 16. Pagination

Long lists are shown 10 rows at a time. The database does the paging, so only one page of rows is read and sent.

```
Browser GET /Assets?page=3
UI   AssetsController.Index(page = 3) → BaseController.GetPageAsync<Asset>("api/assets/paged", 3)
       GET /api/assets/paged?page=3&pageSize=10         (X-Api-Key header added)
API  AssetsController.GetPaged → AssetService.GetPagedAsync → AssetRepository.GetPagedAsync
       _db.Assets.AsNoTracking().OrderBy(a => a.AssetTag).ToPagedResultAsync(3, 10)
SQL  SELECT COUNT(*) FROM [Assets]
     SELECT ... FROM [Assets] ORDER BY [AssetTag] OFFSET 20 ROWS FETCH NEXT 10 ROWS ONLY
API  PagedResult<Asset>.Map(ToDto) → PagedResult<AssetDto>
       { "items": [10 rows], "page": 3, "pageSize": 10, "totalCount": 208, "totalPages": 21 }
UI   ViewBag.Assets = items, ViewBag.Paged = the result → table + Views/Shared/_Pager.cshtml
```

| Piece | Where | Job |
|---|---|---|
| `PagedResult<T>` | `Api/Common/PagedResult.cs` | the reply: items plus page, pageSize, totalCount, totalPages; `Map` converts entities to DTOs |
| `ToPagedResultAsync` | `Api/Repositories/PagingExtensions.cs` | counts, clamps `pageSize` to 1-100 and `page` to the valid range, then `Skip/Take` |
| `/paged` endpoints | the four API controllers | `?page=1&pageSize=10`; `allocations/paged` also takes `active` |
| `PagedResult<T>`, `IPaged` | `UI/Models/PagedResult.cs` | the same shape in the UI; `IPaged` lets one pager partial serve every list |
| `GetPageAsync` | `UI/Controllers/BaseController.cs` | adds `page` and `pageSize` (10) to the URL |
| `_Pager.cshtml` | `UI/Views/Shared` | First, Previous, "Page x of y", Next, Last; hidden when there is one page |

Rules and details:

- **Stable order.** Every paged query is ordered, and ties are broken by `Id` (allocations, returns). Without that, rows with the same date could appear on two pages or none.
- **The full-list endpoints stay.** The Allocation and Returns dropdowns need every asset, employee and open allocation, so they still call the unpaged endpoints.
- **The page is remembered.** The pager keeps other query values (like `show=all`) and drops `editId`. The Edit, Cancel, Save and Delete actions carry `page` and redirect back to it. A page past the end is moved back to the last page, so deleting the last row on the last page lands on the new last page.
- **Not paged:** the "Assets held by" table on the Employees page, which only lists one employee's open allocations.
