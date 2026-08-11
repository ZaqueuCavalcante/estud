using Estud.Back.Domain.Classes;

namespace Estud.Tests.Domain;

public class ScheduleIntersectUnitTests
{
    private static Schedule Window(Day day, Hour start, Hour end) => Schedule.Window(day, start, end);

    private static Schedule ClassSchedule(
        Day day,
        Hour start,
        Hour end,
        int? classId = null,
        int? teacherId = null,
        int? classroomId = null
    ) {
        var schedule = Schedule.New(day, start, end, teacherId, classroomId).Success;
        schedule.ClassId = classId;
        return schedule;
    }

    #region Nível 1 — janela sem dono

    [Test]
    public void Schedule_Window_Should_keep_the_day_and_the_hours_it_was_given()
    {
        // Act
        var window = Schedule.Window(Day.Monday, Hour.H07_00, Hour.H12_00);

        // Assert
        window.Day.Should().Be(Day.Monday);
        (window.Start, window.End).Should().Be((Hour.H07_00, Hour.H12_00));
    }

    [Test]
    public void Schedule_Window_Should_be_born_without_class_teacher_or_classroom()
    {
        // Arrange — a janela representa um intervalo, não uma alocação.
        var window = Schedule.Window(Day.Monday, Hour.H07_00, Hour.H12_00);

        // Assert
        window.ClassId.Should().BeNull();
        window.TeacherId.Should().BeNull();
        window.ClassroomId.Should().BeNull();
    }

    [Test]
    public void Schedule_Window_Should_be_born_without_id()
    {
        // Arrange — janela não é linha do banco, é intervalo em memória.
        var window = Schedule.Window(Day.Monday, Hour.H07_00, Hour.H12_00);

        // Assert
        window.Id.Should().Be(0);
    }

    [Test]
    public void Schedule_Window_Should_not_validate_the_interval_it_receives()
    {
        // Arrange — Window não passa pela validação do New: quem chama (o turno e a
        // janela de funcionamento já persistida) traz valores válidos por construção.
        // New continua sendo o único caminho validado para horário de turma.
        var inverted = Schedule.Window(Day.Monday, Hour.H12_00, Hour.H07_00);

        // Assert
        (inverted.Start, inverted.End).Should().Be((Hour.H12_00, Hour.H07_00));
    }

    [Test]
    public void Schedule_New_Should_reject_the_inverted_interval_that_Window_accepts()
    {
        // Act — o mesmo intervalo invertido, pelo caminho validado.
        var result = Schedule.New(Day.Monday, Hour.H12_00, Hour.H07_00);

        // Assert
        result.IsError.Should().BeTrue();
    }

    #endregion

    #region Nível 2 — sem sobreposição

