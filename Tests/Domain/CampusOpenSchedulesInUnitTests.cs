using Estud.Back.Domain.Campi;

namespace Estud.Tests.Domain;

public class CampusOpenSchedulesInUnitTests
{
    private const int InstitutionId = 1;

    // Janelas dos turnos: manhã 07h–12h (300min), tarde 12h–18h (360min), noite 18h–23h (300min).

    private static Campus NewCampus(params OpeningHour[] hours) =>
        new(InstitutionId, "Campus Central", BrazilState.SP, "São Paulo")
        {
            OpeningHours = [.. hours],
        };

    private static Campus DefaultCampus() =>
        new(InstitutionId, "Campus Central", BrazilState.SP, "São Paulo");

    private static OpeningHour Window(Day day, Hour start, Hour end) => new(day, start, end);

    #region Nível 1 — campus fechado

    [Test]
    public void Campus_OpenSchedulesIn_Should_return_nothing_when_the_campus_has_no_window_at_all()
    {
        // Arrange — ausência de janela significa fechado.
        var campus = NewCampus();

        // Act
        var schedules = campus.OpenSchedulesIn(Day.Monday, Shift.Morning);

        // Assert
        schedules.Should().BeEmpty();
    }

    [Test]
    public void Campus_OpenSchedulesIn_Should_return_nothing_when_the_day_asked_has_no_window()
    {
        // Arrange — abre só na segunda, e a pergunta é sobre a terça.
        var campus = NewCampus(Window(Day.Monday, Hour.H07_00, Hour.H12_00));

        // Act
        var schedules = campus.OpenSchedulesIn(Day.Tuesday, Shift.Morning);

        // Assert
        schedules.Should().BeEmpty();
    }

    [Test]
    public void Campus_OpenSchedulesIn_Should_return_nothing_on_saturday_with_the_default_opening_hours()
    {
        // Arrange — o padrão vai de segunda a sexta.
        var campus = DefaultCampus();

        // Act
        var morning = campus.OpenSchedulesIn(Day.Saturday, Shift.Morning);
        var afternoon = campus.OpenSchedulesIn(Day.Saturday, Shift.Afternoon);
        var evening = campus.OpenSchedulesIn(Day.Saturday, Shift.Evening);

        // Assert
        morning.Should().BeEmpty();
        afternoon.Should().BeEmpty();
        evening.Should().BeEmpty();
    }

    [Test]
    public void Campus_OpenSchedulesIn_Should_return_nothing_when_the_window_ends_exactly_when_the_shift_starts()
    {
        // Arrange — janela 07h–12h e turno da tarde 12h–18h só se encostam.
        var campus = NewCampus(Window(Day.Monday, Hour.H07_00, Hour.H12_00));

        // Act
        var schedules = campus.OpenSchedulesIn(Day.Monday, Shift.Afternoon);

        // Assert
        schedules.Should().BeEmpty();
    }

    [Test]
    public void Campus_OpenSchedulesIn_Should_return_nothing_when_the_window_starts_exactly_when_the_shift_ends()
    {
        // Arrange — janela 12h–18h e turno da manhã 07h–12h só se encostam.
        var campus = NewCampus(Window(Day.Monday, Hour.H12_00, Hour.H18_00));

        // Act
        var schedules = campus.OpenSchedulesIn(Day.Monday, Shift.Morning);

        // Assert
        schedules.Should().BeEmpty();
    }

    [Test]
    public void Campus_OpenSchedulesIn_Should_return_nothing_in_the_morning_when_the_window_is_only_at_night()
    {
        // Arrange
        var campus = NewCampus(Window(Day.Monday, Hour.H19_00, Hour.H22_00));

        // Act
        var schedules = campus.OpenSchedulesIn(Day.Monday, Shift.Morning);

        // Assert
        schedules.Should().BeEmpty();
    }

    #endregion

    #region Nível 2 — uma janela

    [Test]
    public void Campus_OpenSchedulesIn_Should_return_the_whole_shift_when_the_window_covers_it()
    {
        // Arrange — 07h–22h cobre a manhã inteira.
        var campus = NewCampus(Window(Day.Monday, Hour.H07_00, Hour.H22_00));

        // Act
        var schedules = campus.OpenSchedulesIn(Day.Monday, Shift.Morning);

        // Assert — o recorte é o turno, não a janela.
        schedules.Should().ContainSingle();
        (schedules[0].Start, schedules[0].End).Should().Be((Hour.H07_00, Hour.H12_00));
    }

