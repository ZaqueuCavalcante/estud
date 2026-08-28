using Estud.Back.Features.CourseCurriculums.CreateCourseCurriculum;

namespace Estud.Tests.Integration;

public partial class IntegrationTests
{
    [Test]
    public async Task ScheduleFunctional_Simple_one_online_class()
    {
        // Arrange
        var directorClient = await _back.LoggedAsDirector();

        var discipline = await directorClient.CreateDiscipline().Success();
        var period = await directorClient.GetFirstAcademicPeriod();
        var @class = await directorClient.CreateClass(discipline.Id, period.Id).Success();

        var teacher = await directorClient.CreateTeacher(DataGen.UserName, DataGen.Email).Success();
        await directorClient.AssignDisciplinesToTeacher(teacher.Id, [discipline.Id]);
        await directorClient.UpdateClassTeachers(@class.Id, [teacher.Id]);
        await directorClient.UpdateClassSchedules(@class.Id, [(Day.Monday, Hour.H07_00, Hour.H10_00, teacher.Id, null)]);

        await directorClient.ReleaseClassForEnrollment(@class.Id);

        var student = await directorClient.CreateStudent(DataGen.UserName, DataGen.Email).Success();
        await directorClient.AssignStudentToClass(student.Id, @class.Id);

        await directorClient.StartClass(@class.Id);

        var teacherClient = await _back.LoginAs(teacher.Email);
        var studentClient = await _back.LoginAs(student.Email);

        // Act
        var teacherAgenda = await teacherClient.GetTeacherAgenda().Success();
        var studentAgenda = await studentClient.GetStudentAgenda().Success();

        // Assert
        var teacherDay = teacherAgenda.Days.Should().ContainSingle().Which;
        teacherDay.Day.Should().Be(Day.Monday);
        var teacherDiscipline = teacherDay.Disciplines.Should().ContainSingle().Which;
        teacherDiscipline.ClassId.Should().Be(@class.Id);
        teacherDiscipline.Name.Should().Be("Geometria");
        teacherDiscipline.Start.Should().Be(Hour.H07_00);
        teacherDiscipline.End.Should().Be(Hour.H10_00);
        teacherDiscipline.ClassroomName.Should().BeNull();

        var studentDay = studentAgenda.Days.Should().ContainSingle().Which;
        studentDay.Day.Should().Be(Day.Monday);
        var studentDiscipline = studentDay.Disciplines.Should().ContainSingle().Which;
        studentDiscipline.ClassId.Should().Be(@class.Id);
        studentDiscipline.Name.Should().Be("Geometria");
        studentDiscipline.Start.Should().Be(Hour.H07_00);
        studentDiscipline.End.Should().Be(Hour.H10_00);
        studentDiscipline.ClassroomName.Should().BeNull();
    }

    [Test]
    public async Task ScheduleFunctional_One_presential_class()
    {
        // Arrange
        var directorClient = await _back.LoggedAsDirector();

        var campus = await directorClient.CreateCampus(name: "Agreste I").Success();
        var sala01 = await directorClient.CreateClassroom(campus.Id, name: "Sala 01").Success();

        var course = await directorClient.CreateCourse(name: "ADS").Success();
        var discipline = await directorClient.CreateDiscipline().Success();
        await directorClient.AssignDisciplinesToCourse(course.Id, [discipline.Id]);
        var curriculum = await directorClient.CreateCourseCurriculum(course.Id, "Grade 2024", [new(discipline.Id, 1, 4, 80)]).Success();

        var period = await directorClient.GetFirstAcademicPeriod();
        var offering = await directorClient.CreateCourseOffering(campus.Id, course.Id, curriculum.Id, period.Id, CourseSession.Morning).Success();

        var @class = await directorClient.CreateClass(discipline.Id, period.Id, campusId: campus.Id).Success();

        var teacher = await directorClient.CreateTeacher(DataGen.UserName, DataGen.Email).Success();
        await directorClient.AssignDisciplinesToTeacher(teacher.Id, [discipline.Id]);
        await directorClient.AssignCampiToTeacher(teacher.Id, [campus.Id]);
        await directorClient.UpdateClassTeachers(@class.Id, [teacher.Id]);
        await directorClient.UpdateClassSchedules(@class.Id, [(Day.Monday, Hour.H07_00, Hour.H10_00, teacher.Id, sala01.Id)]);

        await directorClient.ReleaseClassForEnrollment(@class.Id);

        var student = await directorClient.CreateStudent(DataGen.UserName, DataGen.Email).Success();
        await directorClient.EnrollStudentInCourseOffering(student.Id, offering.Id);
        await directorClient.AssignStudentToClass(student.Id, @class.Id);

        await directorClient.StartClass(@class.Id);

        var teacherClient = await _back.LoginAs(teacher.Email);
        var studentClient = await _back.LoginAs(student.Email);

        // Act
        var occupancy = await directorClient.GetCampusOccupancy(campus.Id).Success();
        var teacherAgenda = await teacherClient.GetTeacherAgenda().Success();
        var studentAgenda = await studentClient.GetStudentAgenda().Success();

        // Assert — manager: uma única célula com uso, o resto da semana aberta e vazia.
        occupancy.TotalClassrooms.Should().Be(1);
        occupancy.OpenCells.Should().Be(15);

        var monday = occupancy.Cells.First(c => c.Day == Day.Monday && c.Shift == Shift.Morning);
        monday.Open.Should().BeTrue();
        monday.UsedMinutes.Should().Be(180);
        monday.AvailableMinutes.Should().Be(300); // 1 sala x 07h–12h
        monday.UsedMinutesRate.Should().Be(60M);
        monday.Classrooms.Should().ContainSingle().Which.UsedMinutes.Should().Be(180);

        // Espaço ocioso: a mesma sala aparece livre em todas as outras células abertas.
        var otherOpenCells = occupancy.Cells.Where(c => c.Open && !(c.Day == Day.Monday && c.Shift == Shift.Morning)).ToList();
        otherOpenCells.Should().HaveCount(14);
        otherOpenCells.Should().OnlyContain(c => c.UsedMinutes == 0 && c.UsedMinutesRate == 0);
        otherOpenCells.Should().OnlyContain(c => c.Classrooms.Count == 1);

        // 180min de 3.900min (1 sala x 13h * 60min x 5 dias abertos).
        occupancy.OverallUsedMinutesRate.Should().Be(4.62M);

        // Assert — teacher e student: 1 dia, 1 disciplina, agora com o nome da sala.
        var teacherDay = teacherAgenda.Days.Should().ContainSingle().Which;
        teacherDay.Day.Should().Be(Day.Monday);
        var teacherDiscipline = teacherDay.Disciplines.Should().ContainSingle().Which;
        teacherDiscipline.ClassId.Should().Be(@class.Id);
        teacherDiscipline.Name.Should().Be("Geometria");
        teacherDiscipline.Start.Should().Be(Hour.H07_00);
        teacherDiscipline.End.Should().Be(Hour.H10_00);
        teacherDiscipline.ClassroomName.Should().Be("Sala 01");

        var studentDay = studentAgenda.Days.Should().ContainSingle().Which;
        studentDay.Day.Should().Be(Day.Monday);
        var studentDiscipline = studentDay.Disciplines.Should().ContainSingle().Which;
        studentDiscipline.ClassId.Should().Be(@class.Id);
        studentDiscipline.Name.Should().Be("Geometria");
        studentDiscipline.ClassroomName.Should().Be("Sala 01");
    }

    [Test]
    public async Task ScheduleFunctional_One_classroom_two_shifts()
    {
        // Arrange
        var directorClient = await _back.LoggedAsDirector();

        var campus = await directorClient.CreateCampus(name: "Agreste I").Success();
        var sala01 = await directorClient.CreateClassroom(campus.Id, name: "Sala 01").Success();

        var period = await directorClient.GetFirstAcademicPeriod();

        // Curso da manhã
        var courseA = await directorClient.CreateCourse(name: "ADS").Success();
        var disciplineA = await directorClient.CreateDiscipline(name: "Geometria").Success();
        await directorClient.AssignDisciplinesToCourse(courseA.Id, [disciplineA.Id]);
        var curriculumA = await directorClient.CreateCourseCurriculum(courseA.Id, "Grade ADS 2024", [new(disciplineA.Id, 1, 4, 80)]).Success();
        var offeringA = await directorClient.CreateCourseOffering(campus.Id, courseA.Id, curriculumA.Id, period.Id, CourseSession.Morning).Success();

        // Curso da noite
        var courseB = await directorClient.CreateCourse(name: "Direito").Success();
        var disciplineB = await directorClient.CreateDiscipline(name: "Direito Civil").Success();
        await directorClient.AssignDisciplinesToCourse(courseB.Id, [disciplineB.Id]);
        var curriculumB = await directorClient.CreateCourseCurriculum(courseB.Id, "Grade Direito 2024", [new(disciplineB.Id, 1, 4, 80)]).Success();
        var offeringB = await directorClient.CreateCourseOffering(campus.Id, courseB.Id, curriculumB.Id, period.Id, CourseSession.Evening).Success();

        var teacherA = await directorClient.CreateTeacher(DataGen.UserName, DataGen.Email).Success();
        await directorClient.AssignDisciplinesToTeacher(teacherA.Id, [disciplineA.Id]);
        await directorClient.AssignCampiToTeacher(teacherA.Id, [campus.Id]);

        var teacherB = await directorClient.CreateTeacher(DataGen.UserName, DataGen.Email).Success();
        await directorClient.AssignDisciplinesToTeacher(teacherB.Id, [disciplineB.Id]);
        await directorClient.AssignCampiToTeacher(teacherB.Id, [campus.Id]);

        var classA = await directorClient.CreateClass(disciplineA.Id, period.Id, campusId: campus.Id).Success();
        await directorClient.UpdateClassTeachers(classA.Id, [teacherA.Id]);
        await directorClient.UpdateClassSchedules(classA.Id, [(Day.Monday, Hour.H07_00, Hour.H10_00, teacherA.Id, sala01.Id)]);
        await directorClient.ReleaseClassForEnrollment(classA.Id);

        // Mesma sala, mesmo dia, outro turno.
        var classB = await directorClient.CreateClass(disciplineB.Id, period.Id, campusId: campus.Id).Success();
        await directorClient.UpdateClassTeachers(classB.Id, [teacherB.Id]);
        await directorClient.UpdateClassSchedules(classB.Id, [(Day.Monday, Hour.H19_00, Hour.H22_00, teacherB.Id, sala01.Id)]);
        await directorClient.ReleaseClassForEnrollment(classB.Id);

        var studentA = await directorClient.CreateStudent(DataGen.UserName, DataGen.Email).Success();
        await directorClient.EnrollStudentInCourseOffering(studentA.Id, offeringA.Id);
        await directorClient.AssignStudentToClass(studentA.Id, classA.Id);

        var studentB = await directorClient.CreateStudent(DataGen.UserName, DataGen.Email).Success();
        await directorClient.EnrollStudentInCourseOffering(studentB.Id, offeringB.Id);
        await directorClient.AssignStudentToClass(studentB.Id, classB.Id);

        await directorClient.StartClass(classA.Id);
        await directorClient.StartClass(classB.Id);

        var teacherAClient = await _back.LoginAs(teacherA.Email);
        var teacherBClient = await _back.LoginAs(teacherB.Email);
        var studentAClient = await _back.LoginAs(studentA.Email);
        var studentBClient = await _back.LoginAs(studentB.Email);

        // Act
        var occupancy = await directorClient.GetCampusOccupancy(campus.Id).Success();
        var teacherAAgenda = await teacherAClient.GetTeacherAgenda().Success();
        var teacherBAgenda = await teacherBClient.GetTeacherAgenda().Success();
        var studentAAgenda = await studentAClient.GetStudentAgenda().Success();
        var studentBAgenda = await studentBClient.GetStudentAgenda().Success();

        // Assert — manager: duas células com uso no mesmo dia, cada uma medida
        // contra os minutos de abertura do seu próprio turno.
        occupancy.TotalClassrooms.Should().Be(1);

        var morning = occupancy.Cells.First(c => c.Day == Day.Monday && c.Shift == Shift.Morning);
        morning.UsedMinutes.Should().Be(180);
        morning.AvailableMinutes.Should().Be(300); // 07h–12h
        morning.UsedMinutesRate.Should().Be(60M);

        var evening = occupancy.Cells.First(c => c.Day == Day.Monday && c.Shift == Shift.Evening);
        evening.UsedMinutes.Should().Be(180);
        evening.AvailableMinutes.Should().Be(180); // 17h–22h
        evening.UsedMinutesRate.Should().Be(100);

        // Turno cheio x turno vazio no mesmo dia.
        var afternoon = occupancy.Cells.First(c => c.Day == Day.Monday && c.Shift == Shift.Afternoon);
        afternoon.Open.Should().BeTrue();
        afternoon.UsedMinutes.Should().Be(0);
        afternoon.AvailableMinutes.Should().Be(300); // 13h–18h

        occupancy.Cells.Where(c => c.Day != Day.Monday).Should().OnlyContain(c => c.UsedMinutes == 0);

        // 360min de 3.900min.
        occupancy.OverallUsedMinutesRate.Should().Be(9.23M);

        // Assert — cada professor vê só a sua aula.
        var teacherADay = teacherAAgenda.Days.Should().ContainSingle().Which;
        teacherADay.Day.Should().Be(Day.Monday);
        teacherADay.Disciplines.Should().ContainSingle().Which.Name.Should().Be("Geometria");
        teacherADay.Disciplines[0].Start.Should().Be(Hour.H07_00);
        teacherADay.Disciplines[0].ClassroomName.Should().Be("Sala 01");

        var teacherBDay = teacherBAgenda.Days.Should().ContainSingle().Which;
        teacherBDay.Day.Should().Be(Day.Monday);
        teacherBDay.Disciplines.Should().ContainSingle().Which.Name.Should().Be("Direito Civil");
        teacherBDay.Disciplines[0].Start.Should().Be(Hour.H19_00);
        teacherBDay.Disciplines[0].ClassroomName.Should().Be("Sala 01");

        // Assert — cada aluno vê só a aula do curso em que está matriculado.
        var studentADay = studentAAgenda.Days.Should().ContainSingle().Which;
        studentADay.Disciplines.Should().ContainSingle().Which.ClassId.Should().Be(classA.Id);

        var studentBDay = studentBAgenda.Days.Should().ContainSingle().Which;
        studentBDay.Disciplines.Should().ContainSingle().Which.ClassId.Should().Be(classB.Id);
    }

