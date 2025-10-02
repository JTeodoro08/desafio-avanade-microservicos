using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace VendasService.Models.Dto
{
    public class PedidoCreateDto
    {
        [JsonPropertyName("clienteNome")]
        [Required(ErrorMessage = "O nome do cliente é obrigatório.")]
        [StringLength(100, ErrorMessage = "O nome do cliente deve ter no máximo 100 caracteres.")]
        public string ClienteNome { get; set; } = string.Empty;

        [JsonPropertyName("itens")]
        [Required(ErrorMessage = "Itens são obrigatórios.")]
        [MinLength(1, ErrorMessage = "O pedido deve conter pelo menos um item.")]
        public List<PedidoItemDto> Itens { get; set; } = new();
    }
}

