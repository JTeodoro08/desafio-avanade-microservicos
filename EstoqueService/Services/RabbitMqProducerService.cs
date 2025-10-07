using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using EstoqueService.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace EstoqueService.Services
{
    // ============================
    // Implementação do Producer
    // ============================
    public class RabbitMqProducerService : IRabbitMqProducerService, IDisposable
    {
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private readonly ILogger<RabbitMqProducerService> _logger;
        private readonly string _queueName;

        public RabbitMqProducerService(ILogger<RabbitMqProducerService> logger, IConfiguration configuration)
        {
            _logger = logger;

            var rabbitConfig = configuration.GetSection("RabbitMQ");
            var hostName = rabbitConfig.GetValue<string>("HostName") ?? "localhost";
            var port = rabbitConfig.GetValue<int>("Port");
            var userName = rabbitConfig.GetValue<string>("UserName") ?? "guest";
            var password = rabbitConfig.GetValue<string>("Password") ?? "guest";
            _queueName = rabbitConfig.GetValue<string>("QueueName") ?? "estoque_queue";

            try
            {
                var factory = new ConnectionFactory
                {
                    HostName = hostName,
                    Port = port,
                    UserName = userName,
                    Password = password
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

                _logger.LogInformation("[ESTOQUE] 🟢 RabbitMQ Producer conectado à fila '{Fila}' ({Host}:{Port})", _queueName, hostName, port);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ESTOQUE] ❌ Falha ao conectar RabbitMQ Producer");
                throw;
            }
        }

        // ============================
        // Método genérico de envio
        // ============================
        private Task EnviarMensagemAsync(string evento, object payload)
        {
            try
            {
                var mensagem = new
                {
                    TipoEvento = evento,
                    DataEnvio = DateTime.UtcNow,
                    Conteudo = payload
                };

                var json = JsonSerializer.Serialize(mensagem, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = false
                });

                var body = Encoding.UTF8.GetBytes(json);

                _channel.BasicPublish(
                    exchange: "",
                    routingKey: _queueName,
                    basicProperties: null,
                    body: body
                );

                _logger.LogInformation("[ESTOQUE] 📤 Evento '{Evento}' publicado na fila '{Fila}'", evento, _queueName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ESTOQUE] ⚠️ Falha ao publicar evento '{Evento}'", evento);
                throw;
            }

            return Task.CompletedTask;
        }

        // ============================
        // Eventos específicos
        // ============================
        public Task EnviarProdutoCriadoAsync(Produto produto)
            => EnviarMensagemAsync("PRODUTO_CRIADO", new { Produto = produto });

        public Task EnviarProdutoAtualizadoAsync(Produto produto)
            => EnviarMensagemAsync("PRODUTO_ATUALIZADO", new { Produto = produto });

        public Task EnviarProdutoRemovidoAsync(int produtoId)
            => EnviarMensagemAsync("PRODUTO_REMOVIDO", new { Id = produtoId });

        public Task EnviarPedidoCriadoAsync(PedidoMessage pedido)
            => EnviarMensagemAsync("PEDIDO_CRIADO", new { Pedido = pedido });

        // ============================
        // Liberação de recursos
        // ============================
        public void Dispose()
        {
            try
            {
                _channel?.Close();
                _connection?.Close();
                _logger.LogInformation("[ESTOQUE] 🔻 Conexão RabbitMQ encerrada com sucesso.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[ESTOQUE] ⚠️ Erro ao encerrar conexão RabbitMQ");
            }
        }
    }
}















