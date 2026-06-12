using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PairCode.Application.Services;

namespace PairCode.Web.Controllers;

[Authorize(Roles = "Admin")]
public class AuditController : Controller
{
    private readonly AuditService _auditService;

    public AuditController(AuditService auditService)
    {
        _auditService = auditService;
    }

    public async Task<IActionResult> Index()
    {
        var logs = await _auditService.GetAllLogsAsync();
        return View(logs);
    }
}
