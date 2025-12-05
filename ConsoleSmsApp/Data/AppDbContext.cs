using Microsoft.EntityFrameworkCore;

namespace ConsoleSmsApp.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<DishEntity> Dishes => Set<DishEntity>();

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DishEntity>(entity =>
            {
                entity.ToTable("dishes");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
                entity.Property(e => e.ExternalId).HasColumnName("external_id");
                entity.Property(e => e.Article).HasColumnName("article");
                entity.Property(e => e.Name).HasColumnName("name");
                entity.Property(e => e.Price).HasColumnName("price");
                entity.Property(e => e.IsWeighted).HasColumnName("is_weighted");
                entity.Property(e => e.FullPath).HasColumnName("full_path");
            });
        }
    }
}
