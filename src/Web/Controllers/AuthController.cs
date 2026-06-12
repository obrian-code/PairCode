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
            Response.Cookies.Append("AuthToken", response.Token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddHours(8)
            });
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
            Response.Cookies.Append("AuthToken", response.Token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddHours(8)
            });
            return RedirectToAction("Index", "Home");
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError("", ex.Message);
            return View(dto);
        }
    }

    [HttpPost]
    public IActionResult Logout()
    {
        Response.Cookies.Delete("AuthToken");
        return RedirectToAction("Login");
    }
}
