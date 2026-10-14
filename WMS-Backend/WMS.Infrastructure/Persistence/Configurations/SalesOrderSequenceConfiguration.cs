namespace WMS.Infrastructure.Persistence.Configurations;

internal sealed class SalesOrderSequenceConfiguration : IEntityTypeConfiguration<SalesOrderSequence>
{
    public void Configure(EntityTypeBuilder<SalesOrderSequence> builder)
    {
        builder.ToTable("SalesOrderSequences", table =>
            table.HasCheckConstraint("CK_SalesOrderSequences_LastNumber", "[LastNumber] >= 0"));
        builder.HasKey(sequence => sequence.TenantId).HasName("PK_SalesOrderSequences");
        builder.Property(sequence => sequence.TenantId).ValueGeneratedNever();
        builder.Property(sequence => sequence.LastNumber).HasColumnType("bigint").IsRequired();
        builder.HasOne<Tenant>().WithMany().HasForeignKey(sequence => sequence.TenantId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_SalesOrderSequences_Tenants");
    }
}