    [Test]
    public async Task ScheduleFunctional_One_class_with_many_disciplines_in_the_week()
    {
        // Arrange
        var directorClient = await _back.LoggedAsDirector();

        var campus = await directorClient.CreateCampus(name: "Agreste I").Success();
        var sala01 = await directorClient.CreateClassroom(campus.Id, name: "Sala 01").Success();

        var period = await directorClient.GetFirstAcademicPeriod();

        var course = await directorClient.CreateCourse(name: "ADS").Success();
        var geometria = await directorClient.CreateDiscipline(name: "Geometria").Success();
        var algoritmos = await directorClient.CreateDiscipline(name: "Algoritmos").Success();
        var bancoDeDados = await directorClient.CreateDiscipline(name: "Banco de Dados").Success();
        var redes = await directorClient.CreateDiscipline(name: "Redes").Success();
        await directorClient.AssignDisciplinesToCourse(course.Id, [geometria.Id, algoritmos.Id, bancoDeDados.Id, redes.Id]);

        var curriculum = await directorClient.CreateCourseCurriculum(course.Id, "Grade ADS 2024",
        [
            new(geometria.Id, 1, 4, 80),
            new(algoritmos.Id, 1, 4, 80),
            new(bancoDeDados.Id, 1, 4, 80),
            new(redes.Id, 1, 4, 80),
        ]).Success();
        var offering = await directorClient.CreateCourseOffering(campus.Id, course.Id, curriculum.Id, period.Id, CourseSession.Morning).Success();

        // Professor A com 1 disciplina, professor B com 2, professor C com 1.
        var teacherA = await directorClient.CreateTeacher(DataGen.UserName, DataGen.Email).Success();
        await directorClient.AssignDisciplinesToTeacher(teacherA.Id, [geometria.Id]);
        await directorClient.AssignCampiToTeacher(teacherA.Id, [campus.Id]);

        var teacherB = await directorClient.CreateTeacher(DataGen.UserName, DataGen.Email).Success();
        await directorClient.AssignDisciplinesToTeacher(teacherB.Id, [algoritmos.Id, bancoDeDados.Id]);
        await directorClient.AssignCampiToTeacher(teacherB.Id, [campus.Id]);

        var teacherC = await directorClient.CreateTeacher(DataGen.UserName, DataGen.Email).Success();
        await directorClient.AssignDisciplinesToTeacher(teacherC.Id, [redes.Id]);
        await directorClient.AssignCampiToTeacher(teacherC.Id, [campus.Id]);

        var student = await directorClient.CreateStudent(DataGen.UserName, DataGen.Email).Success();
        await directorClient.EnrollStudentInCourseOffering(student.Id, offering.Id);

        var week = new (int DisciplineId, int TeacherId, Day Day)[]
        {
            (geometria.Id, teacherA.Id, Day.Monday),
            (algoritmos.Id, teacherB.Id, Day.Tuesday),
            (bancoDeDados.Id, teacherB.Id, Day.Wednesday),
            (redes.Id, teacherC.Id, Day.Thursday),
        };

        var classIds = new List<int>();
        foreach (var (disciplineId, teacherId, day) in week)
        {
            var @class = await directorClient.CreateClass(disciplineId, period.Id, campusId: campus.Id).Success();
            await directorClient.UpdateClassTeachers(@class.Id, [teacherId]);
            await directorClient.UpdateClassSchedules(@class.Id, [(day, Hour.H07_00, Hour.H10_00, teacherId, sala01.Id)]);
            await directorClient.ReleaseClassForEnrollment(@class.Id);
            await directorClient.AssignStudentToClass(student.Id, @class.Id);
            await directorClient.StartClass(@class.Id);
            classIds.Add(@class.Id);
        }

        var teacherAClient = await _back.LoginAs(teacherA.Email);
        var teacherBClient = await _back.LoginAs(teacherB.Email);
        var teacherCClient = await _back.LoginAs(teacherC.Email);
        var studentClient = await _back.LoginAs(student.Email);

        // Act
        var occupancy = await directorClient.GetCampusOccupancy(campus.Id).Success();
        var teacherAAgenda = await teacherAClient.GetTeacherAgenda().Success();
        var teacherBAgenda = await teacherBClient.GetTeacherAgenda().Success();
        var teacherCAgenda = await teacherCClient.GetTeacherAgenda().Success();
        var studentAgenda = await studentClient.GetStudentAgenda().Success();

        // Assert — manager: 4 células usadas, todas no turno da manhã.
        var usedCells = occupancy.Cells.Where(c => c.UsedMinutes > 0).ToList();
        usedCells.Should().HaveCount(4);
        usedCells.Should().OnlyContain(c => c.Shift == Shift.Morning);
        usedCells.Should().OnlyContain(c => c.UsedMinutes == 180 && c.UsedMinutesRate == 60M);
        usedCells.Select(c => c.Day).Should().BeEquivalentTo([Day.Monday, Day.Tuesday, Day.Wednesday, Day.Thursday]);

        occupancy.OverallUsedMinutesRate.Should().Be(18.46M);

        // Assert — carga horária: professor B com 2 dias, os outros com 1.
        teacherAAgenda.Days.Should().ContainSingle().Which.Day.Should().Be(Day.Monday);

        teacherBAgenda.Days.Should().HaveCount(2);
        teacherBAgenda.Days[0].Day.Should().Be(Day.Tuesday);
        teacherBAgenda.Days[0].Disciplines.Should().ContainSingle().Which.Name.Should().Be("Algoritmos");
        teacherBAgenda.Days[1].Day.Should().Be(Day.Wednesday);
        teacherBAgenda.Days[1].Disciplines.Should().ContainSingle().Which.Name.Should().Be("Banco de Dados");

        teacherCAgenda.Days.Should().ContainSingle().Which.Day.Should().Be(Day.Thursday);

        // Assert — student: 4 dias preenchidos, disciplinas diferentes, mesma sala.
        studentAgenda.Days.Should().HaveCount(4);
        studentAgenda.Days.Select(d => d.Day).Should().Equal(
            Day.Monday, Day.Tuesday, Day.Wednesday, Day.Thursday);
        studentAgenda.Days.Should().OnlyContain(d => d.Disciplines.Count == 1);
        studentAgenda.Days.SelectMany(d => d.Disciplines).Select(d => d.Name).Should().Equal(
            "Geometria", "Algoritmos", "Banco de Dados", "Redes");
        studentAgenda.Days.SelectMany(d => d.Disciplines)
            .Should().OnlyContain(d => d.ClassroomName == "Sala 01");
        studentAgenda.Days.SelectMany(d => d.Disciplines).Select(d => d.ClassId)
            .Should().BeEquivalentTo(classIds.ConvertAll(id => (int?)id));
    }

