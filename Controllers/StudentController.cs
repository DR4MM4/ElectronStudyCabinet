using ElectronStudyCabinet.Data;
using ElectronStudyCabinet.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ElectronStudyCabinet.Controllers;

[Authorize(Roles = "Student")]
public class StudentController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _users;
    public StudentController(ApplicationDbContext db, UserManager<ApplicationUser> users) { _db = db; _users = users; }



    public async Task<IActionResult> Index()
    {
        var courses = await _db.Courses
            .Include(c => c.Topics)
                .ThenInclude(t => t.Lectures)
            .ToListAsync();

        var userId = _users.GetUserId(User)!;

        var completed = await _db.LectureProgress
            .Where(p => p.StudentId == userId
                && p.ControlQuestionsPassed
                && p.TestPassed)
            .Select(p => p.LectureId)
            .ToListAsync();

        ViewBag.Progress = completed;

        var allLectures = courses
            .SelectMany(c => c.Topics ?? new List<Topic>())
            .OrderBy(t => t.Order)
            .SelectMany(t => t.Lectures.OrderBy(l => l.Order))
            .ToList();

        ViewBag.TotalLectures = allLectures.Count;

        // Находим первую незавершённую доступную лекцию.
        int? continueLectureId = null;

        foreach (var course in courses)
        {
            var lectures = (course.Topics ?? new List<Topic>())
                .OrderBy(t => t.Order)
                .SelectMany(t => t.Lectures.OrderBy(l => l.Order))
                .ToList();

            for (int i = 0; i < lectures.Count; i++)
            {
                bool isCompleted = completed.Contains(lectures[i].Id);

                bool unlocked = i == 0
                    || completed.Contains(lectures[i - 1].Id);

                if (!isCompleted && unlocked)
                {
                    continueLectureId = lectures[i].Id;
                    break;
                }
            }

            if (continueLectureId.HasValue)
                break;
        }

        ViewBag.ContinueLectureId = continueLectureId;

        return View(courses);
    }



    public async Task<IActionResult> Lecture(int id)
    {
        var lecture = await _db.Lectures
            .Include(l => l.Topic)
                .ThenInclude(t => t!.Course)
            .Include(l => l.ControlQuestions)
            .Include(l => l.TestQuestions)
                .ThenInclude(q => q.Options)
            .FirstOrDefaultAsync(l => l.Id == id);

        if (lecture == null)
            return NotFound();

        var userId = _users.GetUserId(User)!;

        // Получаем лекции этой дисциплины в правильном порядке.
        var ordered = await _db.Lectures
            .Include(l => l.Topic)
            .Where(l => l.Topic!.CourseId == lecture.Topic!.CourseId)
            .OrderBy(l => l.Topic!.Order)
            .ThenBy(l => l.Order)
            .ThenBy(l => l.Id)
            .ToListAsync();

        var index = ordered.FindIndex(l => l.Id == id);

        // Проверяем предыдущую лекцию, если она существует.
        if (index > 0)
        {
            var previousId = ordered[index - 1].Id;

            var previousProgress = await _db.LectureProgress
                .FirstOrDefaultAsync(p =>
                    p.StudentId == userId &&
                    p.LectureId == previousId);

            bool previousCompleted =
                previousProgress != null &&
                previousProgress.ControlQuestionsPassed &&
                previousProgress.TestPassed;

            if (!previousCompleted)
            {
                TempData["Error"] =
                    "Эта лекция закрыта. Сначала завершите предыдущую лекцию и наберите проходной балл.";

                return RedirectToAction(nameof(Index));
            }
        }

        var orderedLectures = await _db.Lectures
    .Include(l => l.Topic)
    .Where(l => l.Topic!.CourseId == lecture.Topic!.CourseId)
    .OrderBy(l => l.Topic!.Order)
    .ThenBy(l => l.Order)
    .ThenBy(l => l.Id)
    .ToListAsync();

        var currentIndex = orderedLectures.FindIndex(l => l.Id == id);

        ViewBag.NextLecture = currentIndex >= 0 && currentIndex < orderedLectures.Count - 1
            ? orderedLectures[currentIndex + 1]
            : null;

        ViewBag.Progress = await _db.LectureProgress
            .FirstOrDefaultAsync(p =>
                p.StudentId == userId &&
                p.LectureId == id);

        ViewBag.Attempts = await _db.TestAttempts
            .Where(a => a.StudentId == userId && a.LectureId == id)
            .OrderByDescending(a => a.AttemptedAt)
            .ToListAsync();

        return View(lecture);
    }


    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitControl(int lectureId, Dictionary<int, string> answers)
    {
        var lecture = await _db.Lectures
            .Include(l => l.ControlQuestions)
            .Include(l => l.Topic)
            .FirstOrDefaultAsync(l => l.Id == lectureId);

        if (lecture == null)
            return NotFound();

        if (!await CanOpen(lecture))
            return Forbid();

        var allCorrect = lecture.ControlQuestions.Count > 0;

        foreach (var q in lecture.ControlQuestions)
        {
            answers.TryGetValue(q.Id, out var answer);

            var userAnswer = (answer ?? "").Trim();
            var correctAnswer = (q.CorrectAnswer ?? "").Trim();

            if (!string.Equals(userAnswer, correctAnswer,
                StringComparison.OrdinalIgnoreCase))
            {
                allCorrect = false;
            }
        }

        var userId = _users.GetUserId(User)!;
        var progress = await GetProgress(userId, lectureId);

        progress.ControlQuestionsPassed = allCorrect;

        if (!allCorrect)
        {
            progress.TestPassed = false;
            progress.CompletedAt = null;
        }

        await _db.SaveChangesAsync();

        TempData[allCorrect ? "Success" : "Error"] = allCorrect
            ? "Контрольные вопросы пройдены."
            : "Есть неверные ответы. Проверьте ответы и попробуйте ещё раз.";

        return RedirectToAction(nameof(Lecture), new { id = lectureId });
    }


    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitTest(int lectureId, Dictionary<int, int> answers)
    {
        var lecture = await _db.Lectures
            .Include(l => l.TestQuestions)
                .ThenInclude(q => q.Options)
            .Include(l => l.Topic)
            .FirstOrDefaultAsync(l => l.Id == lectureId);

        if (lecture == null)
            return NotFound();

        if (!await CanOpen(lecture))
            return Forbid();

        var userId = _users.GetUserId(User)!;

        var progress = await GetProgress(userId, lectureId);

        // Сначала студент должен пройти контрольные вопросы.
        if (!progress.ControlQuestionsPassed)
        {
            TempData["Error"] = "Сначала правильно ответьте на контрольные вопросы.";
            return RedirectToAction(nameof(Lecture), new { id = lectureId });
        }

        var questions = lecture.TestQuestions;

        var correct = questions.Count(q =>
            answers.TryGetValue(q.Id, out var optionId) &&
            q.Options.Any(o => o.Id == optionId && o.IsCorrect));

        var total = questions.Count;
        var percent = total == 0
            ? 0
            : (int)Math.Round(correct * 100.0 / total);

        var passed = total > 0 && percent >= 70;

        _db.TestAttempts.Add(new TestAttempt
        {
            StudentId = userId,
            LectureId = lectureId,
            CorrectAnswers = correct,
            TotalQuestions = total,
            Percent = percent,
            Passed = passed,
            AttemptedAt = DateTime.UtcNow
        });

        progress.TestPassed = passed;

        if (progress.ControlQuestionsPassed && progress.TestPassed)
            progress.CompletedAt = DateTime.UtcNow;
        else
            progress.CompletedAt = null;

        await _db.SaveChangesAsync();

        TempData[passed ? "Success" : "Error"] =
            $"Результат: {correct} из {total} ({percent}%). " +
            (passed
                ? "Тест пройден."
                : "Для прохождения необходимо набрать не менее 70%.");

        return RedirectToAction(nameof(Lecture), new { id = lectureId });
    }

    private async Task<bool> CanOpen(Lecture lecture)
    {
        var userId = _users.GetUserId(User)!;

        var ordered = await _db.Lectures
            .Include(l => l.Topic)
            .Where(l => l.Topic!.CourseId == lecture.Topic!.CourseId)
            .OrderBy(l => l.Topic!.Order)
            .ThenBy(l => l.Order)
            .ThenBy(l => l.Id)
            .ToListAsync();

        var index = ordered.FindIndex(l => l.Id == lecture.Id);

        if (index < 0)
            return false;

        // Первая лекция всегда доступна.
        if (index == 0)
            return true;

        var previousId = ordered[index - 1].Id;

        return await _db.LectureProgress.AnyAsync(p =>
            p.StudentId == userId &&
            p.LectureId == previousId &&
            p.ControlQuestionsPassed &&
            p.TestPassed);
    }

    private async Task<LectureProgress> GetProgress(string userId, int lectureId)
    {
        var progress = await _db.LectureProgress.FirstOrDefaultAsync(p => p.StudentId == userId && p.LectureId == lectureId);
        if (progress != null) return progress;
        progress = new LectureProgress { StudentId = userId, LectureId = lectureId };
        _db.LectureProgress.Add(progress);
        return progress;
    }
}
