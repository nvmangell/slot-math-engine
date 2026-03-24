using Microsoft.AspNetCore.Mvc;

namespace SlotMathEngine.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AnalyticsController : ControllerBase
{
    private readonly SimulationService _simulationService;

    public AnalyticsController(SimulationService simulationService)
    {
        _simulationService = simulationService;
    }

    /// <summary>
    /// Returns the paytable definition and per-reel symbol probability distribution for a game.
    /// </summary>
    /// <param name="gameId">The game identifier (must match a config file in the configs directory).</param>
    [HttpGet("paytable/{gameId}")]
    [ProducesResponseType(typeof(PaytableInfo), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPaytable(string gameId)
    {
        try
        {
            var info = await _simulationService.GetPaytableInfoAsync(gameId);
            return Ok(info);
        }
        catch (FileNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }
}
