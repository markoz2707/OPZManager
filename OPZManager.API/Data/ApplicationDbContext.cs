using Microsoft.EntityFrameworkCore;
using OPZManager.API.Models;

namespace OPZManager.API.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        // DbSets
        public DbSet<User> Users { get; set; }
        public DbSet<UserSession> UserSessions { get; set; }
        public DbSet<Manufacturer> Manufacturers { get; set; }
        public DbSet<EquipmentType> EquipmentTypes { get; set; }
        public DbSet<EquipmentModel> EquipmentModels { get; set; }
        public DbSet<Document> Documents { get; set; }
        public DbSet<DocumentSpec> DocumentSpecs { get; set; }
        public DbSet<OPZDocument> OPZDocuments { get; set; }
        public DbSet<OPZRequirement> OPZRequirements { get; set; }
        public DbSet<EquipmentMatch> EquipmentMatches { get; set; }
        public DbSet<TrainingData> TrainingData { get; set; }
        public DbSet<OPZVerificationResult> OPZVerificationResults { get; set; }
        public DbSet<LeadCapture> LeadCaptures { get; set; }
        public DbSet<KnowledgeDocument> KnowledgeDocuments { get; set; }
        public DbSet<KnowledgeChunk> KnowledgeChunks { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.HasPostgresExtension("vector");

            // User configuration
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Username).IsUnique();
                entity.HasIndex(e => e.Email).IsUnique();
                entity.Property(e => e.Role).HasDefaultValue("User");
            });

            // UserSession configuration
            modelBuilder.Entity<UserSession>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.UserId);
                entity.HasOne(e => e.User)
                    .WithMany(e => e.UserSessions)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Manufacturer configuration
            modelBuilder.Entity<Manufacturer>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Name).IsUnique();
            });

            // EquipmentType configuration
            modelBuilder.Entity<EquipmentType>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Name).IsUnique();
            });

            // EquipmentModel configuration
            modelBuilder.Entity<EquipmentModel>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.ManufacturerId);
                entity.HasIndex(e => e.TypeId);
                entity.HasOne(e => e.Manufacturer)
                    .WithMany(e => e.EquipmentModels)
                    .HasForeignKey(e => e.ManufacturerId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Type)
                    .WithMany(e => e.EquipmentModels)
                    .HasForeignKey(e => e.TypeId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.Ignore(e => e.Specifications); // Ignore the helper property
            });

            // Document configuration
            modelBuilder.Entity<Document>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.Manufacturer)
                    .WithMany(e => e.Documents)
                    .HasForeignKey(e => e.ManufacturerId)
                    .OnDelete(DeleteBehavior.SetNull);
                entity.HasOne(e => e.Type)
                    .WithMany(e => e.Documents)
                    .HasForeignKey(e => e.TypeId)
                    .OnDelete(DeleteBehavior.SetNull);
                entity.HasOne(e => e.Model)
                    .WithMany(e => e.Documents)
                    .HasForeignKey(e => e.ModelId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // DocumentSpec configuration
            modelBuilder.Entity<DocumentSpec>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.Document)
                    .WithMany(e => e.DocumentSpecs)
                    .HasForeignKey(e => e.DocumentId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // OPZDocument configuration
            modelBuilder.Entity<OPZDocument>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.AnonymousSessionId);
                entity.HasOne(e => e.User)
                    .WithMany(e => e.OPZDocuments)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // OPZRequirement configuration
            modelBuilder.Entity<OPZRequirement>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.OPZId);
                entity.HasOne(e => e.OPZDocument)
                    .WithMany(e => e.OPZRequirements)
                    .HasForeignKey(e => e.OPZId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.Ignore(e => e.ExtractedSpecs); // Ignore the helper property
            });

            // EquipmentMatch configuration
            modelBuilder.Entity<EquipmentMatch>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.OPZId);
                entity.HasIndex(e => e.ModelId);
                entity.HasOne(e => e.OPZDocument)
                    .WithMany(e => e.EquipmentMatches)
                    .HasForeignKey(e => e.OPZId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.EquipmentModel)
                    .WithMany(e => e.EquipmentMatches)
                    .HasForeignKey(e => e.ModelId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.Property(e => e.MatchScore).HasPrecision(5, 4);
            });

            // TrainingData configuration
            modelBuilder.Entity<TrainingData>(entity =>
            {
                entity.HasKey(e => e.Id);
            });

            // OPZVerificationResult configuration
            modelBuilder.Entity<OPZVerificationResult>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.OPZDocumentId).IsUnique();
                entity.HasOne(e => e.OPZDocument)
                    .WithOne(e => e.VerificationResult)
                    .HasForeignKey<OPZVerificationResult>(e => e.OPZDocumentId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // LeadCapture configuration
            modelBuilder.Entity<LeadCapture>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.AnonymousSessionId);
                entity.HasIndex(e => e.DownloadToken);
                entity.HasIndex(e => e.Email);
                entity.HasOne(e => e.OPZDocument)
                    .WithMany(e => e.LeadCaptures)
                    .HasForeignKey(e => e.OPZDocumentId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // KnowledgeDocument configuration
            modelBuilder.Entity<KnowledgeDocument>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.EquipmentModelId);
                entity.HasIndex(e => e.Status);
                entity.HasOne(e => e.EquipmentModel)
                    .WithMany(e => e.KnowledgeDocuments)
                    .HasForeignKey(e => e.EquipmentModelId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // KnowledgeChunk configuration
            modelBuilder.Entity<KnowledgeChunk>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.KnowledgeDocumentId);
                entity.HasOne(e => e.KnowledgeDocument)
                    .WithMany(e => e.Chunks)
                    .HasForeignKey(e => e.KnowledgeDocumentId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.Property(e => e.Embedding)
                    .HasColumnType("vector(1024)");
            });

            // Seed data
            SeedData(modelBuilder);
        }

        private static readonly DateTime SeedDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        private void SeedData(ModelBuilder modelBuilder)
        {
            // Seed default manufacturers
            modelBuilder.Entity<Manufacturer>().HasData(
                new Manufacturer { Id = 1, Name = "DELL", Description = "Dell Technologies", CreatedAt = SeedDate, UpdatedAt = SeedDate },
                new Manufacturer { Id = 2, Name = "HPE", Description = "Hewlett Packard Enterprise", CreatedAt = SeedDate, UpdatedAt = SeedDate },
                new Manufacturer { Id = 3, Name = "IBM", Description = "International Business Machines", CreatedAt = SeedDate, UpdatedAt = SeedDate }
            );

            // Seed default equipment types
            modelBuilder.Entity<EquipmentType>().HasData(
                new EquipmentType { Id = 1, Name = "Macierze dyskowe", Description = "Storage Arrays", CreatedAt = SeedDate, UpdatedAt = SeedDate },
                new EquipmentType { Id = 2, Name = "Serwery", Description = "Servers", CreatedAt = SeedDate, UpdatedAt = SeedDate },
                new EquipmentType { Id = 3, Name = "Przełączniki sieciowe", Description = "Network Switches", CreatedAt = SeedDate, UpdatedAt = SeedDate }
            );

            // Seed default admin user (password: admin123)
            modelBuilder.Entity<User>().HasData(
                new User
                {
                    Id = 1,
                    Username = "admin",
                    Email = "admin@opzmanager.local",
                    PasswordHash = "$2a$11$0Nq64.illqaehtCyPXTFL.UcaNhym9jJEeru5MRnXVtacxQw28/4m",
                    Role = "Admin",
                    CreatedAt = SeedDate,
                    UpdatedAt = SeedDate
                }
            );
        }
    }
}
