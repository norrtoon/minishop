using Microsoft.AspNetCore.Mvc;

namespace MiniShop.Controllers;

[ApiController]
[Route("api/diagnostics")]
public class DiagnosticsController : ControllerBase
{
    [HttpGet("error")]
    public IActionResult ThrowError() =>
        throw new InvalidOperationException("Тестовая ошибка для демонстрации обработки 500.");
}
