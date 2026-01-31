using System.ComponentModel.DataAnnotations;

namespace Bipins.AI.Guardian.Models;

public class ChatRequestModel
{
    [Required(ErrorMessage = "Message is required")]
    [MinLength(1, ErrorMessage = "Message cannot be empty")]
    [MaxLength(1000, ErrorMessage = "Message cannot exceed 1000 characters")]
    public string Message { get; set; } = string.Empty;
}
