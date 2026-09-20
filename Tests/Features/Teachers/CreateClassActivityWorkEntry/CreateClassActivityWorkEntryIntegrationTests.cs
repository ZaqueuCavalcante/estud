using Newtonsoft.Json.Linq;

namespace Estud.Tests.Integration;

public partial class IntegrationTests
{
    #region Authentication

    [Test]
    public async Task Teachers_CreateClassActivityWorkEntry_Should_not_create_entry_when_not_authenticated()
    {
        // Arrange
        var client = _back.GetTestsClient();

        // Act
        var result = await client.CreateClassActivityWorkEntry(activityId: 1, workId: 1, note: 8.5m);

        // Assert
        result.ShouldBeError(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Authorization

    [Test]
    public async Task Teachers_CreateClassActivityWorkEntry_Should_not_create_entry_when_user_is_not_a_teacher()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.CreateClassActivityWorkEntry(activityId: 1, workId: 1, note: 8.5m);

        // Assert
        result.ShouldBeError(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task Teachers_CreateClassActivityWorkEntry_Should_not_create_entry_when_user_is_a_student()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var student = await director.CreateStudent(DataGen.UserName, DataGen.Email).Success();
        var client = await _back.LoginAs(student.Email);

        // Act
        var result = await client.CreateClassActivityWorkEntry(activityId: 1, workId: 1, note: 8.5m);

        // Assert
        result.ShouldBeError(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Validation errors

    [Test]
    public async Task Teachers_CreateClassActivityWorkEntry_Should_not_create_entry_when_activity_not_found()
    {
        // Arrange
        var client = await _back.LoggedAsTeacher();

        // Act
        var result = await client.CreateClassActivityWorkEntry(activityId: 999999, workId: 1, note: 8.5m);

        // Assert
        result.ShouldBeError(ClassActivityNotFound.I);
    }

    [Test]
    public async Task Teachers_CreateClassActivityWorkEntry_Should_not_create_entry_on_activity_of_another_teacher()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass();
        var teacher = await _back.LoginAs(@class.TeacherEmail);
        var activity = await teacher.CreateClassActivity(@class.Id).Success();
        var work = (await teacher.GetTeacherClassActivity(@class.Id, activity.Id).Success()).Works[0];

        var otherTeacher = await director.CreateTeacher(DataGen.UserName, DataGen.Email).Success();
        var client = await _back.LoginAs(otherTeacher.Email);

        // Act
        var result = await client.CreateClassActivityWorkEntry(activity.Id, work.Id, note: 8.5m);

        // Assert
        result.ShouldBeError(TeacherNotAssignedToClass.I);
    }

    [Test]
    public async Task Teachers_CreateClassActivityWorkEntry_Should_not_create_entry_on_activity_of_another_institution()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass();
        var teacher = await _back.LoginAs(@class.TeacherEmail);
        var activity = await teacher.CreateClassActivity(@class.Id).Success();
        var work = (await teacher.GetTeacherClassActivity(@class.Id, activity.Id).Success()).Works[0];

        var client = await _back.LoggedAsTeacher();

        // Act
        var result = await client.CreateClassActivityWorkEntry(activity.Id, work.Id, note: 8.5m);

        // Assert
        result.ShouldBeError(ClassActivityNotFound.I);
    }

    [Test]
    public async Task Teachers_CreateClassActivityWorkEntry_Should_not_create_entry_when_work_not_found()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass();
        var client = await _back.LoginAs(@class.TeacherEmail);
        var activity = await client.CreateClassActivity(@class.Id).Success();

        // Act
        var result = await client.CreateClassActivityWorkEntry(activity.Id, workId: 999999, note: 8.5m);

        // Assert
        result.ShouldBeError(ClassActivityWorkNotFound.I);
    }

    [Test]
    public async Task Teachers_CreateClassActivityWorkEntry_Should_not_create_entry_on_work_of_another_activity()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass();
        var client = await _back.LoginAs(@class.TeacherEmail);

        var activityA = await client.CreateClassActivity(@class.Id, ClassNoteType.N1, weight: 40).Success();
        var activityB = await client.CreateClassActivity(@class.Id, ClassNoteType.N2, weight: 40).Success();
        var work = (await client.GetTeacherClassActivity(@class.Id, activityB.Id).Success()).Works[0];

        // Act
        var result = await client.CreateClassActivityWorkEntry(activityA.Id, work.Id, note: 8.5m);

        // Assert
        result.ShouldBeError(ClassActivityWorkNotFound.I);
    }

    [TestCase(-1)]
    [TestCase(10.1)]
    public async Task Teachers_CreateClassActivityWorkEntry_Should_not_create_entry_with_invalid_note(decimal note)
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass();
        var client = await _back.LoginAs(@class.TeacherEmail);
        var activity = await client.CreateClassActivity(@class.Id).Success();
        var work = (await client.GetTeacherClassActivity(@class.Id, activity.Id).Success()).Works[0];

        // Act
        var result = await client.CreateClassActivityWorkEntry(activity.Id, work.Id, note: note);

