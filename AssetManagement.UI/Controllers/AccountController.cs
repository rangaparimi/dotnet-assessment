using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssetManagement.UI.Controllers;

public class AccountController : Controller
{
    // GET /Account/Login - page with the "Sign in with Google" button
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Assets");

        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    // POST /Account/GoogleLogin - sends the browser to Google; Google sends it back to /signin-google
    [AllowAnonymous]
    [HttpPost]
    public IActionResult GoogleLogin(string? returnUrl)
    {
        var properties = new AuthenticationProperties
        {
            RedirectUri = Url.IsLocalUrl(returnUrl) ? returnUrl : Url.Action("Index", "Assets")
        };
        return Challenge(properties, GoogleDefaults.AuthenticationScheme);
    }

    // POST /Account/Logout - deletes the sign-in cookie
    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    // GET /Account/AccessDenied - signed in, but not an Admin
    [AllowAnonymous]
    public IActionResult AccessDenied() => View();
}
