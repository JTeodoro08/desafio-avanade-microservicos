using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EstoqueService.Models
{
    // =========================
    // 🔹 Representa um pedido recebido via RabbitMQ
    // =========================
    public class PedidoMessage
    {
        public int PedidoId { get; set; }

        [Required]
        public string ClienteNome { get; set; } = string.Empty;

        public List<PedidoItem> Itens { get; set; } = new();
    }

    // =========================
    // 🔹 Representa um item dentro do pedido
    // =========================
    public class PedidoItem
    {
        public int ProdutoId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Quantidade deve ser pelo menos 1.")]
        public int Quantidade { get; set; }
    }
}
