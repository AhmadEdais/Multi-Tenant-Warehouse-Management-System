namespace WMS.Infrastructure.Persistence.Configurations;

internal sealed class PurchaseOrderSequenceConfiguration : IEntityTypeConfiguration<PurchaseOrderSequence>
{
    public void Configure(EntityTypeBuilder<PurchaseOrderSequence> builder)
    {
        builder.ToTable("PurchaseOrderSequences", table =>
            table.HasCheckConstraint("CK_PurchaseOrderSequences_LastNumber", "[LastNumber] >= 0"));
        builder.HasKey(x => x.TenantId).HasName("PK_PurchaseOrderSequences");
        builder.Property(x => x.TenantId).ValueGeneratedNever();
        builder.Property(x => x.LastNumber).HasColumnType("bigint").IsRequired();
        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_PurchaseOrderSequences_Tenants");
    }
}
