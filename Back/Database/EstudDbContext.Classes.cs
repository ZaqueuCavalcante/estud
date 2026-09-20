using Estud.Back.Domain.Classes;
using Estud.Back.Database.Classes;

namespace Estud.Back.Database;

public partial class EstudDbContext
{
    public DbSet<Class> Classes { get; set; }
    public DbSet<Schedule> Schedules { get; set; }
    public DbSet<ClassLesson> ClassLessons { get; set; }
    public DbSet<ClassStudent> ClassStudents { get; set; }
    public DbSet<ClassTeacher> ClassTeachers { get; set; }
    public DbSet<ClassActivity> ClassActivities { get; set; }
    public DbSet<ClassActivityWork> ClassActivityWorks { get; set; }
    public DbSet<ClassLessonAttendance> ClassLessonAttendances { get; set; }
    public DbSet<ClassActivityWorkEntry> ClassActivityWorkEntries { get; set; }

    private static void ConfigureClasses(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new ClassDbConfig());
        modelBuilder.ApplyConfiguration(new ScheduleDbConfig());
        modelBuilder.ApplyConfiguration(new ClassLessonDbConfig());
        modelBuilder.ApplyConfiguration(new ClassStudentDbConfig());
        modelBuilder.ApplyConfiguration(new ClassTeacherDbConfig());
        modelBuilder.ApplyConfiguration(new ClassActivityDbConfig());
        modelBuilder.ApplyConfiguration(new ClassActivityWorkDbConfig());
        modelBuilder.ApplyConfiguration(new ClassLessonAttendanceDbConfig());
        modelBuilder.ApplyConfiguration(new ClassActivityWorkEntryDbConfig());
    }

    // O :* é colocado em cada lexema do texto gerado pelo websearch_to_tsquery.
    // O to_tsquery usa 'simple' para não aplicar o stemming de novo sobre o radical.
    public IQueryable<ClassLesson> SearchClassLessons(string search)
    {
        return ClassLessons.FromSql($"""
            SELECT * FROM estud.class_lessons
            WHERE planned_content IS NOT NULL
              AND to_tsvector('portuguese', unaccent(planned_content))
                  @@ to_tsquery('simple', regexp_replace(
                       websearch_to_tsquery('portuguese', unaccent({search}))::text,
                       '''(?:[^'']|'''')*''', '\&:*', 'g'))
            """);
    }
}
