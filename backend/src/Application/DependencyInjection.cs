using Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IPedidoService, PedidoService>();
        services.AddScoped<IRelatorioService, RelatorioService>();
        services.AddScoped<ICatalogoService, CatalogoService>();
        return services;
    }
}
