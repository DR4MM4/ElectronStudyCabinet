using ElectronStudyCabinet.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ElectronStudyCabinet.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Topic> Topics => Set<Topic>();
    public DbSet<Lecture> Lectures => Set<Lecture>();
    public DbSet<ControlQuestion> ControlQuestions => Set<ControlQuestion>();
    public DbSet<TestQuestion> TestQuestions => Set<TestQuestion>();
    public DbSet<AnswerOption> AnswerOptions => Set<AnswerOption>();
    public DbSet<TestAttempt> TestAttempts => Set<TestAttempt>();
    public DbSet<LectureProgress> LectureProgress => Set<LectureProgress>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<Course>().HasMany(c => c.Topics).WithOne(t => t.Course)
            .HasForeignKey(t => t.CourseId).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<Topic>().HasMany(t => t.Lectures).WithOne(l => l.Topic)
            .HasForeignKey(l => l.TopicId).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<Lecture>().HasMany(l => l.ControlQuestions).WithOne(q => q.Lecture)
            .HasForeignKey(q => q.LectureId).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<Lecture>().HasMany(l => l.TestQuestions).WithOne(q => q.Lecture)
            .HasForeignKey(q => q.LectureId).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<TestQuestion>().HasMany(q => q.Options).WithOne(o => o.TestQuestion)
            .HasForeignKey(o => o.TestQuestionId).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<LectureProgress>().HasIndex(p => new { p.StudentId, p.LectureId }).IsUnique();
        builder.Entity<LectureProgress>().HasOne(p => p.Student).WithMany()
            .HasForeignKey(p => p.StudentId).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<TestAttempt>().HasOne(a => a.Student).WithMany()
            .HasForeignKey(a => a.StudentId).OnDelete(DeleteBehavior.Cascade);
    }
}
