namespace Estud.Tests.Integration;

public partial class IntegrationTests
{
    #region Authentication

    [Test]
    public async Task Classes_FinalizeClass_Should_not_finalize_class_when_not_authenticated()
    {
        // Arrange
        var client = _back.GetTestsClient();

        // Act
        var result = await client.FinalizeClass(classId: 1);

        // Assert
        result.ShouldBeError(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Authorization

    [Test]
    public async Task Classes_FinalizeClass_Should_not_finalize_class_when_user_has_no_permission()
    {
        // Arrange
        var client = await _back.LoggedAsTeacher();

        // Act
        var result = await client.FinalizeClass(classId: 1);

        // Assert
        result.ShouldBeError(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Validation errors

    [Test]
    public async Task Classes_FinalizeClass_Should_not_finalize_class_when_class_not_found()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.FinalizeClass(classId: 999999);

        // Assert
        result.ShouldBeError(ClassNotFound.I);
    }

    [Test]
    public async Task Classes_FinalizeClass_Should_not_finalize_class_when_class_is_not_started()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var discipline = await client.CreateDiscipline().Success();
        var period = await client.ShortcutGetFirstAcademicPeriod();
        var @class = await client.CreateClass(discipline.Id, period.Id).Success();

        // Act
        var result = await client.FinalizeClass(@class.Id);

        // Assert
        result.ShouldBeError(ClassMustBeStarted.I);
    }

    [Test]
    public async Task Classes_FinalizeClass_Should_not_finalize_class_when_class_is_already_finalized()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var @class = await client.ShortcutCreateStartedClass(students: []);
        await client.FinalizeClass(@class.Id).Success();

        // Act
        var result = await client.FinalizeClass(@class.Id);

        // Assert
        result.ShouldBeError(ClassAlreadyFinalized.I);
    }

    #endregion

    #region Happy path

    [Test]
    public async Task Classes_FinalizeClass_Should_finalize_started_class()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var @class = await client.ShortcutCreateStartedClass(students: []);

        // Act
        var result = await client.FinalizeClass(@class.Id);

        // Assert
        result.ShouldBeSuccess();

        var classData = await client.GetClass(@class.Id).Success();
        classData.Status.Should().Be(ClassStatus.Finalized);
    }

    [Test]
    public async Task Classes_FinalizeClass_Should_create_notes_for_each_student()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var ana = await director.CreateStudent("Ana Beatriz", DataGen.Email).Success();
        var bruno = await director.CreateStudent("Bruno Silva", DataGen.Email).Success();
        var @class = await director.ShortcutCreateStartedClass([ana.Id, bruno.Id]);
        var teacher = await _back.LoginAs(@class.TeacherEmail);

        var n1 = await teacher.CreateClassActivity(@class.Id, ClassNoteType.N1, weight: 100).Success();
        var n2First = await teacher.CreateClassActivity(@class.Id, ClassNoteType.N2, weight: 40).Success();
        var n2Second = await teacher.CreateClassActivity(@class.Id, ClassNoteType.N2, weight: 60).Success();
        await teacher.ShortcutAddStudentActivityNote(@class.Id, n1.Id, ana.Id, 8M);
        await teacher.ShortcutAddStudentActivityNote(@class.Id, n2First.Id, ana.Id, 10M);
        await teacher.ShortcutAddStudentActivityNote(@class.Id, n2Second.Id, ana.Id, 5M);
        await teacher.ShortcutAddStudentActivityNote(@class.Id, n1.Id, bruno.Id, 6.5M);

        // Act
        var result = await director.FinalizeClass(@class.Id);

        // Assert
        result.ShouldBeSuccess();

        await using var ctx = _back.GetDbContext();
        var notes = await ctx.StudentClassNotes.AsNoTracking().Where(n => n.ClassId == @class.Id).ToListAsync();

