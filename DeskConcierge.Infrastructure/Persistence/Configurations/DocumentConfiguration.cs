using DeskConcierge.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeskConcierge.Infrastructure.Persistence.Configurations;

public sealed class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        builder.HasKey(d => d.Id);

        builder.Property(d => d.OriginalPath)
            .IsRequired();

        builder.Property(d => d.CreatedAt)
            .IsRequired();

        builder.Property(d => d.ContentHash)
            .IsRequired()
            .HasMaxLength(64);

        builder.HasIndex(d => d.ContentHash)
            .IsUnique();

        builder.Property(d => d.OcrText);
        builder.Property(d => d.OcrConfidence);

        builder.Property(d => d.Iban);
        builder.Property(d => d.IbanConfidence);
        builder.Property(d => d.Date);
        builder.Property(d => d.DateConfidence);
        builder.Property(d => d.Amount);
        builder.Property(d => d.AmountConfidence);
        builder.Property(d => d.InvoiceNumber);
        builder.Property(d => d.InvoiceNumberConfidence);
    }
}
