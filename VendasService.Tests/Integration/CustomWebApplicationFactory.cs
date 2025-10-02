using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Threading.Tasks;
using VendasService.Models;
using VendasService.Services;

namespace VendasService.Tests.Integration
{
    public class CustomWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
    {
        public FakeRabbitMQProducerService FakeRabbit { get; private set; } = new();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                // Substitui o serviço real por um fake
                FakeRabbit = new FakeRabbitMQProducerService();
                services.AddSingleton<IRabbitMqProducerService>(FakeRabbit);
            });
        }
    }

    // Fake para testes
    public class FakeRabbitMQProducerService : IRabbitMqProducerService
    {
        public List<PedidoMessage> MensagensEnviadas { get; private set; } = new();

        public Task EnviarPedidoAsync(PedidoMessage pedido)
        {
            MensagensEnviadas.Add(pedido);
            return Task.CompletedTask;
        }

        // Implementa o método sincronizado caso algum teste chame
        public void EnviarPedido(PedidoMessage pedido)
        {
            MensagensEnviadas.Add(pedido);
        }
    }
}