        notes.Where(n => n.StudentId == ana.Id).Select(n => (n.Type, n.Note)).Should().BeEquivalentTo(
        [
            (ClassNoteType.N1, 8M),
            (ClassNoteType.N2, 7M),
            (ClassNoteType.N3, 0M),
        ]);
        notes.Where(n => n.StudentId == bruno.Id).Select(n => (n.Type, n.Note)).Should().BeEquivalentTo(
        [
            (ClassNoteType.N1, 6.5M),
            (ClassNoteType.N2, 0M),
            (ClassNoteType.N3, 0M),
        ]);
    }

    [Test]
    public async Task Classes_FinalizeClass_Should_create_only_the_notes_of_the_institution_grade_rule()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        await director.SetupInstitutionConfig(gradeRule: ClassGradeRule.AverageOfTwo);
        var @class = await director.ShortcutCreateStartedClass();
        var teacher = await _back.LoginAs(@class.TeacherEmail);

        var activity = await teacher.CreateClassActivity(@class.Id, ClassNoteType.N1, weight: 100).Success();
        await teacher.ShortcutAddStudentActivityNote(@class.Id, activity.Id, @class.StudentIds[0], 9M);

        // Act
        var result = await director.FinalizeClass(@class.Id);

        // Assert
        result.ShouldBeSuccess();

        await using var ctx = _back.GetDbContext();
        var notes = await ctx.StudentClassNotes.AsNoTracking().Where(n => n.ClassId == @class.Id).ToListAsync();
        notes.Select(n => n.Type).Should().BeEquivalentTo([ClassNoteType.N1, ClassNoteType.N2]);
    }

    [Test]
    public async Task Classes_FinalizeClass_Should_approve_student_with_enough_grade_and_attendance()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass();
        var studentId = @class.StudentIds[0];
        var teacher = await _back.LoginAs(@class.TeacherEmail);

        var n1 = await teacher.CreateClassActivity(@class.Id, ClassNoteType.N1, weight: 100).Success();
        var n2 = await teacher.CreateClassActivity(@class.Id, ClassNoteType.N2, weight: 100).Success();
        await teacher.ShortcutAddStudentActivityNote(@class.Id, n1.Id, studentId, 7M);
        await teacher.ShortcutAddStudentActivityNote(@class.Id, n2.Id, studentId, 7M);

        var lessons = (await teacher.GetTeacherClassLessons(@class.Id).Success()).Lessons;
        await teacher.CreateLessonAttendance(lessons[0].Id, [studentId]);
        await teacher.CreateLessonAttendance(lessons[1].Id, [studentId]);
        await teacher.CreateLessonAttendance(lessons[2].Id, [studentId]);
        await teacher.CreateLessonAttendance(lessons[3].Id, []);

        // Act
        var result = await director.FinalizeClass(@class.Id);

        // Assert
        result.ShouldBeSuccess();

        var classData = await director.GetClass(@class.Id).Success();
        classData.Students.Single().Status.Should().Be(StudentClassStatus.Aprovado);
    }

    [Test]
    public async Task Classes_FinalizeClass_Should_fail_student_by_grade_when_average_is_below_the_note_limit()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass();
        var studentId = @class.StudentIds[0];
        var teacher = await _back.LoginAs(@class.TeacherEmail);

        var n1 = await teacher.CreateClassActivity(@class.Id, ClassNoteType.N1, weight: 100).Success();
        var n2 = await teacher.CreateClassActivity(@class.Id, ClassNoteType.N2, weight: 100).Success();
        await teacher.ShortcutAddStudentActivityNote(@class.Id, n1.Id, studentId, 7M);
        await teacher.ShortcutAddStudentActivityNote(@class.Id, n2.Id, studentId, 6.8M);

        var lessons = (await teacher.GetTeacherClassLessons(@class.Id).Success()).Lessons;
        await teacher.CreateLessonAttendance(lessons[0].Id, [studentId]);

        // Act
        var result = await director.FinalizeClass(@class.Id);

        // Assert
        result.ShouldBeSuccess();

        var classData = await director.GetClass(@class.Id).Success();
        classData.Students.Single().Status.Should().Be(StudentClassStatus.ReprovadoPorNota);
    }

    [Test]
    public async Task Classes_FinalizeClass_Should_fail_student_by_absence_when_attendance_is_below_the_frequency_limit()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass();
        var studentId = @class.StudentIds[0];
        var teacher = await _back.LoginAs(@class.TeacherEmail);

        var n1 = await teacher.CreateClassActivity(@class.Id, ClassNoteType.N1, weight: 100).Success();
        var n2 = await teacher.CreateClassActivity(@class.Id, ClassNoteType.N2, weight: 100).Success();
        await teacher.ShortcutAddStudentActivityNote(@class.Id, n1.Id, studentId, 10M);
        await teacher.ShortcutAddStudentActivityNote(@class.Id, n2.Id, studentId, 10M);

        var lessons = (await teacher.GetTeacherClassLessons(@class.Id).Success()).Lessons;
        await teacher.CreateLessonAttendance(lessons[0].Id, [studentId]);
        await teacher.CreateLessonAttendance(lessons[1].Id, [studentId]);
        await teacher.CreateLessonAttendance(lessons[2].Id, []);

        // Act
        var result = await director.FinalizeClass(@class.Id);

        // Assert
        result.ShouldBeSuccess();

        var classData = await director.GetClass(@class.Id).Success();
        classData.Students.Single().Status.Should().Be(StudentClassStatus.ReprovadoPorFalta);
    }

    [Test]
    public async Task Classes_FinalizeClass_Should_fail_student_by_absence_when_both_grade_and_attendance_are_below_the_limits()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass();
        var teacher = await _back.LoginAs(@class.TeacherEmail);

        var lessons = (await teacher.GetTeacherClassLessons(@class.Id).Success()).Lessons;
        await teacher.CreateLessonAttendance(lessons[0].Id, []);

        // Act
        var result = await director.FinalizeClass(@class.Id);

        // Assert
        result.ShouldBeSuccess();

        var classData = await director.GetClass(@class.Id).Success();
        classData.Students.Single().Status.Should().Be(StudentClassStatus.ReprovadoPorFalta);
    }

    [Test]
    public async Task Classes_FinalizeClass_Should_not_fail_student_by_absence_when_no_attendance_was_recorded()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass();

        // Act
        var result = await director.FinalizeClass(@class.Id);

        // Assert
        result.ShouldBeSuccess();

        var classData = await director.GetClass(@class.Id).Success();
        classData.Students.Single().Status.Should().Be(StudentClassStatus.ReprovadoPorNota);
    }

    [Test]
    public async Task Classes_FinalizeClass_Should_use_the_institution_limits_to_define_the_student_status()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        await director.SetupInstitutionConfig(noteLimit: 5M, frequencyLimit: 50M, gradeRule: ClassGradeRule.AverageOfTwo);
        var @class = await director.ShortcutCreateStartedClass();
        var studentId = @class.StudentIds[0];
        var teacher = await _back.LoginAs(@class.TeacherEmail);

        var n1 = await teacher.CreateClassActivity(@class.Id, ClassNoteType.N1, weight: 100).Success();
        var n2 = await teacher.CreateClassActivity(@class.Id, ClassNoteType.N2, weight: 100).Success();
        await teacher.ShortcutAddStudentActivityNote(@class.Id, n1.Id, studentId, 4M);
        await teacher.ShortcutAddStudentActivityNote(@class.Id, n2.Id, studentId, 6M);

        var lessons = (await teacher.GetTeacherClassLessons(@class.Id).Success()).Lessons;
        await teacher.CreateLessonAttendance(lessons[0].Id, [studentId]);
        await teacher.CreateLessonAttendance(lessons[1].Id, []);

        // Act
        var result = await director.FinalizeClass(@class.Id);

        // Assert
        result.ShouldBeSuccess();

        var classData = await director.GetClass(@class.Id).Success();
        classData.Students.Single().Status.Should().Be(StudentClassStatus.Aprovado);
    }

    [Test]
    public async Task Classes_FinalizeClass_Should_take_every_student_out_of_the_enrolled_status()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass(studentsCount: 3);

        // Act
        var result = await director.FinalizeClass(@class.Id);

        // Assert
        result.ShouldBeSuccess();

        var classData = await director.GetClass(@class.Id).Success();
        classData.Students.Should().HaveCount(3);
        classData.Students.Should().AllSatisfy(s => s.Status.Should().NotBe(StudentClassStatus.Matriculado));
    }

    #endregion
}
