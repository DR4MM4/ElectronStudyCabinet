
using System.ComponentModel.DataAnnotations;

namespace ElectronStudyCabinet.Models;

public class TestQuestionEditViewModel
{
    public int Id { get; set; }

    public int LectureId { get; set; }

    [Required(ErrorMessage = "Введите текст вопроса")]
    [StringLength(1000)]
    [Display(Name = "Текст вопроса")]
    public string Text { get; set; } = "";

    [Required]
    [Display(Name = "Вариант ответа №1")]
    public string Option1 { get; set; } = "";

    [Required]
    [Display(Name = "Вариант ответа №2")]
    public string Option2 { get; set; } = "";

    [Required]
    [Display(Name = "Вариант ответа №3")]
    public string Option3 { get; set; } = "";

    [Range(1, 3, ErrorMessage = "Выберите правильный вариант")]
    [Display(Name = "Правильный ответ")]
    public int CorrectOption { get; set; } = 1;

    public string OptionA
    {
        get => Option1;
        set => Option1 = value;
    }

    public string OptionB
    {
        get => Option2;
        set => Option2 = value;
    }

    public string OptionC
    {
        get => Option3;
        set => Option3 = value;
    }
}
