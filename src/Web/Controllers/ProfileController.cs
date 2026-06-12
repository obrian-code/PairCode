using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PairCode.Application.DTOs;
using PairCode.Application.Services;
using System.Security.Claims;

namespace PairCode.Web.Controllers;

[Authorize]
public class ProfileController : Controller
{
    private readonly UserService _userService;

    public ProfileController(UserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var profile = await _userService.GetProfileAsync(userId);
        if (profile == null) return NotFound();

        return View(profile);
    }

    [HttpPost]
    public async Task<IActionResult> Update(string name)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await _userService.UpdateProfileAsync(userId, new(name));
        return RedirectToAction("Index");
    }

    [HttpPost]
    public async Task<IActionResult> ChangePassword([FromForm] ChangePasswordDto dto)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        try
        {
            await _userService.ChangePasswordAsync(userId, dto);
            TempData["SuccessMessage"] = "Password changed successfully";
        }
        catch (UnauthorizedAccessException)
        {
            ModelState.AddModelError("CurrentPassword", "Current password is incorrect");
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError("", ex.Message);
        }

        return RedirectToAction("Index");
    }
}
