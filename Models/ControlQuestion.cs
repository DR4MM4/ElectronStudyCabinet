using System.ComponentModel.DataAnnotations;

namespace ElectronStudyCabinet.Models;

public class ControlQuestion
{
    public int Id { get; set; }
    [Required, StringLength(1000)]
    public string Text { get; set; } = "";
    [Required, StringLength(300)]
    public string CorrectAnswer { get; set; } = "";
    public int LectureId { get; set; }
    public Lecture? Lecture { get; set; }
}
