namespace ElectronStudyCabinet.Models;

public class TestAttempt
{
    public int Id { get; set; }
    public string StudentId { get; set; } = "";
    public ApplicationUser? Student { get; set; }
    public int LectureId { get; set; }
    public Lecture? Lecture { get; set; }
    public DateTime AttemptedAt { get; set; } = DateTime.UtcNow;
    public int CorrectAnswers { get; set; }
    public int TotalQuestions { get; set; }
    public int Percent { get; set; }
    public bool Passed { get; set; }
}
