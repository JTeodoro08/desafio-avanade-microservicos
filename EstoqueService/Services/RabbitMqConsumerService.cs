using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using EstoqueService.Data;
using EstoqueService.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EstoqueService.Services
{
    public class RabbitMqConsumerService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;
        private readonly ILogger<RabbitMqConsumerService> _logger;

        private IConnection _connection = null!;
        private IModel _channel = null!;

        public RabbitMqConsumerService(IServiceProvider serviceProvider, IConfiguration configuration, ILogger<RabbitMqConsumerService> logger)
        {
            _serviceProvider = serviceProvider;
            _configuration = configuration;
            _logger = logger;
        }

        private string GetTimestamp() => DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        private void InitializeRabbitMq()
        {
            int retryCount = 0;
            const int maxRetries = 5;
            const int delayMs = 5000;

            var host = _configuration["RabbitMQ:HostName"] ?? "localhost";
            var port = int.TryParse(_configuration["RabbitMQ:Port"], out var p) ? p : 5672;
            var user = _configuration["RabbitMQ:User"] ?? "admin";
            var password = _configuration["RabbitMQ:Password"] ?? "admin";
            var queueName = _configuration["RabbitMQ:QueueName"] ?? "estoque_eventos";

            _logger.LogInformation("[{Time}] 🔌 Tentando conectar RabbitMQ em {Host}:{Port}, fila '{Queue}'", GetTimestamp(), host, port, queueName);

            while (retryCount < maxRetries)
            {
                try
                {
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
                    _channel.QueueDeclare(queue: queueName, durable: true, exclusive: false, autoDelete: false, arguments: null);

                    _logger.LogInformation("[{Time}] ✅ RabbitMQ conectado. Fila '{Queue}' pronta.", GetTimestamp(), queueName);
                    break;
                }
                catch (BrokerUnreachableException ex)
                {
                    retryCount++;
                    _logger.LogWarning("[{Time}] ⚠️ Tentativa {Retry}/{Max} falhou: {Message}", GetTimestamp(), retryCount, maxRetries, ex.Message);
                    Task.Delay(delayMs).Wait();
                }
                catch (Exception ex)
                {
                    retryCount++;
                    _logger.LogError("[{Time}] ❌ Erro inesperado: {Message}", GetTimestamp(), ex.Message);
                    Task.Delay(delayMs).Wait();
                }
            }

            if (_channel == null)
                _logger.LogError("[{Time}] ❌ Não foi possível conectar ao RabbitMQ após {Max} tentativas.", GetTimestamp(), maxRetries);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var queueName = _configuration["RabbitMQ:QueueName"] ?? "estoque_eventos";

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    if (_channel == null || _connection == null || !_connection.IsOpen || !_channel.IsOpen)
                    {
                        _logger.LogWarning("[{Time}] 🔄 Conexão perdida. Tentando reconectar...", GetTimestamp());
                        InitializeRabbitMq();
                        if (_channel == null)
                        {
                            await Task.Delay(5000, stoppingToken);
                            continue;
                        }
                    }

                    var consumer = new AsyncEventingBasicConsumer(_channel);
                    consumer.Received += async (model, ea) =>
                    {
                        var body = ea.Body.ToArray();
                        var message = Encoding.UTF8.GetString(body);

                        _logger.LogInformation("[{Time}] 📩 Mensagem recebida: {Message}", GetTimestamp(), message);

                        try
                        {
                            var baseWrapper = JsonSerializer.Deserialize<BaseWrapper>(message, new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            });

                            if (baseWrapper?.Evento == "ProdutoCriado")
                            {
                                var wrapper = JsonSerializer.Deserialize<ProdutoWrapper>(message);
                                if (wrapper?.Produto == null)
                                {
                                    _logger.LogWarning("[{Time}] ⚠️ ProdutoCriado inválido: {Message}", GetTimestamp(), message);
                                    _channel.BasicReject(ea.DeliveryTag, requeue: false);
                                    return;
                                }

                                await ProcessarProdutoCriado(wrapper.Produto);
                            }
                            else if (baseWrapper?.Evento == "ProdutoAtualizado")
                            {
                                var wrapper = JsonSerializer.Deserialize<ProdutoWrapper>(message);
                                if (wrapper?.Produto == null)
                                {
                                    _logger.LogWarning("[{Time}] ⚠️ ProdutoAtualizado inválido: {Message}", GetTimestamp(), message);
                                    _channel.BasicReject(ea.DeliveryTag, requeue: false);
                                    return;
                                }

                                await ProcessarProdutoAtualizado(wrapper.Produto);
                            }
                            else if (baseWrapper?.Evento == "ProdutoRemovido")
                            {
                                var wrapper = JsonSerializer.Deserialize<ProdutoWrapper>(message);
                                if (wrapper?.Produto == null)
                                {
                                    _logger.LogWarning("[{Time}] ⚠️ ProdutoRemovido inválido: {Message}", GetTimestamp(), message);
                                    _channel.BasicReject(ea.DeliveryTag, requeue: false);
                                    return;
                                }

                                await ProcessarProdutoRemovido(wrapper.Produto.Id);
                            }
                            else
                            {
                                // se não tem campo Evento, tenta interpretar como Pedido
                                var pedido = JsonSerializer.Deserialize<PedidoMessage>(message, new JsonSerializerOptions
                                {
                                    PropertyNameCaseInsensitive = true
                                });

                                if (pedido == null || pedido.Itens == null || !pedido.Itens.Any())
                                {
                                    _logger.LogWarning("[{Time}] ⚠️ Pedido inválido: {Message}", GetTimestamp(), message);
                                    _channel.BasicReject(ea.DeliveryTag, requeue: false);
                                    return;
                                }

                                await ProcessarPedido(pedido);
                            }

                            _channel.BasicAck(ea.DeliveryTag, multiple: false);
                        }
                        catch (JsonException ex)
                        {
                            _logger.LogWarning("[{Time}] ⚠️ JSON inválido: {Message}. Erro: {Error}", GetTimestamp(), message, ex.Message);
                            _channel.BasicReject(ea.DeliveryTag, requeue: false);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError("[{Time}] ❌ Erro ao processar mensagem: {Error}", GetTimestamp(), ex.Message);
                            _channel.BasicReject(ea.DeliveryTag, requeue: false);
                        }
                    };

                    _channel.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);
                    await Task.Delay(Timeout.Infinite, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError("[{Time}] ❌ Erro no consumer: {Message}. Tentando reiniciar em 5s...", GetTimestamp(), ex.Message);
                    await Task.Delay(5000, stoppingToken);
                }
            }
        }

        // =========================
        // PROCESSADORES
        // =========================
        private async Task ProcessarPedido(PedidoMessage pedido)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<EstoqueContext>();

            _logger.LogInformation("[{Time}] [RABBIT] Pedido recebido: {PedidoId}, {ItensCount} itens", GetTimestamp(), pedido.PedidoId, pedido.Itens.Count);

            foreach (var item in pedido.Itens)
            {
                var produto = await context.Produtos.FirstOrDefaultAsync(p => p.Id == item.ProdutoId);
                if (produto != null)
                {
                    if (produto.Quantidade >= item.Quantidade)
                    {
                        var antes = produto.Quantidade;
                        produto.Quantidade -= item.Quantidade;
                        context.Produtos.Update(produto);

                        _logger.LogInformation("[{Time}] Estoque atualizado: Produto {ProdutoNome}, -{Quantidade} unidades. Restante: {QuantidadeRestante} (antes: {QuantidadeAntes})",
                            GetTimestamp(), produto.Nome, item.Quantidade, produto.Quantidade, antes);
                    }
                    else
                    {
                        _logger.LogWarning("[{Time}] Estoque insuficiente: Produto {ProdutoNome}, solicitado {QuantidadeSolicitada}, disponível {QuantidadeDisponivel}.",
                            GetTimestamp(), produto.Nome, item.Quantidade, produto.Quantidade);
                    }
                }
                else
                {
                    _logger.LogWarning("[{Time}] Produto {ProdutoId} não encontrado no estoque.", GetTimestamp(), item.ProdutoId);
                }
            }

            await context.SaveChangesAsync();
        }

        private async Task ProcessarProdutoCriado(Produto produto)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<EstoqueContext>();

            var existente = await context.Produtos.FindAsync(produto.Id);
            if (existente == null)
            {
                context.Produtos.Add(produto);
                await context.SaveChangesAsync();
                _logger.LogInformation("[{Time}] 📦 ProdutoCriado processado: {ProdutoNome} (Id={ProdutoId})", GetTimestamp(), produto.Nome, produto.Id);
            }
            else
            {
                _logger.LogWarning("[{Time}] ⚠️ Produto {ProdutoId} já existe, ignorando ProdutoCriado.", GetTimestamp(), produto.Id);
            }
        }

        private async Task ProcessarProdutoAtualizado(Produto produto)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<EstoqueContext>();

            var existente = await context.Produtos.FindAsync(produto.Id);
            if (existente != null)
            {
                existente.Nome = produto.Nome;
                existente.Descricao = produto.Descricao;
                existente.Preco = produto.Preco;
                existente.Quantidade = produto.Quantidade;

                context.Produtos.Update(existente);
                await context.SaveChangesAsync();

                _logger.LogInformation("[{Time}] ✏️ ProdutoAtualizado processado: {ProdutoNome}", GetTimestamp(), produto.Nome);
            }
            else
            {
                _logger.LogWarning("[{Time}] ⚠️ Produto {ProdutoId} não encontrado ao processar ProdutoAtualizado.", GetTimestamp(), produto.Id);
            }
        }

        private async Task ProcessarProdutoRemovido(int produtoId)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<EstoqueContext>();

            var existente = await context.Produtos.FindAsync(produtoId);
            if (existente != null)
            {
                context.Produtos.Remove(existente);
                await context.SaveChangesAsync();
                _logger.LogInformation("[{Time}] 🗑️ ProdutoRemovido processado: Id={ProdutoId}", GetTimestamp(), produtoId);
            }
            else
            {
                _logger.LogWarning("[{Time}] ⚠️ Produto {ProdutoId} não encontrado ao tentar remover.", GetTimestamp(), produtoId);
            }
        }

        // =========================
        // DTOs
        // =========================
        public class BaseWrapper
        {
            public string Evento { get; set; } = string.Empty;
        }

        public class ProdutoWrapper : BaseWrapper
        {
            public Produto Produto { get; set; } = new();
        }

        public class PedidoMessage
        {
            public int PedidoId { get; set; }
            public List<PedidoItemMessage> Itens { get; set; } = new();
        }

        public class PedidoItemMessage
        {
            public int ProdutoId { get; set; }
            public int Quantidade { get; set; }
        }
    }
}














