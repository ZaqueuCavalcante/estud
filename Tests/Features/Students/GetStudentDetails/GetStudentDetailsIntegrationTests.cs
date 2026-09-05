namespace Estud.Tests.Integration;

public partial class IntegrationTests
{
    #region Authentication

    [Test]
    public async Task Students_GetStudentDetails_Should_not_get_student_details_when_not_authenticated()
    {
        // Arrange
        var client = _back.GetTestsClient();

        // Act
        var result = await client.GetStudentDetails(1);

        // Assert
        result.ShouldBeError(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Authorization

    [Test]
    public async Task Students_GetStudentDetails_Should_not_get_student_details_when_user_is_a_teacher()
    {
        // Arrange
        var client = await _back.LoggedAsTeacher();

        // Act
        var result = await client.GetStudentDetails(1);

        // Assert
        result.ShouldBeError(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task Students_GetStudentDetails_Should_not_get_student_details_when_user_is_a_student()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var student = await director.CreateStudent(DataGen.UserName, DataGen.Email).Success();
        var client = await _back.LoginAs(student.Email);

        // Act
        var result = await client.GetStudentDetails(student.Id);

        // Assert
        result.ShouldBeError(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Validation errors

    [Test]
    public async Task Students_GetStudentDetails_Should_not_get_student_details_when_student_does_not_exist()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.GetStudentDetails(999999);

        // Assert
        result.ShouldBeError(StudentNotFound.I);
    }

    [Test]
    public async Task Students_GetStudentDetails_Should_not_get_student_details_of_another_institution()
    {
        // Arrange
        var otherClient = await _back.LoggedAsDirector();
        var student = await otherClient.CreateStudent(DataGen.UserName, DataGen.Email).Success();

        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.GetStudentDetails(student.Id);

        // Assert
        result.ShouldBeError(StudentNotFound.I);
    }

    #endregion

    #region Happy path

    [Test]
    public async Task Students_GetStudentDetails_Should_get_student_details()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var student = await client.CreateStudent("Ana Lima", DataGen.Email).Success();

        // Act
        var result = await client.GetStudentDetails(student.Id);

        // Assert
        var details = result.Success;
        details.Id.Should().Be(student.Id);
        details.Name.Should().Be("Ana Lima");
        details.Email.Should().Be(student.Email);
        details.EnrollmentCode.Should().NotBeEmpty();
        details.Status.Should().Be(StudentStatus.Enrolled);
        details.Course.Should().BeNull();
        details.Classes.Should().BeEmpty();
    }

    [Test]
    public async Task Students_GetStudentDetails_Should_get_student_details_with_current_course_offering()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var student = await client.CreateStudent(DataGen.UserName, DataGen.Email).Success();
        var campus = await client.CreateCampus().Success();
        var course = await client.CreateCourse().Success();
        var curriculum = await client.CreateCourseCurriculum(course.Id).Success();
        var period = await client.GetFirstAcademicPeriod();
        var offering = await client.CreateCourseOffering(campus.Id, course.Id, curriculum.Id, period.Id).Success();
        await client.EnrollStudentInCourseOffering(student.Id, offering.Id);

        // Act
        var result = await client.GetStudentDetails(student.Id);

        // Assert
        var details = result.Success;
        details.Course.Should().NotBeNull();
        details.Course!.CourseOfferingId.Should().Be(offering.Id);
        details.Course.Period.Should().Be(period.Name);
    }

    [Test]
    public async Task Students_GetStudentDetails_Should_get_student_details_with_classes()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var student = await client.CreateStudent(DataGen.UserName, DataGen.Email).Success();
        var discipline = await client.CreateDiscipline().Success();
        var period = await client.GetFirstAcademicPeriod();
        var @class = await client.CreateClass(discipline.Id, period.Id).Success();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        await client.CreateEnrollmentPeriod(startAt: today.AddDays(-2), endAt: today.AddDays(2));
        await client.ReleaseClassForEnrollment(@class.Id);
        await client.AssignStudentToClass(student.Id, @class.Id);

        // Act
        var result = await client.GetStudentDetails(student.Id);

        // Assert
        var details = result.Success;
        details.Classes.Should().ContainSingle();
        details.Classes[0].Id.Should().Be(@class.Id);
        details.Classes[0].Discipline.Should().Be("Geometria");
        details.Classes[0].Period.Should().Be(period.Name);
        details.Classes[0].Status.Should().Be(ClassStatus.OnEnrollment);
        details.Classes[0].MyStatus.Should().Be(StudentClassStatus.Matriculado);
    }

    [Test]
    public async Task Students_GetStudentDetails_Should_get_attendances_of_each_class()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var student = await director.CreateStudent(DataGen.UserName, DataGen.Email).Success();

        var geometry = await director.ShortcutCreateStartedClass([student.Id]);
        var algebra = await director.ShortcutCreateStartedClass([student.Id], "Álgebra", Day.Tuesday);

        var geometryTeacher = await _back.LoginAs(geometry.TeacherEmail);
        var algebraTeacher = await _back.LoginAs(algebra.TeacherEmail);

        var geometryLessons = (await geometryTeacher.GetTeacherClassLessons(geometry.Id).Success()).Lessons;
        await geometryTeacher.CreateLessonAttendance(geometryLessons[0].Id, [student.Id]);
        await geometryTeacher.CreateLessonAttendance(geometryLessons[1].Id, [student.Id]);
        await geometryTeacher.CreateLessonAttendance(geometryLessons[2].Id, []);

        var algebraLessons = (await algebraTeacher.GetTeacherClassLessons(algebra.Id).Success()).Lessons;
        await algebraTeacher.CreateLessonAttendance(algebraLessons[0].Id, [student.Id]);
        await algebraTeacher.CreateLessonAttendance(algebraLessons[1].Id, []);

        // Act
        var result = await director.GetStudentDetails(student.Id);

        // Assert
        var details = result.Success;
        details.Classes.First(c => c.Id == geometry.Id).AverageAttendance.Should().Be(66.7M);
        details.Classes.First(c => c.Id == algebra.Id).AverageAttendance.Should().Be(50M);
        details.AverageAttendance.Should().Be(60M);
    }

