using EstoqueService.Data;
using EstoqueService.Models;
using EstoqueService.Services;
using EstoqueService.Helpers;
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
            var produtos = await _context.Produtos.ToListAsync();

            foreach (var p in produtos)
            {
                EstoqueLogger.LogProduto(_logger, p, "CONSULTA");
            }

            return produtos;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Produto>> GetProduto(int id)
        {
            var produto = await _context.Produtos.FindAsync(id);
            if (produto == null)
            {
                _logger.LogWarning("[ESTOQUE] ⚠️ Produto não encontrado | Id: {Id}", id);
                return NotFound();
            }

            EstoqueLogger.LogProduto(_logger, produto, "CONSULTA");

            return produto;
        }

        [HttpGet("{id}/disponibilidade/{quantidade}")]
        public async Task<ActionResult<bool>> VerificarDisponibilidade(int id, int quantidade)
        {
            var produto = await _context.Produtos.FindAsync(id);
            if (produto == null)
            {
                _logger.LogWarning("[ESTOQUE] ⚠️ Produto não encontrado ao verificar disponibilidade | Id: {Id}", id);
                return NotFound();
            }

            var disponivel = produto.Quantidade >= quantidade;

            _logger.LogInformation(
                "[ESTOQUE] 📊 Disponibilidade | Produto: {Nome} | Solicitado: {QtdSolicitada} | Em estoque: {QtdAtual} | Disponível: {Disponivel}",
                produto.Nome, quantidade, produto.Quantidade, disponivel
            );

            return Ok(disponivel);
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

                EstoqueLogger.LogProduto(_logger, produto, "CRIADO");

                try
                {
                    await _rabbitMq.EnviarProdutoCriadoAsync(produto);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "[ESTOQUE] ❌ Falha ao enviar evento 'PRODUTO_CRIADO' para RabbitMQ | Produto {Id} - {Nome}",
                        produto.Id, produto.Nome
                    );
                }

                return CreatedAtAction(nameof(GetProduto), new { id = produto.Id }, produto);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "[ESTOQUE] ❌ Erro ao salvar produto no banco de dados.");
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
                return BadRequest("O ID do produto não confere com o ID informado na URL.");

            var existente = await _context.Produtos.FindAsync(id);
            if (existente == null)
            {
                _logger.LogWarning("[ESTOQUE] ⚠️ Tentativa de atualização em produto inexistente | Id: {Id}", id);
                return NotFound();
            }

            existente.Nome = produto.Nome;
            existente.Descricao = produto.Descricao;
            existente.Preco = produto.Preco;
            existente.Quantidade = produto.Quantidade;

            try
            {
                await _context.SaveChangesAsync();

                EstoqueLogger.LogProduto(_logger, existente, "ATUALIZADO");

                try
                {
                    await _rabbitMq.EnviarProdutoAtualizadoAsync(existente);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "[ESTOQUE] ❌ Falha ao enviar evento 'PRODUTO_ATUALIZADO' para RabbitMQ | Produto {Id} - {Nome}",
                        existente.Id, existente.Nome
                    );
                }

                return NoContent();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "[ESTOQUE] ❌ Erro ao atualizar produto no banco de dados.");
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
            if (produto == null)
            {
                _logger.LogWarning("[ESTOQUE] ⚠️ Tentativa de exclusão de produto inexistente | Id: {Id}", id);
                return NotFound();
            }

            _context.Produtos.Remove(produto);

            try
            {
                await _context.SaveChangesAsync();

                EstoqueLogger.LogProduto(_logger, produto, "DELETADO");

                try
                {
                    await _rabbitMq.EnviarProdutoRemovidoAsync(produto.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "[ESTOQUE] ❌ Falha ao enviar evento 'PRODUTO_REMOVIDO' para RabbitMQ | Produto {Id} - {Nome}",
                        produto.Id, produto.Nome
                    );
                }

                return NoContent();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "[ESTOQUE] ❌ Erro ao remover produto do banco de dados.");
                return StatusCode(500, "Erro interno ao remover produto.");
            }
        }
    }
}


































