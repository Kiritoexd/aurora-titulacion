using Microsoft.EntityFrameworkCore;
using AURORA.Models;

namespace AURORA.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Tb_Usuario> Usuarios { get; set; }
        public DbSet<Tb_Libro> Libros { get; set; }
        public DbSet<Tb_UsuarioLibro> UsuarioLibros { get; set; }
        public DbSet<TopLibro> TopLibros { get; set; }
        public DbSet<Tb_Racha> Tb_Racha { get; set; }
        public DbSet<Tb_Racha> Rachas { get; set; }

        // ── NUEVO ────────────────────────────────────────────────
        public DbSet<Tb_Sesion> Sesiones { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Tb_Usuario>().ToTable("Tb_Usuario");
            modelBuilder.Entity<Tb_Libro>().ToTable("Tb_Libro");
            modelBuilder.Entity<Tb_UsuarioLibro>().ToTable("Tb_UsuarioLibro");

            // ── NUEVO ────────────────────────────────────────────
            modelBuilder.Entity<Tb_Sesion>().ToTable("Tb_Sesion");

            modelBuilder.Entity<Tb_Sesion>()
                .HasOne(s => s.UsuarioLibro)
                .WithMany()
                .HasForeignKey(s => s.UsuarioLibroId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
