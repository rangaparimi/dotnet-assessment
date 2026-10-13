# Prompt: Build an Asset Management System (.NET + EF Core + MySQL)

You are an experienced .NET backend developer. Build a **simple, clean Asset Management System** as an **ASP.NET Core Web API** using **Entity Framework Core** and **MySQL**. Follow every requirement below exactly. Keep the code simple, readable and beginner-friendly. Do not over-engineer.

---

## 1. Purpose

Maintain company assets and record which employee has each asset.

The application must have **4 modules**:

| Module | Responsibility |
|---|---|
| **Assets** | Add, edit, view, delete assets |
| **Employees** | Add and manage employees |
| **Allocation** | Assign an asset to an employee |
| **Returns** | Record returned assets |

---

## 2. Tech Stack

- ASP.NET Core Web API (latest LTS .NET version)
- Entity Framework Core (Code-First with migrations)
- MySQL via `Pomelo.EntityFrameworkCore.MySql`
- Swagger for testing endpoints

Required packages:
```
dotnet add package Pomelo.EntityFrameworkCore.MySql
dotnet add package Microsoft.EntityFrameworkCore.Design
```

---

## 3. Architecture

Use a **single Web API project with a layered folder structure**. Do NOT split into multiple class-library projects.

```
Client (Swagger / Postman / UI)
        │
   Controllers      → handle HTTP, validate input, return responses
        │
   Services         → business rules
        │
   AppDbContext     → EF Core talks to MySQL
        │
     MySQL
```

Rules:
- Each layer only talks to the layer directly below it.
- Controllers must **never** use `AppDbContext` directly. They call services only.
- All business logic lives in services.
- Controllers accept and return **DTOs**, never EF entities.

---

## 4. Folder Structure

Create exactly this structure:

```
AssetManagement.Api/
├── Controllers/
│   ├── AssetsController.cs
│   ├── EmployeesController.cs
│   ├── AllocationsController.cs
│   └── ReturnsController.cs
├── Services/
│   ├── Interfaces/
│   │   ├── IAssetService.cs
│   │   ├── IEmployeeService.cs
│   │   ├── IAllocationService.cs
│   │   └── IReturnService.cs
│   ├── AssetService.cs
│   ├── EmployeeService.cs
│   ├── AllocationService.cs
│   └── ReturnService.cs
├── Data/
│   └── AppDbContext.cs
├── Models/                    (EF Core entities = DB tables)
│   ├── Asset.cs
│   ├── Employee.cs
│   ├── AssetAllocation.cs
│   └── Enums/
│       └── AssetStatus.cs
├── DTOs/                      (request/response shapes)
│   ├── AssetDtos.cs
│   ├── EmployeeDtos.cs
│   ├── AllocationDtos.cs
│   └── ReturnDtos.cs
├── Migrations/                (auto-generated)
├── appsettings.json           (MySQL connection string)
└── Program.cs                 (DI registration)
```

---

## 5. Database Design (only 3 tables)

**Important design decision:** Allocation and Returns share **one table** (`AssetAllocations`). A return is not a separate record. It closes an open allocation by filling in `ReturnedDate`. This keeps the full history of who had which asset and when.

Relationships:
```
Employees 1 ──── * AssetAllocations * ──── 1 Assets
```

Use these entities exactly:

```csharp
public enum AssetStatus { Available, Allocated, UnderRepair, Retired }

public class Asset
{
    public int Id { get; set; }
    public string AssetTag { get; set; } = "";     // unique, e.g. "LAP-0012"
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";     // Laptop, Monitor, Phone...
    public string? SerialNumber { get; set; }
    public DateTime PurchaseDate { get; set; }
    public AssetStatus Status { get; set; } = AssetStatus.Available;
    public ICollection<AssetAllocation> Allocations { get; set; } = new List<AssetAllocation>();
}

public class Employee
{
    public int Id { get; set; }
    public string EmployeeCode { get; set; } = "";  // unique
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";         // unique
    public string Department { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public ICollection<AssetAllocation> Allocations { get; set; } = new List<AssetAllocation>();
}

public class AssetAllocation
{
    public int Id { get; set; }
    public int AssetId { get; set; }
    public Asset Asset { get; set; } = null!;
    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;
    public DateTime AllocatedDate { get; set; }
    public DateTime? ReturnedDate { get; set; }     // null = still with employee
    public string? ReturnCondition { get; set; }    // Good / Damaged
    public string? Remarks { get; set; }
}
```

