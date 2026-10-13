using System.Text.Json.Serialization;
using AssetManagement.Api.Data;
using AssetManagement.Api.Middleware;
using AssetManagement.Api.Repositories;
using AssetManagement.Api.Repositories.Interfaces;
using AssetManagement.Api.Services;
using AssetManagement.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Database (SQL Server)
var cs = builder.Configuration.GetConnectionString("Default");
builder.Services.AddDbContext<AppDbContext>(o => o.UseSqlServer(cs));  // so here database and dbcontext are connecting to each other and the connection string is being passed to the dbcontext so that it can connect to the database and perform operations on it

// Repositories (database access) - all share the same scoped AppDbContext
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IAssetRepository, AssetRepository>();
builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();
builder.Services.AddScoped<IAllocationRepository, AllocationRepository>();

// Services (business logic)
builder.Services.AddScoped<IAssetService, AssetService>(); //so addscope is used to register the service with the dependency injection container and it will create a new instance of the service for each request and it will be disposed of at the end of the request
builder.Services.AddScoped<IEmployeeService, EmployeeService>(); //other than addscoped i can use addsingleton and addtransient but addscoped is used here because it is the most common and it is used for services that are used in the request pipeline and it is also used for services that are used in the controllers
builder.Services.AddScoped<IAllocationService, AllocationService>();
builder.Services.AddScoped<IReturnService, ReturnService>();

// Controllers - show enums like AssetStatus as text ("Available") instead of numbers
builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS - allow the AssetManagement.UI site to call this API from the browser
var uiOrigins = builder.Configuration.GetSection("UiOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(o => o.AddPolicy("ui", p =>
    p.WithOrigins(uiOrigins).AllowAnyHeader().AllowAnyMethod()));

// Global exception handling - unexpected errors become JSON { message }
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

// In Docker the database starts empty, so apply migrations on startup.
// Off by default; docker-compose.yml turns it on with ApplyMigrationsOnStartup=true.
if (app.Configuration.GetValue<bool>("ApplyMigrationsOnStartup"))
{
    using var scope = app.Services.CreateScope();
    scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.Migrate();
}

// CORS first so error responses also carry CORS headers and the UI can read them
app.UseCors("ui");
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
