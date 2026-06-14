using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PairCode.Application.DTOs;
using PairCode.Application.Services;
using PairCode.Application.Validators;

namespace PairCode.Web.Controllers;

public class AuthController : Controller
{
    private readonly UserService _userService;
    private readonly RegisterUserValidator _registerValidator;

    public AuthController(UserService userService, RegisterUserValidator registerValidator)
    {
        _userService = userService;
        _registerValidator = registerValidator;
    }

    [HttpGet]
    public IActionResult Login()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");

        return View();
    }

    [HttpPost]
    [EnableRateLimiting("Login")]
    public async Task<IActionResult> Login([FromForm] LoginDto dto)
    {
        try
        {
            var response = await _userService.LoginAsync(dto);
            SetAuthCookies(response.Token, response.RefreshToken);
            return RedirectToAction("Index", "Home");
        }
        catch (UnauthorizedAccessException)
        {
            ModelState.AddModelError("", "Invalid email or password");
            return View(dto);
        }
    }

    [HttpGet]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");

        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Register([FromForm] RegisterUserDto dto)
    {
        var result = await _registerValidator.ValidateAsync(dto);

        if (!result.IsValid)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.ErrorMessage);
            return View(dto);
        }

        if (dto.Role == "Admin" && !(User.Identity?.IsAuthenticated == true && User.IsInRole("Admin")))
        {
            ModelState.AddModelError("", "You are not authorized to create an Admin account");
            return View(dto);
        }

        try
        {
            var response = await _userService.RegisterAsync(dto);
            SetAuthCookies(response.Token, response.RefreshToken);
            return RedirectToAction("Index", "Home");
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError("", ex.Message);
            return View(dto);
        }
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            await _userService.LogoutAsync(userId);
        }

        Response.Cookies.Delete("AuthToken");
        Response.Cookies.Delete("RefreshToken");
        return RedirectToAction("Login");
    }

    [HttpGet]
    public IActionResult ForgotPassword()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> ForgotPassword([FromForm] ForgotPasswordDto dto)
    {
        await _userService.ForgotPasswordAsync(dto);
        TempData["SuccessMessage"] = "If the email exists, a reset link has been sent.";
        return RedirectToAction("Login");
    }

    [HttpGet]
    public IActionResult ResetPassword(string email, string token)
    {
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token))
            return RedirectToAction("Login");

        ViewBag.Email = email;
        ViewBag.Token = token;
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> ResetPassword([FromForm] ResetPasswordDto dto)
    {
        try
        {
            await _userService.ResetPasswordAsync(dto);
            TempData["SuccessMessage"] = "Password reset successfully. Please sign in.";
            return RedirectToAction("Login");
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError("", ex.Message);
            return View(dto);
        }
    }

    [HttpGet]
    public async Task<IActionResult> VerifyEmail(string token)
    {
        if (string.IsNullOrEmpty(token))
            return RedirectToAction("Login");

        try
        {
            await _userService.VerifyEmailAsync(token);
            ViewBag.Success = true;
        }
        catch (InvalidOperationException ex)
        {
            ViewBag.Success = false;
            ViewBag.Error = ex.Message;
        }

        return View();
    }

    private void SetAuthCookies(string token, string? refreshToken)
    {
        Response.Cookies.Append("AuthToken", token, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTime.UtcNow.AddMinutes(30)
        });

        if (refreshToken != null)
        {
            Response.Cookies.Append("RefreshToken", refreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddDays(7)
            });
        }
    }
}
