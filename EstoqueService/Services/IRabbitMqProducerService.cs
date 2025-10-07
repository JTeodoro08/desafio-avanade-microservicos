using EstoqueService.Models;
using System.Threading.Tasks;

namespace EstoqueService.Services
{
    public interface IRabbitMqProducerService
    {
        Task EnviarProdutoCriadoAsync(Produto produto);
        Task EnviarProdutoAtualizadoAsync(Produto produto);
        Task EnviarProdutoRemovidoAsync(int produtoId);
        Task EnviarPedidoCriadoAsync(PedidoMessage pedido);
    }
}