    [Test]
    public void Campus_OpenSchedulesIn_Should_return_the_window_when_it_is_inside_the_shift()
    {
        // Arrange — 08h–10h dentro da manhã: o recorte é a janela, não o turno.
        var campus = NewCampus(Window(Day.Monday, Hour.H08_00, Hour.H10_00));

        // Act
        var schedules = campus.OpenSchedulesIn(Day.Monday, Shift.Morning);

        // Assert
        schedules.Should().ContainSingle();
        (schedules[0].Start, schedules[0].End).Should().Be((Hour.H08_00, Hour.H10_00));
    }

    [Test]
    public void Campus_OpenSchedulesIn_Should_cut_the_window_at_the_end_of_the_shift()
    {
        // Arrange — 10h–16h atravessa o meio-dia; na manhã só vale até as 12h.
        var campus = NewCampus(Window(Day.Monday, Hour.H10_00, Hour.H16_00));

        // Act
        var schedules = campus.OpenSchedulesIn(Day.Monday, Shift.Morning);

        // Assert
        schedules.Should().ContainSingle();
        (schedules[0].Start, schedules[0].End).Should().Be((Hour.H10_00, Hour.H12_00));
    }

    [Test]
    public void Campus_OpenSchedulesIn_Should_cut_the_window_at_the_start_of_the_shift()
    {
        // Arrange — a mesma janela 10h–16h, agora vista pela tarde.
        var campus = NewCampus(Window(Day.Monday, Hour.H10_00, Hour.H16_00));

        // Act
        var schedules = campus.OpenSchedulesIn(Day.Monday, Shift.Afternoon);

        // Assert
        schedules.Should().ContainSingle();
        (schedules[0].Start, schedules[0].End).Should().Be((Hour.H12_00, Hour.H16_00));
    }

    [Test]
    public void Campus_OpenSchedulesIn_Should_cut_the_window_that_closes_before_the_end_of_the_evening()
    {
        // Arrange — a noite vai até as 23h, mas o campus fecha às 22h.
        var campus = NewCampus(Window(Day.Monday, Hour.H07_00, Hour.H22_00));

        // Act
        var schedules = campus.OpenSchedulesIn(Day.Monday, Shift.Evening);

        // Assert
        schedules.Should().ContainSingle();
        (schedules[0].Start, schedules[0].End).Should().Be((Hour.H18_00, Hour.H22_00));
    }

    [Test]
    public void Campus_OpenSchedulesIn_Should_keep_the_day_that_was_asked()
    {
        // Arrange
        var campus = NewCampus(Window(Day.Wednesday, Hour.H07_00, Hour.H12_00));

        // Act
        var schedules = campus.OpenSchedulesIn(Day.Wednesday, Shift.Morning);

        // Assert
        schedules.Should().ContainSingle();
        schedules[0].Day.Should().Be(Day.Wednesday);
    }

    [Test]
    public void Campus_OpenSchedulesIn_Should_return_windows_without_class_teacher_or_classroom()
    {
        // Arrange — o que volta é intervalo de funcionamento, não alocação.
        var campus = NewCampus(Window(Day.Monday, Hour.H07_00, Hour.H12_00));

        // Act
        var schedules = campus.OpenSchedulesIn(Day.Monday, Shift.Morning);

        // Assert
        schedules.Should().ContainSingle();
        schedules[0].ClassId.Should().BeNull();
        schedules[0].TeacherId.Should().BeNull();
        schedules[0].ClassroomId.Should().BeNull();
        schedules[0].Id.Should().Be(0);
    }

    [Test]
    public void Campus_OpenSchedulesIn_Should_return_a_window_of_fifteen_minutes()
    {
        // Arrange — o menor intervalo que a grade permite.
        var campus = NewCampus(Window(Day.Monday, Hour.H07_00, Hour.H07_15));

        // Act
        var schedules = campus.OpenSchedulesIn(Day.Monday, Shift.Morning);

        // Assert
        schedules.Should().ContainSingle();
        schedules[0].GetDiffInMinutes().Should().Be(15);
    }

