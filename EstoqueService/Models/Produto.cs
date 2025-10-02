using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace EstoqueService.Models
{
    public class Produto
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        [JsonPropertyName("Nome")]
        public string Nome { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        [JsonPropertyName("Descricao")]
        public string Descricao { get; set; } = string.Empty;

        [Range(0, double.MaxValue, ErrorMessage = "Preço deve ser maior ou igual a 0.")]
        [JsonPropertyName("Preco")]
        public decimal Preco { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Quantidade não pode ser negativa.")]
        [JsonPropertyName("Quantidade")]
        public int Quantidade { get; set; }
    }
}

