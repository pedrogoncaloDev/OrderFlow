using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderFlow.Domain.Entities;

namespace OrderFlow.Infrastructure.Persistence.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("customers");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();

        builder.Property(c => c.Name).IsRequired().HasMaxLength(200);
        builder.Property(c => c.Email).IsRequired().HasMaxLength(320);
        builder.Property(c => c.Phone).HasMaxLength(30);

        // Dono do cadastro. Restrict: um usuário com clientes não pode ser apagado por acidente.
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(c => c.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);

        // O mesmo e-mail pode existir para donos diferentes, mas não duas vezes para o mesmo dono.
        builder.HasIndex(c => new { c.OwnerId, c.Email }).IsUnique();

        // A listagem é sempre "os clientes do dono, por nome".
        builder.HasIndex(c => new { c.OwnerId, c.Name });
    }
}
