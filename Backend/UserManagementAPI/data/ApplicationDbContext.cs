using Microsoft.EntityFrameworkCore;
using UserManagementAPI.Models;
using Pgvector;

namespace UserManagementAPI.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }
        public DbSet<Document> Documents { get; set; }
        public DbSet<DocumentChunk> DocumentChunks { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("users");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id)
                    .HasColumnName("id");

                entity.Property(e => e.Username)
                    .HasColumnName("username")
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(e => e.PasswordHash)
                    .HasColumnName("password_hash")
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(e => e.IsDeleted)
                    .HasColumnName("is_deleted")
                    .IsRequired()
                    .HasDefaultValue(false);

                entity.HasIndex(e => e.Username)
                    .IsUnique();
            });

            modelBuilder.Entity<ChatMessage>(entity =>
            {
                entity.ToTable("chat_messages");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id)
                    .HasColumnName("id");

                entity.Property(e => e.UserId)
                    .HasColumnName("user_id")
                    .IsRequired();

                entity.Property(e => e.Role)
                    .HasColumnName("role")
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(e => e.Content)
                    .HasColumnName("content")
                    .IsRequired();

                entity.Property(e => e.CreatedAt)
                    .HasColumnName("created_at")
                    .IsRequired();

                entity.HasIndex(e => e.UserId);
            });

            modelBuilder.Entity<Document>(entity =>
            {
                entity.ToTable("documents");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id)
                    .HasColumnName("id");

                entity.Property(e => e.FileName)
                    .HasColumnName("file_name")
                    .IsRequired()
                    .HasMaxLength(500);

                entity.Property(e => e.FileType)
                    .HasColumnName("file_type")
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.OriginalText)
                    .HasColumnName("original_text")
                    .IsRequired();

                entity.Property(e => e.UploadedAt)
                    .HasColumnName("uploaded_at")
                    .IsRequired();

                entity.Property(e => e.UploadedByUserId)
                    .HasColumnName("uploaded_by_user_id");

                entity.Property(e => e.IsDeleted)
                    .HasColumnName("is_deleted")
                    .IsRequired()
                    .HasDefaultValue(false);

                entity.Property(e => e.DeletedAt)
                    .HasColumnName("deleted_at");

                entity.HasIndex(e => e.IsDeleted);
                entity.HasIndex(e => e.FileType);
            });

            modelBuilder.Entity<DocumentChunk>(entity =>
            {
                entity.ToTable("document_chunks");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id)
                    .HasColumnName("id");

                entity.Property(e => e.DocumentId)
                    .HasColumnName("document_id")
                    .IsRequired();

                entity.Property(e => e.Content)
                    .HasColumnName("content")
                    .IsRequired();

                entity.Property(e => e.ChunkIndex)
                    .HasColumnName("chunk_index")
                    .IsRequired();

                entity.Property(e => e.Embedding)
                    .HasColumnName("embedding")
                    .HasColumnType("real[]"); // Store as PostgreSQL float array

                entity.Property(e => e.CreatedAt)
                    .HasColumnName("created_at")
                    .IsRequired();

                entity.Property(e => e.IsDeleted)
                    .HasColumnName("is_deleted")
                    .IsRequired()
                    .HasDefaultValue(false);

                entity.HasIndex(e => e.DocumentId);
                entity.HasIndex(e => e.IsDeleted);
            });
        }
    }
}
