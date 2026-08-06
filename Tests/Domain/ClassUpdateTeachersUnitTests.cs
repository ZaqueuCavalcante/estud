using Estud.Back.Domain.Classes;
using Estud.Back.Domain.Identity;
using Estud.Back.Domain.Teachers;

namespace Estud.Tests.Domain;

public class ClassUpdateTeachersUnitTests
{
    private const int InstitutionId = 1;

    private static EstudTeacher Teacher(int id) =>
        new(new EstudUser(InstitutionId, $"Professor {id}", $"professor{id}@estud.com"), InstitutionId, $"Professor {id}")
        {
            Id = id,
        };

    private static Class NewClass(params EstudTeacher[] teachers)
    {
        var @class = new Class(InstitutionId, 1, 1, 40, 1) { Id = 1 };
        @class.Teachers.AddRange(teachers);
        return @class;
    }

    private static Schedule Slot(Day day, Hour start, Hour end, int? teacherId) =>
        Schedule.New(day, start, end, teacherId).Success;

    #region Nível 1 — sem alteração

    [Test]
    public void Class_UpdateTeachers_Should_do_nothing_when_the_class_has_no_teacher_and_the_list_is_empty()
    {
        // Arrange
        var @class = NewClass();

        // Act
        @class.UpdateTeachers([]);

        // Assert
        @class.Teachers.Should().BeEmpty();
    }

    [Test]
    public void Class_UpdateTeachers_Should_keep_the_only_teacher_when_the_list_is_the_same()
    {
        // Arrange
        var teacher = Teacher(1);
        var @class = NewClass(teacher);

        // Act
        @class.UpdateTeachers([teacher]);

        // Assert
        @class.Teachers.Select(x => x.Id).Should().Equal(1);
    }

    [Test]
    public void Class_UpdateTeachers_Should_keep_both_teachers_when_the_list_is_the_same()
    {
        // Arrange
        var first = Teacher(1);
        var second = Teacher(2);
        var @class = NewClass(first, second);

        // Act
        @class.UpdateTeachers([first, second]);

        // Assert
        @class.Teachers.Select(x => x.Id).Should().Equal(1, 2);
    }

    [Test]
    public void Class_UpdateTeachers_Should_keep_both_teachers_when_the_list_is_the_same_in_another_order()
    {
        // Arrange — a ordem da lista de entrada não deve reordenar nem recriar nada.
        var first = Teacher(1);
        var second = Teacher(2);
        var @class = NewClass(first, second);

        // Act
        @class.UpdateTeachers([second, first]);

        // Assert
        @class.Teachers.Select(x => x.Id).Should().Equal(1, 2);
    }

    #endregion

    #region Nível 2 — adição

    [Test]
    public void Class_UpdateTeachers_Should_add_the_first_teacher_to_a_class_without_teachers()
    {
        // Arrange
        var @class = NewClass();

        // Act
        @class.UpdateTeachers([Teacher(1)]);

        // Assert
        @class.Teachers.Select(x => x.Id).Should().Equal(1);
    }

    [Test]
    public void Class_UpdateTeachers_Should_add_a_second_teacher_keeping_the_first_one()
    {
        // Arrange
        var first = Teacher(1);
        var @class = NewClass(first);

        // Act
        @class.UpdateTeachers([first, Teacher(2)]);

        // Assert
        @class.Teachers.Select(x => x.Id).Should().Equal(1, 2);
    }

    [Test]
    public void Class_UpdateTeachers_Should_add_two_teachers_at_once_to_a_class_without_teachers()
    {
        // Arrange
        var @class = NewClass();

        // Act
        @class.UpdateTeachers([Teacher(1), Teacher(2)]);

        // Assert
        @class.Teachers.Select(x => x.Id).Should().Equal(1, 2);
    }

    #endregion

    #region Nível 3 — remoção

    [Test]
    public void Class_UpdateTeachers_Should_remove_the_only_teacher_when_the_list_is_empty()
    {
        // Arrange
        var @class = NewClass(Teacher(1));

        // Act
        @class.UpdateTeachers([]);

        // Assert
        @class.Teachers.Should().BeEmpty();
    }

    [Test]
    public void Class_UpdateTeachers_Should_remove_one_of_the_two_teachers()
    {
        // Arrange
        var first = Teacher(1);
        var second = Teacher(2);
        var @class = NewClass(first, second);

        // Act
        @class.UpdateTeachers([second]);

        // Assert
        @class.Teachers.Select(x => x.Id).Should().Equal(2);
    }

    [Test]
    public void Class_UpdateTeachers_Should_remove_both_teachers_when_the_list_is_empty()
    {
        // Arrange
        var @class = NewClass(Teacher(1), Teacher(2));

        // Act
        @class.UpdateTeachers([]);

        // Assert
        @class.Teachers.Should().BeEmpty();
    }

