using Application.DTOs;
using Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("relatorios")]
[Produces("application/json")]
public class RelatoriosController : ControllerBase
{
    private readonly IRelatorioService _relatorios;

    public RelatoriosController(IRelatorioService relatorios)
    {
        _relatorios = relatorios;
    }

    /// <summary>Os cinco clientes com maior valor total de pedidos no período informado.</summary>
    [HttpGet("top-clientes")]
    [ProducesResponseType(typeof(IReadOnlyList<TopClienteResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<TopClienteResponse>>> TopClientes(
        [FromQuery] DateTime? dataInicio,
        [FromQuery] DateTime? dataFim,
        CancellationToken cancellationToken)
    {
        var ranking = await _relatorios.TopClientesAsync(dataInicio, dataFim, cancellationToken);
        return Ok(ranking);
    }
}
