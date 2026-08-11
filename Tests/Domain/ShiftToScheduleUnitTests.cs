namespace Estud.Tests.Domain;

public class ShiftToScheduleUnitTests
{
    #region Nível 1 — o intervalo de cada turno

    [Test]
    public void ShiftExtensions_ToSchedule_Should_turn_the_morning_into_six_to_noon()
    {
        // Act
        var schedule = Shift.Morning.ToSchedule(Day.Monday);

        // Assert
        (schedule.Start, schedule.End).Should().Be((Hour.H06_00, Hour.H12_00));
        schedule.GetDiffInMinutes().Should().Be(360);
    }

    [Test]
    public void ShiftExtensions_ToSchedule_Should_turn_the_afternoon_into_noon_to_six()
    {
        // Act
        var schedule = Shift.Afternoon.ToSchedule(Day.Monday);

        // Assert
        (schedule.Start, schedule.End).Should().Be((Hour.H12_00, Hour.H18_00));
        schedule.GetDiffInMinutes().Should().Be(360);
    }

    [Test]
    public void ShiftExtensions_ToSchedule_Should_turn_the_evening_into_six_to_eleven()
    {
        // Act
        var schedule = Shift.Evening.ToSchedule(Day.Monday);

        // Assert
        (schedule.Start, schedule.End).Should().Be((Hour.H18_00, Hour.H24_00));
        schedule.GetDiffInMinutes().Should().Be(360);
    }

    [Test]
    public void ShiftExtensions_ToSchedule_Should_use_the_same_hours_of_the_shift()
    {
        // Act & Assert — o horário do turno é só outra forma de ler StartAtHour/EndAtHour.
        foreach (var shift in Enum.GetValues<Shift>())
        {
            var schedule = shift.ToSchedule(Day.Monday);
            (schedule.Start, schedule.End).Should().Be((shift.StartAtHour, shift.EndAtHour));
        }
    }

    #endregion

    #region Nível 2 — o dia pedido

    [Test]
    public void ShiftExtensions_ToSchedule_Should_keep_the_day_it_was_asked_for()
    {
        // Act
        var schedule = Shift.Morning.ToSchedule(Day.Thursday);

        // Assert
        schedule.Day.Should().Be(Day.Thursday);
    }

    [Test]
    public void ShiftExtensions_ToSchedule_Should_produce_the_same_interval_on_every_day()
    {
        // Act & Assert — o turno não muda de tamanho conforme o dia da semana.
        foreach (var day in Enum.GetValues<Day>())
        {
            var schedule = Shift.Morning.ToSchedule(day);
            schedule.Day.Should().Be(day);
            schedule.GetDiffInMinutes().Should().Be(360);
        }
    }

    #endregion

    #region Nível 3 — os turnos entre si

    [Test]
    public void ShiftExtensions_ToSchedule_Should_produce_shifts_that_do_not_overlap()
    {
        // Arrange — os turnos se encostam pela ponta, e encostar não é sobrepor.
        var morning = Shift.Morning.ToSchedule(Day.Monday);
        var afternoon = Shift.Afternoon.ToSchedule(Day.Monday);
        var evening = Shift.Evening.ToSchedule(Day.Monday);

        // Assert — é o que impede a aula que cruza a fronteira de ser contada duas vezes.
        morning.Intersect(afternoon).Should().BeNull();
        afternoon.Intersect(evening).Should().BeNull();
        morning.Intersect(evening).Should().BeNull();
    }

    [Test]
    public void ShiftExtensions_ToSchedule_Should_produce_shifts_that_start_where_the_previous_one_ends()
    {
        // Arrange
        var morning = Shift.Morning.ToSchedule(Day.Monday);
        var afternoon = Shift.Afternoon.ToSchedule(Day.Monday);
        var evening = Shift.Evening.ToSchedule(Day.Monday);

        // Assert
        morning.End.Should().Be(afternoon.Start);
        afternoon.End.Should().Be(evening.Start);
    }

    [Test]
    public void ShiftExtensions_ToSchedule_Should_cover_six_to_twelve_with_the_three_shifts()
    {
        // Act
        var total = Enum.GetValues<Shift>().Sum(s => s.ToSchedule(Day.Monday).GetDiffInMinutes());

        // Assert
        total.Should().Be(1080);
    }

    #endregion

    #region Nível 4 — janela sem dono

    [Test]
    public void ShiftExtensions_ToSchedule_Should_return_a_window_without_class_teacher_or_classroom()
    {
        // Act — o turno é intervalo, não alocação.
        var schedule = Shift.Morning.ToSchedule(Day.Monday);

        // Assert
        schedule.Id.Should().Be(0);
        schedule.ClassId.Should().BeNull();
        schedule.TeacherId.Should().BeNull();
        schedule.ClassroomId.Should().BeNull();
    }

    #endregion
}
