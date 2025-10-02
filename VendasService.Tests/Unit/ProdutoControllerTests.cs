using Xunit;
using VendasService.Controllers;
using VendasService.Models;
using VendasService.Data;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

public class ProdutoControllerTests
{
    private readonly AppDbContext _context;
    private readonly ProdutoController _controller;

    public ProdutoControllerTests()
    {
        _context = InMemoryDbContextFactory.Create();
        _controller = new ProdutoController(_context);
    }

    [Fact]
    public async Task CriarProduto_DeveAdicionarProduto()
    {
        var produto = new Produto { Nome = "Teste", Preco = 10, Quantidade = 5 };
        await _controller.PostProduto(produto);
        var dbProduto = await _context.Produtos.FirstAsync();
        Assert.Equal("Teste", dbProduto.Nome);
    }

    [Fact]
    public async Task AtualizarProduto_Inexistente_DeveRetornarNotFound()
    {
        var produto = new Produto { Id = 999, Nome = "Inválido" };
        var result = await _controller.PutProduto(999, produto);
        Assert.IsType<Microsoft.AspNetCore.Mvc.NotFoundResult>(result);
    }

    [Fact]
    public async Task DeletarProduto_DeveRemoverProduto()
    {
        var produto = new Produto { Nome = "Remover", Preco = 5, Quantidade = 2 };
        _context.Produtos.Add(produto);
        await _context.SaveChangesAsync();
        await _controller.DeleteProduto(produto.Id);
        Assert.Empty(_context.Produtos);
    }
}
