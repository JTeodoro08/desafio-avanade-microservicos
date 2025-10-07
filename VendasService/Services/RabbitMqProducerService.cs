using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using VendasService.Helpers;

namespace VendasService.Services
{
    /// <summary>
    /// Responsável pela publicação de eventos de pedidos no RabbitMQ.
    /// Logs técnicos e detalhados unificados via PedidoLogger.
    /// </summary>
    public class RabbitMqProducerService : IRabbitMqProducerService, IDisposable
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<RabbitMqProducerService> _logger;
        private IConnection? _connection;
        private IModel? _channel;
        private readonly string _queueName;

        public RabbitMqProducerService(IConfiguration configuration, ILogger<RabbitMqProducerService> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _queueName = _configuration["RabbitMQ:QueueName"] ?? "estoque_eventos";
            InitializeRabbitMq();
        }

        private string GetTimestamp() => DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        private void InitializeRabbitMq()
        {
            int retryCount = 0;
            const int maxRetries = 5;
            const int delayMs = 5000;

            var host = _configuration["RabbitMQ:HostName"] ?? "localhost";
            var port = int.TryParse(_configuration["RabbitMQ:Port"], out var p) ? p : 5672;
            var user = _configuration["RabbitMQ:UserName"] ?? "guest";
            var password = _configuration["RabbitMQ:Password"] ?? "guest";

            _logger.LogInformation("[{Time}] 🔌 Tentando conectar RabbitMQ em {Host}:{Port}, fila '{Queue}'", 
                GetTimestamp(), host, port, _queueName);

            while (retryCount < maxRetries)
            {
                try
                {
                    var factory = new ConnectionFactory
                    {
                        HostName = host,
                        Port = port,
                        UserName = user,
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

                    _logger.LogInformation("[{Time}] ✅ RabbitMQ conectado. Fila '{Queue}' pronta.", 
                        GetTimestamp(), _queueName);
                    break;
                }
                catch (BrokerUnreachableException ex)
                {
                    retryCount++;
                    _logger.LogWarning("[{Time}] ⚠️ Tentativa {Retry}/{Max} falhou: {Message}", 
                        GetTimestamp(), retryCount, maxRetries, ex.Message);
                    Task.Delay(delayMs).Wait();
                }
                catch (Exception ex)
                {
                    retryCount++;
                    _logger.LogError("[{Time}] ❌ Erro inesperado: {Message}", 
                        GetTimestamp(), ex.Message);
                    Task.Delay(delayMs).Wait();
                }
            }

            if (_channel == null)
                _logger.LogError("[{Time}] ❌ Não foi possível conectar ao RabbitMQ após {Max} tentativas.", 
                    GetTimestamp(), maxRetries);
        }

        public void EnviarEventoPedido(PedidoMessage pedido, string tipoEvento)
        {
            if (_channel == null || _channel.IsClosed)
            {
                _logger.LogWarning("[{Time}] ⚠️ Canal RabbitMQ fechado. Tentando reconectar...", GetTimestamp());
                InitializeRabbitMq();

                if (_channel == null || _channel.IsClosed)
                {
                    _logger.LogError("[{Time}] ❌ Falha ao reconectar ao RabbitMQ. Evento '{TipoEvento}' não enviado.", 
                        GetTimestamp(), tipoEvento);
                    return;
                }
            }

            const int maxRetries = 5;
            const int delayMs = 3000;
            int attempt = 0;

            while (attempt < maxRetries)
            {
                try
                {
                    var envelope = new PedidoEnvelope
                    {
                        TipoEvento = tipoEvento,
                        Pedido = pedido,
                        DataEnvio = DateTime.UtcNow
                    };

                    var json = JsonSerializer.Serialize(envelope, new JsonSerializerOptions { WriteIndented = false });
                    var body = Encoding.UTF8.GetBytes(json);

                    var props = _channel.CreateBasicProperties();
                    props.Persistent = true;

                    _channel.BasicPublish(
                        exchange: "",
                        routingKey: _queueName,
                        basicProperties: props,
                        body: body
                    );

                    // 🌟 Log unificado e visual do pedido
                    _logger.LogInformation(@"
🌐 [VENDAS SERVICE]
──────────────────────────────────────────────
📦 PEDIDO {TipoEvento}
→ Data/Hora: {Hora}
→ PedidoId: {PedidoId}
→ Cliente: {Cliente}
→ Fila: {Fila}
→ Total Itens: {TotalItens}
{Itens}
──────────────────────────────────────────────",
                        tipoEvento.ToUpper(),
                        GetTimestamp(),
                        pedido.PedidoId,
                        pedido.ClienteNome,
                        _queueName,
                        pedido.Itens.Sum(i => i.Quantidade),
                        string.Join(Environment.NewLine, pedido.Itens.Select(i => $"→ ProdutoId {i.ProdutoId}: Qtd {i.Quantidade}"))
                    );

                    break; // sucesso
                }
                catch (Exception ex)
                {
                    attempt++;
                    _logger.LogWarning("[{Time}] ⚠️ Tentativa {Attempt}/{Max} falhou ao publicar mensagem. Erro: {Error}", 
                        GetTimestamp(), attempt, maxRetries, ex.Message);

                    Task.Delay(delayMs).Wait();

                    if (attempt == maxRetries)
                    {
                        _logger.LogError("[{Time}] ❌ Falha definitiva: não foi possível publicar evento '{TipoEvento}' após {Max} tentativas.", 
                            GetTimestamp(), tipoEvento, maxRetries);
                    }
                }
            }
        }

        public void Dispose()
        {
            try
            {
                if (_channel != null && _channel.IsOpen)
                    _channel.Close();

                if (_connection != null && _connection.IsOpen)
                    _connection.Close();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[{Time}] ❌ Erro ao fechar conexão RabbitMQ.", GetTimestamp());
            }
        }
    }

    // 📦 Estruturas auxiliares
    public class PedidoEnvelope
    {
        public string TipoEvento { get; set; } = string.Empty;
        public PedidoMessage Pedido { get; set; } = new();
        public DateTime DataEnvio { get; set; }
    }

    public class PedidoMessage
    {
        public int PedidoId { get; set; }
        public string ClienteNome { get; set; } = string.Empty;
        public List<PedidoItemMessage> Itens { get; set; } = new();
    }

    public class PedidoItemMessage
    {
        public int ProdutoId { get; set; }
        public int Quantidade { get; set; }
    }
}


