    [Test]
    public async Task Students_GetStudentDetails_Should_get_average_attendance_only_of_started_classes()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var student = await director.CreateStudent(DataGen.UserName, DataGen.Email).Success();

        var started = await director.ShortcutCreateStartedClass([student.Id]);
        var finalized = await director.ShortcutCreateStartedClass([student.Id], "Álgebra", Day.Tuesday);

        var startedTeacher = await _back.LoginAs(started.TeacherEmail);
        var finalizedTeacher = await _back.LoginAs(finalized.TeacherEmail);

        var startedLessons = (await startedTeacher.GetTeacherClassLessons(started.Id).Success()).Lessons;
        await startedTeacher.CreateLessonAttendance(startedLessons[0].Id, [student.Id]);
        await startedTeacher.CreateLessonAttendance(startedLessons[1].Id, []);

        var finalizedLessons = (await finalizedTeacher.GetTeacherClassLessons(finalized.Id).Success()).Lessons;
        await finalizedTeacher.CreateLessonAttendance(finalizedLessons[0].Id, [student.Id]);
        await finalizedTeacher.CreateLessonAttendance(finalizedLessons[1].Id, [student.Id]);

        // Nenhum endpoint encerra uma turma, então o status vai direto no banco.
        await using (var arrangeCtx = _back.GetDbContext())
        {
            var @class = await arrangeCtx.Classes.FirstAsync(c => c.Id == finalized.Id);
            @class.Status = ClassStatus.Finalized;
            await arrangeCtx.SaveChangesAsync();
        }

        // Act
        var result = await director.GetStudentDetails(student.Id);