    [Test]
    public void Schedule_Intersect_Should_not_intersect_schedules_of_different_days()
    {
        // Arrange — mesmo intervalo, dias diferentes.
        var monday = Window(Day.Monday, Hour.H07_00, Hour.H12_00);
        var tuesday = Window(Day.Tuesday, Hour.H07_00, Hour.H12_00);

        // Act
        var result = monday.Intersect(tuesday);

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public void Schedule_Intersect_Should_not_intersect_when_the_other_comes_later_in_the_day()
    {
        // Arrange — 07h–10h e 14h–18h não se encontram.
        var morning = Window(Day.Monday, Hour.H07_00, Hour.H10_00);
        var afternoon = Window(Day.Monday, Hour.H14_00, Hour.H18_00);

        // Act
        var result = morning.Intersect(afternoon);

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public void Schedule_Intersect_Should_not_intersect_when_the_other_comes_earlier_in_the_day()
    {
        // Arrange — o mesmo par, na ordem inversa.
        var morning = Window(Day.Monday, Hour.H07_00, Hour.H10_00);
        var afternoon = Window(Day.Monday, Hour.H14_00, Hour.H18_00);

        // Act
        var result = afternoon.Intersect(morning);

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public void Schedule_Intersect_Should_not_intersect_schedules_that_only_touch_at_the_end()
    {
        // Arrange — 10h–12h termina exatamente quando 12h–14h começa. Encostar não é
        // sobrepor: é isso que faz a aula de 10h–14h render 120min de manhã e 120min à
        // tarde, em vez de contar o meio-dia duas vezes.
        var first = Window(Day.Monday, Hour.H10_00, Hour.H12_00);
        var second = Window(Day.Monday, Hour.H12_00, Hour.H14_00);

        // Act
        var result = first.Intersect(second);

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public void Schedule_Intersect_Should_not_intersect_schedules_that_only_touch_at_the_start()
    {
        // Arrange — o mesmo encosto, pelo outro lado.
        var first = Window(Day.Monday, Hour.H10_00, Hour.H12_00);
        var second = Window(Day.Monday, Hour.H12_00, Hour.H14_00);

        // Act
        var result = second.Intersect(first);

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public void Schedule_Intersect_Should_not_intersect_when_the_other_is_a_single_instant()
    {
        // Arrange — janela degenerada (começa e termina no mesmo ponto) não ocupa minuto.
        var schedule = Window(Day.Monday, Hour.H07_00, Hour.H12_00);
        var instant = Window(Day.Monday, Hour.H09_00, Hour.H09_00);

        // Act
        var result = schedule.Intersect(instant);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region Nível 3 — sobreposição parcial

    [Test]
    public void Schedule_Intersect_Should_cut_the_start_when_it_begins_before_the_other()
    {
        // Arrange — aula 10h–14h contra o turno da manhã 07h–12h: sobra 10h–12h.
        var schedule = Window(Day.Monday, Hour.H10_00, Hour.H14_00);
        var shift = Window(Day.Monday, Hour.H07_00, Hour.H12_00);

        // Act
        var result = schedule.Intersect(shift);

        // Assert
        result.Should().NotBeNull();
        (result!.Start, result.End).Should().Be((Hour.H10_00, Hour.H12_00));
        result.GetDiffInMinutes().Should().Be(120);
    }

    [Test]
    public void Schedule_Intersect_Should_cut_the_end_when_it_finishes_after_the_other()
    {
        // Arrange — a mesma aula 10h–14h contra a tarde 12h–18h: sobra 12h–14h.
        var schedule = Window(Day.Monday, Hour.H10_00, Hour.H14_00);
        var shift = Window(Day.Monday, Hour.H12_00, Hour.H18_00);

        // Act
        var result = schedule.Intersect(shift);

        // Assert
        result.Should().NotBeNull();
        (result!.Start, result.End).Should().Be((Hour.H12_00, Hour.H14_00));
        result.GetDiffInMinutes().Should().Be(120);
    }

    [Test]
    public void Schedule_Intersect_Should_cut_both_ends_when_the_other_is_inside_it()
    {
        // Arrange — aula 07h–22h contra a janela de almoço fechado 12h–13h.
        var schedule = Window(Day.Monday, Hour.H07_00, Hour.H22_00);
        var lunch = Window(Day.Monday, Hour.H12_00, Hour.H13_00);

        // Act
        var result = schedule.Intersect(lunch);

        // Assert
        result.Should().NotBeNull();
        (result!.Start, result.End).Should().Be((Hour.H12_00, Hour.H13_00));
        result.GetDiffInMinutes().Should().Be(60);
    }

    [Test]
    public void Schedule_Intersect_Should_intersect_a_window_of_fifteen_minutes()
    {
        // Arrange — a grade tem passo de 15min, então esse é o menor recorte possível.
        var schedule = Window(Day.Monday, Hour.H07_00, Hour.H11_45);
        var other = Window(Day.Monday, Hour.H11_30, Hour.H14_00);

        // Act
        var result = schedule.Intersect(other);

        // Assert
        result.Should().NotBeNull();
        (result!.Start, result.End).Should().Be((Hour.H11_30, Hour.H11_45));
        result.GetDiffInMinutes().Should().Be(15);
    }

    [Test]
    public void Schedule_Intersect_Should_intersect_hours_that_are_not_aligned_to_the_hour()
    {
        // Arrange — 07h30–10h45 contra 09h15–12h00.
        var schedule = Window(Day.Monday, Hour.H07_30, Hour.H10_45);
        var other = Window(Day.Monday, Hour.H09_15, Hour.H12_00);

        // Act
        var result = schedule.Intersect(other);

        // Assert
        result.Should().NotBeNull();
        (result!.Start, result.End).Should().Be((Hour.H09_15, Hour.H10_45));
        result.GetDiffInMinutes().Should().Be(90);
    }

    [Test]
    public void Schedule_Intersect_Should_keep_the_day_of_the_schedules()
    {
        // Arrange — recorte de quinta continua sendo de quinta.
        var schedule = Window(Day.Thursday, Hour.H10_00, Hour.H14_00);
        var shift = Window(Day.Thursday, Hour.H12_00, Hour.H18_00);

        // Act
        var result = schedule.Intersect(shift);

        // Assert
        result.Should().NotBeNull();
        result!.Day.Should().Be(Day.Thursday);
    }

    #endregion

    #region Nível 4 — contenção

    [Test]
    public void Schedule_Intersect_Should_return_itself_when_it_is_inside_the_other()
    {
        // Arrange — aula 08h–10h dentro da manhã 07h–12h: nada é recortado.
        var schedule = Window(Day.Monday, Hour.H08_00, Hour.H10_00);
        var shift = Window(Day.Monday, Hour.H07_00, Hour.H12_00);

        // Act
        var result = schedule.Intersect(shift);

        // Assert
        result.Should().NotBeNull();
        (result!.Start, result.End).Should().Be((Hour.H08_00, Hour.H10_00));
        result.GetDiffInMinutes().Should().Be(120);
    }

    [Test]
    public void Schedule_Intersect_Should_return_the_other_when_the_other_is_inside_it()
    {
        // Arrange — o mesmo par, na ordem inversa: o menor manda.
        var schedule = Window(Day.Monday, Hour.H08_00, Hour.H10_00);
        var shift = Window(Day.Monday, Hour.H07_00, Hour.H12_00);

        // Act
        var result = shift.Intersect(schedule);

        // Assert
        result.Should().NotBeNull();
        (result!.Start, result.End).Should().Be((Hour.H08_00, Hour.H10_00));
    }

    [Test]
    public void Schedule_Intersect_Should_return_the_same_interval_when_both_are_identical()
    {
        // Arrange — aula que ocupa o turno inteiro.
        var schedule = Window(Day.Monday, Hour.H07_00, Hour.H12_00);
        var shift = Window(Day.Monday, Hour.H07_00, Hour.H12_00);

        // Act
        var result = schedule.Intersect(shift);

        // Assert
        result.Should().NotBeNull();
        (result!.Start, result.End).Should().Be((Hour.H07_00, Hour.H12_00));
        result.GetDiffInMinutes().Should().Be(300);
    }

    [Test]
    public void Schedule_Intersect_Should_return_the_same_interval_when_intersected_with_itself()
    {
        // Arrange
        var schedule = Window(Day.Monday, Hour.H09_00, Hour.H11_30);

        // Act
        var result = schedule.Intersect(schedule);

        // Assert
        result.Should().NotBeNull();
        (result!.Start, result.End).Should().Be((Hour.H09_00, Hour.H11_30));
    }

    [Test]
    public void Schedule_Intersect_Should_return_the_same_interval_when_they_share_the_start()
    {
        // Arrange — aula que começa junto com o turno e termina antes.
        var schedule = Window(Day.Monday, Hour.H07_00, Hour.H09_00);
        var shift = Window(Day.Monday, Hour.H07_00, Hour.H12_00);

        // Act
        var result = schedule.Intersect(shift);

        // Assert
        result.Should().NotBeNull();
        (result!.Start, result.End).Should().Be((Hour.H07_00, Hour.H09_00));
    }

    [Test]
    public void Schedule_Intersect_Should_return_the_same_interval_when_they_share_the_end()
    {
        // Arrange — aula que termina junto com o turno.
        var schedule = Window(Day.Monday, Hour.H10_00, Hour.H12_00);
        var shift = Window(Day.Monday, Hour.H07_00, Hour.H12_00);

        // Act
        var result = schedule.Intersect(shift);

        // Assert
        result.Should().NotBeNull();
        (result!.Start, result.End).Should().Be((Hour.H10_00, Hour.H12_00));
    }

    #endregion

    #region Nível 5 — o recorte nunca inventa minuto

    [Test]
    public void Schedule_Intersect_Should_never_be_longer_than_the_shortest_of_the_two()
    {
        // Arrange — 07h–22h (900min) contra 09h–10h (60min).
        var schedule = Window(Day.Monday, Hour.H07_00, Hour.H22_00);
        var other = Window(Day.Monday, Hour.H09_00, Hour.H10_00);

        // Act
        var result = schedule.Intersect(other);

        // Assert
        result.Should().NotBeNull();
        result!.GetDiffInMinutes().Should().BeLessThanOrEqualTo(other.GetDiffInMinutes());
        result.GetDiffInMinutes().Should().Be(60);
    }

    [Test]
    public void Schedule_Intersect_Should_produce_the_same_interval_in_both_directions()
    {
        // Arrange — a interseção é simétrica no intervalo (só os metadados é que não são).
        var schedule = Window(Day.Monday, Hour.H10_00, Hour.H14_00);
        var shift = Window(Day.Monday, Hour.H12_00, Hour.H18_00);

        // Act
        var direct = schedule.Intersect(shift);
        var inverse = shift.Intersect(schedule);

        // Assert
        direct.Should().NotBeNull();
        inverse.Should().NotBeNull();
        (direct!.Start, direct.End).Should().Be((inverse!.Start, inverse.End));
    }

    [Test]
    public void Schedule_Intersect_Should_not_change_the_schedules_it_received()
    {
        // Arrange — o recorte é um objeto novo; os dois originais ficam intactos.
        var schedule = Window(Day.Monday, Hour.H10_00, Hour.H14_00);
        var shift = Window(Day.Monday, Hour.H12_00, Hour.H18_00);

        // Act
        schedule.Intersect(shift);

        // Assert
        (schedule.Start, schedule.End).Should().Be((Hour.H10_00, Hour.H14_00));
        (shift.Start, shift.End).Should().Be((Hour.H12_00, Hour.H18_00));
    }

    [Test]
    public void Schedule_Intersect_Should_split_a_schedule_that_crosses_the_shift_boundary_without_double_counting()
    {
        // Arrange — a aula de 10h–14h contra os três turnos do dia. É o caso que o
        // cálculo de ocupação errava: contava 240min na manhã e 240min à tarde.
        var schedule = Window(Day.Monday, Hour.H10_00, Hour.H14_00);
        var morning = Window(Day.Monday, Hour.H07_00, Hour.H12_00);
        var afternoon = Window(Day.Monday, Hour.H12_00, Hour.H18_00);
        var evening = Window(Day.Monday, Hour.H18_00, Hour.H23_00);

        // Act
        var inMorning = schedule.Intersect(morning);
        var inAfternoon = schedule.Intersect(afternoon);
        var inEvening = schedule.Intersect(evening);

        // Assert — a soma dos pedaços é exatamente a aula, nem um minuto a mais.
        var total = (inMorning?.GetDiffInMinutes() ?? 0)
            + (inAfternoon?.GetDiffInMinutes() ?? 0)
            + (inEvening?.GetDiffInMinutes() ?? 0);

        (inMorning!.GetDiffInMinutes(), inAfternoon!.GetDiffInMinutes()).Should().Be((120, 120));
        inEvening.Should().BeNull();
        total.Should().Be(schedule.GetDiffInMinutes());
    }

    [Test]
    public void Schedule_Intersect_Should_drop_the_part_that_falls_outside_the_open_window()
    {
        // Arrange — aula 21h–23h num campus que fecha às 22h: só a primeira hora existe.
        var schedule = Window(Day.Monday, Hour.H21_00, Hour.H23_00);
        var open = Window(Day.Monday, Hour.H18_00, Hour.H22_00);

        // Act
        var result = schedule.Intersect(open);

        // Assert
        result.Should().NotBeNull();
        (result!.Start, result.End).Should().Be((Hour.H21_00, Hour.H22_00));
        result.GetDiffInMinutes().Should().Be(60);
    }

    #endregion

    #region Nível 6 — metadados do recorte

    [Test]
    public void Schedule_Intersect_Should_keep_the_class_of_the_schedule_being_cut()
    {
        // Arrange — quem é recortado carrega a alocação; quem recorta é só intervalo.
        var schedule = ClassSchedule(Day.Monday, Hour.H10_00, Hour.H14_00, classId: 7);
        var shift = Window(Day.Monday, Hour.H12_00, Hour.H18_00);

        // Act
        var result = schedule.Intersect(shift);

        // Assert
        result.Should().NotBeNull();
        result!.ClassId.Should().Be(7);
    }

    [Test]
    public void Schedule_Intersect_Should_keep_the_classroom_of_the_schedule_being_cut()
    {
        // Arrange
        var schedule = ClassSchedule(Day.Monday, Hour.H10_00, Hour.H14_00, classroomId: 42);
        var shift = Window(Day.Monday, Hour.H12_00, Hour.H18_00);

        // Act
        var result = schedule.Intersect(shift);

        // Assert
        result.Should().NotBeNull();
        result!.ClassroomId.Should().Be(42);
    }

    [Test]
    public void Schedule_Intersect_Should_keep_the_teacher_of_the_schedule_being_cut()
    {
        // Arrange — horário preferencial de professor, sem turma nem sala.
        var schedule = ClassSchedule(Day.Monday, Hour.H10_00, Hour.H14_00, teacherId: 3);
        var shift = Window(Day.Monday, Hour.H12_00, Hour.H18_00);

        // Act
        var result = schedule.Intersect(shift);

        // Assert
        result.Should().NotBeNull();
        result!.TeacherId.Should().Be(3);
        result.ClassId.Should().BeNull();
        result.ClassroomId.Should().BeNull();
    }

    [Test]
    public void Schedule_Intersect_Should_not_take_the_metadata_of_the_schedule_that_cuts()
    {
        // Arrange — a operação não é simétrica nos metadados: só o receptor os empresta.
        var window = Window(Day.Monday, Hour.H12_00, Hour.H18_00);
        var allocated = ClassSchedule(Day.Monday, Hour.H10_00, Hour.H14_00,
            classId: 7, teacherId: 3, classroomId: 42);

        // Act
        var result = window.Intersect(allocated);

        // Assert
        result.Should().NotBeNull();
        result!.ClassId.Should().BeNull();
        result.TeacherId.Should().BeNull();
        result.ClassroomId.Should().BeNull();
    }

    [Test]
    public void Schedule_Intersect_Should_not_be_born_with_the_id_of_the_schedule_it_cut()
    {
        // Arrange — o recorte é intervalo em memória, não a linha do banco recortada.
        var schedule = ClassSchedule(Day.Monday, Hour.H10_00, Hour.H14_00, classId: 7);
        schedule.Id = 99;
        var shift = Window(Day.Monday, Hour.H12_00, Hour.H18_00);

        // Act
        var result = schedule.Intersect(shift);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(0);
    }

    #endregion
}
