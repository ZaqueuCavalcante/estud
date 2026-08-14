using Estud.Back.Domain.Campi;

namespace Estud.Tests.Domain;

public class OpeningHourToScheduleUnitTests
{
    #region Nível 1 — a janela vira horário

    [Test]
    public void OpeningHour_ToSchedule_Should_keep_the_day_and_the_hours_of_the_window()
    {
        // Arrange
        var window = new OpeningHour(Day.Monday, Hour.H07_00, Hour.H22_00);

        // Act
        var schedule = window.ToSchedule();

        // Assert
        schedule.Day.Should().Be(Day.Monday);
        (schedule.Start, schedule.End).Should().Be((Hour.H07_00, Hour.H22_00));
    }

    [Test]
    public void OpeningHour_ToSchedule_Should_keep_the_length_of_the_window()
    {
        // Arrange — 07h–22h são 15h de funcionamento.
        var window = new OpeningHour(Day.Monday, Hour.H07_00, Hour.H22_00);

        // Act
        var schedule = window.ToSchedule();

        // Assert
        schedule.GetDiffInMinutes().Should().Be(900);
    }

    [Test]
    public void OpeningHour_ToSchedule_Should_keep_a_window_of_fifteen_minutes()
    {
        // Arrange — o menor intervalo que a grade permite.
        var window = new OpeningHour(Day.Monday, Hour.H07_00, Hour.H07_15);

        // Act
        var schedule = window.ToSchedule();

        // Assert
        schedule.GetDiffInMinutes().Should().Be(15);
    }

    [Test]
    public void OpeningHour_ToSchedule_Should_keep_hours_that_are_not_aligned_to_the_hour()
    {
        // Arrange
        var window = new OpeningHour(Day.Tuesday, Hour.H07_30, Hour.H22_45);

        // Act
        var schedule = window.ToSchedule();

        // Assert
        (schedule.Start, schedule.End).Should().Be((Hour.H07_30, Hour.H22_45));
    }

    [Test]
    public void OpeningHour_ToSchedule_Should_convert_a_window_of_every_day_of_the_week()
    {
        // Act & Assert
        foreach (var day in Enum.GetValues<Day>())
        {
            var schedule = new OpeningHour(day, Hour.H07_00, Hour.H12_00).ToSchedule();
            schedule.Day.Should().Be(day);
        }
    }

    [Test]
    public void OpeningHour_ToSchedule_Should_return_a_window_without_class_teacher_or_classroom()
    {
        // Arrange — funcionamento do campus não é alocação de sala.
        var window = new OpeningHour(Day.Monday, Hour.H07_00, Hour.H22_00);

        // Act
        var schedule = window.ToSchedule();

        // Assert
        schedule.Id.Should().Be(0);
        schedule.ClassId.Should().BeNull();
        schedule.TeacherId.Should().BeNull();
        schedule.ClassroomId.Should().BeNull();
    }

    [Test]
    public void OpeningHour_ToSchedule_Should_convert_the_default_opening_hours()
    {
        // Arrange — o padrão do campus novo: seg–sex, 07h–22h.
        var windows = Campus.DefaultOpeningHours();

        // Act
        var schedules = windows.Select(w => w.ToSchedule()).ToList();

        // Assert
        schedules.Should().HaveCount(15);
        schedules.Select(s => s.Day).Should().NotContain(Day.Saturday);
    }

    #endregion

    #region Nível 2 — janelas convertidas se cruzam como horários

    [Test]
    public void OpeningHour_ToSchedule_Should_not_intersect_two_windows_of_different_days()
    {
        // Arrange
        var monday = new OpeningHour(Day.Monday, Hour.H07_00, Hour.H12_00).ToSchedule();
        var tuesday = new OpeningHour(Day.Tuesday, Hour.H07_00, Hour.H12_00).ToSchedule();

        // Act
        var result = monday.Intersect(tuesday);

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public void OpeningHour_ToSchedule_Should_not_intersect_the_two_windows_around_the_lunch_break()
    {
        // Arrange — 07h–12h e 13h–18h: a hora fechada fica de fora das duas.
        var before = new OpeningHour(Day.Monday, Hour.H07_00, Hour.H12_00).ToSchedule();
        var after = new OpeningHour(Day.Monday, Hour.H13_00, Hour.H18_00).ToSchedule();

        // Act
        var result = before.Intersect(after);

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public void OpeningHour_ToSchedule_Should_not_intersect_two_adjacent_windows()
    {
        // Arrange — 07h–12h e 12h–18h se encostam sem sobrepor.
        var before = new OpeningHour(Day.Monday, Hour.H07_00, Hour.H12_00).ToSchedule();
        var after = new OpeningHour(Day.Monday, Hour.H12_00, Hour.H18_00).ToSchedule();

        // Act
        var result = before.Intersect(after);

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public void OpeningHour_ToSchedule_Should_intersect_two_windows_that_overlap()
    {
        // Arrange — janelas sobrepostas não deveriam existir (invariante garantida no
        // UpdateCampusOpeningHours), mas se existirem o cruzamento as enxerga.
        var first = new OpeningHour(Day.Monday, Hour.H07_00, Hour.H10_00).ToSchedule();
        var second = new OpeningHour(Day.Monday, Hour.H09_00, Hour.H12_00).ToSchedule();

        // Act
        var result = first.Intersect(second);

        // Assert
        result.Should().NotBeNull();
        (result!.Start, result.End).Should().Be((Hour.H09_00, Hour.H10_00));
    }

    [Test]
    public void OpeningHour_ToSchedule_Should_agree_with_the_overlaps_check_of_the_window()
    {
        // Arrange — Overlaps e Intersect respondem a mesma pergunta em formatos diferentes.
        var pairs = new[]
        {
            (new OpeningHour(Day.Monday, Hour.H07_00, Hour.H12_00), new OpeningHour(Day.Monday, Hour.H09_00, Hour.H14_00)),
            (new OpeningHour(Day.Monday, Hour.H07_00, Hour.H12_00), new OpeningHour(Day.Monday, Hour.H12_00, Hour.H18_00)),
            (new OpeningHour(Day.Monday, Hour.H07_00, Hour.H12_00), new OpeningHour(Day.Tuesday, Hour.H07_00, Hour.H12_00)),
            (new OpeningHour(Day.Monday, Hour.H07_00, Hour.H22_00), new OpeningHour(Day.Monday, Hour.H12_00, Hour.H13_00)),
        };

        // Act & Assert
        foreach (var (first, second) in pairs)
        {
            var overlaps = first.Overlaps(second);
            var intersects = first.ToSchedule().Intersect(second.ToSchedule()) != null;

            intersects.Should().Be(overlaps);
        }
    }

    #endregion
}
