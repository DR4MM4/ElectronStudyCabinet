using System.ComponentModel.DataAnnotations;

namespace ElectronStudyCabinet.Models;

public class AnswerOption
{
    public int Id { get; set; }
    [Required, StringLength(500)]
    public string Text { get; set; } = "";
    public bool IsCorrect { get; set; }
    public int TestQuestionId { get; set; }
    public TestQuestion? TestQuestion { get; set; }
}
