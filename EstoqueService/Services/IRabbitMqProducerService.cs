using System.Threading.Tasks;
using EstoqueService.Models;

namespace EstoqueService.Services
{
    public interface IRabbitMqProducerService
    {
        Task EnviarProdutoCriadoAsync(ProdutoMessage produto);
        Task EnviarProdutoAtualizadoAsync(ProdutoMessage produto);
        Task EnviarProdutoRemovidoAsync(int produtoId);
    }
}