    #endregion

    #region Nível 4 — troca

    [Test]
    public void Class_UpdateTeachers_Should_replace_the_only_teacher_with_another_one()
    {
        // Arrange
        var @class = NewClass(Teacher(1));

        // Act
        @class.UpdateTeachers([Teacher(2)]);

        // Assert
        @class.Teachers.Select(x => x.Id).Should().Equal(2);
    }

    [Test]
    public void Class_UpdateTeachers_Should_replace_the_only_teacher_and_add_a_new_one_at_the_same_time()
    {
        // Arrange — o único professor sai e dois entram na mesma chamada.
        var @class = NewClass(Teacher(1));

        // Act
        @class.UpdateTeachers([Teacher(2), Teacher(3)]);

        // Assert
        @class.Teachers.Select(x => x.Id).Should().Equal(2, 3);
    }

    [Test]
    public void Class_UpdateTeachers_Should_replace_one_of_the_two_teachers()
    {
        // Arrange
        var kept = Teacher(2);
        var @class = NewClass(Teacher(1), kept);

        // Act
        @class.UpdateTeachers([kept, Teacher(3)]);

        // Assert
        @class.Teachers.Select(x => x.Id).Should().Equal(2, 3);
    }

    [Test]
    public void Class_UpdateTeachers_Should_replace_both_teachers()
    {
        // Arrange
        var @class = NewClass(Teacher(1), Teacher(2));

        // Act
        @class.UpdateTeachers([Teacher(3), Teacher(4)]);

        // Assert
        @class.Teachers.Select(x => x.Id).Should().Equal(3, 4);
    }

    [Test]
    public void Class_UpdateTeachers_Should_replace_both_teachers_with_a_single_one()
    {
        // Arrange
        var @class = NewClass(Teacher(1), Teacher(2));

        // Act
        @class.UpdateTeachers([Teacher(3)]);

        // Assert
        @class.Teachers.Select(x => x.Id).Should().Equal(3);
    }

    #endregion

    #region Nível 5 — horários

    [Test]
    public void Class_UpdateTeachers_Should_not_change_any_schedule_when_the_removed_teacher_has_no_schedule()
    {
        // Arrange — a turma tem horários, mas nenhum deles é do professor removido.
        var @class = NewClass(Teacher(1));
        @class.Schedules.AddRange([
            Slot(Day.Monday, Hour.H19_00, Hour.H22_00, null),
            Slot(Day.Wednesday, Hour.H19_00, Hour.H22_00, null),
        ]);

        // Act
        @class.UpdateTeachers([]);

        // Assert
        @class.Teachers.Should().BeEmpty();
        @class.Schedules.Should().HaveCount(2);
        @class.Schedules.Should().OnlyContain(x => x.TeacherId == null);
    }

    [Test]
    public void Class_UpdateTeachers_Should_unassign_the_single_schedule_of_the_removed_teacher()
    {
        // Arrange
        var @class = NewClass(Teacher(1));
        @class.Schedules.Add(Slot(Day.Monday, Hour.H19_00, Hour.H22_00, 1));

        // Act
        @class.UpdateTeachers([]);

        // Assert
        @class.Teachers.Should().BeEmpty();
        @class.Schedules.Should().ContainSingle().Which.TeacherId.Should().BeNull();
    }

    [Test]
    public void Class_UpdateTeachers_Should_unassign_every_schedule_of_the_removed_teacher()
    {
        // Arrange — grade noturna inteira do mesmo professor.
        var @class = NewClass(Teacher(1));
        @class.Schedules.AddRange([
            Slot(Day.Monday, Hour.H19_00, Hour.H22_00, 1),
            Slot(Day.Wednesday, Hour.H19_00, Hour.H22_00, 1),
            Slot(Day.Friday, Hour.H19_00, Hour.H22_00, 1),
        ]);

        // Act
        @class.UpdateTeachers([]);

        // Assert
        @class.Schedules.Should().HaveCount(3);
        @class.Schedules.Should().OnlyContain(x => x.TeacherId == null);
    }

    [Test]
    public void Class_UpdateTeachers_Should_keep_the_schedules_of_the_teacher_that_stays()
    {
        // Arrange
        var kept = Teacher(1);
        var @class = NewClass(kept);
        @class.Schedules.AddRange([
            Slot(Day.Monday, Hour.H19_00, Hour.H22_00, 1),
            Slot(Day.Wednesday, Hour.H19_00, Hour.H22_00, 1),
        ]);

        // Act
        @class.UpdateTeachers([kept]);

        // Assert
        @class.Schedules.Should().OnlyContain(x => x.TeacherId == 1);
    }