    [Test]
    public async Task ScheduleFunctional_Two_classes_two_classrooms()
    {
        // Arrange
        var directorClient = await _back.LoggedAsDirector();

        var campus = await directorClient.CreateCampus(name: "Agreste I").Success();
        var sala01 = await directorClient.CreateClassroom(campus.Id, name: "Sala 01").Success();
        var sala02 = await directorClient.CreateClassroom(campus.Id, name: "Sala 02").Success();

        var period = await directorClient.GetFirstAcademicPeriod();

        var course = await directorClient.CreateCourse(name: "ADS").Success();
        var geometria = await directorClient.CreateDiscipline(name: "Geometria").Success();
        var algoritmos = await directorClient.CreateDiscipline(name: "Algoritmos").Success();
        var bancoDeDados = await directorClient.CreateDiscipline(name: "Banco de Dados").Success();
        var redes = await directorClient.CreateDiscipline(name: "Redes").Success();
        await directorClient.AssignDisciplinesToCourse(course.Id, [geometria.Id, algoritmos.Id, bancoDeDados.Id, redes.Id]);

        var curriculum = await directorClient.CreateCourseCurriculum(course.Id, "Grade ADS 2024",
        [
            new(geometria.Id, 1, 4, 80),
            new(algoritmos.Id, 1, 4, 80),
            new(bancoDeDados.Id, 1, 4, 80),
            new(redes.Id, 1, 4, 80),
        ]).Success();
        var offering = await directorClient.CreateCourseOffering(campus.Id, course.Id, curriculum.Id, period.Id, CourseSession.Morning).Success();

        var teacherA = await directorClient.CreateTeacher(DataGen.UserName, DataGen.Email).Success();
        await directorClient.AssignDisciplinesToTeacher(teacherA.Id, [geometria.Id]);
        await directorClient.AssignCampiToTeacher(teacherA.Id, [campus.Id]);

        var teacherB = await directorClient.CreateTeacher(DataGen.UserName, DataGen.Email).Success();
        await directorClient.AssignDisciplinesToTeacher(teacherB.Id, [algoritmos.Id, bancoDeDados.Id]);
        await directorClient.AssignCampiToTeacher(teacherB.Id, [campus.Id]);

        var teacherC = await directorClient.CreateTeacher(DataGen.UserName, DataGen.Email).Success();
        await directorClient.AssignDisciplinesToTeacher(teacherC.Id, [redes.Id]);
        await directorClient.AssignCampiToTeacher(teacherC.Id, [campus.Id]);

        var studentA = await directorClient.CreateStudent(DataGen.UserName, DataGen.Email).Success();
        await directorClient.EnrollStudentInCourseOffering(studentA.Id, offering.Id);

        // Turma 1 de cada disciplina: manhã, Sala 01, um dia por disciplina.
        var week = new (int DisciplineId, int TeacherId, Day Day)[]
        {
            (geometria.Id, teacherA.Id, Day.Monday),
            (algoritmos.Id, teacherB.Id, Day.Tuesday),
            (bancoDeDados.Id, teacherB.Id, Day.Wednesday),
            (redes.Id, teacherC.Id, Day.Thursday),
        };

        foreach (var (disciplineId, teacherId, day) in week)
        {
            var @class = await directorClient.CreateClass(disciplineId, period.Id, campusId: campus.Id).Success();
            await directorClient.UpdateClassTeachers(@class.Id, [teacherId]);
            await directorClient.UpdateClassSchedules(@class.Id, [(day, Hour.H07_00, Hour.H10_00, teacherId, sala01.Id)]);
            await directorClient.ReleaseClassForEnrollment(@class.Id);
            await directorClient.AssignStudentToClass(studentA.Id, @class.Id);
            await directorClient.StartClass(@class.Id);
        }

        // Turma 2 de Algoritmos: mesmo curso, mesmo período, mesmo professor,
        // à tarde — terça na Sala 02 e quinta na Sala 01.
        var secondClass = await directorClient.CreateClass(algoritmos.Id, period.Id, campusId: campus.Id).Success();
        await directorClient.UpdateClassTeachers(secondClass.Id, [teacherB.Id]);
        await directorClient.UpdateClassSchedules(secondClass.Id,
        [
            (Day.Tuesday, Hour.H13_00, Hour.H16_00, teacherB.Id, sala02.Id),
            (Day.Thursday, Hour.H13_00, Hour.H16_00, teacherB.Id, sala01.Id),
        ]);
        await directorClient.ReleaseClassForEnrollment(secondClass.Id);

        var studentB = await directorClient.CreateStudent(DataGen.UserName, DataGen.Email).Success();
        await directorClient.EnrollStudentInCourseOffering(studentB.Id, offering.Id);
        await directorClient.AssignStudentToClass(studentB.Id, secondClass.Id);

        var studentC = await directorClient.CreateStudent(DataGen.UserName, DataGen.Email).Success();
        await directorClient.EnrollStudentInCourseOffering(studentC.Id, offering.Id);
        await directorClient.AssignStudentToClass(studentC.Id, secondClass.Id);

        await directorClient.StartClass(secondClass.Id);

        var teacherBClient = await _back.LoginAs(teacherB.Email);
        var studentAClient = await _back.LoginAs(studentA.Email);
        var studentBClient = await _back.LoginAs(studentB.Email);

        // Act
        var occupancy = await directorClient.GetCampusOccupancy(campus.Id).Success();
        var teacherBAgenda = await teacherBClient.GetTeacherAgenda().Success();
        var studentAAgenda = await studentAClient.GetStudentAgenda().Success();
        var studentBAgenda = await studentBClient.GetStudentAgenda().Success();

        // Assert — manager: 2 salas, com detalhe por sala em cada célula.
        occupancy.TotalClassrooms.Should().Be(2);
        occupancy.Cells.Where(c => c.Open).Should().OnlyContain(c => c.Classrooms.Count == 2);

        var tuesdayAfternoon = occupancy.Cells.First(c => c.Day == Day.Tuesday && c.Shift == Shift.Afternoon);
        tuesdayAfternoon.UsedMinutes.Should().Be(180);
        tuesdayAfternoon.AvailableMinutes.Should().Be(600); // 2 salas x 13h–18h
        tuesdayAfternoon.UsedMinutesRate.Should().Be(30M);
        tuesdayAfternoon.Classrooms.First(c => c.Id == sala01.Id).UsedMinutes.Should().Be(0);
        tuesdayAfternoon.Classrooms.First(c => c.Id == sala02.Id).UsedMinutes.Should().Be(180);
        tuesdayAfternoon.Classrooms.First(c => c.Id == sala02.Id).UsedMinutesRate.Should().Be(60M);

        var thursdayAfternoon = occupancy.Cells.First(c => c.Day == Day.Thursday && c.Shift == Shift.Afternoon);
        thursdayAfternoon.Classrooms.First(c => c.Id == sala01.Id).UsedMinutes.Should().Be(180);
        thursdayAfternoon.Classrooms.First(c => c.Id == sala02.Id).UsedMinutes.Should().Be(0);

        // Comparação entre salas: a Sala 01 aparece em dois turnos, a Sala 02 em um só.
        var sala01Shifts = occupancy.Cells
            .Where(c => c.Classrooms.Any(x => x.Id == sala01.Id && x.UsedMinutes > 0))
            .Select(c => c.Shift).Distinct().ToList();
        sala01Shifts.Should().BeEquivalentTo([Shift.Morning, Shift.Afternoon]);

        var sala02Shifts = occupancy.Cells
            .Where(c => c.Classrooms.Any(x => x.Id == sala02.Id && x.UsedMinutes > 0))
            .Select(c => c.Shift).Distinct().ToList();
        sala02Shifts.Should().BeEquivalentTo([Shift.Afternoon]);
    
        // Espaço ocioso em minutos: 2 salas x 780min/dia x 5 dias = 7800min abertos,
        // 6 blocos de 180min usados.
        var openMinutes = occupancy.Cells.Sum(c => c.AvailableMinutes);
        var usedMinutes = occupancy.Cells.Sum(c => c.UsedMinutes);
        openMinutes.Should().Be(7800);
        usedMinutes.Should().Be(1080);
        (openMinutes - usedMinutes).Should().Be(6720);
        occupancy.OverallUsedMinutesRate.Should().Be(13.85M);

        // Assert — o professor das duas turmas vê 2 blocos na terça, em salas diferentes.
        teacherBAgenda.Days.Select(d => d.Day).Should().Equal(
            Day.Tuesday, Day.Wednesday, Day.Thursday);

        var tuesday = teacherBAgenda.Days.First(d => d.Day == Day.Tuesday);
        tuesday.Disciplines.Should().HaveCount(2);
        tuesday.Disciplines[0].Start.Should().Be(Hour.H07_00);
        tuesday.Disciplines[0].ClassroomName.Should().Be("Sala 01");
        tuesday.Disciplines[1].Start.Should().Be(Hour.H13_00);
        tuesday.Disciplines[1].ClassroomName.Should().Be("Sala 02");

        // Assert — cada aluno vê só a agenda da sua turma.
        studentAAgenda.Days.Should().HaveCount(4);
        studentAAgenda.Days.SelectMany(d => d.Disciplines)
            .Should().OnlyContain(d => d.ClassroomName == "Sala 01" && d.Start == Hour.H07_00);

        studentBAgenda.Days.Select(d => d.Day).Should().Equal(Day.Tuesday, Day.Thursday);
        studentBAgenda.Days.SelectMany(d => d.Disciplines)
            .Should().OnlyContain(d => d.ClassId == secondClass.Id && d.Start == Hour.H13_00);
        studentBAgenda.Days[0].Disciplines[0].ClassroomName.Should().Be("Sala 02");
        studentBAgenda.Days[1].Disciplines[0].ClassroomName.Should().Be("Sala 01");
    }

