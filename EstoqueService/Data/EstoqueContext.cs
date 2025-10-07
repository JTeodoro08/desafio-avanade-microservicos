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
                entity.Property(p => p.Preco)
                      .HasColumnType("decimal(18,2)")
                      .IsRequired();

                entity.Property(p => p.Nome)
                      .HasMaxLength(100)
                      .IsRequired();

                entity.Property(p => p.Descricao)
                      .HasMaxLength(500);

                // 🔸 (Opcional) Índice único no nome
                entity.HasIndex(p => p.Nome)
                      .IsUnique()
                      .HasDatabaseName("IX_Produto_Nome");
            });
        }
    }
}



