using EstoqueService.Data;
using EstoqueService.Models;
using EstoqueService.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace EstoqueService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProdutosController : ControllerBase
    {
        private readonly EstoqueContext _context;
        private readonly IRabbitMqProducerService _rabbitMq;
        private readonly ILogger<ProdutosController> _logger;

        public ProdutosController(
            EstoqueContext context,
            IRabbitMqProducerService rabbitMq,
            ILogger<ProdutosController> logger)
        {
            _context = context;
            _rabbitMq = rabbitMq;
            _logger = logger;
        }

        // =====================
        // GETs
        // =====================

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Produto>>> GetProdutos()
        {
            return await _context.Produtos.ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Produto>> GetProduto(int id)
        {
            var produto = await _context.Produtos.FindAsync(id);
            if (produto == null) return NotFound();
            return produto;
        }

        [HttpGet("{id}/disponibilidade/{quantidade}")]
        public async Task<ActionResult<bool>> VerificarDisponibilidade(int id, int quantidade)
        {
            var produto = await _context.Produtos.FindAsync(id);
            if (produto == null) return NotFound();

            return Ok(produto.Quantidade >= quantidade);
        }

        // =====================
        // POST (CRIAR)
        // =====================

        [HttpPost]
        [Authorize]
        public async Task<ActionResult<Produto>> CreateProduto([FromBody] Produto produto)
        {
            if (produto == null)
                return BadRequest("Produto inválido.");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                _context.Produtos.Add(produto);
                await _context.SaveChangesAsync();

                // Envia evento via RabbitMQ
                try
                {
                    await _rabbitMq.EnviarProdutoCriadoAsync(MapToMessage(produto));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Falha ao enviar evento de criação para RabbitMQ.");
                }

                return CreatedAtAction(nameof(GetProduto), new { id = produto.Id }, produto);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Erro ao salvar produto no banco de dados.");
                return StatusCode(500, "Erro interno ao salvar produto.");
            }
        }

        // =====================
        // PUT (ATUALIZAR)
        // =====================

        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateProduto(int id, [FromBody] Produto produto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (produto.Id != 0 && produto.Id != id)
                return BadRequest();

            var existente = await _context.Produtos.FindAsync(id);
            if (existente == null) return NotFound();

            existente.Nome = produto.Nome;
            existente.Descricao = produto.Descricao;
            existente.Preco = produto.Preco;
            existente.Quantidade = produto.Quantidade;

            try
            {
                await _context.SaveChangesAsync();

                // Envia evento via RabbitMQ
                try
                {
                    await _rabbitMq.EnviarProdutoAtualizadoAsync(MapToMessage(existente));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Falha ao enviar evento de atualização para RabbitMQ.");
                }

                return NoContent();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Erro ao atualizar produto no banco de dados.");
                return StatusCode(500, "Erro interno ao atualizar produto.");
            }
        }

        // =====================
        // DELETE
        // =====================

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteProduto(int id)
        {
            var produto = await _context.Produtos.FindAsync(id);
            if (produto == null) return NotFound();

            _context.Produtos.Remove(produto);

            try
            {
                await _context.SaveChangesAsync();

                // Envia evento via RabbitMQ (só Id é necessário)
                try
                {
                    await _rabbitMq.EnviarProdutoRemovidoAsync(produto.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Falha ao enviar evento de remoção para RabbitMQ.");
                }

                return NoContent();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Erro ao remover produto do banco de dados.");
                return StatusCode(500, "Erro interno ao remover produto.");
            }
        }

        // =====================
        // MAPEAMENTO
        // =====================

        private ProdutoMessage MapToMessage(Produto produto)
        {
            return new ProdutoMessage
            {
                Id = produto.Id,
                Nome = produto.Nome,
                Descricao = produto.Descricao,
                Preco = produto.Preco,
                Quantidade = produto.Quantidade
            };
        }
    }
}













