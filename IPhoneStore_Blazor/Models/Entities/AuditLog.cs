using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IPhoneStore_Blazor.Models.Entities;

[Table("AuditLogs")]
public class AuditLog
{
    [Key]
    public int Id { get; set; }

    [StringLength(450)]
    public string? UserId { get; set; }

    [StringLength(256)]
    public string? UserName { get; set; }

    [Required]
    [StringLength(50)]
    public string Action { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string EntityName { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string RecordId { get; set; } = string.Empty;

    public string? OldValues { get; set; }

    public string? NewValues { get; set; }

    [StringLength(50)]
    public string? IpAddress { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.Now;

    // Navigation
    [ForeignKey("UserId")]
    public virtual ApplicationUser? User { get; set; }
}
