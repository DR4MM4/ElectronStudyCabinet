using System.ComponentModel.DataAnnotations;

namespace ElectronStudyCabinet.ViewModels;

public class LectureEditViewModel
{
    public int Id { get; set; }
    [Required, StringLength(180), Display(Name = "Название лекции")]
    public string Title { get; set; } = "";
    [StringLength(2000), Display(Name = "Краткое описание")]
    public string Description { get; set; } = "";
    [Display(Name = "Теоретический материал")]
    public string Content { get; set; } = "";
    [Range(1, 9999), Display(Name = "Порядок")]
    public int Order { get; set; } = 1;
    [Required]
    public int TopicId { get; set; }
    [Display(Name = "Файл лекции (PDF/DOCX)")]
    public IFormFile? Material { get; set; }
}

public class CourseEditViewModel
{
    public int Id { get; set; }
    [Required, StringLength(160), Display(Name = "Название дисциплины")]
    public string Title { get; set; } = "";
    [StringLength(1200), Display(Name = "Описание")]
    public string Description { get; set; } = "";
}

public class TopicEditViewModel
{
    public int Id { get; set; }
    [Required, StringLength(160), Display(Name = "Название темы")]
    public string Title { get; set; } = "";
    [Range(1, 9999), Display(Name = "Порядок")]
    public int Order { get; set; } = 1;
    [Required]
    public int CourseId { get; set; }
}

public class QuestionEditViewModel
{
    public int LectureId { get; set; }
    [Required, StringLength(1000), Display(Name = "Текст вопроса")]
    public string Text { get; set; } = "";
    [Required, StringLength(300), Display(Name = "Правильный ответ")]
    public string CorrectAnswer { get; set; } = "";
}