    #endregion

    #region Nível 3 — várias janelas no mesmo dia

    [Test]
    public void Campus_OpenSchedulesIn_Should_return_the_two_windows_that_fall_in_the_same_shift()
    {
        // Arrange — 07h–09h e 10h–11h, as duas de manhã.
        var campus = NewCampus(
            Window(Day.Monday, Hour.H07_00, Hour.H09_00),
            Window(Day.Monday, Hour.H10_00, Hour.H11_00));

        // Act
        var schedules = campus.OpenSchedulesIn(Day.Monday, Shift.Morning);

        // Assert
        schedules.Should().HaveCount(2);
        (schedules[0].Start, schedules[0].End).Should().Be((Hour.H07_00, Hour.H09_00));
        (schedules[1].Start, schedules[1].End).Should().Be((Hour.H10_00, Hour.H11_00));
    }

    [Test]
    public void Campus_OpenSchedulesIn_Should_return_three_windows_of_the_same_shift()
    {
        // Arrange
        var campus = NewCampus(
            Window(Day.Monday, Hour.H07_00, Hour.H08_00),
            Window(Day.Monday, Hour.H09_00, Hour.H10_00),
            Window(Day.Monday, Hour.H11_00, Hour.H12_00));

        // Act
        var schedules = campus.OpenSchedulesIn(Day.Monday, Shift.Morning);

        // Assert
        schedules.Should().HaveCount(3);
        schedules.Sum(s => s.GetDiffInMinutes()).Should().Be(180);
    }

    [Test]
    public void Campus_OpenSchedulesIn_Should_split_the_lunch_break_between_the_two_shifts()
    {
        // Arrange — 07h–12h e 13h–18h: o campus fecha para o almoço.
        var campus = NewCampus(
            Window(Day.Monday, Hour.H07_00, Hour.H12_00),
            Window(Day.Monday, Hour.H13_00, Hour.H18_00));

        // Act
        var morning = campus.OpenSchedulesIn(Day.Monday, Shift.Morning);
        var afternoon = campus.OpenSchedulesIn(Day.Monday, Shift.Afternoon);

        // Assert — a hora de almoço não aparece em turno nenhum.
        morning.Should().ContainSingle();
        (morning[0].Start, morning[0].End).Should().Be((Hour.H07_00, Hour.H12_00));
        afternoon.Should().ContainSingle();
        (afternoon[0].Start, afternoon[0].End).Should().Be((Hour.H13_00, Hour.H18_00));
    }

    [Test]
    public void Campus_OpenSchedulesIn_Should_return_only_the_windows_that_touch_the_shift()
    {
        // Arrange — três janelas no dia, só uma cai na tarde.
        var campus = NewCampus(
            Window(Day.Monday, Hour.H07_00, Hour.H09_00),
            Window(Day.Monday, Hour.H14_00, Hour.H16_00),
            Window(Day.Monday, Hour.H19_00, Hour.H22_00));

        // Act
        var schedules = campus.OpenSchedulesIn(Day.Monday, Shift.Afternoon);

        // Assert
        schedules.Should().ContainSingle();
        (schedules[0].Start, schedules[0].End).Should().Be((Hour.H14_00, Hour.H16_00));
    }

    [Test]
    public void Campus_OpenSchedulesIn_Should_not_return_the_windows_of_the_other_days()
    {
        // Arrange — a mesma janela em três dias.
        var campus = NewCampus(
            Window(Day.Monday, Hour.H07_00, Hour.H12_00),
            Window(Day.Tuesday, Hour.H07_00, Hour.H12_00),
            Window(Day.Wednesday, Hour.H07_00, Hour.H12_00));

        // Act
        var schedules = campus.OpenSchedulesIn(Day.Tuesday, Shift.Morning);

        // Assert
        schedules.Should().ContainSingle();
        schedules[0].Day.Should().Be(Day.Tuesday);
    }

