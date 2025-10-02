using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace VendasService.Models
{
    public class Pedido
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string ClienteNome { get; set; } = string.Empty;

        public DateTime DataPedido { get; set; } = DateTime.UtcNow;

        [Required]
        [MinLength(1, ErrorMessage = "O pedido deve conter pelo menos um item.")]
        public ICollection<PedidoItem> Itens { get; set; } = new List<PedidoItem>();

        // Propriedade calculada (não persistida)
        public decimal ValorTotal => Itens.Sum(i => i.ValorTotal);

        // Concurrency token para evitar atualizações simultâneas
        [Timestamp]
        public byte[]? RowVersion { get; set; }
    }
}








