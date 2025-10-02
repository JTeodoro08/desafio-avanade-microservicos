using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using EstoqueService.Data;
using EstoqueService.Models;
using EstoqueService.Services;
using Microsoft.Extensions.Options;
using System.Collections.Generic;

namespace EstoqueService.Tests.Integration
{
    // Handler fake para testes de autenticação
    public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public TestAuthHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock? clock = null) // mantém o clock obsoleto
            : base(options, logger, encoder, clock ?? new SystemClock())
        { }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var claims = new[] { new Claim(ClaimTypes.Name, "TestUser") };
            var identity = new ClaimsIdentity(claims, "Test");
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, "Test");
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }

    // Factory de testes customizada
    public class TestWebAppFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
    {
        public InMemoryRabbitProducer FakeRabbit { get; } = new();

        protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                // Remove Rabbit real e adiciona fake
                services.RemoveAll(typeof(IRabbitMqProducerService));
                services.AddSingleton<IRabbitMqProducerService>(FakeRabbit);

                // Remove DbContext real e adiciona InMemory
                services.RemoveAll(typeof(DbContextOptions<EstoqueContext>));
                services.AddDbContext<EstoqueContext>(options =>
                    options.UseInMemoryDatabase("TestDb"));

                // Inicializa DB InMemory limpo
                var sp = services.BuildServiceProvider();
                using var scope = sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<EstoqueContext>();
                db.Database.EnsureDeleted();
                db.Database.EnsureCreated();

                // Substitui autenticação JWT real por fake
                services.AddAuthentication("Test")
                        .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", options => { });

                // Define Test como esquema default
                services.PostConfigure<AuthenticationOptions>(opts =>
                {
                    opts.DefaultAuthenticateScheme = "Test";
                    opts.DefaultChallengeScheme = "Test";
                });
            });
        }
    }

    // Fake RabbitMQ em memória, registra eventos enviados
    public class InMemoryRabbitProducer : IRabbitMqProducerService
    {
        public List<ProdutoMessage> MensagensEnviadas { get; } = new();
        public List<string> Eventos { get; } = new(); // opcional para verificar tipo de evento

        public Task EnviarProdutoCriadoAsync(ProdutoMessage produto)
        {
            MensagensEnviadas.Add(produto);
            Eventos.Add("ProdutoCriado");
            return Task.CompletedTask;
        }

        public Task EnviarProdutoAtualizadoAsync(ProdutoMessage produto)
        {
            MensagensEnviadas.Add(produto);
            Eventos.Add("ProdutoAtualizado");
            return Task.CompletedTask;
        }

        public Task EnviarProdutoRemovidoAsync(int produtoId)
        {
            Eventos.Add($"ProdutoRemovido:{produtoId}");
            return Task.CompletedTask;
        }
    }
}