    [Test]
    public void Campus_OpenSchedulesIn_Should_split_a_single_window_across_the_three_shifts()
    {
        // Arrange — 07h–22h aparece recortada em cada turno.
        var campus = NewCampus(Window(Day.Monday, Hour.H07_00, Hour.H22_00));

        // Act
        var morning = campus.OpenSchedulesIn(Day.Monday, Shift.Morning);
        var afternoon = campus.OpenSchedulesIn(Day.Monday, Shift.Afternoon);
        var evening = campus.OpenSchedulesIn(Day.Monday, Shift.Evening);

        // Assert — os três recortes somam a janela inteira, sem sobrepor.
        (morning[0].Start, morning[0].End).Should().Be((Hour.H07_00, Hour.H12_00));
        (afternoon[0].Start, afternoon[0].End).Should().Be((Hour.H12_00, Hour.H18_00));
        (evening[0].Start, evening[0].End).Should().Be((Hour.H18_00, Hour.H22_00));

        var total = morning.Concat(afternoon).Concat(evening).Sum(s => s.GetDiffInMinutes());
        total.Should().Be(900);
    }

    [Test]
    public void Campus_OpenSchedulesIn_Should_keep_the_order_of_the_windows_it_was_given()
    {
        // Arrange — as janelas entram fora de ordem e saem como entraram: o cálculo de
        // ocupação só soma, então a ordem não importa para o resultado.
        var campus = NewCampus(
            Window(Day.Monday, Hour.H10_00, Hour.H11_00),
            Window(Day.Monday, Hour.H07_00, Hour.H09_00));

        // Act
        var schedules = campus.OpenSchedulesIn(Day.Monday, Shift.Morning);

        // Assert
        schedules.Should().HaveCount(2);
        (schedules[0].Start, schedules[0].End).Should().Be((Hour.H10_00, Hour.H11_00));
        (schedules[1].Start, schedules[1].End).Should().Be((Hour.H07_00, Hour.H09_00));
    }

    #endregion

    #region Nível 4 — acordo com o MinutesOpenIn

    [Test]
    public void Campus_OpenSchedulesIn_Should_sum_the_same_minutes_that_MinutesOpenIn_reports()
    {
        // Arrange — os dois lados do cálculo de ocupação saem daqui: o denominador é a
        // soma, o numerador é o cruzamento com estes horários. Se divergirem, a taxa mente.
        var campus = NewCampus(
            Window(Day.Monday, Hour.H07_00, Hour.H12_00),
            Window(Day.Monday, Hour.H13_00, Hour.H22_00));

        // Act & Assert
        foreach (var shift in Enum.GetValues<Shift>())
        {
            var minutes = campus.OpenSchedulesIn(Day.Monday, shift).Sum(s => s.GetDiffInMinutes());
            minutes.Should().Be(campus.MinutesOpenIn(Day.Monday, shift));
        }
    }

    [Test]
    public void Campus_OpenSchedulesIn_Should_agree_with_MinutesOpenIn_on_every_day_and_shift()
    {
        // Arrange — semana com grade diferente por dia.
        var campus = NewCampus(
            Window(Day.Monday, Hour.H07_00, Hour.H12_00),
            Window(Day.Monday, Hour.H13_00, Hour.H22_00),
            Window(Day.Tuesday, Hour.H18_00, Hour.H22_30),
            Window(Day.Friday, Hour.H10_00, Hour.H16_00),
            Window(Day.Saturday, Hour.H08_00, Hour.H12_00));

        // Act & Assert
        foreach (var day in Enum.GetValues<Day>())
        {
            foreach (var shift in Enum.GetValues<Shift>())
            {
                var minutes = campus.OpenSchedulesIn(day, shift).Sum(s => s.GetDiffInMinutes());
                minutes.Should().Be(campus.MinutesOpenIn(day, shift));
            }
        }
    }

    [Test]
    public void Campus_OpenSchedulesIn_Should_report_the_default_opening_hours_of_a_weekday()
    {
        // Arrange — campus novo nasce seg–sex, 07h–22h.
        var campus = DefaultCampus();

        // Act
        var morning = campus.OpenSchedulesIn(Day.Monday, Shift.Morning);
        var afternoon = campus.OpenSchedulesIn(Day.Monday, Shift.Afternoon);
        var evening = campus.OpenSchedulesIn(Day.Monday, Shift.Evening);

        // Assert
        morning.Sum(s => s.GetDiffInMinutes()).Should().Be(300);
        afternoon.Sum(s => s.GetDiffInMinutes()).Should().Be(300);
        evening.Sum(s => s.GetDiffInMinutes()).Should().Be(180);
    }

    #endregion
}
