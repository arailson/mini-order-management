using Application.DTOs;
using Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("pedidos")]
[Produces("application/json")]
public class PedidosController : ControllerBase
{
    private readonly IPedidoService _pedidos;

    public PedidosController(IPedidoService pedidos)
    {
        _pedidos = pedidos;
    }

    /// <summary>Cria um novo pedido (cliente, itens, total e baixa de estoque em uma única transação).</summary>
    [HttpPost]
    [ProducesResponseType(typeof(PedidoResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PedidoResponse>> Criar(
        [FromBody] CriarPedidoRequest request,
        CancellationToken cancellationToken)
    {
        var pedido = await _pedidos.CriarAsync(request, cancellationToken);
        return Created($"/pedidos/{pedido.Id}", pedido);
    }

    /// <summary>Lista pedidos com filtros opcionais e paginação (mais recentes primeiro).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<PedidoListItemResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<PedidoListItemResponse>>> Listar(
        [FromQuery] int? clienteId,
        [FromQuery] DateTime? dataInicio,
        [FromQuery] DateTime? dataFim,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken)
    {
        var resultado = await _pedidos.ListarAsync(
            new ConsultaPedidosFiltro(clienteId, dataInicio, dataFim, page, pageSize),
            cancellationToken);

        return Ok(resultado);
    }
}
