using System.Threading.Tasks;
using VendasService.Models;

namespace VendasService.Services
{
    public interface IEstoqueClientService
    {
        Task<Produto?> GetProdutoAsync(int produtoId);
    }
}

