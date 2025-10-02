using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VendasService.Models;
using VendasService.Services;
using Xunit;

namespace VendasService.Tests.IntegrationTests
{
    // ===========================
    // Handler de autenticação de teste
    // ===========================
    #pragma warning disable CS0618 // Desativa warning de ISystemClock obsoleto
    public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public TestAuthHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            Microsoft.AspNetCore.Authentication.ISystemClock clock)
            : base(options, logger, encoder, clock) { }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var claims = new[] { new Claim(ClaimTypes.Name, "TestUser") };
            var identity = new ClaimsIdentity(claims, "Test");
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, "Test");
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
    #pragma warning restore CS0618

    // ===========================
    // Fake RabbitMQ
    // ===========================
    public class FakeRabbitMQProducerService : IRabbitMqProducerService
    {
        public readonly List<PedidoMessage> MensagensEnviadas = new();
        public void EnviarPedido(PedidoMessage pedido) => MensagensEnviadas.Add(pedido);
    }

    // ===========================
    // Factory customizada para injetar Auth fake e RabbitMQ fake
    // ===========================
    public class CustomWebApplicationFactoryWithAuth<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
    {
        public FakeRabbitMQProducerService FakeRabbit { get; private set; } = new();

        protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                // Define esquema Test como padrão
                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = "Test";
                    options.DefaultChallengeScheme = "Test";
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", options => { });

                // Substitui RabbitMQ real por fake
                services.AddSingleton<IRabbitMqProducerService>(_ => FakeRabbit);
            });
        }
    }

    // ===========================
    // Testes de integração
    // ===========================
    public class PedidoIntegrationTests : IClassFixture<CustomWebApplicationFactoryWithAuth<Program>>
    {
        private readonly HttpClient _client;
        private readonly CustomWebApplicationFactoryWithAuth<Program> _factory;

        public PedidoIntegrationTests(CustomWebApplicationFactoryWithAuth<Program> factory)
        {
            _factory = factory;
            _client = factory.CreateClient();

            // Força usar esquema "Test" no header
            _client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Test", "");
        }

        [Fact]
        public async Task CriarPedido_DevePublicarEventoNoRabbit()
        {
            // Arrange
            var pedido = new
            {
                clienteNome = "Cliente Teste",
                itens = new[]
                {
                    new { produtoId = 1, quantidade = 2 }
                }
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/pedidos", pedido);

            // Assert
            response.EnsureSuccessStatusCode(); // ✅ Não retorna 401

            // Verifica se RabbitMQ fake recebeu a mensagem
            Assert.Single(_factory.FakeRabbit.MensagensEnviadas);
            var mensagem = _factory.FakeRabbit.MensagensEnviadas.First();
            Assert.Single(mensagem.Itens);
            Assert.Equal(1, mensagem.Itens.First().ProdutoId);
            Assert.Equal(2, mensagem.Itens.First().Quantidade);
        }
    }
}








