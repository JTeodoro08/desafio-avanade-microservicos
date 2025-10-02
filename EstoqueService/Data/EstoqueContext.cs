using Microsoft.EntityFrameworkCore;
using EstoqueService.Models;

namespace EstoqueService.Data
{
    public class EstoqueContext : DbContext
    {
        public EstoqueContext(DbContextOptions<EstoqueContext> options)
            : base(options)
        {
        }

        // =====================
        // DbSets
        // =====================
        public DbSet<Produto> Produtos { get; set; } = null!;

        // =====================
        // Configurações de mapeamento
        // =====================
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 🔹 Configuração da entidade Produto
            modelBuilder.Entity<Produto>(entity =>
            {
                // Preço com precisão/escala para evitar warnings do EF
                entity.Property(p => p.Preco)
                      .HasColumnType("decimal(18,2)") 
                      .IsRequired();

                // Nome limitado a 100 caracteres (obrigatório)
                entity.Property(p => p.Nome)
                      .HasMaxLength(100)
                      .IsRequired();

                // Descrição até 500 caracteres (opcional)
                entity.Property(p => p.Descricao)
                      .HasMaxLength(500);
            });
        }
    }
}