    [Test]
    public void Class_UpdateTeachers_Should_unassign_only_the_schedules_of_the_removed_teacher()
    {
        // Arrange — dois professores, vários horários cada um; só um sai.
        var kept = Teacher(1);
        var @class = NewClass(kept, Teacher(2));
        @class.Schedules.AddRange([
            Slot(Day.Monday, Hour.H07_00, Hour.H08_45, 1),
            Slot(Day.Monday, Hour.H09_00, Hour.H10_45, 2),
            Slot(Day.Wednesday, Hour.H07_00, Hour.H08_45, 1),
            Slot(Day.Wednesday, Hour.H09_00, Hour.H10_45, 2),
        ]);

        // Act
        @class.UpdateTeachers([kept]);

        // Assert
        @class.Teachers.Select(x => x.Id).Should().Equal(1);
        @class.Schedules.Select(x => x.TeacherId).Should().Equal(1, null, 1, null);
    }

    [Test]
    public void Class_UpdateTeachers_Should_unassign_the_schedules_when_only_the_removed_teacher_has_schedules()
    {
        // Arrange — dos dois professores, apenas o que sai tem horários.
        var kept = Teacher(1);
        var @class = NewClass(kept, Teacher(2));
        @class.Schedules.AddRange([
            Slot(Day.Tuesday, Hour.H13_00, Hour.H15_00, 2),
            Slot(Day.Thursday, Hour.H13_00, Hour.H15_00, 2),
        ]);

        // Act
        @class.UpdateTeachers([kept]);

        // Assert
        @class.Teachers.Select(x => x.Id).Should().Equal(1);
        @class.Schedules.Should().OnlyContain(x => x.TeacherId == null);
    }

    [Test]
    public void Class_UpdateTeachers_Should_not_change_any_schedule_when_only_the_teacher_that_stays_has_schedules()
    {
        // Arrange — dos dois professores, apenas o que fica tem horários.
        var kept = Teacher(1);
        var @class = NewClass(kept, Teacher(2));
        @class.Schedules.AddRange([
            Slot(Day.Tuesday, Hour.H13_00, Hour.H15_00, 1),
            Slot(Day.Thursday, Hour.H13_00, Hour.H15_00, 1),
        ]);

        // Act
        @class.UpdateTeachers([kept]);

        // Assert
        @class.Teachers.Select(x => x.Id).Should().Equal(1);
        @class.Schedules.Should().OnlyContain(x => x.TeacherId == 1);
    }

    [Test]
    public void Class_UpdateTeachers_Should_keep_the_schedules_without_teacher_untouched()
    {
        // Arrange — horários sem professor continuam sem professor.
        var @class = NewClass(Teacher(1));
        @class.Schedules.AddRange([
            Slot(Day.Monday, Hour.H19_00, Hour.H22_00, 1),
            Slot(Day.Wednesday, Hour.H19_00, Hour.H22_00, null),
        ]);

        // Act
        @class.UpdateTeachers([Teacher(2)]);

        // Assert
        @class.Teachers.Select(x => x.Id).Should().Equal(2);
        @class.Schedules.Should().OnlyContain(x => x.TeacherId == null);
    }

    [Test]
    public void Class_UpdateTeachers_Should_unassign_every_schedule_when_all_teachers_are_removed()
    {
        // Arrange
        var @class = NewClass(Teacher(1), Teacher(2));
        @class.Schedules.AddRange([
            Slot(Day.Monday, Hour.H07_00, Hour.H09_00, 1),
            Slot(Day.Tuesday, Hour.H07_00, Hour.H09_00, 2),
            Slot(Day.Friday, Hour.H07_00, Hour.H09_00, 1),
        ]);

        // Act
        @class.UpdateTeachers([]);

        // Assert
        @class.Teachers.Should().BeEmpty();
        @class.Schedules.Should().HaveCount(3);
        @class.Schedules.Should().OnlyContain(x => x.TeacherId == null);
    }

    [Test]
    public void Class_UpdateTeachers_Should_not_assign_any_schedule_to_the_added_teacher()
    {
        // Arrange — o professor novo entra sem herdar nenhum horário da turma.
        var kept = Teacher(1);
        var @class = NewClass(kept);
        @class.Schedules.AddRange([
            Slot(Day.Monday, Hour.H19_00, Hour.H22_00, 1),
            Slot(Day.Wednesday, Hour.H19_00, Hour.H22_00, null),
        ]);

        // Act
        @class.UpdateTeachers([kept, Teacher(2)]);

        // Assert
        @class.Teachers.Select(x => x.Id).Should().Equal(1, 2);
        @class.Schedules.Select(x => x.TeacherId).Should().Equal(1, null);
    }

