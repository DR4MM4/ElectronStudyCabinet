namespace ElectronStudyCabinet.Models;

public class LectureProgress
{
    public int Id { get; set; }
    public string StudentId { get; set; } = "";
    public ApplicationUser? Student { get; set; }
    public int LectureId { get; set; }
    public Lecture? Lecture { get; set; }
    public bool ControlQuestionsPassed { get; set; }
    public bool TestPassed { get; set; }
    public DateTime? CompletedAt { get; set; }
}