    [Test]
    public async Task ScheduleFunctional_Two_campi()
    {
        // Arrange
        var directorClient = await _back.LoggedAsDirector();

        var campusA = await directorClient.CreateCampus(name: "Agreste I").Success();
        var sala01 = await directorClient.CreateClassroom(campusA.Id, name: "Sala 01").Success();
        var sala02 = await directorClient.CreateClassroom(campusA.Id, name: "Sala 02").Success();

        // Campus 2 abre só de manhã e à tarde — as células de noite nem existem.
        var campusB = await directorClient.CreateCampus(name: "Suassuna", city: "Recife").Success();
        await directorClient.UpdateCampusOpeningHours(campusB.Id,
        [
            (Day.Monday, [(Hour.H07_00, Hour.H12_00), (Hour.H12_00, Hour.H18_00)]),
            (Day.Tuesday, [(Hour.H07_00, Hour.H12_00), (Hour.H12_00, Hour.H18_00)]),
            (Day.Wednesday, [(Hour.H07_00, Hour.H12_00), (Hour.H12_00, Hour.H18_00)]),
            (Day.Thursday, [(Hour.H07_00, Hour.H12_00), (Hour.H12_00, Hour.H18_00)]),
            (Day.Friday, [(Hour.H07_00, Hour.H12_00), (Hour.H12_00, Hour.H18_00)]),
        ]);
        var sala03 = await directorClient.CreateClassroom(campusB.Id, name: "Sala 03").Success();
        var sala04 = await directorClient.CreateClassroom(campusB.Id, name: "Sala 04").Success();

        var period = await directorClient.GetFirstAcademicPeriod();

        // Curso do campus 1
        var courseA = await directorClient.CreateCourse(name: "ADS").Success();
        var geometria = await directorClient.CreateDiscipline(name: "Geometria").Success();
        var algoritmos = await directorClient.CreateDiscipline(name: "Algoritmos").Success();
        await directorClient.AssignDisciplinesToCourse(courseA.Id, [geometria.Id, algoritmos.Id]);
        var curriculumA = await directorClient.CreateCourseCurriculum(courseA.Id, "Grade ADS 2024",
        [
            new(geometria.Id, 1, 4, 80),
            new(algoritmos.Id, 1, 4, 80),
        ]).Success();
        var offeringA = await directorClient.CreateCourseOffering(campusA.Id, courseA.Id, curriculumA.Id, period.Id, CourseSession.Morning).Success();

        // Curso do campus 2
        var courseB = await directorClient.CreateCourse(name: "Pedagogia").Success();
        var didatica = await directorClient.CreateDiscipline(name: "Didática").Success();
        var psicologia = await directorClient.CreateDiscipline(name: "Psicologia da Educação").Success();
        await directorClient.AssignDisciplinesToCourse(courseB.Id, [didatica.Id, psicologia.Id]);
        var curriculumB = await directorClient.CreateCourseCurriculum(courseB.Id, "Grade Pedagogia 2024",
        [
            new(didatica.Id, 1, 4, 80),
            new(psicologia.Id, 1, 4, 80),
        ]).Success();
        var offeringB = await directorClient.CreateCourseOffering(campusB.Id, courseB.Id, curriculumB.Id, period.Id, CourseSession.Afternoon).Success();

        // Professor de um só campus.
        var teacherA = await directorClient.CreateTeacher(DataGen.UserName, DataGen.Email).Success();
        await directorClient.AssignDisciplinesToTeacher(teacherA.Id, [geometria.Id]);
        await directorClient.AssignCampiToTeacher(teacherA.Id, [campusA.Id]);

        // Professor dos dois campi.
        var teacherB = await directorClient.CreateTeacher(DataGen.UserName, DataGen.Email).Success();
        await directorClient.AssignDisciplinesToTeacher(teacherB.Id, [algoritmos.Id, psicologia.Id]);
        await directorClient.AssignCampiToTeacher(teacherB.Id, [campusA.Id, campusB.Id]);

        // Professor do campus 2.
        var teacherC = await directorClient.CreateTeacher(DataGen.UserName, DataGen.Email).Success();
        await directorClient.AssignDisciplinesToTeacher(teacherC.Id, [didatica.Id]);
        await directorClient.AssignCampiToTeacher(teacherC.Id, [campusB.Id]);

        // Campus 1: Geometria segunda de manhã na Sala 01, Algoritmos terça de manhã na Sala 02.
        var geometriaClass = await directorClient.CreateClass(geometria.Id, period.Id, campusId: campusA.Id).Success();
        await directorClient.UpdateClassTeachers(geometriaClass.Id, [teacherA.Id]);
        await directorClient.UpdateClassSchedules(geometriaClass.Id, [(Day.Monday, Hour.H07_00, Hour.H10_00, teacherA.Id, sala01.Id)]);
        await directorClient.ReleaseClassForEnrollment(geometriaClass.Id);

        var algoritmosClass = await directorClient.CreateClass(algoritmos.Id, period.Id, campusId: campusA.Id).Success();
        await directorClient.UpdateClassTeachers(algoritmosClass.Id, [teacherB.Id]);
        await directorClient.UpdateClassSchedules(algoritmosClass.Id, [(Day.Tuesday, Hour.H07_00, Hour.H10_00, teacherB.Id, sala02.Id)]);
        await directorClient.ReleaseClassForEnrollment(algoritmosClass.Id);

        // Campus 2: Didática segunda de manhã na Sala 03, Psicologia sexta à tarde na Sala 04.
        var didaticaClass = await directorClient.CreateClass(didatica.Id, period.Id, campusId: campusB.Id).Success();
        await directorClient.UpdateClassTeachers(didaticaClass.Id, [teacherC.Id]);
        await directorClient.UpdateClassSchedules(didaticaClass.Id, [(Day.Monday, Hour.H08_00, Hour.H11_00, teacherC.Id, sala03.Id)]);
        await directorClient.ReleaseClassForEnrollment(didaticaClass.Id);

        var psicologiaClass = await directorClient.CreateClass(psicologia.Id, period.Id, campusId: campusB.Id).Success();
        await directorClient.UpdateClassTeachers(psicologiaClass.Id, [teacherB.Id]);
        await directorClient.UpdateClassSchedules(psicologiaClass.Id, [(Day.Friday, Hour.H14_00, Hour.H16_00, teacherB.Id, sala04.Id)]);
        await directorClient.ReleaseClassForEnrollment(psicologiaClass.Id);

        var studentA = await directorClient.CreateStudent(DataGen.UserName, DataGen.Email).Success();
        await directorClient.EnrollStudentInCourseOffering(studentA.Id, offeringA.Id);
        await directorClient.AssignStudentToClass(studentA.Id, geometriaClass.Id);
        await directorClient.AssignStudentToClass(studentA.Id, algoritmosClass.Id);

        var studentB = await directorClient.CreateStudent(DataGen.UserName, DataGen.Email).Success();
        await directorClient.EnrollStudentInCourseOffering(studentB.Id, offeringB.Id);
        await directorClient.AssignStudentToClass(studentB.Id, didaticaClass.Id);
        await directorClient.AssignStudentToClass(studentB.Id, psicologiaClass.Id);

        await directorClient.StartClass(geometriaClass.Id);
        await directorClient.StartClass(algoritmosClass.Id);
        await directorClient.StartClass(didaticaClass.Id);
        await directorClient.StartClass(psicologiaClass.Id);

        var teacherAClient = await _back.LoginAs(teacherA.Email);
        var teacherBClient = await _back.LoginAs(teacherB.Email);
        var studentBClient = await _back.LoginAs(studentB.Email);

        // Act
        var occupancyA = await directorClient.GetCampusOccupancy(campusA.Id).Success();
        var occupancyB = await directorClient.GetCampusOccupancy(campusB.Id).Success();
        var teacherAAgenda = await teacherAClient.GetTeacherAgenda().Success();
        var teacherBAgenda = await teacherBClient.GetTeacherAgenda().Success();
        var studentBAgenda = await studentBClient.GetStudentAgenda().Success();

        // Assert — uma ocupação por campus, com OpenCells diferente entre eles.
        occupancyA.Campus.Should().Be("Agreste I");
        occupancyA.TotalClassrooms.Should().Be(2);
        occupancyA.OpenCells.Should().Be(15);

        occupancyB.Campus.Should().Be("Suassuna");
        occupancyB.TotalClassrooms.Should().Be(2);
        occupancyB.OpenCells.Should().Be(10); // fecha à noite: 5 dias x 2 turnos

        // As células de noite do campus 2 não contam como ociosas — não existem.
        occupancyB.Cells.Where(c => c.Shift == Shift.Evening)
            .Should().OnlyContain(c => !c.Open && c.AvailableMinutes == 0);

        // Campus 1: 360min de 7800min (2 salas x 780min x 5 dias — 300 de manhã,
        // 300 de tarde e 180 de noite, que é o que o campus abre em cada turno).
        occupancyA.Cells.First(c => c.Day == Day.Monday && c.Shift == Shift.Morning).UsedMinutes.Should().Be(180);
        occupancyA.Cells.First(c => c.Day == Day.Tuesday && c.Shift == Shift.Morning).UsedMinutes.Should().Be(180);
        occupancyA.Cells.Sum(c => c.UsedMinutes).Should().Be(360);
        occupancyA.Cells.Sum(c => c.AvailableMinutes).Should().Be(7800);
        occupancyA.OverallUsedMinutesRate.Should().Be(4.62M);

        // Campus 2: 300min de 6600min (2 salas x 660min x 5 dias). O denominador menor
        // é o que permite ranquear campi sem penalizar quem abre menos.
        var mondayMorningB = occupancyB.Cells.First(c => c.Day == Day.Monday && c.Shift == Shift.Morning);
        mondayMorningB.UsedMinutes.Should().Be(180);
        mondayMorningB.AvailableMinutes.Should().Be(600); // 2 salas x 07h–12h
        mondayMorningB.UsedMinutesRate.Should().Be(30M);

        var fridayAfternoonB = occupancyB.Cells.First(c => c.Day == Day.Friday && c.Shift == Shift.Afternoon);
        fridayAfternoonB.UsedMinutes.Should().Be(120);
        fridayAfternoonB.AvailableMinutes.Should().Be(720); // 2 salas x 12h–18h
        fridayAfternoonB.UsedMinutesRate.Should().Be(16.67M);

        occupancyB.Cells.Sum(c => c.UsedMinutes).Should().Be(300);
        occupancyB.Cells.Sum(c => c.AvailableMinutes).Should().Be(6600);
        occupancyB.OverallUsedMinutesRate.Should().Be(4.55M);

        // Assert — professor de um só campus vê agenda só daquele campus.
        var teacherADay = teacherAAgenda.Days.Should().ContainSingle().Which;
        teacherADay.Day.Should().Be(Day.Monday);
        teacherADay.Disciplines.Should().ContainSingle().Which.ClassroomName.Should().Be("Sala 01");

        // Assert — professor de dois campi vê os dois na mesma agenda. Por isso o
        // nome da sala, sozinho, não basta: "Sala 02" e "Sala 04" são de campi diferentes.
        teacherBAgenda.Days.Select(d => d.Day).Should().Equal(Day.Tuesday, Day.Friday);
        teacherBAgenda.Days[0].Disciplines.Should().ContainSingle().Which.ClassroomName.Should().Be("Sala 02");
        teacherBAgenda.Days[0].Disciplines[0].Name.Should().Be("Algoritmos");
        teacherBAgenda.Days[1].Disciplines.Should().ContainSingle().Which.ClassroomName.Should().Be("Sala 04");
        teacherBAgenda.Days[1].Disciplines[0].Name.Should().Be("Psicologia da Educação");

        // Assert — o aluno vê a agenda do campus onde a turma dele acontece.
        studentBAgenda.Days.Select(d => d.Day).Should().Equal(Day.Monday, Day.Friday);
        studentBAgenda.Days[0].Disciplines.Should().ContainSingle().Which.ClassroomName.Should().Be("Sala 03");
        studentBAgenda.Days[1].Disciplines.Should().ContainSingle().Which.ClassroomName.Should().Be("Sala 04");
    }

