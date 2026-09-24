using Microsoft.AspNetCore.Mvc;
using Resonance.Routing;
using Resonance.Services;

namespace Resonance.Http.Controllers;

[ApiController]
[Subdomain("production", "socket", "socketdev")]
[WebSocketRequest(false)]
[Route("/")]
public sealed class StatusController(IOnlinePlayers onlinePlayers) : ControllerBase
{
    [HttpGet]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public ContentResult Get()
    {
        return Content(@$"Resonance API<br><br>{onlinePlayers.Count} player(s) online<br><br>
            <a href=""https://github.com/mrflashstudio/Resonance"">Source Code</a>",  "text/html", System.Text.Encoding.UTF8);
    }
}