    [Test]
    public void Class_UpdateTeachers_Should_keep_the_days_and_hours_of_the_schedules_when_the_teacher_is_replaced()
    {
        // Arrange — trocar o professor só mexe no vínculo, nunca na grade.
        var @class = NewClass(Teacher(1));
        @class.Schedules.AddRange([
            Slot(Day.Monday, Hour.H19_00, Hour.H22_00, 1),
            Slot(Day.Wednesday, Hour.H19_00, Hour.H22_00, 1),
        ]);

        // Act
        @class.UpdateTeachers([Teacher(2)]);

        // Assert
        @class.Schedules.Select(x => (x.Day, x.Start, x.End)).Should().Equal(
            (Day.Monday, Hour.H19_00, Hour.H22_00),
            (Day.Wednesday, Hour.H19_00, Hour.H22_00));
        @class.Schedules.Should().OnlyContain(x => x.TeacherId == null);
    }

    [Test]
    public void Class_UpdateTeachers_Should_not_change_the_schedule_of_a_teacher_that_is_not_in_the_class()
    {
        // Arrange — horário apontando pra um professor que não está na lista da turma:
        // como a limpeza é feita só pelos ids removidos, esse vínculo órfão é preservado.
        var @class = NewClass(Teacher(1));
        @class.Schedules.AddRange([
            Slot(Day.Monday, Hour.H19_00, Hour.H22_00, 1),
            Slot(Day.Wednesday, Hour.H19_00, Hour.H22_00, 99),
        ]);

        // Act
        @class.UpdateTeachers([]);

        // Assert
        @class.Schedules.Select(x => x.TeacherId).Should().Equal(null, 99);
    }

    #endregion

    #region Nível 6 — casos combinados e de borda

    [Test]
    public void Class_UpdateTeachers_Should_add_the_teacher_only_once_when_it_is_duplicated_in_the_list()
    {
        // Arrange
        var @class = NewClass();
        var teacher = Teacher(1);

        // Act
        @class.UpdateTeachers([teacher, teacher]);

        // Assert
        @class.Teachers.Select(x => x.Id).Should().Equal(1);
    }

    [Test]
    public void Class_UpdateTeachers_Should_keep_the_original_instance_when_another_instance_with_the_same_id_is_sent()
    {
        // Arrange — a comparação é por id, então a instância já rastreada não é substituída.
        var tracked = Teacher(1);
        var @class = NewClass(tracked);
        var detached = Teacher(1);

        // Act
        @class.UpdateTeachers([detached]);

        // Assert
        @class.Teachers.Should().ContainSingle().Which.Should().BeSameAs(tracked);
    }

    [Test]
    public void Class_UpdateTeachers_Should_replace_one_teacher_of_a_full_week_grade_keeping_the_other_schedules()
    {
        // Arrange — grade cheia dividida entre dois professores; só o professor 2 é trocado.
        var kept = Teacher(1);
        var @class = NewClass(kept, Teacher(2));
        @class.Schedules.AddRange([
            Slot(Day.Monday, Hour.H07_00, Hour.H08_45, 1),
            Slot(Day.Monday, Hour.H09_00, Hour.H10_45, 1),
            Slot(Day.Tuesday, Hour.H07_00, Hour.H08_45, 2),
            Slot(Day.Wednesday, Hour.H13_00, Hour.H14_45, 1),
            Slot(Day.Thursday, Hour.H13_00, Hour.H16_30, 2),
            Slot(Day.Friday, Hour.H19_00, Hour.H22_00, null),
        ]);

        // Act
        @class.UpdateTeachers([kept, Teacher(3)]);

        // Assert
        @class.Teachers.Select(x => x.Id).Should().Equal(1, 3);
        @class.Schedules.Select(x => x.TeacherId).Should().Equal(1, 1, null, 1, null, null);
    }

    [Test]
    public void Class_UpdateTeachers_Should_replace_both_teachers_of_a_full_week_grade_and_unassign_every_schedule()
    {
        // Arrange — os dois professores saem de uma vez e dois novos entram.
        var @class = NewClass(Teacher(1), Teacher(2));
        @class.Schedules.AddRange([
            Slot(Day.Monday, Hour.H07_00, Hour.H08_45, 1),
            Slot(Day.Monday, Hour.H09_00, Hour.H10_45, 2),
            Slot(Day.Tuesday, Hour.H07_00, Hour.H08_45, 1),
            Slot(Day.Wednesday, Hour.H13_00, Hour.H14_45, 2),
            Slot(Day.Thursday, Hour.H13_00, Hour.H16_30, 1),
            Slot(Day.Saturday, Hour.H08_00, Hour.H12_00, 2),
        ]);

        // Act
        @class.UpdateTeachers([Teacher(3), Teacher(4)]);

        // Assert
        @class.Teachers.Select(x => x.Id).Should().Equal(3, 4);
        @class.Schedules.Should().HaveCount(6);
        @class.Schedules.Should().OnlyContain(x => x.TeacherId == null);
    }

    #endregion
}
