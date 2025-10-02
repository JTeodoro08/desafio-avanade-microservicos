using Microsoft.EntityFrameworkCore;
using VendasService.Models;

namespace VendasService.Data
{
    public class VendasContext : DbContext
    {
        public VendasContext(DbContextOptions<VendasContext> options) : base(options) { }

        // Tabelas
        public virtual DbSet<Pedido> Pedidos { get; set; }
        public virtual DbSet<PedidoItem> PedidoItens { get; set; }
        // Caso queira persistir Produto, basta reativar:
        // public virtual DbSet<Produto> Produtos { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Relacionamento Pedido -> PedidoItens
            modelBuilder.Entity<Pedido>()
                .HasMany(p => p.Itens)
                .WithOne(pi => pi.Pedido)
                .HasForeignKey(pi => pi.PedidoId)
                .OnDelete(DeleteBehavior.Cascade);

            // PedidoItem -> ProdutoId (somente referência)
            modelBuilder.Entity<PedidoItem>()
                .Property(pi => pi.ProdutoId)
                .IsRequired();

            // Precisão para valor total
            modelBuilder.Entity<PedidoItem>()
                .Property(pi => pi.ValorTotal)
                .HasPrecision(18, 2);

            // Caso Produto seja ativado, definir precisão para o preço
            modelBuilder.Entity<Produto>()
                .Property(p => p.Preco)
                .HasPrecision(18, 2);

            // ============================
            // 🔹 Ignorar RowVersion se existir
            // ============================
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                var property = entityType.FindProperty("RowVersion");
                if (property != null)
                {
                    modelBuilder.Entity(entityType.ClrType)
                                .Ignore("RowVersion");
                }
            }
        }
    }
}







