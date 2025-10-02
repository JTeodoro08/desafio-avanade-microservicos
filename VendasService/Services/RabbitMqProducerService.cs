using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace VendasService.Services
{
    public class RabbitMqProducerService : IRabbitMqProducerService, IDisposable
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<RabbitMqProducerService> _logger;

        private IConnection _connection = default!;
        private IModel _channel = default!;
        private readonly string _queueName;

        public RabbitMqProducerService(IConfiguration configuration, ILogger<RabbitMqProducerService> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _queueName = _configuration["RabbitMQ:QueueName"] ?? "estoque_eventos";
            InitializeRabbitMq();
        }

        private void InitializeRabbitMq()
        {
            try
            {
                var factory = new ConnectionFactory()
                {
                    HostName = _configuration["RabbitMQ:HostName"] ?? "localhost",
                    UserName = _configuration["RabbitMQ:UserName"] ?? "guest",
                    Password = _configuration["RabbitMQ:Password"] ?? "guest",
                    Port = int.TryParse(_configuration["RabbitMQ:Port"], out var port) ? port : 5672
                };

                _connection = factory.CreateConnection();
                _channel = _connection.CreateModel();

                _channel.QueueDeclare(
                    queue: _queueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null
                );

                _logger.LogInformation("✅ RabbitMQ conectado e fila '{Queue}' pronta.", _queueName);
            }
            catch (BrokerUnreachableException ex)
            {
                _logger.LogError(ex, "❌ Não foi possível conectar ao RabbitMQ.");
            }
        }

        public void EnviarPedido(PedidoMessage pedido)
        {
            if (_channel == null || _channel.IsClosed)
            {
                _logger.LogWarning("⚠️ Canal RabbitMQ não inicializado. Mensagem não enviada.");
                return;
            }

            try
            {
                var json = JsonSerializer.Serialize(pedido);
                var body = Encoding.UTF8.GetBytes(json);

                var props = _channel.CreateBasicProperties();
                props.Persistent = true;

                _channel.BasicPublish(
                    exchange: "",
                    routingKey: _queueName,
                    basicProperties: props,
                    body: body
                );

                _logger.LogInformation("📦 Pedido enviado para fila '{Queue}': {Mensagem}", _queueName, json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erro ao enviar mensagem para RabbitMQ.");
            }
        }

        public void Dispose()
        {
            try
            {
                if (_channel != null && _channel.IsOpen) _channel.Close();
                if (_connection != null && _connection.IsOpen) _connection.Close();
                _logger.LogDebug("🔌 Conexão com RabbitMQ finalizada corretamente.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao fechar conexão RabbitMQ.");
            }
        }
    }

    // 🔹 Modelos de mensagem ajustados
    public class PedidoMessage
    {
        public int PedidoId { get; set; }
        public string ClienteNome { get; set; } = string.Empty; // Adicionado
        public List<PedidoItemMessage> Itens { get; set; } = new();
    }

    public class PedidoItemMessage
    {
        public int ProdutoId { get; set; }
        public int Quantidade { get; set; }
    }
}






