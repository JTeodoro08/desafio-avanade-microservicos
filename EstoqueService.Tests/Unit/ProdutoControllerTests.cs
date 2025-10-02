using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EstoqueService.Controllers;
using EstoqueService.Data;
using EstoqueService.Models;
using EstoqueService.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace EstoqueService.Tests.Unit
{
    public class ProdutoControllerTests : IDisposable
    {
        private readonly ProdutosController _controller;
        private readonly EstoqueContext _context;
        private readonly Mock<IRabbitMqProducerService> _mockRabbit;

        public ProdutoControllerTests()
        {
            // Cada teste recebe um banco InMemory separado
            var options = new DbContextOptionsBuilder<EstoqueContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new EstoqueContext(options);

            _mockRabbit = new Mock<IRabbitMqProducerService>();

            var logger = LoggerFactory.Create(b => b.AddConsole())
                                      .CreateLogger<ProdutosController>();

            _controller = new ProdutosController(_context, _mockRabbit.Object, logger);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [Fact]
        public async Task GetProdutos_DeveRetornarListaVazia_QuandoNaoHaProdutos()
        {
            var result = await _controller.GetProdutos();
            var produtos = result.Value ?? new List<Produto>();

            Assert.Empty(produtos);
        }

        [Fact]
        public async Task CreateProduto_DeveAdicionarProduto()
        {
            var produto = new Produto
            {
                Nome = "Produto Teste",
                Descricao = "Descrição Teste",
                Preco = 10,
                Quantidade = 5
            };

            var result = await _controller.CreateProduto(produto);
            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var produtoCriado = Assert.IsType<Produto>(createdResult.Value!);

            Assert.NotNull(produtoCriado);
            Assert.Equal("Produto Teste", produtoCriado.Nome);
            Assert.True(produtoCriado.Id > 0);
            Assert.Single(_context.Produtos);

            // Verifica se o método de RabbitMQ foi chamado
            _mockRabbit.Verify(r => r.EnviarProdutoCriadoAsync(It.IsAny<ProdutoMessage>()), Times.Once);
        }

        [Fact]
        public async Task GetProduto_DeveRetornarProdutoCriado()
        {
            var produto = new Produto
            {
                Nome = "Produto Existente",
                Descricao = "Teste",
                Preco = 20,
                Quantidade = 10
            };

            _context.Produtos.Add(produto);
            await _context.SaveChangesAsync();

            var result = await _controller.GetProduto(produto.Id);
            var produtoRetornado = result.Value!;

            Assert.NotNull(produtoRetornado);
            Assert.Equal(produto.Nome, produtoRetornado.Nome);
        }

        [Fact]
        public async Task UpdateProduto_DeveAlterarProdutoExistente()
        {
            var produto = new Produto
            {
                Nome = "Produto Original",
                Descricao = "Original",
                Preco = 15,
                Quantidade = 3
            };

            _context.Produtos.Add(produto);
            await _context.SaveChangesAsync();

            var produtoAtualizado = new Produto
            {
                Nome = "Produto Atualizado",
                Descricao = "Atualizado",
                Preco = 25,
                Quantidade = 7
            };

            var result = await _controller.UpdateProduto(produto.Id, produtoAtualizado);
            Assert.IsType<NoContentResult>(result);

            var produtoNoBanco = await _context.Produtos.FindAsync(produto.Id);
            Assert.NotNull(produtoNoBanco);
            Assert.Equal("Produto Atualizado", produtoNoBanco!.Nome);
            Assert.Equal(25, produtoNoBanco.Preco);

            // Verifica se o método de RabbitMQ foi chamado
            _mockRabbit.Verify(r => r.EnviarProdutoAtualizadoAsync(It.IsAny<ProdutoMessage>()), Times.Once);
        }

        [Fact]
        public async Task UpdateProduto_DeveRetornarBadRequest_QuandoIdNaoBate()
        {
            var produto = new Produto
            {
                Id = 1,
                Nome = "Produto Teste",
                Descricao = "Desc",
                Preco = 10,
                Quantidade = 5
            };

            var result = await _controller.UpdateProduto(999, produto);
            Assert.IsType<BadRequestResult>(result);
        }

        [Fact]
        public async Task DeleteProduto_DeveRemoverProdutoExistente()
        {
            var produto = new Produto
            {
                Nome = "Produto Para Deletar",
                Descricao = "Teste Delete",
                Preco = 12,
                Quantidade = 2
            };

            _context.Produtos.Add(produto);
            await _context.SaveChangesAsync();

            var result = await _controller.DeleteProduto(produto.Id);
            Assert.IsType<NoContentResult>(result);

            var produtoNoBanco = await _context.Produtos.FindAsync(produto.Id);
            Assert.Null(produtoNoBanco);

            // Verifica se o método de RabbitMQ foi chamado
            _mockRabbit.Verify(r => r.EnviarProdutoRemovidoAsync(It.IsAny<int>()), Times.Once);
        }

        [Fact]
        public async Task GetProdutos_DeveRetornarListaComProdutos()
        {
            var produto1 = new Produto { Nome = "P1", Descricao = "Desc1", Preco = 10, Quantidade = 5 };
            var produto2 = new Produto { Nome = "P2", Descricao = "Desc2", Preco = 20, Quantidade = 15 };
            _context.Produtos.AddRange(produto1, produto2);
            await _context.SaveChangesAsync();

            var result = await _controller.GetProdutos();
            var produtos = Assert.IsAssignableFrom<IEnumerable<Produto>>(result.Value);

            Assert.Equal(2, produtos.Count());
            Assert.Contains(produtos, p => p.Nome == "P1");
            Assert.Contains(produtos, p => p.Nome == "P2");
        }

        [Fact]
        public async Task GetProduto_DeveRetornarNotFound_QuandoProdutoNaoExiste()
        {
            var result = await _controller.GetProduto(999);
            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task UpdateProduto_DeveRetornarNotFound_QuandoProdutoNaoExiste()
        {
            var produto = new Produto { Nome = "Não existe", Descricao = "X", Preco = 1, Quantidade = 1 };
            var result = await _controller.UpdateProduto(999, produto);
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task DeleteProduto_DeveRetornarNotFound_QuandoProdutoNaoExiste()
        {
            var result = await _controller.DeleteProduto(999);
            Assert.IsType<NotFoundResult>(result);
        }
    }
}