    [Test]
    public async Task ScheduleFunctional_Year_calendar()
    {
        // Arrange
        var directorClient = await _back.LoggedAsDirector();

        var campus = await directorClient.CreateCampus(name: "Agreste I").Success();
        var sala01 = await directorClient.CreateClassroom(campus.Id, name: "Sala 01").Success();

        // 2 períodos acadêmicos no mesmo ano.
        var firstPeriod = await directorClient.CreateAcademicPeriod("2024.1").Success();
        var secondPeriod = await directorClient.CreateAcademicPeriod("2024.2").Success();

        var course = await directorClient.CreateCourse(name: "ADS").Success();
        var discipline = await directorClient.CreateDiscipline().Success();
        await directorClient.AssignDisciplinesToCourse(course.Id, [discipline.Id]);
        var curriculum = await directorClient.CreateCourseCurriculum(course.Id, "Grade ADS 2024", [new(discipline.Id, 1, 4, 80)]).Success();
        var offering = await directorClient.CreateCourseOffering(campus.Id, course.Id, curriculum.Id, firstPeriod.Id, CourseSession.Morning).Success();

        var teacher = await directorClient.CreateTeacher(DataGen.UserName, DataGen.Email).Success();
        await directorClient.AssignDisciplinesToTeacher(teacher.Id, [discipline.Id]);
        await directorClient.AssignCampiToTeacher(teacher.Id, [campus.Id]);

        var @class = await directorClient.CreateClass(discipline.Id, firstPeriod.Id, campusId: campus.Id).Success();
        await directorClient.UpdateClassTeachers(@class.Id, [teacher.Id]);
        await directorClient.UpdateClassSchedules(@class.Id, [(Day.Monday, Hour.H07_00, Hour.H10_00, teacher.Id, sala01.Id)]);
        await directorClient.ReleaseClassForEnrollment(@class.Id);

        var student = await directorClient.CreateStudent(DataGen.UserName, DataGen.Email).Success();
        await directorClient.EnrollStudentInCourseOffering(student.Id, offering.Id);
        await directorClient.AssignStudentToClass(student.Id, @class.Id);
        await directorClient.StartClass(@class.Id);

        // Férias de verão: 02/01 a 05/01 (o 01/01 já é feriado nacional).
        foreach (var day in Enumerable.Range(2, 4))
        {
            await directorClient.CreateCalendarDay(new DateTime(2024, 1, day), DayType.Vacation, "Férias de verão");
        }

        // Recesso de meio de ano: 15/07 a 19/07, entre os dois períodos acadêmicos.
        foreach (var day in Enumerable.Range(15, 5))
        {
            await directorClient.CreateCalendarDay(new DateTime(2024, 7, day), DayType.Recess, "Recesso de meio de ano");
        }

        // A semana de provas do doc não entra aqui: o DayType tem Default, Vacation,
        // Recess, Holiday e Weekend, e nenhum deles é "prova". Enquanto não existir um
        // DayType.Exam, esse pedaço da contagem por período fica de fora.

        var teacherClient = await _back.LoginAs(teacher.Email);
        var studentClient = await _back.LoginAs(student.Email);

        // Act
        var calendar = await directorClient.GetCalendar(2024).Success();
        var periods = await directorClient.GetAcademicPeriods().Success();
        var teacherAgenda = await teacherClient.GetTeacherAgenda().Success();
        var studentAgenda = await studentClient.GetStudentAgenda().Success();

        // Assert — manager: contagem de dias letivos, recesso e feriados no ano.
        calendar.Year.Should().Be(2024);
        calendar.Total.Should().Be(366); // 2024 é bissexto
        calendar.Items.Should().HaveCount(366);

        var byType = calendar.Items.GroupBy(x => x.DayType).ToDictionary(g => g.Key, g => g.Count());
        byType[DayType.Vacation].Should().Be(4);
        byType[DayType.Recess].Should().Be(5);
        byType[DayType.Holiday].Should().Be(13);  // feriados nacionais de 2024
        byType[DayType.Weekend].Should().Be(100); // 104 sábados/domingos, 4 deles feriados
        byType[DayType.Default].Should().Be(244); // dias letivos

        // Os dias customizados ganham do padrão, inclusive a descrição.
        var vacation = calendar.Items.First(x => x.Date == new DateTime(2024, 1, 3));
        vacation.Id.Should().NotBeNull();
        vacation.DayType.Should().Be(DayType.Vacation);
        vacation.Description.Should().Be("Férias de verão");

        var recess = calendar.Items.First(x => x.Date == new DateTime(2024, 7, 17));
        recess.DayType.Should().Be(DayType.Recess);
        recess.Description.Should().Be("Recesso de meio de ano");

        // Feriado nacional entra sem ninguém cadastrar.
        var holiday = calendar.Items.First(x => x.Date == new DateTime(2024, 1, 1));
        holiday.Id.Should().BeNull();
        holiday.DayType.Should().Be(DayType.Holiday);
        holiday.Description.Should().Be("Confraternização Universal");

        // Os 2 períodos de 2024, pra conferir se cada um cobre a carga horária mínima.
        var periodsOf2024 = periods.Items.FindAll(x => x.Name.StartsWith("2024."));
        periodsOf2024.Should().HaveCount(2);
        periodsOf2024.Select(x => x.Id).Should().BeEquivalentTo(new[] { firstPeriod.Id, secondPeriod.Id });

        // Assert — a agenda semanal não muda com o calendário: continua 1 dia, 1 disciplina.
        // As ocorrências é que caem nos dias não letivos.
        teacherAgenda.Days.Should().ContainSingle().Which.Day.Should().Be(Day.Monday);
        studentAgenda.Days.Should().ContainSingle().Which.Day.Should().Be(Day.Monday);

        // A leitura do calendário por teacher e student ainda não existe: a policy
        // GetCalendar é só de Manager, então o insight do doc está pendente
        // de um endpoint de calendário pra esses dois perfis.
        (await teacherClient.GetCalendar(2024)).ShouldBeError(HttpStatusCode.Forbidden);
        (await studentClient.GetCalendar(2024)).ShouldBeError(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task ScheduleFunctional_Student_with_parent()
    {
        // Arrange
        var directorClient = await _back.LoggedAsDirector();

        var campus = await directorClient.CreateCampus(name: "Agreste I").Success();
        var sala01 = await directorClient.CreateClassroom(campus.Id, name: "Sala 01").Success();
        var sala02 = await directorClient.CreateClassroom(campus.Id, name: "Sala 02").Success();

        var period = await directorClient.GetFirstAcademicPeriod();

        var course = await directorClient.CreateCourse(name: "ADS").Success();
        var geometria = await directorClient.CreateDiscipline(name: "Geometria").Success();
        var algoritmos = await directorClient.CreateDiscipline(name: "Algoritmos").Success();
        await directorClient.AssignDisciplinesToCourse(course.Id, [geometria.Id, algoritmos.Id]);
        var curriculum = await directorClient.CreateCourseCurriculum(course.Id, "Grade ADS 2024",
        [
            new(geometria.Id, 1, 4, 80),
            new(algoritmos.Id, 1, 4, 80),
        ]).Success();
        var offering = await directorClient.CreateCourseOffering(campus.Id, course.Id, curriculum.Id, period.Id, CourseSession.Morning).Success();

        var teacherA = await directorClient.CreateTeacher(DataGen.UserName, DataGen.Email).Success();
        await directorClient.AssignDisciplinesToTeacher(teacherA.Id, [geometria.Id]);
        await directorClient.AssignCampiToTeacher(teacherA.Id, [campus.Id]);

        var teacherB = await directorClient.CreateTeacher(DataGen.UserName, DataGen.Email).Success();
        await directorClient.AssignDisciplinesToTeacher(teacherB.Id, [algoritmos.Id]);
        await directorClient.AssignCampiToTeacher(teacherB.Id, [campus.Id]);

        var geometriaClass = await directorClient.CreateClass(geometria.Id, period.Id, campusId: campus.Id).Success();
        await directorClient.UpdateClassTeachers(geometriaClass.Id, [teacherA.Id]);
        await directorClient.UpdateClassSchedules(geometriaClass.Id, [(Day.Monday, Hour.H07_00, Hour.H10_00, teacherA.Id, sala01.Id)]);
        await directorClient.ReleaseClassForEnrollment(geometriaClass.Id);

        var algoritmosClass = await directorClient.CreateClass(algoritmos.Id, period.Id, campusId: campus.Id).Success();
        await directorClient.UpdateClassTeachers(algoritmosClass.Id, [teacherB.Id]);
        await directorClient.UpdateClassSchedules(algoritmosClass.Id, [(Day.Wednesday, Hour.H19_00, Hour.H22_00, teacherB.Id, sala02.Id)]);
        await directorClient.ReleaseClassForEnrollment(algoritmosClass.Id);

        // Aluno com um responsável só.
        var onlyChild = await directorClient.CreateStudent(DataGen.UserName, DataGen.Email).Success();
        await directorClient.EnrollStudentInCourseOffering(onlyChild.Id, offering.Id);
        await directorClient.AssignStudentToClass(onlyChild.Id, geometriaClass.Id);

        // Irmãos em turmas diferentes.
        var firstSibling = await directorClient.CreateStudent(DataGen.UserName, DataGen.Email).Success();
        await directorClient.EnrollStudentInCourseOffering(firstSibling.Id, offering.Id);
        await directorClient.AssignStudentToClass(firstSibling.Id, geometriaClass.Id);

        var secondSibling = await directorClient.CreateStudent(DataGen.UserName, DataGen.Email).Success();
        await directorClient.EnrollStudentInCourseOffering(secondSibling.Id, offering.Id);
        await directorClient.AssignStudentToClass(secondSibling.Id, algoritmosClass.Id);

        await directorClient.StartClass(geometriaClass.Id);
        await directorClient.StartClass(algoritmosClass.Id);

        // Fotografia da ocupação antes de existir qualquer responsável.
        var occupancyBeforeParents = await directorClient.GetCampusOccupancy(campus.Id).Success();

        var singleParentEmail = DataGen.Email;
        await directorClient.CreateParent(DataGen.UserName, singleParentEmail,
        [
            new() { StudentId = onlyChild.Id, Relationship = ParentRelationship.Mother },
        ]);

        var siblingsParentEmail = DataGen.Email;
        await directorClient.CreateParent(DataGen.UserName, siblingsParentEmail,
        [
            new() { StudentId = firstSibling.Id, Relationship = ParentRelationship.Father },
            new() { StudentId = secondSibling.Id, Relationship = ParentRelationship.Father },
        ]);

        var singleParentClient = await _back.LoginAs(singleParentEmail);
        var siblingsParentClient = await _back.LoginAs(siblingsParentEmail);

        // Act
        var occupancy = await directorClient.GetCampusOccupancy(campus.Id).Success();
        var singleParentChildren = await singleParentClient.GetParentStudents().Success();
        var siblingsParentChildren = await siblingsParentClient.GetParentStudents().Success();
        var onlyChildAgenda = await singleParentClient.GetParentStudentAgenda(onlyChild.Id).Success();
        var firstSiblingAgenda = await siblingsParentClient.GetParentStudentAgenda(firstSibling.Id).Success();
        var secondSiblingAgenda = await siblingsParentClient.GetParentStudentAgenda(secondSibling.Id).Success();

        // Assert — manager: o Parent não muda nenhum indicador.
        occupancy.OverallUsedMinutesRate.Should().Be(occupancyBeforeParents.OverallUsedMinutesRate);
        occupancy.Cells.Sum(c => c.UsedMinutes).Should().Be(360);

        // Assert — parent: lista de filhos vinculados.
        var onlyChildItem = singleParentChildren.Items.Should().ContainSingle().Which;
        onlyChildItem.Id.Should().Be(onlyChild.Id);
        onlyChildItem.Relationship.Should().Be(ParentRelationship.Mother);
        onlyChildItem.EnrollmentCode.Should().NotBeEmpty();

        siblingsParentChildren.Items.Should().HaveCount(2);
        siblingsParentChildren.Items.Select(x => x.Id).Should().BeEquivalentTo(
            new[] { firstSibling.Id, secondSibling.Id });

        // Assert — parent: agenda de cada filho, com disciplina, horário e sala.
        var onlyChildDay = onlyChildAgenda.Days.Should().ContainSingle().Which;
        onlyChildDay.Day.Should().Be(Day.Monday);
        onlyChildDay.Disciplines.Should().ContainSingle().Which.Name.Should().Be("Geometria");
        onlyChildDay.Disciplines[0].ClassroomName.Should().Be("Sala 01");

        // Irmãos em turmas diferentes: cada agenda é a da sua turma.
        var firstSiblingDay = firstSiblingAgenda.Days.Should().ContainSingle().Which;
        firstSiblingDay.Day.Should().Be(Day.Monday);
        firstSiblingDay.Disciplines[0].ClassId.Should().Be(geometriaClass.Id);
        firstSiblingDay.Disciplines[0].ClassroomName.Should().Be("Sala 01");

        var secondSiblingDay = secondSiblingAgenda.Days.Should().ContainSingle().Which;
        secondSiblingDay.Day.Should().Be(Day.Wednesday);
        secondSiblingDay.Disciplines[0].ClassId.Should().Be(algoritmosClass.Id);
        secondSiblingDay.Disciplines[0].Name.Should().Be("Algoritmos");
        secondSiblingDay.Disciplines[0].ClassroomName.Should().Be("Sala 02");

        // O calendário da instituição do filho ainda não é exposto ao Parent — a
        // policy GetCalendar é só de Manager.
        (await singleParentClient.GetCalendar(2024)).ShouldBeError(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task ScheduleFunctional_Enrollment_period()
    {
        // Arrange
        var directorClient = await _back.LoggedAsDirector();

        var campus = await directorClient.CreateCampus(name: "Agreste I").Success();
        var sala01 = await directorClient.CreateClassroom(campus.Id, name: "Sala 01", capacity: 40).Success();
        var sala02 = await directorClient.CreateClassroom(campus.Id, name: "Sala 02", capacity: 40).Success();

        var period = await directorClient.GetFirstAcademicPeriod();

        // Período de matrícula aberto dentro do ano do calendário.
        var enrollmentPeriod = await directorClient.CreateEnrollmentPeriod(
            "Matrículas 2024.1", new DateOnly(2024, 1, 15), new DateOnly(2024, 2, 1)).Success();

        var course = await directorClient.CreateCourse(name: "ADS").Success();
        var geometria = await directorClient.CreateDiscipline(name: "Geometria").Success();
        var algoritmos = await directorClient.CreateDiscipline(name: "Algoritmos").Success();
        await directorClient.AssignDisciplinesToCourse(course.Id, [geometria.Id, algoritmos.Id]);
        var curriculum = await directorClient.CreateCourseCurriculum(course.Id, "Grade ADS 2024",
        [
            new(geometria.Id, 1, 4, 80),
            new(algoritmos.Id, 1, 4, 80),
        ]).Success();

        // Ofertas publicadas para o período.
        var morningOffering = await directorClient.CreateCourseOffering(campus.Id, course.Id, curriculum.Id, period.Id, CourseSession.Morning).Success();
        var eveningOffering = await directorClient.CreateCourseOffering(campus.Id, course.Id, curriculum.Id, period.Id, CourseSession.Evening).Success();

        var teacherA = await directorClient.CreateTeacher(DataGen.UserName, DataGen.Email).Success();
        await directorClient.AssignDisciplinesToTeacher(teacherA.Id, [geometria.Id]);
        await directorClient.AssignCampiToTeacher(teacherA.Id, [campus.Id]);

        var teacherB = await directorClient.CreateTeacher(DataGen.UserName, DataGen.Email).Success();
        await directorClient.AssignDisciplinesToTeacher(teacherB.Id, [algoritmos.Id]);
        await directorClient.AssignCampiToTeacher(teacherB.Id, [campus.Id]);

        // Turma cheia: 40 vagas, 3 matriculados.
        var fullClass = await directorClient.CreateClass(geometria.Id, period.Id, vacancies: 40, campusId: campus.Id).Success();
        await directorClient.UpdateClassTeachers(fullClass.Id, [teacherA.Id]);
        await directorClient.UpdateClassSchedules(fullClass.Id, [(Day.Monday, Hour.H07_00, Hour.H10_00, teacherA.Id, sala01.Id)]);

        // Turma vazia: mesmas 40 vagas, 1 matriculado — candidata a cancelamento.
        var emptyClass = await directorClient.CreateClass(algoritmos.Id, period.Id, vacancies: 40, campusId: campus.Id).Success();
        await directorClient.UpdateClassTeachers(emptyClass.Id, [teacherB.Id]);
        await directorClient.UpdateClassSchedules(emptyClass.Id, [(Day.Tuesday, Hour.H19_00, Hour.H22_00, teacherB.Id, sala02.Id)]);

        await directorClient.ReleaseClassForEnrollment(fullClass.Id);
        await directorClient.ReleaseClassForEnrollment(emptyClass.Id);

        // Alunos se matriculando ao longo do período.
        var enrolledStudents = new List<int>();
        foreach (var _ in Enumerable.Range(0, 3))
        {
            var student = await directorClient.CreateStudent(DataGen.UserName, DataGen.Email).Success();
            await directorClient.EnrollStudentInCourseOffering(student.Id, morningOffering.Id);
            await directorClient.AssignStudentToClass(student.Id, fullClass.Id);
            enrolledStudents.Add(student.Id);
        }

        var loneStudent = await directorClient.CreateStudent(DataGen.UserName, DataGen.Email).Success();
        await directorClient.EnrollStudentInCourseOffering(loneStudent.Id, eveningOffering.Id);
        await directorClient.AssignStudentToClass(loneStudent.Id, emptyClass.Id);

        await directorClient.StartClass(fullClass.Id);
        await directorClient.StartClass(emptyClass.Id);

        var teacherAClient = await _back.LoginAs(teacherA.Email);
        var loneStudentClient = await _back.LoginAs(loneStudent.Email);

        // Act
        var enrollmentPeriods = await directorClient.GetEnrollmentPeriods().Success();
        var offerings = await directorClient.GetCourseOfferings().Success();
        var fullClassDetails = await directorClient.GetClass(fullClass.Id).Success();
        var emptyClassDetails = await directorClient.GetClass(emptyClass.Id).Success();
        var teacherClasses = await teacherAClient.GetTeacherCurrentClasses().Success();
        var teacherClassStudents = await teacherAClient.GetTeacherClassStudents(fullClass.Id).Success();
        var courseDetails = await loneStudentClient.GetStudentCourseDetails().Success();
        var proof = await loneStudentClient.GenerateEnrollmentProof().Success();

        // Assert — manager: o período e as ofertas publicadas.
        var periodItem = enrollmentPeriods.Items.Should().ContainSingle().Which;
        periodItem.Id.Should().Be(enrollmentPeriod.Id);
        periodItem.StartAt.Should().Be(new DateOnly(2024, 1, 15));
        periodItem.EndAt.Should().Be(new DateOnly(2024, 2, 1));

        offerings.Total.Should().Be(2);
        offerings.Items.Select(x => x.Session).Should().BeEquivalentTo(
            new[] { CourseSession.Morning, CourseSession.Evening });

        // Assert — manager: ocupação projetada (vagas) x real (matriculados).
        fullClassDetails.Vacancies.Should().Be(40);
        fullClassDetails.Students.Should().HaveCount(3);
        fullClassDetails.Students.Select(x => x.Id).Should().BeEquivalentTo(enrolledStudents);

        // Turma que não bateu um mínimo de alunos: cancelá-la devolve a Sala 02 e a
        // carga horária do professor B.
        emptyClassDetails.Vacancies.Should().Be(40);
        emptyClassDetails.Students.Should().ContainSingle();
        emptyClassDetails.Schedules.Should().ContainSingle().Which.Classroom.Should().Be("Sala 02");
        emptyClassDetails.Teachers.Should().ContainSingle().Which.Id.Should().Be(teacherB.Id);

        // Assert — teacher: turmas atribuídas com a quantidade de alunos por turma.
        teacherClasses.Classes.Should().ContainSingle().Which.Id.Should().Be(fullClass.Id);
        teacherClassStudents.Students.Should().HaveCount(3);
        teacherClassStudents.Students.Should().OnlyContain(x => x.Status == StudentClassStatus.Matriculado);

        // Assert — student: a oferta em que se matriculou, com campus e turno.
        courseDetails.CourseOfferingId.Should().Be(eveningOffering.Id);
        courseDetails.Course.Should().Be("ADS");
        courseDetails.Campus.Should().Be("Agreste I");
        courseDetails.Session.Should().Be(CourseSession.Evening);
        courseDetails.Disciplines.Should().HaveCount(2);
        courseDetails.Disciplines.First(d => d.Id == algoritmos.Id).Status.Should().Be(StudentDisciplineStatus.Cursando);
        courseDetails.Disciplines.First(d => d.Id == geometria.Id).Status.Should().Be(StudentDisciplineStatus.NaoCursada);

        // Assert — student: comprovante de matrícula depois de matriculado.
        proof.Should().NotBeEmpty();
    }

    [Test]
    public async Task ScheduleFunctional_Unbalanced_teachers_workload()
    {
        // Arrange
        var directorClient = await _back.LoggedAsDirector();

        var campus = await directorClient.CreateCampus(name: "Agreste I").Success();
        var sala01 = await directorClient.CreateClassroom(campus.Id, name: "Sala 01").Success();

        var period = await directorClient.GetFirstAcademicPeriod();

        var course = await directorClient.CreateCourse(name: "ADS").Success();

        var disciplineNames = new[] { "Geometria", "Algoritmos", "Banco de Dados", "Redes", "Compiladores", "Estatística" };
        var disciplines = new List<int>();
        foreach (var name in disciplineNames)
        {
            var discipline = await directorClient.CreateDiscipline(name).Success();
            disciplines.Add(discipline.Id);
        }
        await directorClient.AssignDisciplinesToCourse(course.Id, disciplines);

        var curriculum = await directorClient.CreateCourseCurriculum(course.Id, "Grade ADS 2024",
            disciplines.ConvertAll(id => new CreateCourseCurriculumDisciplineIn(id, 1, 4, 80))).Success();
        var offering = await directorClient.CreateCourseOffering(campus.Id, course.Id, curriculum.Id, period.Id, CourseSession.Morning).Success();

        // Professor sobrecarregado: as 5 primeiras disciplinas, uma por dia da semana.
        var busyTeacher = await directorClient.CreateTeacher(DataGen.UserName, DataGen.Email).Success();
        await directorClient.AssignDisciplinesToTeacher(busyTeacher.Id, disciplines.Take(5).ToList());
        await directorClient.AssignCampiToTeacher(busyTeacher.Id, [campus.Id]);

        // Professor com 1 turma.
        var lightTeacher = await directorClient.CreateTeacher(DataGen.UserName, DataGen.Email).Success();
        await directorClient.AssignDisciplinesToTeacher(lightTeacher.Id, [disciplines[5]]);
        await directorClient.AssignCampiToTeacher(lightTeacher.Id, [campus.Id]);

        // Professor cadastrado, habilitado em 2 disciplinas e sem nenhuma turma.
        var idleTeacher = await directorClient.CreateTeacher(DataGen.UserName, DataGen.Email).Success();
        await directorClient.AssignDisciplinesToTeacher(idleTeacher.Id, [disciplines[0], disciplines[1]]);
        await directorClient.AssignCampiToTeacher(idleTeacher.Id, [campus.Id]);

        var student = await directorClient.CreateStudent(DataGen.UserName, DataGen.Email).Success();
        await directorClient.EnrollStudentInCourseOffering(student.Id, offering.Id);

        var busyDays = new[] { Day.Monday, Day.Tuesday, Day.Wednesday, Day.Thursday, Day.Friday };
        foreach (var (disciplineId, day) in disciplines.Take(5).Zip(busyDays))
        {
            var @class = await directorClient.CreateClass(disciplineId, period.Id, campusId: campus.Id).Success();
            await directorClient.UpdateClassTeachers(@class.Id, [busyTeacher.Id]);
            await directorClient.UpdateClassSchedules(@class.Id, [(day, Hour.H07_00, Hour.H11_00, busyTeacher.Id, sala01.Id)]);
            await directorClient.ReleaseClassForEnrollment(@class.Id);
            await directorClient.AssignStudentToClass(student.Id, @class.Id);
            await directorClient.StartClass(@class.Id);
        }

        var lightClass = await directorClient.CreateClass(disciplines[5], period.Id, campusId: campus.Id).Success();
        await directorClient.UpdateClassTeachers(lightClass.Id, [lightTeacher.Id]);
        await directorClient.UpdateClassSchedules(lightClass.Id, [(Day.Monday, Hour.H19_00, Hour.H21_00, lightTeacher.Id, sala01.Id)]);
        await directorClient.ReleaseClassForEnrollment(lightClass.Id);
        await directorClient.AssignStudentToClass(student.Id, lightClass.Id);
        await directorClient.StartClass(lightClass.Id);

        var busyTeacherClient = await _back.LoginAs(busyTeacher.Email);
        var lightTeacherClient = await _back.LoginAs(lightTeacher.Email);
        var idleTeacherClient = await _back.LoginAs(idleTeacher.Email);

        // Act
        var teachers = await directorClient.GetTeachers().Success();
        var classes = await directorClient.GetClasses(pageSize: 20).Success();
        var busyAgenda = await busyTeacherClient.GetTeacherAgenda().Success();
        var lightAgenda = await lightTeacherClient.GetTeacherAgenda().Success();
        var idleAgenda = await idleTeacherClient.GetTeacherAgenda().Success();
        var idleClasses = await idleTeacherClient.GetTeacherCurrentClasses().Success();
        var busyDetails = await directorClient.GetTeacherDetails(busyTeacher.Id).Success();
        var lightDetails = await directorClient.GetTeacherDetails(lightTeacher.Id).Success();
        var idleDetails = await directorClient.GetTeacherDetails(idleTeacher.Id).Success();

        // Assert — manager: os 3 professores aparecem, inclusive o sem turma.
        teachers.Total.Should().Be(3);
        teachers.Items.Select(x => x.Id).Should().BeEquivalentTo(
            new[] { busyTeacher.Id, lightTeacher.Id, idleTeacher.Id });

        // O professor ocioso não pode sumir por falta de dado, e as disciplinas
        // habilitadas dele são o cruzamento de "quem está ocioso e poderia assumir o quê".
        var idleItem = teachers.Items.First(x => x.Id == idleTeacher.Id);
        idleItem.Disciplines.Should().Be(2);
        idleItem.Campi.Should().Be(1);
        idleDetails.Disciplines.Should().HaveCount(2);

        // Ranking por horas/semana: 5 turmas de 240min contra 1 de 120min contra nenhuma.
        classes.Total.Should().Be(6);

        busyDetails.Classes.Should().HaveCount(5);
        busyDetails.Classes.SelectMany(c => c.Schedules)
            .Sum(s => s.EndAt.ToMinutes() - s.StartAt.ToMinutes()).Should().Be(1200);

        lightDetails.Classes.Should().ContainSingle();
        lightDetails.Classes.SelectMany(c => c.Schedules)
            .Sum(s => s.EndAt.ToMinutes() - s.StartAt.ToMinutes()).Should().Be(120);

        idleDetails.Classes.Should().BeEmpty();

        // Assert — teacher: o total de horas da própria semana sai da agenda.
        busyAgenda.Days.Should().HaveCount(5);
        busyAgenda.Days.Should().OnlyContain(d => d.Disciplines.Count == 1);
        busyAgenda.Days.SelectMany(d => d.Disciplines)
            .Sum(d => d.End.ToMinutes() - d.Start.ToMinutes()).Should().Be(1200);

        lightAgenda.Days.Should().ContainSingle().Which.Day.Should().Be(Day.Monday);
        lightAgenda.Days.SelectMany(d => d.Disciplines)
            .Sum(d => d.End.ToMinutes() - d.Start.ToMinutes()).Should().Be(120);

        // O professor sem turma tem agenda vazia — e é justamente ele que o insight
        // de carga horária precisa mostrar.
        idleAgenda.Days.Should().BeEmpty();
        idleClasses.Classes.Should().BeEmpty();
    }

    [Test]
    public async Task ScheduleFunctional_Complete_institution()
    {
        // Arrange
        var directorClient = await _back.LoggedAsDirector();

        // 2 campi com OpeningHours diferentes: o Agreste abre até 22h, o Suassuna fecha às 18h.
        var agreste = await directorClient.CreateCampus(name: "Agreste I").Success();
        var suassuna = await directorClient.CreateCampus(name: "Suassuna", city: "Recife").Success();
        await directorClient.UpdateCampusOpeningHours(suassuna.Id,
        [
            (Day.Monday, [(Hour.H07_00, Hour.H12_00), (Hour.H12_00, Hour.H18_00)]),
            (Day.Tuesday, [(Hour.H07_00, Hour.H12_00), (Hour.H12_00, Hour.H18_00)]),
            (Day.Wednesday, [(Hour.H07_00, Hour.H12_00), (Hour.H12_00, Hour.H18_00)]),
            (Day.Thursday, [(Hour.H07_00, Hour.H12_00), (Hour.H12_00, Hour.H18_00)]),
            (Day.Friday, [(Hour.H07_00, Hour.H12_00), (Hour.H12_00, Hour.H18_00)]),
        ]);

        // 10 salas: 6 no Agreste (a Sala A6 fica ociosa a semana toda) e 4 no Suassuna.
        var agresteRooms = new List<int>();
        foreach (var number in Enumerable.Range(1, 6))
        {
            var room = await directorClient.CreateClassroom(agreste.Id, name: $"Sala A{number}", capacity: 100).Success();
            agresteRooms.Add(room.Id);
        }

        var suassunaRooms = new List<int>();
        foreach (var number in Enumerable.Range(1, 4))
        {
            var room = await directorClient.CreateClassroom(suassuna.Id, name: $"Sala B{number}", capacity: 100).Success();
            suassunaRooms.Add(room.Id);
        }

        // 2 períodos acadêmicos e 2 períodos de matrícula no ano.
        var firstPeriod = await directorClient.CreateAcademicPeriod("2024.1").Success();
        var secondPeriod = await directorClient.CreateAcademicPeriod("2024.2").Success();
        await directorClient.CreateEnrollmentPeriod("Matrículas 2024.1", new DateOnly(2024, 1, 15), new DateOnly(2024, 2, 1));
        await directorClient.CreateEnrollmentPeriod("Matrículas 2024.2", new DateOnly(2024, 6, 1), new DateOnly(2024, 6, 20));

        // Calendário do ano com férias e recesso.
        foreach (var day in Enumerable.Range(2, 4))
        {
            await directorClient.CreateCalendarDay(new DateTime(2024, 1, day), DayType.Vacation, "Férias de verão");
        }
        foreach (var day in Enumerable.Range(15, 5))
        {
            await directorClient.CreateCalendarDay(new DateTime(2024, 7, day), DayType.Recess, "Recesso de meio de ano");
        }

        // 3 cursos com 5 disciplinas e 1 grade cada.
        var ads = await directorClient.CreateCourse(name: "ADS", type: CourseType.Tecnologo).Success();
        var direito = await directorClient.CreateCourse(name: "Direito", type: CourseType.Bacharelado).Success();
        var pedagogia = await directorClient.CreateCourse(name: "Pedagogia", type: CourseType.Licenciatura).Success();

        var adsDisciplines = new List<int>();
        foreach (var name in new[] { "Algoritmos", "Banco de Dados", "Redes", "Compiladores", "Engenharia de Software" })
        {
            adsDisciplines.Add((await directorClient.CreateDiscipline(name).Success()).Id);
        }
        await directorClient.AssignDisciplinesToCourse(ads.Id, adsDisciplines);
        var adsCurriculum = await directorClient.CreateCourseCurriculum(ads.Id, "Grade ADS 2024",
            adsDisciplines.ConvertAll(id => new CreateCourseCurriculumDisciplineIn(id, 1, 4, 80))).Success();

        var direitoDisciplines = new List<int>();
        foreach (var name in new[] { "Direito Civil", "Direito Penal", "Direito do Trabalho", "Processo Civil", "Direito Tributário" })
        {
            direitoDisciplines.Add((await directorClient.CreateDiscipline(name).Success()).Id);
        }
        await directorClient.AssignDisciplinesToCourse(direito.Id, direitoDisciplines);
        var direitoCurriculum = await directorClient.CreateCourseCurriculum(direito.Id, "Grade Direito 2024",
            direitoDisciplines.ConvertAll(id => new CreateCourseCurriculumDisciplineIn(id, 1, 4, 80))).Success();

        var pedagogiaDisciplines = new List<int>();
        foreach (var name in new[] { "Didática", "Psicologia da Educação", "Alfabetização", "Currículo", "Gestão Escolar" })
        {
            pedagogiaDisciplines.Add((await directorClient.CreateDiscipline(name).Success()).Id);
        }
        await directorClient.AssignDisciplinesToCourse(pedagogia.Id, pedagogiaDisciplines);
        var pedagogiaCurriculum = await directorClient.CreateCourseCurriculum(pedagogia.Id, "Grade Pedagogia 2024",
            pedagogiaDisciplines.ConvertAll(id => new CreateCourseCurriculumDisciplineIn(id, 1, 4, 80))).Success();

        // 10 ofertas de curso espalhadas entre campi, turnos e os 2 períodos.
        var adsMorning = await directorClient.CreateCourseOffering(agreste.Id, ads.Id, adsCurriculum.Id, firstPeriod.Id, CourseSession.Morning).Success();
        await directorClient.CreateCourseOffering(agreste.Id, ads.Id, adsCurriculum.Id, firstPeriod.Id, CourseSession.Evening);
        await directorClient.CreateCourseOffering(agreste.Id, ads.Id, adsCurriculum.Id, secondPeriod.Id, CourseSession.Morning);
        await directorClient.CreateCourseOffering(suassuna.Id, ads.Id, adsCurriculum.Id, firstPeriod.Id, CourseSession.Afternoon);
        var direitoEvening = await directorClient.CreateCourseOffering(agreste.Id, direito.Id, direitoCurriculum.Id, firstPeriod.Id, CourseSession.Evening).Success();
        await directorClient.CreateCourseOffering(agreste.Id, direito.Id, direitoCurriculum.Id, secondPeriod.Id, CourseSession.Evening);
        await directorClient.CreateCourseOffering(suassuna.Id, direito.Id, direitoCurriculum.Id, firstPeriod.Id, CourseSession.Afternoon);
        var pedagogiaAfternoon = await directorClient.CreateCourseOffering(suassuna.Id, pedagogia.Id, pedagogiaCurriculum.Id, firstPeriod.Id, CourseSession.Afternoon).Success();
        await directorClient.CreateCourseOffering(suassuna.Id, pedagogia.Id, pedagogiaCurriculum.Id, secondPeriod.Id, CourseSession.Afternoon);
        await directorClient.CreateCourseOffering(agreste.Id, pedagogia.Id, pedagogiaCurriculum.Id, firstPeriod.Id, CourseSession.Morning);

        // 10 professores com campi e disciplinas vinculados. A distribuição é
        // desbalanceada de propósito: uns com 2 disciplinas, outros com 1.
        var teacherDisciplines = new List<int>[]
        {
            [adsDisciplines[0], adsDisciplines[1]],
            [adsDisciplines[2], adsDisciplines[3]],
            [adsDisciplines[4]],
            [direitoDisciplines[0], direitoDisciplines[1]],
            [direitoDisciplines[2], direitoDisciplines[3]],
            [direitoDisciplines[4]],
            [pedagogiaDisciplines[0], pedagogiaDisciplines[1]],
            [pedagogiaDisciplines[2]],
            [pedagogiaDisciplines[3]],
            [pedagogiaDisciplines[4]],
        };

        var teacherIds = new List<int>();
        var teacherEmails = new List<string>();
        foreach (var (assigned, index) in teacherDisciplines.Select((x, i) => (x, i)))
        {
            var email = DataGen.Email;
            var teacher = await directorClient.CreateTeacher(DataGen.UserName, email).Success();
            await directorClient.AssignDisciplinesToTeacher(teacher.Id, assigned);
            // Os 6 primeiros no Agreste, os 4 últimos no Suassuna.
            await directorClient.AssignCampiToTeacher(teacher.Id, [index < 6 ? agreste.Id : suassuna.Id]);
            teacherIds.Add(teacher.Id);
            teacherEmails.Add(email);
        }

        var days = new[] { Day.Monday, Day.Tuesday, Day.Wednesday, Day.Thursday, Day.Friday };

        // 15 turmas com os horários espalhados na semana:
        // - ADS de manhã no Agreste (07h–10h), salas A1 a A5;
        // - Direito à noite no Agreste (19h–22h), as mesmas salas em outro turno;
        // - Pedagogia à tarde no Suassuna (13h–17h), salas B1 a B4 (a B1 repete na sexta).
        var plan = new List<(int DisciplineId, int TeacherId, int CampusId, int ClassroomId, Day Day, Hour Start, Hour End)>();

        foreach (var index in Enumerable.Range(0, 5))
        {
            plan.Add((adsDisciplines[index], teacherIds[index / 2], agreste.Id, agresteRooms[index], days[index], Hour.H07_00, Hour.H10_00));
        }
        foreach (var index in Enumerable.Range(0, 5))
        {
            plan.Add((direitoDisciplines[index], teacherIds[3 + (index / 2)], agreste.Id, agresteRooms[index], days[index], Hour.H19_00, Hour.H22_00));
        }
        foreach (var index in Enumerable.Range(0, 5))
        {
            // Os 2 primeiros ficam com o professor 7 (que tem 2 disciplinas), os
            // outros 3 com um professor cada.
            var teacherIndex = index <= 1 ? 6 : 5 + index;
            plan.Add((pedagogiaDisciplines[index], teacherIds[teacherIndex], suassuna.Id, suassunaRooms[index % 4], days[index], Hour.H13_00, Hour.H17_00));
        }

        var classIds = new List<int>();
        foreach (var item in plan)
        {
            var @class = await directorClient.CreateClass(item.DisciplineId, firstPeriod.Id, vacancies: 100, campusId: item.CampusId).Success();
            await directorClient.UpdateClassTeachers(@class.Id, [item.TeacherId]);
            await directorClient.UpdateClassSchedules(@class.Id, [(item.Day, item.Start, item.End, item.TeacherId, item.ClassroomId)]);
            await directorClient.ReleaseClassForEnrollment(@class.Id);
            classIds.Add(@class.Id);
        }

        // 100 alunos, um por turma em round-robin: as 10 primeiras turmas ficam com
        // 7 alunos e as 5 últimas com 6.
        var offeringOf = new[] { adsMorning.Id, direitoEvening.Id, pedagogiaAfternoon.Id };
        var studentIds = new List<int>();
        var studentEmails = new List<string>();
        foreach (var index in Enumerable.Range(0, 100))
        {
            var email = DataGen.Email;
            var student = await directorClient.CreateStudent(DataGen.UserName, email).Success();
            var classIndex = index % 15;
            await directorClient.EnrollStudentInCourseOffering(student.Id, offeringOf[classIndex / 5]);
            await directorClient.AssignStudentToClass(student.Id, classIds[classIndex]);
            studentIds.Add(student.Id);
            studentEmails.Add(email);
        }

        foreach (var classId in classIds)
        {
            await directorClient.StartClass(classId);
        }

        // Responsáveis cobrindo parte dos alunos: 4 com um filho e 1 com dois irmãos
        // em turmas (e campi) diferentes — o aluno 0 está numa turma de ADS no Agreste
        // e o aluno 10 numa de Pedagogia no Suassuna.
        foreach (var index in Enumerable.Range(1, 4))
        {
            await directorClient.CreateParent(DataGen.UserName, DataGen.Email,
            [
                new() { StudentId = studentIds[index], Relationship = ParentRelationship.Mother },
            ]);
        }

        var siblingsParentEmail = DataGen.Email;
        await directorClient.CreateParent(DataGen.UserName, siblingsParentEmail,
        [
            new() { StudentId = studentIds[0], Relationship = ParentRelationship.Father },
            new() { StudentId = studentIds[10], Relationship = ParentRelationship.Father },
        ]);

        var firstTeacherClient = await _back.LoginAs(teacherEmails[0]);
        var lastTeacherClient = await _back.LoginAs(teacherEmails[9]);
        var firstStudentClient = await _back.LoginAs(studentEmails[0]);
        var siblingsParentClient = await _back.LoginAs(siblingsParentEmail);

        // Act
        var agresteOccupancy = await directorClient.GetCampusOccupancy(agreste.Id).Success();
        var suassunaOccupancy = await directorClient.GetCampusOccupancy(suassuna.Id).Success();
        var calendar = await directorClient.GetCalendar(2024).Success();
        var teachers = await directorClient.GetTeachers().Success();
        var students = await directorClient.GetStudents().Success();
        var classes = await directorClient.GetClasses(pageSize: 20).Success();
        var offerings = await directorClient.GetCourseOfferings().Success();
        var enrollmentPeriods = await directorClient.GetEnrollmentPeriods().Success();

        var weeklyMinutesByTeacher = new Dictionary<int, int>();
        foreach (var teacherId in teacherIds)
        {
            var details = await directorClient.GetTeacherDetails(teacherId).Success();
            weeklyMinutesByTeacher[teacherId] = details.Classes
                .SelectMany(c => c.Schedules)
                .Sum(s => s.EndAt.ToMinutes() - s.StartAt.ToMinutes());
        }

        var firstTeacherAgenda = await firstTeacherClient.GetTeacherAgenda().Success();
        var lastTeacherAgenda = await lastTeacherClient.GetTeacherAgenda().Success();
        var firstStudentAgenda = await firstStudentClient.GetStudentAgenda().Success();
        var siblings = await siblingsParentClient.GetParentStudents().Success();
        var firstSiblingAgenda = await siblingsParentClient.GetParentStudentAgenda(studentIds[0]).Success();
        var secondSiblingAgenda = await siblingsParentClient.GetParentStudentAgenda(studentIds[10]).Success();

        // Assert — o volume cadastrado.
        teachers.Total.Should().Be(10);
        students.Total.Should().Be(100);
        classes.Total.Should().Be(15);
        offerings.Total.Should().Be(10);
        enrollmentPeriods.Items.Should().HaveCount(2);

        // Assert — manager: ocupação dos 2 campi lado a lado, com drill down por
        // dia, turno e sala.
        agresteOccupancy.TotalClassrooms.Should().Be(6);
        agresteOccupancy.OpenCells.Should().Be(15);

        foreach (var day in days)
        {
            var morning = agresteOccupancy.Cells.First(c => c.Day == day && c.Shift == Shift.Morning);
            morning.UsedMinutes.Should().Be(180);
            morning.AvailableMinutes.Should().Be(1800); // 6 salas x 07h–12h
            morning.UsedMinutesRate.Should().Be(10M);

            // A noite tem menos minutos abertos, então o mesmo bloco pesa mais.
            var evening = agresteOccupancy.Cells.First(c => c.Day == day && c.Shift == Shift.Evening);
            evening.UsedMinutes.Should().Be(180);
            evening.AvailableMinutes.Should().Be(1080); // 6 salas x 19h–22h
            evening.UsedMinutesRate.Should().Be(16.67M);

            agresteOccupancy.Cells.First(c => c.Day == day && c.Shift == Shift.Afternoon)
                .UsedMinutes.Should().Be(0);
        }

        // A Sala A6 nunca é usada — é o espaço ocioso com nome.
        var salaA6 = agresteRooms[5];
        agresteOccupancy.Cells.SelectMany(c => c.Classrooms).Where(c => c.Id == salaA6)
            .Should().OnlyContain(c => c.UsedMinutes == 0);

        // 1.800min usados de 23400min abertos (6 salas x 13h x 60min x 5 dias).
        agresteOccupancy.Cells.Sum(c => c.AvailableMinutes).Should().Be(23400);
        agresteOccupancy.Cells.Sum(c => c.UsedMinutes).Should().Be(1800);
        agresteOccupancy.OverallUsedMinutesRate.Should().Be(7.69M);

        suassunaOccupancy.TotalClassrooms.Should().Be(4);
        suassunaOccupancy.OpenCells.Should().Be(10);
        suassunaOccupancy.Cells.Where(c => c.Shift == Shift.Evening)
            .Should().OnlyContain(c => !c.Open && c.AvailableMinutes == 0);

        foreach (var day in days)
        {
            var afternoon = suassunaOccupancy.Cells.First(c => c.Day == day && c.Shift == Shift.Afternoon);
            afternoon.UsedMinutes.Should().Be(240);
            afternoon.AvailableMinutes.Should().Be(1440); // 4 salas x 12h–18h
            afternoon.UsedMinutesRate.Should().Be(16.67M);
        }

        // 1200min de 13200min (4 salas x 660min x 5 dias — 300 de manhã e 360 de
        // tarde). Sem o recorte por OpeningHours o denominador seria quase o dobro.
        suassunaOccupancy.Cells.Sum(c => c.UsedMinutes).Should().Be(1200);
        suassunaOccupancy.Cells.Sum(c => c.AvailableMinutes).Should().Be(13200);
        suassunaOccupancy.OverallUsedMinutesRate.Should().Be(9.09M);

        // Assert — manager: distribuição de carga horária dos 10 professores.
        // 10 turmas de 180min no Agreste + 5 de 240min no Suassuna.
        weeklyMinutesByTeacher.Should().HaveCount(10);
        weeklyMinutesByTeacher.Values.Sum().Should().Be(3000);
        weeklyMinutesByTeacher.Values.Should().NotContain(0); // todos com pelo menos 1 turma
        weeklyMinutesByTeacher[teacherIds[0]].Should().Be(360); // 2 turmas de ADS
        weeklyMinutesByTeacher[teacherIds[6]].Should().Be(480); // 2 turmas de Pedagogia — o topo do ranking
        weeklyMinutesByTeacher[teacherIds[9]].Should().Be(240); // 1 turma de Pedagogia
        weeklyMinutesByTeacher.Values.Max().Should().Be(480);
        weeklyMinutesByTeacher.Values.Min().Should().Be(180);

        // Assert — manager: dias letivos x recesso x feriados no ano do calendário.
        calendar.Total.Should().Be(366);
        var byType = calendar.Items.GroupBy(x => x.DayType).ToDictionary(g => g.Key, g => g.Count());
        byType[DayType.Default].Should().Be(244);
        byType[DayType.Vacation].Should().Be(4);
        byType[DayType.Recess].Should().Be(5);
        byType[DayType.Holiday].Should().Be(13);

        // Assert — teacher: agenda semanal com sala, uma linha por turma.
        firstTeacherAgenda.Days.Select(d => d.Day).Should().Equal(Day.Monday, Day.Tuesday);
        firstTeacherAgenda.Days.Should().OnlyContain(d => d.Disciplines.Count == 1);
        firstTeacherAgenda.Days[0].Disciplines[0].Name.Should().Be("Algoritmos");
        firstTeacherAgenda.Days[0].Disciplines[0].ClassroomName.Should().Be("Sala A1");
        firstTeacherAgenda.Days[1].Disciplines[0].Name.Should().Be("Banco de Dados");
        firstTeacherAgenda.Days[1].Disciplines[0].ClassroomName.Should().Be("Sala A2");

        lastTeacherAgenda.Days.Should().ContainSingle().Which.Day.Should().Be(Day.Friday);
        lastTeacherAgenda.Days[0].Disciplines.Should().ContainSingle().Which.Name.Should().Be("Gestão Escolar");
        lastTeacherAgenda.Days[0].Disciplines[0].ClassroomName.Should().Be("Sala B1");

        // Assert — student: a agenda da sua turma.
        var firstStudentDay = firstStudentAgenda.Days.Should().ContainSingle().Which;
        firstStudentDay.Day.Should().Be(Day.Monday);
        firstStudentDay.Disciplines.Should().ContainSingle().Which.ClassId.Should().Be(classIds[0]);
        firstStudentDay.Disciplines[0].ClassroomName.Should().Be("Sala A1");

        // Assert — parent: agenda de cada filho num lugar só, mesmo em campi diferentes.
        siblings.Items.Should().HaveCount(2);
        firstSiblingAgenda.Days.Should().ContainSingle().Which.Disciplines[0].ClassroomName.Should().Be("Sala A1");
        var secondSiblingDay = secondSiblingAgenda.Days.Should().ContainSingle().Which;
        secondSiblingDay.Day.Should().Be(Day.Monday);
        secondSiblingDay.Disciplines[0].Name.Should().Be("Didática");
        secondSiblingDay.Disciplines[0].ClassroomName.Should().Be("Sala B1");
    }
}
