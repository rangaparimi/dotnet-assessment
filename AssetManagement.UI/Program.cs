using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
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

// ---------- Sign-in with Google (OAuth) ----------
// Google proves who the user is. After that the user stays signed in with a cookie.
var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
if (string.IsNullOrWhiteSpace(googleClientId) || string.IsNullOrWhiteSpace(googleClientSecret))
    throw new InvalidOperationException(
        "Google sign-in is not configured. Set Authentication:Google:ClientId and Authentication:Google:ClientSecret (see the README, 'Sign-in setup').");

// Emails listed here get the Admin role (comma separated). Everyone else who signs in is a Viewer.
var adminEmails = (builder.Configuration["Auth:AdminEmails"] ?? "")
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

builder.Services.AddAuthentication(o =>
    {
        o.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        o.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;   // not signed in -> our login page
    })
    .AddCookie(o =>
    {
        o.LoginPath = "/Account/Login";
        o.AccessDeniedPath = "/Account/AccessDenied";
    })
    .AddGoogle(o =>
    {
        o.ClientId = googleClientId;
        o.ClientSecret = googleClientSecret;

        // Runs once, right after Google signs the user in: decide the role
        o.Events.OnCreatingTicket = context =>
        {
            var email = context.Principal?.FindFirstValue(ClaimTypes.Email);
            var isAdmin = email != null && adminEmails.Contains(email, StringComparer.OrdinalIgnoreCase);
            context.Identity?.AddClaim(new Claim(ClaimTypes.Role, isAdmin ? "Admin" : "Viewer"));
            return Task.CompletedTask;
        };
    });

// Authorization: every page needs a signed-in user unless it says [AllowAnonymous].
// Actions that change data add [Authorize(Roles = "Admin")].
builder.Services.AddAuthorization(o =>
    o.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

// HttpClient for the API. Calls go UI server -> API server, so no CORS is involved.
// The shared API key proves to the API that the call comes from this UI.
var apiBaseUrl = builder.Configuration["ApiBaseUrl"]
    ?? throw new InvalidOperationException("ApiBaseUrl is not configured.");
var apiKey = builder.Configuration["ApiKey"]
    ?? throw new InvalidOperationException("ApiKey is not configured. It must match the API's ApiKey.");
builder.Services.AddHttpClient("Api", c =>
{
    c.BaseAddress = new Uri(apiBaseUrl.TrimEnd('/') + "/");
    c.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
});

var app = builder.Build();

app.UseStaticFiles();   // wwwroot/css/site.css (served without sign-in, the login page needs it)
app.UseRouting();
app.UseAuthentication();   // who is the user? (reads the cookie)
app.UseAuthorization();    // is that user allowed here?

// "/" -> AssetsController.Index, "/Employees" -> EmployeesController.Index, ...
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Assets}/{action=Index}/{id?}");

app.Run();
