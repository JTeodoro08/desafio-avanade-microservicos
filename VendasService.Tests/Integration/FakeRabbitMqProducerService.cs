// Caminho: VendasService.Tests/Integration/FakeRabbitMqProducerService.cs
using System.Collections.Generic;
using VendasService.Models;
using VendasService.Services;

namespace VendasService.Tests.Integration
{
    /// <summary>
    /// Implementação fake do IRabbitMqProducerService
    /// usada durante os testes para evitar dependência real do RabbitMQ.
    /// </summary>
    public class FakeRabbitMqProducerService : IRabbitMqProducerService
    {
        // Lista em memória para armazenar mensagens simuladas
        public List<PedidoMessage> MensagensEnviadas { get; } = new();

        public void EnviarPedido(PedidoMessage pedido)
        {
            // Apenas registra a mensagem em memória (não envia de fato)
            MensagensEnviadas.Add(pedido);
        }
    }
}
