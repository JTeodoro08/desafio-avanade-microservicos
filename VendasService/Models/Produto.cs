using System.ComponentModel.DataAnnotations;

namespace VendasService.Models
{
    public class Produto
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Nome { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string Descricao { get; set; } = string.Empty;

        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "Preço deve ser maior ou igual a zero.")]
        public decimal Preco { get; set; }

        [Required]
        [Range(0, int.MaxValue, ErrorMessage = "Quantidade deve ser maior ou igual a zero.")]
        public int Quantidade { get; set; }

        // Concurrency token para evitar conflitos de estoque
        [Timestamp]
        public byte[]? RowVersion { get; set; }
    }
}


