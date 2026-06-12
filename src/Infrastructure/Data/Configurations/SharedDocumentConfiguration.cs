using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PairCode.Domain.Entities;

namespace PairCode.Infrastructure.Data.Configurations;

public class SharedDocumentConfiguration : IEntityTypeConfiguration<SharedDocument>
{
    public void Configure(EntityTypeBuilder<SharedDocument> builder)
    {
        builder.ToTable("SharedDocuments");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Content)
            .HasColumnType("text");

        builder.Property(x => x.Version)
            .IsRequired()
            .HasDefaultValue(1);

        builder.Property(x => x.UpdatedAt)
            .IsRequired();

        builder.HasOne(x => x.Room)
            .WithMany()
            .HasForeignKey(x => x.RoomId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.RoomId)
            .IsUnique();
    }
}
