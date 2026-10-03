using Microsoft.EntityFrameworkCore;
using ProjetoCloud.Api.Models;

namespace ProjetoCloud.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Tarefa> Tarefas => Set<Tarefa>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Tarefa>(e =>
        {
            e.ToTable("tarefas");
            e.Property(t => t.Titulo).HasMaxLength(200).IsRequired();
        });
    }
}
