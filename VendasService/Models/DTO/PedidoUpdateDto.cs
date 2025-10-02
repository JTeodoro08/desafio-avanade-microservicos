using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace VendasService.Models.Dto
{
    public class PedidoUpdateDto
    {
        [JsonPropertyName("clienteNome")]
        [StringLength(100, ErrorMessage = "O nome do cliente deve ter no máximo 100 caracteres.")]
        public string? ClienteNome { get; set; } // Opcional para atualização parcial

        [JsonPropertyName("itens")]
        [MinLength(1, ErrorMessage = "O pedido deve conter ao menos um item.")]
        public List<PedidoItemUpdateDto>? Itens { get; set; } = new();
    }

    public class PedidoItemUpdateDto
    {
        [JsonPropertyName("produtoId")]
        [Range(1, int.MaxValue, ErrorMessage = "ProdutoId deve ser maior que zero.")]
        public int ProdutoId { get; set; }

        [JsonPropertyName("quantidade")]
        [Range(1, int.MaxValue, ErrorMessage = "Quantidade deve ser maior que zero.")]
        public int Quantidade { get; set; }
    }
}
