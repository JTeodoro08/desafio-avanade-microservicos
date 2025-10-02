using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace VendasService.Models.Dto
{
    public class PedidoItemDto
    {
        [JsonPropertyName("produtoId")]
        [Required(ErrorMessage = "ProdutoId é obrigatório.")]
        [Range(1, int.MaxValue, ErrorMessage = "ProdutoId deve ser maior que zero.")]
        public int ProdutoId { get; set; }

        [JsonPropertyName("quantidade")]
        [Required(ErrorMessage = "Quantidade é obrigatória.")]
        [Range(1, int.MaxValue, ErrorMessage = "Quantidade deve ser maior que zero.")]
        public int Quantidade { get; set; }
    }
}
