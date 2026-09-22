using System.ComponentModel.DataAnnotations;

namespace SUI.NotificationService.Mesh.Configuration;

public class NhsMeshConfig
{
    public const string SectionName = "NhsMeshConfig";

    [Required]
    [Url]
    public required string MailboxBaseUrl { get; set; }

    [Required]
    public required string SharedKey { get; set; }

    [Required]
    public required string MailboxId { get; set; }

    [Required]
    public required string MailboxPassword { get; set; }
}
