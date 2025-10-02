using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using VendasService.Controllers;
using VendasService.Data;
using VendasService.Models;
using VendasService.Models.Dto;
using VendasService.Services;
using Xunit;

namespace VendasService.Tests.Unit
{
    public class PedidosControllerTests
    {
        private readonly VendasContext _context;
        private readonly Mock<IRabbitMqProducerService> _rabbitMqMock;
        private readonly Mock<IEstoqueClientService> _estoqueMock;
        private readonly Mock<ILogger<PedidosController>> _loggerMock;
        private readonly PedidosController _controller;

        public PedidosControllerTests()
        {
            // Configura o banco InMemory
            var options = new DbContextOptionsBuilder<VendasContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()) // cada teste usa DB único
                .Options;

            _context = new VendasContext(options);

            _rabbitMqMock = new Mock<IRabbitMqProducerService>();
            _estoqueMock = new Mock<IEstoqueClientService>();
            _loggerMock = new Mock<ILogger<PedidosController>>();

            _controller = new PedidosController(_context, _rabbitMqMock.Object, _estoqueMock.Object, _loggerMock.Object);
        }

        // ===============================
        // GET: Retorna todos os pedidos
        // ===============================
        [Fact]
        public async Task GetPedidos_DeveRetornarPedidosExistentes()
        {
            // Arrange
            _context.Pedidos.Add(new Pedido
            {
                ClienteNome = "José",
                Itens = new List<PedidoItem> { new PedidoItem { ProdutoId = 1, Quantidade = 2, ValorTotal = 100 } }
            });
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.GetPedidos();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var pedidos = Assert.IsAssignableFrom<IEnumerable<Pedido>>(okResult.Value);
            Assert.Single(pedidos);
        }

        // ===============================
        // GET by ID
        // ===============================
        [Fact]
        public async Task GetPedido_DeveRetornarPedido_QuandoExistir()
        {
            // Arrange
            var pedido = new Pedido
            {
                ClienteNome = "Maria",
                Itens = new List<PedidoItem> { new PedidoItem { ProdutoId = 2, Quantidade = 1, ValorTotal = 50 } }
            };
            _context.Pedidos.Add(pedido);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.GetPedido(pedido.Id);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var pedidoRetornado = Assert.IsType<Pedido>(okResult.Value!); // garante não nulo
            Assert.Equal("Maria", pedidoRetornado.ClienteNome);
        }

        // ===============================
        // POST: Criação de pedido válido
        // ===============================
        [Fact]
        public async Task CreatePedido_DeveCriarPedidoValido()
        {
            // Arrange
            var pedidoDto = new PedidoCreateDto
            {
                ClienteNome = "Carlos",
                Itens = new List<PedidoItemDto> { new PedidoItemDto { ProdutoId = 1, Quantidade = 2 } }
            };

            // Mock do estoque → Produto válido
            _estoqueMock.Setup(e => e.GetProdutoAsync(1))
                .ReturnsAsync(new Produto { Id = 1, Nome = "Produto X", Preco = 10, Quantidade = 100 });

            // Act
            var result = await _controller.CreatePedido(pedidoDto);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var pedido = Assert.IsType<Pedido>(createdResult.Value!); // garante não nulo

            Assert.Equal("Carlos", pedido.ClienteNome);
            Assert.Single(pedido.Itens);
            Assert.Equal(20, pedido.Itens.First().ValorTotal);

            // Verifica se RabbitMQ foi chamado
            _rabbitMqMock.Verify(r => r.EnviarPedido(It.IsAny<PedidoMessage>()), Times.Once);
        }

        // ===============================
        // POST: Estoque insuficiente
        // ===============================
        [Fact]
        public async Task CreatePedido_DeveFalhar_QuandoEstoqueInsuficiente()
        {
            // Arrange
            var pedidoDto = new PedidoCreateDto
            {
                ClienteNome = "Ana",
                Itens = new List<PedidoItemDto> { new PedidoItemDto { ProdutoId = 1, Quantidade = 10 } }
            };

            _estoqueMock.Setup(e => e.GetProdutoAsync(1))
                .ReturnsAsync(new Produto { Id = 1, Nome = "Produto Y", Preco = 5, Quantidade = 3 });

            // Act
            var result = await _controller.CreatePedido(pedidoDto);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Contains("Estoque insuficiente", badRequest.Value!.ToString());
        }

