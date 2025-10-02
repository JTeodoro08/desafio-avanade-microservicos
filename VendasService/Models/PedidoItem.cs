using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace VendasService.Models
{
    public class PedidoItem
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ProdutoId { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Quantidade deve ser maior que zero.")]
        public int Quantidade { get; set; }

        // Valor total calculado no momento da criação
        [Range(0, double.MaxValue, ErrorMessage = "ValorTotal deve ser maior ou igual a zero.")]
        public decimal ValorTotal { get; set; }

        // FK para Pedido
        public int PedidoId { get; set; }

        // Navegação para Pedido
        [JsonIgnore]
        public Pedido? Pedido { get; set; }
    }
}






