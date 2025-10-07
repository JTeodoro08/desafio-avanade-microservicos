using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using EstoqueService.Data;
using EstoqueService.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using EstoqueService.Helpers;

namespace EstoqueService.Services
{
    /// <summary>
    /// Serviço de consumo de pedidos do RabbitMQ.
    /// Atualiza estoque de produtos com logs detalhados.
    /// </summary>
    public class RabbitMqConsumerService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;
        private readonly ILogger<RabbitMqConsumerService> _logger;

        private IConnection? _connection;
        private IModel? _channel;

        public RabbitMqConsumerService(IServiceProvider serviceProvider, IConfiguration configuration, ILogger<RabbitMqConsumerService> logger)
        {
            _serviceProvider = serviceProvider;
            _configuration = configuration;
            _logger = logger;
        }

        private string GetTimestamp() => DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        // =========================
        // Inicialização RabbitMQ
        // =========================
        private void InitializeRabbitMq()
        {
            var host = _configuration["RabbitMQ:HostName"] ?? "localhost";
            var port = int.TryParse(_configuration["RabbitMQ:Port"], out var p) ? p : 5672;
            var user = _configuration["RabbitMQ:UserName"] ?? "guest";
            var password = _configuration["RabbitMQ:Password"] ?? "guest";
            var queueName = _configuration["RabbitMQ:QueueName"] ?? "estoque_eventos";

            var factory = new ConnectionFactory
            {
                HostName = host,
                Port = port,
                UserName = user,
                Password = password,
                DispatchConsumersAsync = true
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
            _channel.QueueDeclare(queue: queueName, durable: true, exclusive: false, autoDelete: false);

            _logger.LogInformation("[{Time}] ✅ Conectado ao RabbitMQ. Fila '{Queue}' pronta para consumir.", GetTimestamp(), queueName);
        }

        // =========================
        // Execução principal
        // =========================
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var queueName = _configuration["RabbitMQ:QueueName"] ?? "estoque_eventos";
            InitializeRabbitMq();

            var consumer = new AsyncEventingBasicConsumer(_channel!);
            consumer.Received += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);

                try
                {
                    var pedidoWrapper = JsonSerializer.Deserialize<PedidoWrapper>(message, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (pedidoWrapper?.Pedido != null)
                    {
                        var pedido = pedidoWrapper.Pedido;

                        // ✅ Log com propriedades deserializadas para exibir corretamente caracteres especiais
                        _logger.LogInformation(
                            "[{Time}] 📩 Mensagem recebida | PedidoId={PedidoId}, Cliente={ClienteNome}, TotalItens={TotalItens}",
                            GetTimestamp(),
                            pedido.PedidoId,
                            pedido.ClienteNome,
                            pedido.Itens.Count
                        );

                        await ProcessarPedido(pedido);
                    }
                    else
                    {
                        _logger.LogInformation("[{Time}] ⚠️ Mensagem recebida não contém um pedido válido. Ignorando.", GetTimestamp());
                    }

                    _channel!.BasicAck(ea.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[{Time}] ❌ Erro ao processar mensagem", GetTimestamp());
                    _channel!.BasicReject(ea.DeliveryTag, false);
                }
            };

            _channel!.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }

        // =========================
        // Processamento do pedido
        // =========================
        private async Task ProcessarPedido(PedidoMessage pedido)
        {
            if (pedido == null || pedido.Itens == null || pedido.Itens.Count == 0)
            {
                _logger.LogInformation("[{Time}] ⚠️ Pedido vazio ou sem itens. Ignorando processamento.", GetTimestamp());
                return;
            }

            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<EstoqueContext>();

            // Log início do pedido
            EstoqueLogger.LogInicioPedido(_logger, pedido);

            foreach (var item in pedido.Itens)
            {
                var produto = await context.Produtos.FindAsync(item.ProdutoId);
                if (produto == null)
                {
                    _logger.LogWarning("[{Time}] ⚠️ Produto {ProdutoId} não encontrado no estoque.", GetTimestamp(), item.ProdutoId);
                    continue;
                }

                int estoqueAnterior = produto.Quantidade;
                produto.Quantidade -= item.Quantidade;
                await context.SaveChangesAsync();

                // Log atualização individual do produto
                EstoqueLogger.LogAtualizacaoProduto(_logger, produto, item.Quantidade, estoqueAnterior);
            }

            // Log fim do processamento do pedido
            EstoqueLogger.LogFimPedido(_logger, pedido);
        }

        // =========================
        // DTOs
        // =========================
        public class BaseWrapper { public string TipoEvento { get; set; } = string.Empty; }
        public class PedidoWrapper : BaseWrapper { public PedidoMessage Pedido { get; set; } = new(); }
    }
}








































