using Microsoft.EntityFrameworkCore;
using API_Persona.Models;

namespace API_Persona.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Persona> Personas { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configurar la entidad Persona
            modelBuilder.Entity<Persona>()
                .HasKey(p => p.Id);

            modelBuilder.Entity<Persona>()
                .Property(p => p.Nombre)
                .IsRequired();

            modelBuilder.Entity<Persona>()
                .Property(p => p.Apellido)
                .IsRequired();

            modelBuilder.Entity<Persona>()
                .Property(p => p.FechaNacimiento)
                .IsRequired();

            modelBuilder.Entity<Persona>()
                .Property(p => p.Email)
                .IsRequired();

            modelBuilder.Entity<Persona>()
                .Property(p => p.FechaRegistro)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        }
    }
}