        // ===============================
        // POST: Produto inexistente
        // ===============================
        [Fact]
        public async Task CreatePedido_DeveFalhar_QuandoProdutoNaoExistir()
        {
            // Arrange
            var pedidoDto = new PedidoCreateDto
            {
                ClienteNome = "Pedro",
                Itens = new List<PedidoItemDto> { new PedidoItemDto { ProdutoId = 99, Quantidade = 1 } }
            };

            _estoqueMock.Setup(e => e.GetProdutoAsync(99))
                .ReturnsAsync((Produto?)null); // produto não encontrado

            // Act
            var result = await _controller.CreatePedido(pedidoDto);

            // Assert
            var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.Contains("não encontrado", notFound.Value!.ToString());
        }

        // ===============================
        // PUT: Atualização de pedido
        // ===============================
        [Fact]
        public async Task UpdatePedido_DeveAtualizarClienteEItens()
        {
            // Arrange
            var pedido = new Pedido
            {
                ClienteNome = "Inicial",
                Itens = new List<PedidoItem> { new PedidoItem { ProdutoId = 1, Quantidade = 1, ValorTotal = 10 } }
            };
            _context.Pedidos.Add(pedido);
            await _context.SaveChangesAsync();

            var updateDto = new PedidoUpdateDto
            {
                ClienteNome = "Atualizado",
                Itens = new List<PedidoItemUpdateDto> { new PedidoItemUpdateDto { ProdutoId = 2, Quantidade = 2 } }
            };

            _estoqueMock.Setup(e => e.GetProdutoAsync(2))
                .ReturnsAsync(new Produto { Id = 2, Nome = "Produto Z", Preco = 20, Quantidade = 50 });

            // Act
            var result = await _controller.UpdatePedido(pedido.Id, updateDto);

            // Assert
            Assert.IsType<NoContentResult>(result);

            var atualizado = await _context.Pedidos.Include(p => p.Itens).FirstAsync(p => p.Id == pedido.Id);
            Assert.Equal("Atualizado", atualizado.ClienteNome);
            Assert.Single(atualizado.Itens);
            Assert.Equal(40, atualizado.Itens.First().ValorTotal);
        }

        // ===============================
        // DELETE: Remove pedido
        // ===============================
        [Fact]
        public async Task DeletePedido_DeveRemoverPedido()
        {
            // Arrange
            var pedido = new Pedido
            {
                ClienteNome = "Excluir",
                Itens = new List<PedidoItem> { new PedidoItem { ProdutoId = 1, Quantidade = 1, ValorTotal = 10 } }
            };
            _context.Pedidos.Add(pedido);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.DeletePedido(pedido.Id);

            // Assert
            Assert.IsType<NoContentResult>(result);
            Assert.Empty(_context.Pedidos);

            _rabbitMqMock.Verify(r => r.EnviarPedido(It.IsAny<PedidoMessage>()), Times.Once);
        }

        // ===============================
        // REENVIAR: RabbitMQ
        // ===============================
        [Fact]
        public async Task ReenviarPedidoRabbit_DeveReenviarEvento()
        {
            // Arrange
            var pedido = new Pedido
            {
                ClienteNome = "Rabbit",
                Itens = new List<PedidoItem> { new PedidoItem { ProdutoId = 3, Quantidade = 1, ValorTotal = 30 } }
            };
            _context.Pedidos.Add(pedido);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.ReenviarPedidoRabbit(pedido.Id);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Contains("reenviado", ok.Value!.ToString());

            _rabbitMqMock.Verify(r => r.EnviarPedido(It.IsAny<PedidoMessage>()), Times.Once);
        }
    }
}



