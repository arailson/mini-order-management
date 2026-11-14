using Application.DTOs;
using Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

/// <summary>Endpoints de apoio ao frontend (selects de clientes e produtos).</summary>
[ApiController]
[Produces("application/json")]
public class CatalogoController : ControllerBase
{
    private readonly ICatalogoService _catalogo;

    public CatalogoController(ICatalogoService catalogo)
    {
        _catalogo = catalogo;
    }

    /// <summary>Lista os clientes cadastrados, ordenados por nome.</summary>
    [HttpGet("clientes")]
    [ProducesResponseType(typeof(IReadOnlyList<ClienteResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ClienteResponse>>> Clientes(CancellationToken cancellationToken) =>
        Ok(await _catalogo.ListarClientesAsync(cancellationToken));

    /// <summary>Lista os produtos com preço e estoque atuais, ordenados por nome.</summary>
    [HttpGet("produtos")]
    [ProducesResponseType(typeof(IReadOnlyList<ProdutoResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ProdutoResponse>>> Produtos(CancellationToken cancellationToken) =>
        Ok(await _catalogo.ListarProdutosAsync(cancellationToken));
}
