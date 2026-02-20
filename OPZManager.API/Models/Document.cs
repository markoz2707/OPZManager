using System.ComponentModel.DataAnnotations;

namespace OPZManager.API.Models
{
    public class Document
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(255)]
        public string Filename { get; set; } = string.Empty;
        
        [Required]
        [StringLength(500)]
        public string FilePath { get; set; } = string.Empty;
        
        public int? ManufacturerId { get; set; }
        public int? TypeId { get; set; }
        public int? ModelId { get; set; }
        
        public DateTime UploadDate { get; set; } = DateTime.UtcNow;
        public DateTime? IndexedDate { get; set; }
        
        [StringLength(50)]
        public string Status { get; set; } = "Uploaded"; // Uploaded, Indexed, Failed
        
        // Navigation properties
        public virtual Manufacturer? Manufacturer { get; set; }
        public virtual EquipmentType? Type { get; set; }
        public virtual EquipmentModel? Model { get; set; }
        public virtual ICollection<DocumentSpec> DocumentSpecs { get; set; } = new List<DocumentSpec>();
    }
}
