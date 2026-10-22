using System.ComponentModel.DataAnnotations;

namespace ElectronStudyCabinet.Models;

public class Topic
{
    public int Id { get; set; }
    [Required, StringLength(160)]
    public string Title { get; set; } = "";
    public int Order { get; set; } = 1;
    public int CourseId { get; set; }
    public Course? Course { get; set; }
    public List<Lecture> Lectures { get; set; } = new();
}