        // Assert
        var details = result.Success;
        details.Classes.First(c => c.Id == started.Id).AverageAttendance.Should().Be(50M);
        details.Classes.First(c => c.Id == finalized.Id).AverageAttendance.Should().Be(100M);
        details.AverageAttendance.Should().Be(50M);
    }

    [Test]
    public async Task Students_GetStudentDetails_Should_get_zeroed_attendances_when_no_lesson_was_recorded()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var student = await director.CreateStudent(DataGen.UserName, DataGen.Email).Success();

        await director.ShortcutCreateStartedClass([student.Id]);

        // Act
        var result = await director.GetStudentDetails(student.Id);

        // Assert
        var details = result.Success;
        details.Classes.Should().ContainSingle()
            .Which.AverageAttendance.Should().Be(0M);
        details.AverageAttendance.Should().Be(0M);
    }

    [Test]
    public async Task Students_GetStudentDetails_Should_get_average_grade_of_each_class()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var student = await director.CreateStudent(DataGen.UserName, DataGen.Email).Success();
        var @class = await director.ShortcutCreateStartedClass([student.Id]);
        var teacher = await _back.LoginAs(@class.TeacherEmail);

        var activity = await teacher.CreateClassActivity(@class.Id, ClassNoteType.N1, weight: 100).Success();
        await teacher.AddStudentActivityNote(@class.Id, activity.Id, student.Id, 8M);

        // Act
        var result = await director.GetStudentDetails(student.Id);

        // Assert
        var details = result.Success;
        details.AverageGrade.Should().Be(4M);
        details.Classes.Should().ContainSingle().Which.AverageGrade.Should().Be(4M);
    }

    [Test]
    public async Task Students_GetStudentDetails_Should_get_average_grade_of_each_class_when_student_is_in_two_classes()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var ana = await director.CreateStudent("Ana Beatriz", DataGen.Email).Success();
        var bruno = await director.CreateStudent("Bruno Silva", DataGen.Email).Success();
        var carla = await director.CreateStudent("Carla Dias", DataGen.Email).Success();
        var daniel = await director.CreateStudent("Daniel Rocha", DataGen.Email).Success();
        var elisa = await director.CreateStudent("Elisa Nunes", DataGen.Email).Success();

        var geometry = await director.ShortcutCreateStartedClass([ana.Id, bruno.Id, carla.Id]);
        var algebra = await director.ShortcutCreateStartedClass([ana.Id, daniel.Id, elisa.Id], "Álgebra", Day.Tuesday);

        var geometryTeacher = await _back.LoginAs(geometry.TeacherEmail);
        var geometryFirst = await geometryTeacher.CreateClassActivity(geometry.Id, ClassNoteType.N1, weight: 40).Success();
        var geometrySecond = await geometryTeacher.CreateClassActivity(geometry.Id, ClassNoteType.N1, weight: 60).Success();
        var geometryExam = await geometryTeacher.CreateClassActivity(geometry.Id, ClassNoteType.N2, weight: 100).Success();

        await geometryTeacher.AddStudentActivityNote(geometry.Id, geometryFirst.Id, ana.Id, 10M);
        await geometryTeacher.AddStudentActivityNote(geometry.Id, geometrySecond.Id, ana.Id, 5M);
        await geometryTeacher.AddStudentActivityNote(geometry.Id, geometryExam.Id, ana.Id, 8M);
        await geometryTeacher.AddStudentActivityNote(geometry.Id, geometryFirst.Id, bruno.Id, 6M);
        await geometryTeacher.AddStudentActivityNote(geometry.Id, geometrySecond.Id, bruno.Id, 6M);
        await geometryTeacher.AddStudentActivityNote(geometry.Id, geometryExam.Id, bruno.Id, 4M);
        await geometryTeacher.AddStudentActivityNote(geometry.Id, geometryFirst.Id, carla.Id, 8M);
        await geometryTeacher.AddStudentActivityNote(geometry.Id, geometrySecond.Id, carla.Id, 10M);
        await geometryTeacher.AddStudentActivityNote(geometry.Id, geometryExam.Id, carla.Id, 7M);

        var algebraTeacher = await _back.LoginAs(algebra.TeacherEmail);
        var algebraWork = await algebraTeacher.CreateClassActivity(algebra.Id, ClassNoteType.N1, weight: 50).Success();
        var algebraExam = await algebraTeacher.CreateClassActivity(algebra.Id, ClassNoteType.N2, weight: 50).Success();
        var algebraRetake = await algebraTeacher.CreateClassActivity(algebra.Id, ClassNoteType.N3, weight: 100).Success();

        await algebraTeacher.AddStudentActivityNote(algebra.Id, algebraWork.Id, ana.Id, 9M);
        await algebraTeacher.AddStudentActivityNote(algebra.Id, algebraExam.Id, ana.Id, 8M);
        await algebraTeacher.AddStudentActivityNote(algebra.Id, algebraRetake.Id, ana.Id, 6M);
        await algebraTeacher.AddStudentActivityNote(algebra.Id, algebraWork.Id, daniel.Id, 6M);
        await algebraTeacher.AddStudentActivityNote(algebra.Id, algebraExam.Id, daniel.Id, 10M);
        await algebraTeacher.AddStudentActivityNote(algebra.Id, algebraRetake.Id, daniel.Id, 9M);
        await algebraTeacher.AddStudentActivityNote(algebra.Id, algebraWork.Id, elisa.Id, 4M);
        await algebraTeacher.AddStudentActivityNote(algebra.Id, algebraExam.Id, elisa.Id, 6M);
        await algebraTeacher.AddStudentActivityNote(algebra.Id, algebraRetake.Id, elisa.Id, 5M);

        // Act
        var result = await director.GetStudentDetails(ana.Id);

        // Assert
        var details = result.Success;
        details.Classes.Should().HaveCount(2);
        details.Classes.First(c => c.Id == geometry.Id).AverageGrade.Should().Be(7.5M);
        details.Classes.First(c => c.Id == algebra.Id).AverageGrade.Should().Be(5.3M);
        details.AverageGrade.Should().Be(6.4M);
    }

    [Test]
    public async Task Students_GetStudentDetails_Should_get_a_different_average_grade_for_each_student_of_the_same_class()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var ana = await director.CreateStudent("Ana Beatriz", DataGen.Email).Success();
        var bruno = await director.CreateStudent("Bruno Silva", DataGen.Email).Success();
        var carla = await director.CreateStudent("Carla Dias", DataGen.Email).Success();

        var @class = await director.ShortcutCreateStartedClass([ana.Id, bruno.Id, carla.Id]);
        var teacher = await _back.LoginAs(@class.TeacherEmail);

        var work = await teacher.CreateClassActivity(@class.Id, ClassNoteType.N1, weight: 70).Success();
        var presentation = await teacher.CreateClassActivity(@class.Id, ClassNoteType.N1, weight: 30).Success();
        var exam = await teacher.CreateClassActivity(@class.Id, ClassNoteType.N2, weight: 100).Success();

        await teacher.AddStudentActivityNote(@class.Id, work.Id, ana.Id, 10M);
        await teacher.AddStudentActivityNote(@class.Id, presentation.Id, ana.Id, 4M);
        await teacher.AddStudentActivityNote(@class.Id, exam.Id, ana.Id, 6M);
        await teacher.AddStudentActivityNote(@class.Id, work.Id, bruno.Id, 5M);
        await teacher.AddStudentActivityNote(@class.Id, presentation.Id, bruno.Id, 10M);
        await teacher.AddStudentActivityNote(@class.Id, exam.Id, bruno.Id, 9M);
        await teacher.AddStudentActivityNote(@class.Id, work.Id, carla.Id, 8M);
        await teacher.AddStudentActivityNote(@class.Id, presentation.Id, carla.Id, 4M);
        await teacher.AddStudentActivityNote(@class.Id, exam.Id, carla.Id, 2M);

        // Act
        var anaDetails = await director.GetStudentDetails(ana.Id);
        var brunoDetails = await director.GetStudentDetails(bruno.Id);
        var carlaDetails = await director.GetStudentDetails(carla.Id);

        // Assert
        anaDetails.Success.Classes.Should().ContainSingle().Which.AverageGrade.Should().Be(7.1M);
        anaDetails.Success.AverageGrade.Should().Be(7.1M);
        brunoDetails.Success.Classes.Should().ContainSingle().Which.AverageGrade.Should().Be(7.8M);
        brunoDetails.Success.AverageGrade.Should().Be(7.8M);
        carlaDetails.Success.Classes.Should().ContainSingle().Which.AverageGrade.Should().Be(4.4M);
        carlaDetails.Success.AverageGrade.Should().Be(4.4M);
    }

    [Test]
    public async Task Students_GetStudentDetails_Should_get_partial_average_grade_when_activities_do_not_fill_the_whole_weight()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var student = await director.CreateStudent(DataGen.UserName, DataGen.Email).Success();
        var @class = await director.ShortcutCreateStartedClass([student.Id]);
        var teacher = await _back.LoginAs(@class.TeacherEmail);

        var quiz = await teacher.CreateClassActivity(@class.Id, ClassNoteType.N1, weight: 30).Success();
        var work = await teacher.CreateClassActivity(@class.Id, ClassNoteType.N1, weight: 20).Success();
        await teacher.CreateClassActivity(@class.Id, ClassNoteType.N1, weight: 50);
        var firstExam = await teacher.CreateClassActivity(@class.Id, ClassNoteType.N2, weight: 25).Success();
        var secondExam = await teacher.CreateClassActivity(@class.Id, ClassNoteType.N2, weight: 25).Success();

        await teacher.AddStudentActivityNote(@class.Id, quiz.Id, student.Id, 10M);
        await teacher.AddStudentActivityNote(@class.Id, work.Id, student.Id, 5M);
        await teacher.AddStudentActivityNote(@class.Id, firstExam.Id, student.Id, 8M);
        await teacher.AddStudentActivityNote(@class.Id, secondExam.Id, student.Id, 6M);

        // Act
        var result = await director.GetStudentDetails(student.Id);

        // Assert
        var details = result.Success;
        details.Classes.Should().ContainSingle().Which.AverageGrade.Should().Be(3.8M);
        details.AverageGrade.Should().Be(3.8M);
    }

    [Test]
    public async Task Students_GetStudentDetails_Should_get_average_grade_of_each_class_with_the_average_of_three_rule()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        await director.SetupInstitutionConfig(gradeRule: ClassGradeRule.AverageOfThree);

        var student = await director.CreateStudent(DataGen.UserName, DataGen.Email).Success();
        var geometry = await director.ShortcutCreateStartedClass([student.Id]);
        var algebra = await director.ShortcutCreateStartedClass([student.Id], "Álgebra", Day.Tuesday);

        var geometryTeacher = await _back.LoginAs(geometry.TeacherEmail);
        var geometryN1 = await geometryTeacher.CreateClassActivity(geometry.Id, ClassNoteType.N1, weight: 100).Success();
        var geometryN2 = await geometryTeacher.CreateClassActivity(geometry.Id, ClassNoteType.N2, weight: 100).Success();
        var geometryN3 = await geometryTeacher.CreateClassActivity(geometry.Id, ClassNoteType.N3, weight: 100).Success();
        await geometryTeacher.AddStudentActivityNote(geometry.Id, geometryN1.Id, student.Id, 9M);
        await geometryTeacher.AddStudentActivityNote(geometry.Id, geometryN2.Id, student.Id, 6M);
        await geometryTeacher.AddStudentActivityNote(geometry.Id, geometryN3.Id, student.Id, 3M);

        var algebraTeacher = await _back.LoginAs(algebra.TeacherEmail);
        var algebraN1 = await algebraTeacher.CreateClassActivity(algebra.Id, ClassNoteType.N1, weight: 50).Success();
        var algebraN2 = await algebraTeacher.CreateClassActivity(algebra.Id, ClassNoteType.N2, weight: 100).Success();
        await algebraTeacher.AddStudentActivityNote(algebra.Id, algebraN1.Id, student.Id, 10M);
        await algebraTeacher.AddStudentActivityNote(algebra.Id, algebraN2.Id, student.Id, 5M);

        // Act
        var result = await director.GetStudentDetails(student.Id);

        // Assert
        var details = result.Success;
        details.Classes.First(c => c.Id == geometry.Id).AverageGrade.Should().Be(6M);
        details.Classes.First(c => c.Id == algebra.Id).AverageGrade.Should().Be(3.3M);
        details.AverageGrade.Should().Be(4.7M);
    }

    [Test]
    public async Task Students_GetStudentDetails_Should_get_average_grade_of_each_class_with_the_third_note_as_substitutive()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        await director.SetupInstitutionConfig(gradeRule: ClassGradeRule.AverageOrThird);

        var student = await director.CreateStudent(DataGen.UserName, DataGen.Email).Success();
        var geometry = await director.ShortcutCreateStartedClass([student.Id]);
        var algebra = await director.ShortcutCreateStartedClass([student.Id], "Álgebra", Day.Tuesday);

        var geometryTeacher = await _back.LoginAs(geometry.TeacherEmail);
        var geometryN1 = await geometryTeacher.CreateClassActivity(geometry.Id, ClassNoteType.N1, weight: 100).Success();
        var geometryN2 = await geometryTeacher.CreateClassActivity(geometry.Id, ClassNoteType.N2, weight: 100).Success();
        var geometryN3 = await geometryTeacher.CreateClassActivity(geometry.Id, ClassNoteType.N3, weight: 100).Success();
        await geometryTeacher.AddStudentActivityNote(geometry.Id, geometryN1.Id, student.Id, 4M);
        await geometryTeacher.AddStudentActivityNote(geometry.Id, geometryN2.Id, student.Id, 5M);
        await geometryTeacher.AddStudentActivityNote(geometry.Id, geometryN3.Id, student.Id, 8M);

        var algebraTeacher = await _back.LoginAs(algebra.TeacherEmail);
        var algebraN1 = await algebraTeacher.CreateClassActivity(algebra.Id, ClassNoteType.N1, weight: 100).Success();
        var algebraN2 = await algebraTeacher.CreateClassActivity(algebra.Id, ClassNoteType.N2, weight: 100).Success();
        var algebraN3 = await algebraTeacher.CreateClassActivity(algebra.Id, ClassNoteType.N3, weight: 100).Success();
        await algebraTeacher.AddStudentActivityNote(algebra.Id, algebraN1.Id, student.Id, 10M);
        await algebraTeacher.AddStudentActivityNote(algebra.Id, algebraN2.Id, student.Id, 8M);
        await algebraTeacher.AddStudentActivityNote(algebra.Id, algebraN3.Id, student.Id, 6M);

        // Act
        var result = await director.GetStudentDetails(student.Id);

        // Assert
        var details = result.Success;
        details.Classes.First(c => c.Id == geometry.Id).AverageGrade.Should().Be(8M);
        details.Classes.First(c => c.Id == algebra.Id).AverageGrade.Should().Be(9M);
        details.AverageGrade.Should().Be(8.5M);
    }

    [Test]
    public async Task Students_GetStudentDetails_Should_get_average_grade_only_of_started_classes()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var student = await director.CreateStudent(DataGen.UserName, DataGen.Email).Success();

        var started = await director.ShortcutCreateStartedClass([student.Id]);
        var finalized = await director.ShortcutCreateStartedClass([student.Id], "Álgebra", Day.Tuesday);

        var startedTeacher = await _back.LoginAs(started.TeacherEmail);
        var startedActivity = await startedTeacher.CreateClassActivity(started.Id, ClassNoteType.N1, weight: 100).Success();
        await startedTeacher.AddStudentActivityNote(started.Id, startedActivity.Id, student.Id, 8M);

        var finalizedTeacher = await _back.LoginAs(finalized.TeacherEmail);
        var finalizedN1 = await finalizedTeacher.CreateClassActivity(finalized.Id, ClassNoteType.N1, weight: 100).Success();
        var finalizedN2 = await finalizedTeacher.CreateClassActivity(finalized.Id, ClassNoteType.N2, weight: 100).Success();
        await finalizedTeacher.AddStudentActivityNote(finalized.Id, finalizedN1.Id, student.Id, 10M);
        await finalizedTeacher.AddStudentActivityNote(finalized.Id, finalizedN2.Id, student.Id, 10M);

        // Nenhum endpoint encerra uma turma, então o status vai direto no banco.
        await using (var arrangeCtx = _back.GetDbContext())
        {
            var @class = await arrangeCtx.Classes.FirstAsync(c => c.Id == finalized.Id);
            @class.Status = ClassStatus.Finalized;
            await arrangeCtx.SaveChangesAsync();
        }

        // Act
        var result = await director.GetStudentDetails(student.Id);

        // Assert
        var details = result.Success;
        details.Classes.First(c => c.Id == started.Id).AverageGrade.Should().Be(4M);
        details.Classes.First(c => c.Id == finalized.Id).AverageGrade.Should().Be(10M);
        details.AverageGrade.Should().Be(4M);
    }

    [Test]
    public async Task Students_GetStudentDetails_Should_count_a_class_without_activities_as_zero_in_the_average_grade()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var student = await director.CreateStudent(DataGen.UserName, DataGen.Email).Success();

        var geometry = await director.ShortcutCreateStartedClass([student.Id]);
        var algebra = await director.ShortcutCreateStartedClass([student.Id], "Álgebra", Day.Tuesday);

        var geometryTeacher = await _back.LoginAs(geometry.TeacherEmail);
        var n1 = await geometryTeacher.CreateClassActivity(geometry.Id, ClassNoteType.N1, weight: 100).Success();
        var n2 = await geometryTeacher.CreateClassActivity(geometry.Id, ClassNoteType.N2, weight: 100).Success();
        await geometryTeacher.AddStudentActivityNote(geometry.Id, n1.Id, student.Id, 9M);
        await geometryTeacher.AddStudentActivityNote(geometry.Id, n2.Id, student.Id, 6M);

        // Act
        var result = await director.GetStudentDetails(student.Id);

        // Assert
        var details = result.Success;
        details.Classes.First(c => c.Id == geometry.Id).AverageGrade.Should().Be(7.5M);
        details.Classes.First(c => c.Id == algebra.Id).AverageGrade.Should().Be(0M);
        details.AverageGrade.Should().Be(3.8M);
    }

    #endregion
}
