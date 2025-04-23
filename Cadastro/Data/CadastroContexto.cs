using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Cadastro.Data
{
    public class CadastroContexto : DbContext
    {
        public CadastroContexto(DbContextOptions<CadastroContexto> options) : base(options) { }

        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Endereco> Enderecos { get; set; }
        public DbSet<LogErro> LogsErro { get; set; }
        public DbSet<Funcionarios> Funcionarios { get; set; }

        public DbSet<LogSucesso> LogsSucesso { get; set; }

        public DbSet<LogPerformance> LogsPerformance { get; set; }

        public DbSet<EmailLog> EmailLogs { get; set; }

        public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }

        public DbSet<RefreshToken> RefreshTokens { get; set; }

        public DbSet<Produto> Produtos { get; set; }

        public DbSet<Cupom> Cupons { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Usuario>()
                .HasIndex(Usuario => Usuario.CPF)
                .IsUnique();
            modelBuilder.Entity<Usuario>()
                .HasIndex(Usuario => Usuario.Email)
                .IsUnique();
            modelBuilder.Entity<Usuario>()
                .HasIndex(Usuario => Usuario.Telefone)
                .IsUnique();
            modelBuilder.Entity<Endereco>()
                .HasOne(Endereco => Endereco.Usuario)
                .WithOne(Usuario => Usuario.Endereco)
                .HasForeignKey<Endereco>(Endereco => Endereco.UsuarioId);

            modelBuilder.Entity<LogPerformance>()
            .HasOne(lp => lp.Usuario)
            .WithMany()
            .HasForeignKey(lp => lp.UsuarioId)
            .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Funcionarios>().HasData(SeedData.GetFuncionariosSeed());

            modelBuilder.Entity<Produto>().HasData(SeedData.GetProdutosSeed());

            modelBuilder.Entity<RefreshToken>()
            .HasIndex(rt => rt.TokenHash)
            .IsUnique();

            modelBuilder.Entity<Cupom>(entity =>
            {
                entity.Property(e => e.ValorTotal).HasPrecision(18, 2);
            });

            modelBuilder.Entity<Produto>(entity =>
            {
                entity.Property(e => e.valor).HasPrecision(18, 2);
            });
        }
    }
}
