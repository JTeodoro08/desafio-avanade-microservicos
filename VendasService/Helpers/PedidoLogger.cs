using System;
using System.Linq;
using Microsoft.Extensions.Logging;
using VendasService.Services; // ← mantém referência necessária

namespace VendasService.Helpers
{
    /// <summary>
    /// Classe utilitária para logs centralizados e detalhados de pedidos.
    /// </summary>
    public static class PedidoLogger
    {
        // 🎨 Códigos de cor ANSI para console
        private const string Reset = "\x1b[0m";
        private const string Blue = "\x1b[34m";
        private const string Green = "\x1b[32m";
        private const string Yellow = "\x1b[33m";
        private const string Red = "\x1b[31m";
        private const string Gray = "\x1b[90m";
        private const string Cyan = "\x1b[36m";

        public static void LogPedido(
            ILogger logger,
            PedidoMessage pedido,
            string tipoEvento,
            string queueName)
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            // Define emoji conforme o tipo de evento
            string emoji = tipoEvento switch
            {
                "CRIADO" => "📦",
                "ATUALIZADO" => "✏️",
                "DELETADO" => "🗑️",
                "REENVIADO" => "🔄",
                _ => "📤"
            };

            // Monta a lista de itens para visualização legível
            string itens = pedido.Itens.Any()
                ? string.Join(Environment.NewLine, pedido.Itens.Select(i =>
                    $"→ ProdutoId {i.ProdutoId}: Qtd {i.Quantidade}"))
                : "Nenhum item";

            int totalItens = pedido.Itens.Sum(i => i.Quantidade);

            // Log principal colorido no console
            Console.WriteLine($@"
{Blue}🌐 [VENDAS SERVICE]{Reset}
{Gray}──────────────────────────────────────────────{Reset}
{Cyan}{emoji} PEDIDO {tipoEvento}{Reset}
→ Data/Hora: {timestamp}
→ PedidoId: {pedido.PedidoId}
→ Cliente: {pedido.ClienteNome}
→ Fila: {queueName}
→ Total Itens: {totalItens}
{itens}
{Gray}──────────────────────────────────────────────{Reset}
");

            // Log via ILogger para observabilidade centralizada
            logger.LogInformation(
                "[{Time}] {Emoji} Pedido {PedidoId} enviado para fila '{Queue}' (Evento: {Evento}) | Cliente: {Cliente} | Total Itens: {TotalItens}{Itens}",
                timestamp,
                emoji,
                pedido.PedidoId,
                queueName,
                tipoEvento,
                pedido.ClienteNome,
                totalItens,
                Environment.NewLine + itens
            );
        }
    }
}





