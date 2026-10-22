using System.ComponentModel.DataAnnotations;

namespace ElectronStudyCabinet.Models;

public class Course
{
    public int Id { get; set; }
    [Required, StringLength(160)]
    public string Title { get; set; } = "";
    [StringLength(1200)]
    public string Description { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<Topic> Topics { get; set; } = new();
}