        // Assert
        result.ShouldBeError(InvalidStudentClassNote.I);
    }

    [Test]
    public async Task Teachers_CreateClassActivityWorkEntry_Should_not_create_entry_with_content_too_long()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass();
        var client = await _back.LoginAs(@class.TeacherEmail);
        var activity = await client.CreateClassActivity(@class.Id).Success();
        var work = (await client.GetTeacherClassActivity(@class.Id, activity.Id).Success()).Works[0];

        // Act
        var result = await client.CreateClassActivityWorkEntry(activity.Id, work.Id, content: new string('a', 10_001));

        // Assert
        result.ShouldBeError(InvalidClassActivityWorkContent.I);
    }

    [Test]
    public async Task Teachers_CreateClassActivityWorkEntry_Should_not_create_empty_entry()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass();
        var client = await _back.LoginAs(@class.TeacherEmail);
        var activity = await client.CreateClassActivity(@class.Id).Success();
        var work = (await client.GetTeacherClassActivity(@class.Id, activity.Id).Success()).Works[0];

        // Act
        var result = await client.CreateClassActivityWorkEntry(activity.Id, work.Id);

        // Assert
        result.ShouldBeError(InvalidClassActivityWorkEntry.I);
    }

    #endregion

    #region Happy path

    [Test]
    public async Task Teachers_CreateClassActivityWorkEntry_Should_create_comment_without_changing_note_and_status()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass();
        var client = await _back.LoginAs(@class.TeacherEmail);
        var activity = await client.CreateClassActivity(@class.Id).Success();
        var work = (await client.GetTeacherClassActivity(@class.Id, activity.Id).Success()).Works[0];

        // Act
        var result = await client.CreateClassActivityWorkEntry(activity.Id, work.Id, content: "Cadê o trabalho?");

        // Assert
        result.ShouldBeSuccess();

        var updated = (await client.GetTeacherClassActivity(@class.Id, activity.Id).Success()).Works[0];
        updated.Value.Should().Be(0);
        updated.Status.Should().Be(ClassActivityWorkStatus.Pending);
        updated.Entries.Should().ContainSingle(e =>
            e.Type == ClassActivityWorkEntryType.Comment &&
            e.User == @class.TeacherName &&
            e.Content == "Cadê o trabalho?"
        );
    }

    [Test]
    public async Task Teachers_CreateClassActivityWorkEntry_Should_create_note_change_entry()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass();
        var client = await _back.LoginAs(@class.TeacherEmail);
        var activity = await client.CreateClassActivity(@class.Id).Success();
        var work = (await client.GetTeacherClassActivity(@class.Id, activity.Id).Success()).Works[0];

        // Act
        var result = await client.CreateClassActivityWorkEntry(activity.Id, work.Id, note: 7.5m);

        // Assert
        result.ShouldBeSuccess();

        var updated = (await client.GetTeacherClassActivity(@class.Id, activity.Id).Success()).Works[0];
        updated.Value.Should().Be(7.5m);
        updated.Status.Should().Be(ClassActivityWorkStatus.Pending);

        var entry = updated.Entries.Should().ContainSingle().Subject;
        entry.Type.Should().Be(ClassActivityWorkEntryType.NoteChange);
        entry.Content.Should().BeNull();

        var metadata = (JObject)entry.Metadata!;
        metadata["fromNote"]!.Value<decimal>().Should().Be(0);
        metadata["toNote"]!.Value<decimal>().Should().Be(7.5m);
    }

    [Test]
    public async Task Teachers_CreateClassActivityWorkEntry_Should_create_note_and_status_change_entries()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass();
        var client = await _back.LoginAs(@class.TeacherEmail);
        var activity = await client.CreateClassActivity(@class.Id).Success();

        var student = await _back.LoginAs(@class.StudentEmail);
        await student.CreateClassActivityWorkComment(activity.Id, "https://github.com/ZaqueuCavalcante/estud");

        var work = (await client.GetTeacherClassActivity(@class.Id, activity.Id).Success()).Works[0];

        // Act
        var result = await client.CreateClassActivityWorkEntry(
            activity.Id, work.Id, note: 8.5m, status: ClassActivityWorkStatus.Finalized);

        // Assert
        result.ShouldBeSuccess();

        var updated = (await client.GetTeacherClassActivity(@class.Id, activity.Id).Success()).Works[0];
        updated.Value.Should().Be(8.5m);
        updated.Status.Should().Be(ClassActivityWorkStatus.Finalized);

        updated.Entries.Should().HaveCount(3);
        updated.Entries[0].Type.Should().Be(ClassActivityWorkEntryType.Comment);

        updated.Entries[1].Type.Should().Be(ClassActivityWorkEntryType.NoteChange);
        var noteChange = (JObject)updated.Entries[1].Metadata!;
        noteChange["fromNote"]!.Value<decimal>().Should().Be(0);
        noteChange["toNote"]!.Value<decimal>().Should().Be(8.5m);

        updated.Entries[2].Type.Should().Be(ClassActivityWorkEntryType.StatusChange);
        var statusChange = (JObject)updated.Entries[2].Metadata!;
        statusChange["fromStatus"]!.Value<string>().Should().Be(nameof(ClassActivityWorkStatus.Review));
        statusChange["toStatus"]!.Value<string>().Should().Be(nameof(ClassActivityWorkStatus.Finalized));
    }

    [Test]
    public async Task Teachers_CreateClassActivityWorkEntry_Should_create_comment_note_and_status_change_entries()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass();
        var client = await _back.LoginAs(@class.TeacherEmail);
        var activity = await client.CreateClassActivity(@class.Id).Success();
        var work = (await client.GetTeacherClassActivity(@class.Id, activity.Id).Success()).Works[0];

        // Act
        var result = await client.CreateClassActivityWorkEntry(
            activity.Id, work.Id, "Ficou ótimo depois dos ajustes.", 8.5m, ClassActivityWorkStatus.Finalized);

        // Assert
        result.ShouldBeSuccess();

        var updated = (await client.GetTeacherClassActivity(@class.Id, activity.Id).Success()).Works[0];
        updated.Entries.Should().HaveCount(3);
        updated.Entries[0].Type.Should().Be(ClassActivityWorkEntryType.Comment);
        updated.Entries[0].Content.Should().Be("Ficou ótimo depois dos ajustes.");
        updated.Entries[1].Type.Should().Be(ClassActivityWorkEntryType.NoteChange);
        updated.Entries[2].Type.Should().Be(ClassActivityWorkEntryType.StatusChange);
    }

    [Test]
    public async Task Teachers_CreateClassActivityWorkEntry_Should_not_create_entries_when_note_and_status_do_not_change()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass();
        var client = await _back.LoginAs(@class.TeacherEmail);
        var activity = await client.CreateClassActivity(@class.Id).Success();
        var work = (await client.GetTeacherClassActivity(@class.Id, activity.Id).Success()).Works[0];

        await client.CreateClassActivityWorkEntry(activity.Id, work.Id, note: 7m, status: ClassActivityWorkStatus.Finalized);

        // Act
        var result = await client.CreateClassActivityWorkEntry(
            activity.Id, work.Id, note: 7m, status: ClassActivityWorkStatus.Finalized);

        // Assert
        result.ShouldBeSuccess();

        var updated = (await client.GetTeacherClassActivity(@class.Id, activity.Id).Success()).Works[0];
        updated.Entries.Should().HaveCount(2);
    }

    [Test]
    public async Task Teachers_CreateClassActivityWorkEntry_Should_update_note_of_finalized_work()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass();
        var client = await _back.LoginAs(@class.TeacherEmail);
        var activity = await client.CreateClassActivity(@class.Id).Success();
        var work = (await client.GetTeacherClassActivity(@class.Id, activity.Id).Success()).Works[0];

        await client.CreateClassActivityWorkEntry(activity.Id, work.Id, note: 0, status: ClassActivityWorkStatus.Finalized);

        // Act
        var result = await client.CreateClassActivityWorkEntry(
            activity.Id, work.Id, "Considerei o atestado.", 7m);

        // Assert
        result.ShouldBeSuccess();

        var updated = (await client.GetTeacherClassActivity(@class.Id, activity.Id).Success()).Works[0];
        updated.Value.Should().Be(7m);
        updated.Status.Should().Be(ClassActivityWorkStatus.Finalized);

        updated.Entries.Should().HaveCount(3);
        updated.Entries[1].Type.Should().Be(ClassActivityWorkEntryType.Comment);

        updated.Entries[2].Type.Should().Be(ClassActivityWorkEntryType.NoteChange);
        var noteChange = (JObject)updated.Entries[2].Metadata!;
        noteChange["fromNote"]!.Value<decimal>().Should().Be(0);
        noteChange["toNote"]!.Value<decimal>().Should().Be(7m);
    }

    [Test]
    public async Task Teachers_CreateClassActivityWorkEntry_Should_reopen_finalized_work()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass();
        var client = await _back.LoginAs(@class.TeacherEmail);
        var activity = await client.CreateClassActivity(@class.Id).Success();
        var work = (await client.GetTeacherClassActivity(@class.Id, activity.Id).Success()).Works[0];

        await client.CreateClassActivityWorkEntry(activity.Id, work.Id, note: 0, status: ClassActivityWorkStatus.Finalized);

        // Act
        var result = await client.CreateClassActivityWorkEntry(
            activity.Id, work.Id, "Pode entregar até sexta.", status: ClassActivityWorkStatus.Review);

        // Assert
        result.ShouldBeSuccess();

        var updated = (await client.GetTeacherClassActivity(@class.Id, activity.Id).Success()).Works[0];
        updated.Status.Should().Be(ClassActivityWorkStatus.Review);

        var student = await _back.LoginAs(@class.StudentEmail);
        var comment = await student.CreateClassActivityWorkComment(activity.Id, "Obrigada!");
        comment.ShouldBeSuccess();
    }

    #endregion
}
