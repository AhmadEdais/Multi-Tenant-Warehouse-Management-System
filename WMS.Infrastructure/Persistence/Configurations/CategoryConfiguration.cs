namespace WMS.Infrastructure.Persistence.Configurations
{
    public class CategoryConfiguration : IEntityTypeConfiguration<Category>
    {
        public void Configure(EntityTypeBuilder<Category> builder)
        {
            builder.ToTable("Categories");

            builder.HasKey(c => c.Id);

            builder.Property(c => c.Name)
                .HasMaxLength(200)
                .IsRequired();

            // The Self-Referencing Tree Relationship
            builder.HasOne(c => c.ParentCategory)
                .WithMany(c => c.Children)
                .HasForeignKey(c => c.ParentCategoryId)
                .OnDelete(DeleteBehavior.Restrict); // To prevent cascading cycles

            // Indexes
            builder.HasIndex(c => c.TenantId).HasDatabaseName("IX_Categories_TenantId");
            builder.HasIndex(c => c.ParentCategoryId).HasDatabaseName("IX_Categories_ParentCategoryId");

        }
    }
}