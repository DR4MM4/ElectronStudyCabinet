using System.ComponentModel.DataAnnotations;

namespace ElectronStudyCabinet.Models;

public class TestQuestion
{
    public int Id { get; set; }
    [Required, StringLength(1000)]
    public string Text { get; set; } = "";
    public int LectureId { get; set; }
    public Lecture? Lecture { get; set; }
    public List<AnswerOption> Options { get; set; } = new();
}
