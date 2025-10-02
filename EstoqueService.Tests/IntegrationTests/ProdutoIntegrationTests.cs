using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using EstoqueService.Models;
using EstoqueService.Data;
using EstoqueService.Tests.Integration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EstoqueService.Tests.IntegrationTests
{
    public class ProdutoIntegrationTests : IClassFixture<TestWebAppFactory<Program>>
    {
        private readonly TestWebAppFactory<Program> _factory;
        private readonly HttpClient _client;

        public ProdutoIntegrationTests(TestWebAppFactory<Program> factory)
        {
            _factory = factory;
            _client = factory.CreateClient(); // TestAuthHandler já cuida da auth
        }

        private EstoqueContext CreateScopeDbContext()
        {
            var scope = _factory.Services.CreateScope();
            return scope.ServiceProvider.GetRequiredService<EstoqueContext>();
        }

        [Fact(DisplayName = "POST /api/Produtos deve criar e publicar evento no RabbitMQ")]
        public async Task CriarProduto_DevePublicarEventoNoRabbit()
        {
            _factory.FakeRabbit.MensagensEnviadas.Clear();
            _factory.FakeRabbit.Eventos.Clear();

            var produto = new Produto
            {
                Nome = "Produto Teste",
                Descricao = "Descrição Teste",
                Preco = 10,
                Quantidade = 5
            };

            var response = await _client.PostAsJsonAsync("/api/Produtos", produto);
            response.EnsureSuccessStatusCode();

            // Verifica que o evento foi publicado
            Assert.Single(_factory.FakeRabbit.MensagensEnviadas);
            Assert.Single(_factory.FakeRabbit.Eventos);
            Assert.Equal("ProdutoCriado", _factory.FakeRabbit.Eventos.First());

            var mensagem = _factory.FakeRabbit.MensagensEnviadas.First();
            Assert.Equal("Produto Teste", mensagem.Nome);
            Assert.Equal(5, mensagem.Quantidade);
        }

        [Fact(DisplayName = "GET /api/Produtos deve retornar lista de produtos")]
        public async Task GetProdutos_DeveRetornarListaComProdutos()
        {
            using var context = CreateScopeDbContext();
            context.Produtos.Add(new Produto
            {
                Nome = "Produto Lista",
                Descricao = "Teste Lista",
                Preco = 20,
                Quantidade = 10
            });
            await context.SaveChangesAsync();

            var produtos = await _client.GetFromJsonAsync<List<Produto>>("/api/Produtos");
            Assert.NotNull(produtos);
            Assert.Contains(produtos, p => p.Nome == "Produto Lista");
        }

        [Fact(DisplayName = "PUT /api/Produtos/{id} deve atualizar produto existente e publicar evento")]
        public async Task UpdateProduto_DeveAlterarProdutoExistente()
        {
            int produtoId;
            using (var context = CreateScopeDbContext())
            {
                var produto = new Produto
                {
                    Nome = "Produto Antigo",
                    Descricao = "Descrição Antiga",
                    Preco = 15,
                    Quantidade = 8
                };
                context.Produtos.Add(produto);
                await context.SaveChangesAsync();
                produtoId = produto.Id;
            }

            var produtoAtualizado = new Produto
            {
                Id = produtoId,
                Nome = "Produto Atualizado",
                Descricao = "Descrição Atualizada",
                Preco = 25,
                Quantidade = 12
            };

            _factory.FakeRabbit.MensagensEnviadas.Clear();
            _factory.FakeRabbit.Eventos.Clear();

            var response = await _client.PutAsJsonAsync($"/api/Produtos/{produtoId}", produtoAtualizado);
            response.EnsureSuccessStatusCode();

            using var contextVerify = CreateScopeDbContext();
            var produtoDb = await contextVerify.Produtos.FindAsync(produtoId);
            Assert.NotNull(produtoDb);
            Assert.Equal("Produto Atualizado", produtoDb.Nome);
            Assert.Equal(12, produtoDb.Quantidade);

            // Verifica evento enviado
            Assert.Single(_factory.FakeRabbit.MensagensEnviadas);
            Assert.Single(_factory.FakeRabbit.Eventos);
            Assert.Equal("ProdutoAtualizado", _factory.FakeRabbit.Eventos.First());
        }

        [Fact(DisplayName = "DELETE /api/Produtos/{id} deve remover produto existente e publicar evento")]
        public async Task DeleteProduto_DeveRemoverProdutoExistente()
        {
            int produtoId;
            using (var context = CreateScopeDbContext())
            {
                var produto = new Produto
                {
                    Nome = "Produto Para Remover",
                    Descricao = "Descrição",
                    Preco = 30,
                    Quantidade = 3
                };
                context.Produtos.Add(produto);
                await context.SaveChangesAsync();
                produtoId = produto.Id;
            }

            _factory.FakeRabbit.MensagensEnviadas.Clear();
            _factory.FakeRabbit.Eventos.Clear();

            var response = await _client.DeleteAsync($"/api/Produtos/{produtoId}");
            response.EnsureSuccessStatusCode();

            using var contextVerify = CreateScopeDbContext();
            var produtoDb = await contextVerify.Produtos.FindAsync(produtoId);
            Assert.Null(produtoDb);

            // Verifica evento enviado
            Assert.Single(_factory.FakeRabbit.Eventos);
            Assert.Equal($"ProdutoRemovido:{produtoId}", _factory.FakeRabbit.Eventos.First());
        }
    }
}





















