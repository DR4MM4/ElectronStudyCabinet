using System.ComponentModel.DataAnnotations;

namespace ElectronStudyCabinet.Models;

public class Lecture
{
    public int Id { get; set; }
    [Required, StringLength(180)]
    public string Title { get; set; } = "";
    [StringLength(2000)]
    public string Description { get; set; } = "";
    public string Content { get; set; } = "";
    public string? FilePath { get; set; }
    public int Order { get; set; } = 1;
    public int TopicId { get; set; }
    public Topic? Topic { get; set; }
    public List<ControlQuestion> ControlQuestions { get; set; } = new();
    public List<TestQuestion> TestQuestions { get; set; } = new();
}
