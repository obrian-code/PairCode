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
    private readonly ParticipantService _participantService;

    public ProfileController(UserService userService, ParticipantService participantService)
    {
        _userService = userService;
        _participantService = participantService;
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
    public async Task<IActionResult> Update([FromForm] string name)
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

    [HttpGet]
    public async Task<IActionResult> History()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var history = await _participantService.GetUserHistoryAsync(userId);
        return View(history);
    }
}
