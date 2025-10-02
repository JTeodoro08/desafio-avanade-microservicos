using EstoqueService.Models;
using Xunit;

namespace EstoqueService.Tests.Unit
{
    public class ProdutoTests
    {
        [Fact]
        public void Deve_Criar_Produto_Com_Propriedades_Corretas()
        {
            var produto = new Produto
            {
                Id = 1,
                Nome = "Mouse Gamer",
                Descricao = "Mouse com LED RGB",
                Preco = 150.0m,
                Quantidade = 10
            };

            Assert.Equal(1, produto.Id);
            Assert.Equal("Mouse Gamer", produto.Nome);
            Assert.Equal("Mouse com LED RGB", produto.Descricao);
            Assert.Equal(150.0m, produto.Preco);
            Assert.Equal(10, produto.Quantidade);
        }
    }
}
