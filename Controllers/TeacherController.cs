using ElectronStudyCabinet.Data;
using ElectronStudyCabinet.Models;
using ElectronStudyCabinet.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace ElectronStudyCabinet.Controllers;

[Authorize(Roles = "Teacher")]
public class TeacherController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IWebHostEnvironment _env;
    private readonly UserManager<ApplicationUser> _users;
    public TeacherController(
    ApplicationDbContext db,
    IWebHostEnvironment env,
    UserManager<ApplicationUser> users)
    {
        _db = db;
        _env = env;
        _users = users;
    }

    public async Task<IActionResult> Index()
    {
        ViewBag.StudentCount = (await _users.GetUsersInRoleAsync("Student")).Count;
        ViewBag.CourseCount = await _db.Courses.CountAsync();
        ViewBag.AttemptCount = await _db.TestAttempts.CountAsync();
        return View(await _db.Courses.Include(c => c.Topics).ThenInclude(t => t.Lectures).ThenInclude(l => l.ControlQuestions)
            .Include(c => c.Topics).ThenInclude(t => t.Lectures).ThenInclude(l => l.TestQuestions)
            .OrderBy(c => c.Title).ToListAsync());
    }

    public IActionResult CreateCourse() => View(new CourseEditViewModel());
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateCourse(CourseEditViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        _db.Courses.Add(new Course { Title = model.Title, Description = model.Description });
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> EditCourse(int id)
    {
        var c = await _db.Courses.FindAsync(id);
        if (c == null) return NotFound();
        return View("CreateCourse", new CourseEditViewModel { Id = c.Id, Title = c.Title, Description = c.Description });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditCourse(CourseEditViewModel model)
    {
        if (!ModelState.IsValid) return View("CreateCourse", model);
        var c = await _db.Courses.FindAsync(model.Id);
        if (c == null) return NotFound();
        c.Title = model.Title; c.Description = model.Description;
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCourse(int id)
    {
        var c = await _db.Courses.FindAsync(id);
        if (c != null) { _db.Courses.Remove(c); await _db.SaveChangesAsync(); }
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> CreateTopic(int courseId)
    {
        if (!await _db.Courses.AnyAsync(c => c.Id == courseId)) return NotFound();
        return View(new TopicEditViewModel { CourseId = courseId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateTopic(TopicEditViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        if (!await _db.Courses.AnyAsync(c => c.Id == model.CourseId)) return NotFound();
        _db.Topics.Add(new Topic { Title = model.Title, Order = model.Order, CourseId = model.CourseId });
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }


    public async Task<IActionResult> EditTopic(int id)
    {
        var topic = await _db.Topics.FindAsync(id);

        if (topic == null)
            return NotFound();

        return View(new TopicEditViewModel
        {
            Id = topic.Id,
            Title = topic.Title,
            Order = topic.Order,
            CourseId = topic.CourseId
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditTopic(TopicEditViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var topic = await _db.Topics.FindAsync(model.Id);

        if (topic == null)
            return NotFound();

        if (!await _db.Courses.AnyAsync(c => c.Id == model.CourseId))
            return NotFound();

        topic.Title = model.Title;
        topic.Order = model.Order;
        topic.CourseId = model.CourseId;

        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteTopic(int id)
    {
        var topic = await _db.Topics
            .Include(t => t.Lectures)
                .ThenInclude(l => l.ControlQuestions)
            .Include(t => t.Lectures)
                .ThenInclude(l => l.TestQuestions)
                    .ThenInclude(q => q.Options)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (topic == null)
            return NotFound();

        var lectureIds = topic.Lectures
            .Select(l => l.Id)
            .ToList();

        var progress = await _db.LectureProgress
            .Where(p => lectureIds.Contains(p.LectureId))
            .ToListAsync();

        var attempts = await _db.TestAttempts
            .Where(a => lectureIds.Contains(a.LectureId))
            .ToListAsync();

        var filePaths = topic.Lectures
            .Where(l => !string.IsNullOrEmpty(l.FilePath))
            .Select(l => l.FilePath!)
            .ToList();

        _db.LectureProgress.RemoveRange(progress);
        _db.TestAttempts.RemoveRange(attempts);
        _db.Topics.Remove(topic);

        await _db.SaveChangesAsync();

        foreach (var filePath in filePaths)
        {
            var fileName = Path.GetFileName(filePath);
            var physicalPath = Path.Combine(
                _env.WebRootPath,
                "uploads",
                fileName);

            if (System.IO.File.Exists(physicalPath))
                System.IO.File.Delete(physicalPath);
        }

        return RedirectToAction(nameof(Index));
    }


    public async Task<IActionResult> CreateLecture(int topicId)
    {
        if (!await _db.Topics.AnyAsync(t => t.Id == topicId)) return NotFound();
        return View(new LectureEditViewModel { TopicId = topicId, Order = 1 });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateLecture(LectureEditViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var topic = await _db.Topics.FindAsync(model.TopicId);
        if (topic == null) return NotFound();
        var lecture = new Lecture { Title = model.Title, Description = model.Description, Content = model.Content, Order = model.Order, TopicId = model.TopicId };
        if (model.Material != null)
        {
            var ext = Path.GetExtension(model.Material.FileName).ToLowerInvariant();
            if (!new[] { ".pdf", ".docx", ".doc", ".txt" }.Contains(ext))
            {
                ModelState.AddModelError(nameof(model.Material), "Разрешены только PDF, DOC, DOCX и TXT.");
                return View(model);
            }
            var folder = Path.Combine(_env.WebRootPath, "uploads");
            Directory.CreateDirectory(folder);
            var name = $"{Guid.NewGuid():N}{ext}";
            using var stream = System.IO.File.Create(Path.Combine(folder, name));
            await model.Material.CopyToAsync(stream);
            lecture.FilePath = "/uploads/" + name;
        }
        _db.Lectures.Add(lecture);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }


    public async Task<IActionResult> EditLecture(int id)
    {
        var lecture = await _db.Lectures.FindAsync(id);

        if (lecture == null)
            return NotFound();

        return View(new LectureEditViewModel
        {
            Id = lecture.Id,
            Title = lecture.Title,
            Description = lecture.Description,
            Content = lecture.Content,
            Order = lecture.Order,
            TopicId = lecture.TopicId
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditLecture(LectureEditViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var lecture = await _db.Lectures.FindAsync(model.Id);

        if (lecture == null)
            return NotFound();

        if (!await _db.Topics.AnyAsync(t => t.Id == model.TopicId))
            return NotFound();

        lecture.Title = model.Title;
        lecture.Description = model.Description;
        lecture.Content = model.Content;
        lecture.Order = model.Order;
        lecture.TopicId = model.TopicId;

        if (model.Material != null)
        {
            var ext = Path.GetExtension(model.Material.FileName).ToLowerInvariant();

            if (!new[] { ".pdf", ".docx", ".doc", ".txt" }.Contains(ext))
            {
                ModelState.AddModelError(
                    nameof(model.Material),
                    "Разрешены только PDF, DOC, DOCX и TXT.");

                return View(model);
            }

            var folder = Path.Combine(_env.WebRootPath, "uploads");
            Directory.CreateDirectory(folder);

            var name = $"{Guid.NewGuid():N}{ext}";
            var path = Path.Combine(folder, name);

            using (var stream = System.IO.File.Create(path))
            {
                await model.Material.CopyToAsync(stream);
            }

            lecture.FilePath = "/uploads/" + name;
        }

        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteLecture(int id)
    {
        var lecture = await _db.Lectures
            .Include(l => l.ControlQuestions)
            .Include(l => l.TestQuestions)
                .ThenInclude(q => q.Options)
            .FirstOrDefaultAsync(l => l.Id == id);

        if (lecture == null)
            return NotFound();

        var progress = await _db.LectureProgress
            .Where(p => p.LectureId == id)
            .ToListAsync();

        var attempts = await _db.TestAttempts
            .Where(a => a.LectureId == id)
            .ToListAsync();

        _db.LectureProgress.RemoveRange(progress);
        _db.TestAttempts.RemoveRange(attempts);
        _db.Lectures.Remove(lecture);

        await _db.SaveChangesAsync();

        if (!string.IsNullOrEmpty(lecture.FilePath))
        {
            var fileName = Path.GetFileName(lecture.FilePath);
            var filePath = Path.Combine(_env.WebRootPath, "uploads", fileName);

            if (System.IO.File.Exists(filePath))
                System.IO.File.Delete(filePath);
        }

        return RedirectToAction(nameof(Index));
    }


    public async Task<IActionResult> AddControlQuestion(int lectureId)
    {
        if (!await _db.Lectures.AnyAsync(l => l.Id == lectureId)) return NotFound();
        return View(new QuestionEditViewModel { LectureId = lectureId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddControlQuestion(QuestionEditViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        if (!await _db.Lectures.AnyAsync(l => l.Id == model.LectureId)) return NotFound();
        _db.ControlQuestions.Add(new ControlQuestion { LectureId = model.LectureId, Text = model.Text, CorrectAnswer = model.CorrectAnswer.Trim() });
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> EditControlQuestion(int id)
    {
        var question = await _db.ControlQuestions.FindAsync(id);

        if (question == null)
            return NotFound();

        return View(new QuestionEditViewModel
        {
            LectureId = question.LectureId,
            Text = question.Text,
            CorrectAnswer = question.CorrectAnswer
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditControlQuestion(
        int id, QuestionEditViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var question = await _db.ControlQuestions.FindAsync(id);

        if (question == null)
            return NotFound();

        question.Text = model.Text;
        question.CorrectAnswer = model.CorrectAnswer.Trim();

        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }


    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteControlQuestion(int id)
    {
        var question = await _db.ControlQuestions.FindAsync(id);

        if (question == null)
            return NotFound();

        _db.ControlQuestions.Remove(question);

        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }



    public async Task<IActionResult> AddTestQuestion(int lectureId)
    {
        if (!await _db.Lectures.AnyAsync(l => l.Id == lectureId)) return NotFound();
        return View(new TestQuestionEditViewModel { LectureId = lectureId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddTestQuestion(TestQuestionEditViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        if (!await _db.Lectures.AnyAsync(l => l.Id == model.LectureId)) return NotFound();
        var q = new TestQuestion { LectureId = model.LectureId, Text = model.Text };
        q.Options.Add(new AnswerOption { Text = model.OptionA, IsCorrect = model.CorrectOption == 1 });
        q.Options.Add(new AnswerOption { Text = model.OptionB, IsCorrect = model.CorrectOption == 2 });
        q.Options.Add(new AnswerOption { Text = model.OptionC, IsCorrect = model.CorrectOption == 3 });
        _db.TestQuestions.Add(q);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }



    public async Task<IActionResult> EditTestQuestion(int id)
    {
        var question = await _db.TestQuestions
            .Include(q => q.Options)
            .FirstOrDefaultAsync(q => q.Id == id);

        if (question == null)
            return NotFound();

        var model = new ElectronStudyCabinet.Models.TestQuestionEditViewModel
        {
            Id = question.Id,
            LectureId = question.LectureId,
            Text = question.Text,
            Option1 = question.Options.ElementAtOrDefault(0)?.Text ?? "",
            Option2 = question.Options.ElementAtOrDefault(1)?.Text ?? "",
            Option3 = question.Options.ElementAtOrDefault(2)?.Text ?? "",
            CorrectOption = question.Options.FindIndex(o => o.IsCorrect) + 1
        };

        if (model.CorrectOption == 0)
            model.CorrectOption = 1;

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditTestQuestion(
        ElectronStudyCabinet.Models.TestQuestionEditViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var question = await _db.TestQuestions
            .Include(q => q.Options)
            .FirstOrDefaultAsync(q => q.Id == model.Id);

        if (question == null)
            return NotFound();

        question.Text = model.Text;

        var options = question.Options
            .OrderBy(o => o.Id)
            .ToList();

        var optionTexts = new[]
        {
        model.Option1,
        model.Option2,
        model.Option3
    };

        for (int i = 0; i < 3; i++)
        {
            if (i < options.Count)
            {
                options[i].Text = optionTexts[i];
                options[i].IsCorrect = model.CorrectOption == i + 1;
            }
            else
            {
                question.Options.Add(new ElectronStudyCabinet.Models.AnswerOption
                {
                    Text = optionTexts[i],
                    IsCorrect = model.CorrectOption == i + 1
                });
            }
        }

        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteTestQuestion(int id)
    {
        var question = await _db.TestQuestions
            .Include(q => q.Options)
            .FirstOrDefaultAsync(q => q.Id == id);

        if (question == null)
            return NotFound();

        _db.TestQuestions.Remove(question);

        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }







    public async Task<IActionResult> Results()
    {
        var results = await _db.TestAttempts.Include(a => a.Student).Include(a => a.Lecture)
            .OrderByDescending(a => a.AttemptedAt).ToListAsync();
        return View(results);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ReopenLecture(string studentId, int lectureId)
    {
        var progress = await _db.LectureProgress.FirstOrDefaultAsync(p => p.StudentId == studentId && p.LectureId == lectureId);
        if (progress == null)
            _db.LectureProgress.Add(new LectureProgress { StudentId = studentId, LectureId = lectureId, ControlQuestionsPassed = true, TestPassed = true, CompletedAt = DateTime.UtcNow });
        else
        {
            progress.ControlQuestionsPassed = true;
            progress.TestPassed = true;
            progress.CompletedAt = DateTime.UtcNow;
        }
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Results));
    }
}
