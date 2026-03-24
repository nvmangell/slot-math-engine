using Microsoft.AspNetCore.Mvc;

namespace SlotMathEngine.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class SimulationController : ControllerBase
{
    private readonly SimulationService _simulationService;
    private readonly ILogger<SimulationController> _logger;

    public SimulationController(SimulationService simulationService, ILogger<SimulationController> logger)
    {
        _simulationService = simulationService;
        _logger = logger;
    }

    /// <summary>
    /// Runs a Monte Carlo simulation for the specified game and returns a full statistical report.
    /// </summary>
    /// <param name="request">Simulation parameters including game ID, spin count, and convergence tolerance.</param>
    [HttpPost("run")]
    [ProducesResponseType(typeof(SimulationRunResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RunSimulation([FromBody] SimulationRequest request)
    {
        if (request.SpinCount < 1000 || request.SpinCount > 50_000_000)
            return BadRequest("spinCount must be between 1,000 and 50,000,000");

        try
        {
            var result = await _simulationService.RunSimulationAsync(request);
            return Ok(result);
        }
        catch (FileNotFoundException ex)
        {
            _logger.LogWarning("Config not found for game {GameId}", request.GameId);
            return NotFound(new { error = ex.Message });
        }
    }
}
