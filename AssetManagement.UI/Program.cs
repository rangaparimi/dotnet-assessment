using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// MVC: controllers + Razor views (.cshtml)
builder.Services.AddControllersWithViews(o =>
{
    // Every POST must carry the anti-forgery token that <form method="post"> adds automatically
    o.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());

    // Models are used for both lists and forms, so only validate fields that have attributes
    o.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
});

// HttpClient for the API. Calls go UI server -> API server, so no CORS is involved.
var apiBaseUrl = builder.Configuration["ApiBaseUrl"]
    ?? throw new InvalidOperationException("ApiBaseUrl is not configured.");
builder.Services.AddHttpClient("Api", c => c.BaseAddress = new Uri(apiBaseUrl.TrimEnd('/') + "/"));

var app = builder.Build();

app.UseStaticFiles();   // wwwroot/css/site.css
app.UseRouting();

// "/" -> AssetsController.Index, "/Employees" -> EmployeesController.Index, ...
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Assets}/{action=Index}/{id?}");

app.Run();
