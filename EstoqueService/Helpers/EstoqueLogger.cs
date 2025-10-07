using Microsoft.Extensions.Logging;
using EstoqueService.Models;

namespace EstoqueService.Helpers
{
    public static class EstoqueLogger
    {
        /// <summary>
        /// Log de ações gerais sobre produtos: criação, atualização, exclusão ou consulta.
        /// </summary>
        public static void LogProduto(ILogger logger, Produto produto, string acao)
        {
            string emoji = acao switch
            {
                "CRIADO" => "✅",
                "ATUALIZADO" => "✏️",
                "DELETADO" => "🗑️",
                "CONSULTA" => "🔎",
                _ => "ℹ️"
            };

            logger.LogInformation(
                $"[ESTOQUE] {emoji} Produto {acao} | Id: {produto.Id} | Nome: {produto.Nome} | Qtd: {produto.Quantidade} | Preço: {produto.Preco}"
            );
        }

        /// <summary>
        /// Log detalhado de atualização de estoque ao processar um pedido.
        /// </summary>
        public static void LogAtualizacaoProduto(ILogger logger, Produto produto, int quantidadeRetirada, int estoqueAnterior)
        {
            logger.LogInformation(
                "[ESTOQUE] 🔄 Estoque atualizado | Produto: {Nome} | Estoque Anterior: {EstoqueAnterior} | Qtd Retirada: {QtdRetirada} | Estoque Atual: {EstoqueAtual}",
                produto.Nome, estoqueAnterior, quantidadeRetirada, produto.Quantidade
            );
        }

        /// <summary>
        /// Log do início do processamento de um pedido.
        /// </summary>
        public static void LogInicioPedido(ILogger logger, PedidoMessage pedido)
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            logger.LogInformation($@"
🌐 [ESTOQUE SERVICE]
──────────────────────────────────────────────
📥 INÍCIO PROCESSAMENTO
→ Data/Hora: {timestamp}
→ PedidoId: {pedido.PedidoId}
→ Cliente: {pedido.ClienteNome}
→ Total Itens: {pedido.Itens.Count}
──────────────────────────────────────────────
");
        }

        /// <summary>
        /// Log do fim do processamento de um pedido.
        /// </summary>
        public static void LogFimPedido(ILogger logger, PedidoMessage pedido)
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            logger.LogInformation($@"
✅ Estoque atualizado com sucesso para o pedido {pedido.PedidoId}.
→ Data/Hora: {timestamp}
══════════════════════════════════════════════
");
        }
    }
}


