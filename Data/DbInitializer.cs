using ElectronStudyCabinet.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ElectronStudyCabinet.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<ApplicationDbContext>();
        await db.Database.EnsureCreatedAsync();

        var roles = services.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in new[] { "Teacher", "Student" })
            if (!await roles.RoleExistsAsync(role))
                await roles.CreateAsync(new IdentityRole(role));

        var users = services.GetRequiredService<UserManager<ApplicationUser>>();
        await CreateUser(users, "teacher", "Teacher123!", "Преподаватель", "Teacher");
        await CreateUser(users, "student", "Student123!", "Тестовый студент", "Student");

        if (!await db.Courses.AnyAsync())
        {
            var course = new Course
            {
                Title = "Внедрение и поддержка компьютерных систем",
                Description = "Демонстрационная дисциплина для проверки электронного учебного кабинета.",
                Topics = new List<Topic>
                {
                    new Topic
                    {
                        Title = "Тестирование программного обеспечения", Order = 1,
                        Lectures = new List<Lecture>
                        {
                            new Lecture
                            {
                                Title = "Лекция 1. Основы тестирования", Order = 1,
                                Description = "Вводная лекция с контрольным вопросом и тестом.",
                                Content = "Тестирование программного обеспечения — процесс проверки программы и сравнения её фактического поведения с ожидаемым. Цель тестирования — выявлять дефекты и предоставлять информацию о качестве продукта.\n\nОсновные понятия: тест-кейс, тестовые данные, ожидаемый результат, фактический результат и дефект.",
                                ControlQuestions = new List<ControlQuestion>
                                {
                                    new ControlQuestion { Text = "Как называется процесс проверки программы на соответствие требованиям?", CorrectAnswer = "тестирование" }
                                },
                                TestQuestions = new List<TestQuestion>
                                {
                                    new TestQuestion { Text = "Что является одной из основных целей тестирования?", Options = new List<AnswerOption>
                                    {
                                        new AnswerOption { Text = "Выявление дефектов", IsCorrect = true },
                                        new AnswerOption { Text = "Увеличение размера программы", IsCorrect = false },
                                        new AnswerOption { Text = "Удаление документации", IsCorrect = false }
                                    }},
                                    new TestQuestion { Text = "Что обычно содержит тест-кейс?", Options = new List<AnswerOption>
                                    {
                                        new AnswerOption { Text = "Шаги и ожидаемый результат", IsCorrect = true },
                                        new AnswerOption { Text = "Только название разработчика", IsCorrect = false },
                                        new AnswerOption { Text = "Пароль базы данных", IsCorrect = false }
                                    }}
                                }
                            },
                            new Lecture
                            {
                                Title = "Лекция 2. Тест-кейсы и дефекты", Order = 2,
                                Description = "Эта лекция открывается после успешного завершения первой.",
                                Content = "Тест-кейс описывает условия, шаги и ожидаемый результат проверки. Дефект фиксирует расхождение между фактическим и ожидаемым поведением программы."
                            }
                        }
                    }
                }
            };
            db.Courses.Add(course);
            await db.SaveChangesAsync();
        }
    }

    private static async Task CreateUser(UserManager<ApplicationUser> users, string username,
        string password, string fullName, string role)
    {
        if (await users.FindByNameAsync(username) != null) return;
        var user = new ApplicationUser { UserName = username, FullName = fullName };
        var result = await users.CreateAsync(user, password);
        if (result.Succeeded) await users.AddToRoleAsync(user, role);
    }
}