### AppDbContext configuration (in `OnModelCreating`)
- Unique index on `Asset.AssetTag`
- Unique index on `Employee.EmployeeCode`
- Unique index on `Employee.Email`
- Store `Asset.Status` as a string: `.HasConversion<string>()`
- Configure the relationships shown above with foreign keys `AssetId` and `EmployeeId`
- Use `DeleteBehavior.Restrict` on allocation foreign keys so history is never cascade-deleted

---

## 6. Modules: Endpoints and Business Rules

### 6.1 Assets
Endpoints:
- `GET    /api/assets` – list all assets
- `GET    /api/assets/{id}` – get one asset
- `POST   /api/assets` – create asset
- `PUT    /api/assets/{id}` – update asset
- `DELETE /api/assets/{id}` – delete asset

Rules:
- `AssetTag` must be unique.
- An asset that is currently allocated (status `Allocated`) **cannot be deleted**.
- An asset with allocation history should not be hard-deleted; set status to `Retired` instead.

### 6.2 Employees
Endpoints:
- `GET    /api/employees` – list all employees
- `GET    /api/employees/{id}` – get one employee
- `POST   /api/employees` – create employee
- `PUT    /api/employees/{id}` – update employee
- `DELETE /api/employees/{id}` – delete / deactivate employee
- `GET    /api/employees/{id}/assets` – assets currently held by this employee

Rules:
- `EmployeeCode` and `Email` must be unique.
- If the employee has any allocation history, **deactivate** (`IsActive = false`) instead of hard delete.
- An employee who currently holds assets cannot be deactivated until all assets are returned.

### 6.3 Allocation
Endpoints:
- `POST /api/allocations` – assign an asset to an employee (body: `assetId`, `employeeId`, `remarks`)
- `GET  /api/allocations?active=true` – list allocations (`active=true` → only open ones, where `ReturnedDate` is null)

Rules:
- The asset must exist and have status `Available`.
- The employee must exist and be active.
- On success: create an `AssetAllocation` row with `AllocatedDate = now`, and set the asset status to `Allocated`.

### 6.4 Returns
Endpoints:
- `POST /api/returns` – record a return (body: `allocationId`, `returnCondition`, `remarks`)
- `GET  /api/returns` – list all returned allocations (where `ReturnedDate` is not null)

Rules:
- The allocation must exist and still be open (`ReturnedDate` is null).
- On success: set `ReturnedDate = now`, save condition and remarks.
- Set asset status to `Available`, **or** `UnderRepair` if the condition is `Damaged`.

### Transactions
Allocation and Return each change **two tables** (AssetAllocations + Assets). Make both changes and call `SaveChangesAsync()` **once**, so EF Core runs them in a single transaction.

---

## 7. Program.cs Setup

```csharp
var cs = builder.Configuration.GetConnectionString("Default");
builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseMySql(cs, ServerVersion.AutoDetect(cs)));

builder.Services.AddScoped<IAssetService, AssetService>();
builder.Services.AddScoped<IEmployeeService, EmployeeService>();
builder.Services.AddScoped<IAllocationService, AllocationService>();
builder.Services.AddScoped<IReturnService, ReturnService>();
```

Also enable Swagger and controllers.

`appsettings.json` must contain:
```json
"ConnectionStrings": {
  "Default": "server=localhost;port=3306;database=asset_management;user=root;password=YOUR_PASSWORD"
}
```

Migration commands:
```
dotnet ef migrations add InitialCreate
dotnet ef database update
```

---

## 8. Coding Guidelines

- Use `async`/`await` for all database calls.
- Validate DTOs with data annotations (`[Required]`, `[EmailAddress]`, `[MaxLength]`).
- Return proper HTTP status codes:
  - `200 OK` / `201 Created` on success
  - `400 Bad Request` for validation or business-rule failures (e.g. asset not available)
  - `404 Not Found` when a record doesn't exist
  - `409 Conflict` for duplicate AssetTag / EmployeeCode / Email
- Return clear error messages, e.g. `"Asset LAP-0012 is already allocated."`
- Keep methods short; one responsibility per service method.

---

## 9. Build Order

Implement and test in this order, testing each module in Swagger before moving on:

1. **Assets**
2. **Employees**
3. **Allocation** (depends on Assets + Employees)
4. **Returns** (depends on Allocation)

---

## 10. Expected Output

Provide:
1. The complete code for every file in the folder structure above.
2. The `dotnet` CLI commands to create the project, add packages and run migrations.
3. Sample Swagger/Postman request bodies for each POST/PUT endpoint.
4. A short test flow: create asset → create employee → allocate → return → verify asset status is `Available` again.
