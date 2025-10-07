using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;
using VendasService.Data;
using VendasService.Models;
using VendasService.Models.Dto;
using VendasService.Services;

namespace VendasService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // 🔐 Exige JWT
    public class PedidosController : ControllerBase
    {
        private readonly VendasContext _context;
        private readonly IRabbitMqProducerService _rabbitMqService;
        private readonly IEstoqueClientService _estoqueClient;
        private readonly ILogger<PedidosController> _logger;
       

        public PedidosController(
            VendasContext context,
            IRabbitMqProducerService rabbitMqService,
            IEstoqueClientService estoqueClient,
            ILogger<PedidosController> logger)
        {
            _context = context;
            _rabbitMqService = rabbitMqService;
            _estoqueClient = estoqueClient;
            _logger = logger;
        }

        // 🔹 Retorna os últimos pedidos
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Pedido>>> GetPedidos([FromQuery] int top = 10)
        {
            var pedidos = await _context.Pedidos
                .Include(p => p.Itens)
                .OrderByDescending(p => p.DataPedido)
                .Take(top)
                .ToListAsync();

            return Ok(pedidos);
        }

        // 🔹 Retorna um pedido específico
        [HttpGet("{id:int}")]
        public async Task<ActionResult<Pedido>> GetPedido(int id)
        {
            var pedido = await _context.Pedidos
                .Include(p => p.Itens)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (pedido == null)
                return NotFound(new { message = $"Pedido {id} não encontrado." });

            return Ok(pedido);
        }

        // 🔹 Criação de um novo pedido
        [HttpPost]
        public async Task<ActionResult<Pedido>> CreatePedido([FromBody] PedidoCreateDto pedidoDto)
        {
            if (pedidoDto == null || string.IsNullOrWhiteSpace(pedidoDto.ClienteNome))
                return BadRequest(new { message = "ClienteNome é obrigatório." });

            if (pedidoDto.Itens == null || !pedidoDto.Itens.Any())
                return BadRequest(new { message = "É necessário informar ao menos 1 item no pedido." });

            var itens = new List<PedidoItem>();

            // 🔸 Valida os produtos no serviço de estoque
            foreach (var itemDto in pedidoDto.Itens)
            {
                var produto = await _estoqueClient.GetProdutoAsync(itemDto.ProdutoId);
                if (produto == null)
                    return NotFound(new { message = $"Produto {itemDto.ProdutoId} não encontrado no estoque." });

                if (produto.Quantidade < itemDto.Quantidade)
                    return BadRequest(new { message = $"Estoque insuficiente para ProdutoId {itemDto.ProdutoId}" });

                itens.Add(new PedidoItem
                {
                    ProdutoId = itemDto.ProdutoId,
                    Quantidade = itemDto.Quantidade,
                    ValorTotal = itemDto.Quantidade * produto.Preco
                });
            }

            // 🔸 Cria e persiste o pedido
            var pedido = new Pedido
            {
                ClienteNome = pedidoDto.ClienteNome.Trim(),
                Itens = itens,
                DataPedido = DateTime.UtcNow
            };

            _context.Pedidos.Add(pedido);
            await _context.SaveChangesAsync();

            // 🔸 Prepara mensagem para fila
            var pedidoMessage = new PedidoMessage
            {
                PedidoId = pedido.Id,
                ClienteNome = pedido.ClienteNome,
                Itens = pedido.Itens.Select(i => new PedidoItemMessage
                {
                    ProdutoId = i.ProdutoId,
                    Quantidade = i.Quantidade
                }).ToList()
            };

            // 🔸 Envia mensagem para RabbitMQ (já faz log detalhado)
            _rabbitMqService.EnviarEventoPedido(pedidoMessage, "CRIADO");

            // 🔸 Retorna pedido completo
            var pedidoCompleto = await _context.Pedidos
                .Include(p => p.Itens)
                .FirstOrDefaultAsync(p => p.Id == pedido.Id);

            return CreatedAtAction(nameof(GetPedido), new { id = pedido.Id }, pedidoCompleto);
        }

        // 🔹 Atualização de pedido existente
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdatePedido(int id, [FromBody] PedidoUpdateDto pedidoDto)
        {
            if (pedidoDto == null)
                return BadRequest(new { message = "Pedido inválido." });

            var existente = await _context.Pedidos
                .Include(p => p.Itens)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (existente == null)
                return NotFound(new { message = $"Pedido {id} não encontrado." });

            existente.ClienteNome = pedidoDto.ClienteNome?.Trim() ?? existente.ClienteNome;
            existente.Itens.Clear();

            // 🔸 Atualiza itens com validação no estoque
            if (pedidoDto.Itens != null)
            {
                foreach (var itemDto in pedidoDto.Itens)
                {
                    var produto = await _estoqueClient.GetProdutoAsync(itemDto.ProdutoId);
                    if (produto == null)
                        return NotFound(new { message = $"Produto {itemDto.ProdutoId} não encontrado no estoque." });

                    if (produto.Quantidade < itemDto.Quantidade)
                        return BadRequest(new { message = $"Estoque insuficiente para ProdutoId {itemDto.ProdutoId}" });

                    existente.Itens.Add(new PedidoItem
                    {
                        ProdutoId = itemDto.ProdutoId,
                        Quantidade = itemDto.Quantidade,
                        ValorTotal = itemDto.Quantidade * produto.Preco
                    });
                }
            }

            await _context.SaveChangesAsync();

            var pedidoMessage = new PedidoMessage
            {
                PedidoId = existente.Id,
                ClienteNome = existente.ClienteNome,
                Itens = existente.Itens.Select(i => new PedidoItemMessage
                {
                    ProdutoId = i.ProdutoId,
                    Quantidade = i.Quantidade
                }).ToList()
            };

            // 🔸 Envia atualização para RabbitMQ (já faz log detalhado)
            _rabbitMqService.EnviarEventoPedido(pedidoMessage, "ATUALIZADO");

            return NoContent();
        }

        // 🔹 Exclusão de pedido
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeletePedido(int id)
        {
            var pedido = await _context.Pedidos
                .Include(p => p.Itens)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (pedido == null)
                return NotFound(new { message = $"Pedido {id} não encontrado." });

            _context.PedidoItens.RemoveRange(pedido.Itens);
            _context.Pedidos.Remove(pedido);
            await _context.SaveChangesAsync();

            var pedidoMessage = new PedidoMessage
            {
                PedidoId = pedido.Id,
                ClienteNome = pedido.ClienteNome,
                Itens = new List<PedidoItemMessage>() // exclusão → itens vazios
            };

            // 🔸 Envia exclusão para RabbitMQ (já faz log detalhado)
            _rabbitMqService.EnviarEventoPedido(pedidoMessage, "DELETADO");

            return NoContent();
        }

        // 🔹 Reenvio manual para RabbitMQ
        [HttpPost("reenviar-rabbit/{id:int}")]
        public async Task<IActionResult> ReenviarPedidoRabbit(int id)
        {
            var pedido = await _context.Pedidos
                .Include(p => p.Itens)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (pedido == null)
                return NotFound(new { message = $"Pedido {id} não encontrado." });

            var pedidoMessage = new PedidoMessage
            {
                PedidoId = pedido.Id,
                ClienteNome = pedido.ClienteNome,
                Itens = pedido.Itens.Select(i => new PedidoItemMessage
                {
                    ProdutoId = i.ProdutoId,
                    Quantidade = i.Quantidade
                }).ToList()
            };

            // 🔸 Reenvio para RabbitMQ (já faz log detalhado)
            _rabbitMqService.EnviarEventoPedido(pedidoMessage, "REENVIADO");

            return Ok(new { message = $"Pedido {pedido.Id} reenviado para RabbitMQ." });
        }
    }
}












































