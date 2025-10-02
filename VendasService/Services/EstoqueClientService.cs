using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VendasService.Models;

namespace VendasService.Services
{
    /// <summary>
    /// Serviço de comunicação com o EstoqueService via Ocelot Gateway.
    /// </summary>
    public class EstoqueClientService : IEstoqueClientService
    {
        private readonly HttpClient _http;
        private readonly ILogger<EstoqueClientService> _logger;

        public EstoqueClientService(HttpClient http, ILogger<EstoqueClientService> logger)
        {
            _http = http;
            _logger = logger;
        }

        /// <summary>
        /// Consulta o EstoqueService para obter informações de um produto específico.
        /// Retorna <c>null</c> caso o produto não seja encontrado ou ocorra erro de comunicação.
        /// </summary>
        public async Task<Produto?> GetProdutoAsync(int produtoId)
        {
            try
            {
                // A rota é relativa ao BaseAddress configurado no Program.cs
                var resp = await _http.GetAsync($"produtos/{produtoId}");

                if (resp.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogWarning("Produto {ProdutoId} não encontrado no EstoqueService.", produtoId);
                    return null;
                }

                if (!resp.IsSuccessStatusCode)
                {
                    _logger.LogError(
                        "Falha ao consultar Produto {ProdutoId} no EstoqueService. Status: {StatusCode}",
                        produtoId, resp.StatusCode
                    );
                    return null;
                }

                Produto? produto = await resp.Content.ReadFromJsonAsync<Produto>();

                if (produto is null)
                {
                    _logger.LogWarning("Resposta inválida ao consultar Produto {ProdutoId} no EstoqueService.", produtoId);
                }

                return produto;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Erro de rede ao consultar Produto {ProdutoId} no EstoqueService.", produtoId);
                return null;
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "Timeout ao consultar Produto {ProdutoId} no EstoqueService.", produtoId);
                return null;
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado ao consultar Produto {ProdutoId} no EstoqueService.", produtoId);
                return null;
            }
        }
    }
}



