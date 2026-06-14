using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PairCode.Application.DTOs;
using PairCode.Application.Services;
using PairCode.Application.Validators;
using System.Security.Claims;

namespace PairCode.Web.Controllers;

[Authorize]
public class RoomController : Controller
{
    private readonly RoomService _roomService;
    private readonly ParticipantService _participantService;
    private readonly ChatService _chatService;
    private readonly SharedDocumentService _documentService;
    private readonly CreateRoomValidator _createRoomValidator;
    private readonly JoinRoomValidator _joinRoomValidator;

    public RoomController(
        RoomService roomService,
        ParticipantService participantService,
        ChatService chatService,
        SharedDocumentService documentService,
        CreateRoomValidator createRoomValidator,
        JoinRoomValidator joinRoomValidator)
    {
        _roomService = roomService;
        _participantService = participantService;
        _chatService = chatService;
        _documentService = documentService;
        _createRoomValidator = createRoomValidator;
        _joinRoomValidator = joinRoomValidator;
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromForm] CreateRoomDto dto)
    {
        var result = await _createRoomValidator.ValidateAsync(dto);

        if (!result.IsValid)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.ErrorMessage);
            return View(dto);
        }

        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var room = await _roomService.CreateRoomAsync(dto, userId);
        return RedirectToAction("Details", new { id = room.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Index(int page = 1, int pageSize = 20)
    {
        var result = await _roomService.GetPagedRoomsAsync(page, pageSize);
        return View(result);
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id)
    {
        var room = await _roomService.GetRoomByIdAsync(id);
        if (room == null) return NotFound();

        ViewBag.Messages = await _chatService.GetRecentMessagesAsync(id, 50);
        ViewBag.Participants = await _participantService.GetRoomParticipantsAsync(id);
        ViewBag.Document = await _documentService.GetDocumentAsync(id);

        return View(room);
    }

    [HttpPost]
    public async Task<IActionResult> Open(Guid id)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await _roomService.OpenRoomAsync(id, userId);
        return RedirectToAction("Details", new { id });
    }

    [HttpPost]
    public async Task<IActionResult> Close(Guid id)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await _roomService.CloseRoomAsync(id, userId);
        return RedirectToAction("Details", new { id });
    }

    [HttpPost]
    public async Task<IActionResult> Cancel(Guid id)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await _roomService.CancelRoomAsync(id, userId);
        return RedirectToAction("Index");
    }

    [HttpPost]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await _roomService.DeleteRoomAsync(id, userId);
        return RedirectToAction("Index");
    }

    [HttpGet]
    public IActionResult Join()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Join([FromForm] JoinRoomDto dto)
    {
        var result = await _joinRoomValidator.ValidateAsync(dto);

        if (!result.IsValid)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.ErrorMessage);
            return View(dto);
        }

        try
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var room = await _roomService.JoinRoomByCodeAsync(dto.AccessCode, userId);
            return RedirectToAction("Details", new { id = room.Id });
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError("", ex.Message);
            return View(dto);
        }
    }

    [HttpPost]
    public async Task<IActionResult> UpdateName(Guid id, [FromForm] string name)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        try
        {
            await _roomService.UpdateRoomAsync(id, name, userId);
            TempData["SuccessMessage"] = "Room name updated";
        }
        catch (UnauthorizedAccessException)
        {
            TempData["ErrorMessage"] = "Only the room creator can rename the room";
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }
        return RedirectToAction("Details", new { id });
    }

    [HttpPost]
    public async Task<IActionResult> Leave(Guid id)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await _participantService.LeaveRoomAsync(id, userId);
        return RedirectToAction("Index");
    }
}
