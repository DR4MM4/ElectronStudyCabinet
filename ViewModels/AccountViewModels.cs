using System.ComponentModel.DataAnnotations;

namespace ElectronStudyCabinet.ViewModels;

public class LoginViewModel
{
    [Required, Display(Name = "Логин")]
    public string UserName { get; set; } = "";
    [Required, DataType(DataType.Password), Display(Name = "Пароль")]
    public string Password { get; set; } = "";
    [Display(Name = "Запомнить меня")]
    public bool RememberMe { get; set; }
    public string? ReturnUrl { get; set; }
}

public class RegisterViewModel
{
    [Required, StringLength(120), Display(Name = "ФИО")]
    public string FullName { get; set; } = "";
    [Required, StringLength(50, MinimumLength = 3), Display(Name = "Логин")]
    public string UserName { get; set; } = "";
    [Required, DataType(DataType.Password), MinLength(8), Display(Name = "Пароль (минимум 8 символов)")]
    public string Password { get; set; } = "";
    [Required, DataType(DataType.Password), Compare(nameof(Password)), Display(Name = "Повторите пароль")]
    public string ConfirmPassword { get; set; } = "";
}
