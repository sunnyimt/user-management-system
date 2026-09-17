using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace UserManagementAPI.Models
{
    public class Document
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string FileName { get; set; }

        [Required]
        public string FileType { get; set; } // "pdf", "system-docs", "api-docs", etc.

        [Required]
        public string OriginalText { get; set; }

        [Required]
        public DateTime UploadedAt { get; set; }

        public int? UploadedByUserId { get; set; }

        [ForeignKey("UploadedByUserId")]
        public User? UploadedByUser { get; set; }

        public bool IsDeleted { get; set; } = false;

        public DateTime? DeletedAt { get; set; }
    }

    public class DocumentChunk
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int DocumentId { get; set; }

        [ForeignKey("DocumentId")]
        public Document Document { get; set; }

        [Required]
        public string Content { get; set; }

        [Required]
        public int ChunkIndex { get; set; }

        // Store embeddings as a float array
        public float[] Embedding { get; set; } // Vector embeddings (1536 dimensions for Claude)

        [Required]
        public DateTime CreatedAt { get; set; }

        public bool IsDeleted { get; set; } = false;
    }
}
