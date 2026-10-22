using ElectronStudyCabinet.Models;
using ElectronStudyCabinet.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ElectronStudyCabinet.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly SignInManager<ApplicationUser> _signIn;

    public AccountController(UserManager<ApplicationUser> users, SignInManager<ApplicationUser> signIn)
    {
        _users = users;
        _signIn = signIn;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null) => View(new LoginViewModel { ReturnUrl = returnUrl });


    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var result = await _signIn.PasswordSignInAsync(
            model.UserName,
            model.Password,
            model.RememberMe,
            false);

        if (!result.Succeeded)
        {
            ModelState.AddModelError("", "Неверный логин или пароль.");
            return View(model);
        }

        // Получаем пользователя и его актуальные роли через Identity
        var user = await _users.FindByNameAsync(model.UserName);

        if (user == null)
        {
            await _signIn.SignOutAsync();
            ModelState.AddModelError("", "Не удалось определить пользователя.");
            return View(model);
        }

        if (Url.IsLocalUrl(model.ReturnUrl))
            return Redirect(model.ReturnUrl!);

        var roles = await _users.GetRolesAsync(user);

        if (roles.Contains("Teacher"))
            return RedirectToAction("Index", "Teacher");

        if (roles.Contains("Student"))
            return RedirectToAction("Index", "Student");

        await _signIn.SignOutAsync();
        return RedirectToAction("AccessDenied");
    }


    [HttpGet]
    public IActionResult Register() => View(new RegisterViewModel());


    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = new ApplicationUser
        {
            UserName = model.UserName,
            FullName = model.FullName
        };

        var result = await _users.CreateAsync(user, model.Password);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);

            return View(model);
        }

        // Новая регистрация всегда создаёт обычного студента
        var roleResult = await _users.AddToRoleAsync(user, "Student");

        if (!roleResult.Succeeded)
        {
            await _users.DeleteAsync(user);

            ModelState.AddModelError(
                "",
                "Не удалось назначить роль студента. Проверьте настройки ролей.");

            return View(model);
        }

        await _signIn.SignInAsync(user, false);

        return RedirectToAction("Index", "Student");
    }


    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signIn.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    public IActionResult AccessDenied() => View();
}
