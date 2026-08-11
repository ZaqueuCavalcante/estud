using Estud.Back.Domain.Campi;

namespace Estud.Tests.Domain;

public class CampusMinutesOpenInUnitTests
{
    private const int InstitutionId = 1;

    // Janelas dos turnos: manhã 07h–12h (300min), tarde 12h–18h (360min), noite 18h–24h (360min).
    private const int MorningMinutes = 300;
    private const int AfternoonMinutes = 360;
    private const int EveningMinutes = 360;

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
    public void Campus_MinutesOpenIn_Should_be_closed_when_the_campus_has_no_window_at_all()
    {
        // Arrange — ausência de janela significa fechado.
        var campus = NewCampus();

        // Act
        var minutes = campus.MinutesOpenIn(Day.Monday, Shift.Morning);

        // Assert
        minutes.Should().Be(0);
    }

    [Test]
    public void Campus_MinutesOpenIn_Should_be_closed_in_every_shift_when_the_campus_has_no_window_at_all()
    {
        // Arrange
        var campus = NewCampus();

        // Act
        var morning = campus.MinutesOpenIn(Day.Monday, Shift.Morning);
        var afternoon = campus.MinutesOpenIn(Day.Monday, Shift.Afternoon);
        var evening = campus.MinutesOpenIn(Day.Monday, Shift.Evening);

        // Assert
        (morning, afternoon, evening).Should().Be((0, 0, 0));
    }

    [Test]
    public void Campus_MinutesOpenIn_Should_be_closed_when_the_day_asked_has_no_window()
    {
        // Arrange — abre só na segunda, e a pergunta é sobre a terça.
        var campus = NewCampus(Window(Day.Monday, Hour.H07_00, Hour.H12_00));

        // Act
        var minutes = campus.MinutesOpenIn(Day.Tuesday, Shift.Morning);

        // Assert
        minutes.Should().Be(0);
    }

    [Test]
    public void Campus_MinutesOpenIn_Should_be_closed_on_saturday_with_the_default_opening_hours()
    {
        // Arrange — o padrão vai de segunda a sexta, então sábado fica fechado.
        var campus = DefaultCampus();

        // Act
        var morning = campus.MinutesOpenIn(Day.Saturday, Shift.Morning);
        var afternoon = campus.MinutesOpenIn(Day.Saturday, Shift.Afternoon);
        var evening = campus.MinutesOpenIn(Day.Saturday, Shift.Evening);

        // Assert
        (morning, afternoon, evening).Should().Be((0, 0, 0));
    }

    #endregion

    #region Nível 2 — uma janela cobrindo o turno inteiro

    [Test]
    public void Campus_MinutesOpenIn_Should_open_the_whole_morning_when_the_window_covers_it()
    {
        // Arrange
        var campus = NewCampus(Window(Day.Monday, Hour.H07_00, Hour.H22_00));

        // Act
        var minutes = campus.MinutesOpenIn(Day.Monday, Shift.Morning);

        // Assert
        minutes.Should().Be(MorningMinutes);
    }

    [Test]
    public void Campus_MinutesOpenIn_Should_open_the_whole_afternoon_when_the_window_covers_it()
    {
        // Arrange
        var campus = NewCampus(Window(Day.Monday, Hour.H07_00, Hour.H22_00));

        // Act
        var minutes = campus.MinutesOpenIn(Day.Monday, Shift.Afternoon);

        // Assert
        minutes.Should().Be(AfternoonMinutes);
    }

    [Test]
    public void Campus_MinutesOpenIn_Should_open_only_part_of_the_evening_when_the_window_ends_before_midnight()
    {
        // Arrange — a noite vai até 24h, mas o campus fecha às 22h: 18h–22h = 240min.
        var campus = NewCampus(Window(Day.Monday, Hour.H07_00, Hour.H22_00));

        // Act
        var minutes = campus.MinutesOpenIn(Day.Monday, Shift.Evening);

        // Assert
        minutes.Should().Be(240);
    }

    [Test]
    public void Campus_MinutesOpenIn_Should_open_the_whole_shift_when_the_window_is_exactly_the_shift()
    {
        // Arrange — janela idêntica ao turno da manhã.
        var campus = NewCampus(Window(Day.Monday, Hour.H07_00, Hour.H12_00));

        // Act
        var minutes = campus.MinutesOpenIn(Day.Monday, Shift.Morning);

        // Assert
        minutes.Should().Be(MorningMinutes);
    }

    [Test]
    public void Campus_MinutesOpenIn_Should_open_the_whole_afternoon_when_the_window_is_exactly_the_afternoon()
    {
        // Arrange
        var campus = NewCampus(Window(Day.Monday, Hour.H12_00, Hour.H18_00));

        // Act
        var minutes = campus.MinutesOpenIn(Day.Monday, Shift.Afternoon);

        // Assert
        minutes.Should().Be(AfternoonMinutes);
    }

    #endregion

    #region Nível 3 — janela menor que o turno

    [Test]
    public void Campus_MinutesOpenIn_Should_count_only_the_window_when_it_is_inside_the_shift()
    {
        // Arrange — 09h–11h, todo dentro da manhã.
        var campus = NewCampus(Window(Day.Monday, Hour.H09_00, Hour.H11_00));

        // Act
        var minutes = campus.MinutesOpenIn(Day.Monday, Shift.Morning);

        // Assert
        minutes.Should().Be(120);
    }

    [Test]
    public void Campus_MinutesOpenIn_Should_count_the_window_that_starts_together_with_the_shift()
    {
        // Arrange
        var campus = NewCampus(Window(Day.Monday, Hour.H07_00, Hour.H09_00));

        // Act
        var minutes = campus.MinutesOpenIn(Day.Monday, Shift.Morning);

        // Assert
        minutes.Should().Be(120);
    }

    [Test]
    public void Campus_MinutesOpenIn_Should_count_the_window_that_ends_together_with_the_shift()
    {
        // Arrange
        var campus = NewCampus(Window(Day.Monday, Hour.H08_00, Hour.H12_00));

        // Act
        var minutes = campus.MinutesOpenIn(Day.Monday, Shift.Morning);

        // Assert
        minutes.Should().Be(240);
    }

    [Test]
    public void Campus_MinutesOpenIn_Should_count_a_window_of_fifteen_minutes()
    {
        // Arrange — a menor janela possível, já que o enum Hour anda de 15 em 15min.
        var campus = NewCampus(Window(Day.Monday, Hour.H07_30, Hour.H07_45));

        // Act
        var minutes = campus.MinutesOpenIn(Day.Monday, Shift.Morning);

        // Assert
        minutes.Should().Be(15);
    }

    [Test]
    public void Campus_MinutesOpenIn_Should_count_a_window_that_is_not_aligned_to_the_hour()
    {
        // Arrange — 07:15–07:45.
        var campus = NewCampus(Window(Day.Monday, Hour.H07_15, Hour.H07_45));

        // Act
        var minutes = campus.MinutesOpenIn(Day.Monday, Shift.Morning);

        // Assert
        minutes.Should().Be(30);
    }

    #endregion

    #region Nível 4 — janela e turno sem interseção

    [Test]
    public void Campus_MinutesOpenIn_Should_be_closed_when_the_window_ends_exactly_when_the_shift_starts()
    {
        // Arrange — intervalos são fechados no início e abertos no fim: 07h–12h não
        // encosta na tarde.
        var campus = NewCampus(Window(Day.Monday, Hour.H07_00, Hour.H12_00));

        // Act
        var minutes = campus.MinutesOpenIn(Day.Monday, Shift.Afternoon);

        // Assert
        minutes.Should().Be(0);
    }

    [Test]
    public void Campus_MinutesOpenIn_Should_be_closed_when_the_window_starts_exactly_when_the_shift_ends()
    {
        // Arrange — 12h–18h não conta nada na manhã.
        var campus = NewCampus(Window(Day.Monday, Hour.H12_00, Hour.H18_00));

        // Act
        var minutes = campus.MinutesOpenIn(Day.Monday, Shift.Morning);

        // Assert
        minutes.Should().Be(0);
    }

    [Test]
    public void Campus_MinutesOpenIn_Should_be_closed_at_night_when_the_window_is_only_in_the_afternoon()
    {
        // Arrange
        var campus = NewCampus(Window(Day.Monday, Hour.H12_00, Hour.H18_00));

        // Act
        var minutes = campus.MinutesOpenIn(Day.Monday, Shift.Evening);

        // Assert
        minutes.Should().Be(0);
    }

    [Test]
    public void Campus_MinutesOpenIn_Should_be_closed_in_every_shift_when_the_window_is_before_the_first_shift()
    {
        // Arrange — campus aberto na madrugada, fora de qualquer turno.
        var campus = NewCampus(Window(Day.Monday, Hour.H00_00, Hour.H06_00));

        // Act
        var morning = campus.MinutesOpenIn(Day.Monday, Shift.Morning);
        var afternoon = campus.MinutesOpenIn(Day.Monday, Shift.Afternoon);
        var evening = campus.MinutesOpenIn(Day.Monday, Shift.Evening);

        // Assert
        (morning, afternoon, evening).Should().Be((0, 0, 0));
    }

    [Test]
    public void Campus_MinutesOpenIn_Should_be_closed_in_the_morning_when_the_window_is_only_at_night()
    {
        // Arrange
        var campus = NewCampus(Window(Day.Monday, Hour.H18_00, Hour.H23_45));

        // Act
        var minutes = campus.MinutesOpenIn(Day.Monday, Shift.Morning);

        // Assert
        minutes.Should().Be(0);
    }

    #endregion

    #region Nível 5 — janela atravessando turnos

    [Test]
    public void Campus_MinutesOpenIn_Should_split_a_window_that_crosses_the_morning_and_the_afternoon()
    {
        // Arrange — 10h–14h vira 120min de manhã e 120min de tarde.
        var campus = NewCampus(Window(Day.Monday, Hour.H10_00, Hour.H14_00));

        // Act
        var morning = campus.MinutesOpenIn(Day.Monday, Shift.Morning);
        var afternoon = campus.MinutesOpenIn(Day.Monday, Shift.Afternoon);
        var evening = campus.MinutesOpenIn(Day.Monday, Shift.Evening);

        // Assert
        (morning, afternoon, evening).Should().Be((120, 120, 0));
    }

    [Test]
    public void Campus_MinutesOpenIn_Should_split_a_window_that_crosses_the_afternoon_and_the_evening()
    {
        // Arrange — 17h–19h vira 60min de tarde e 60min de noite.
        var campus = NewCampus(Window(Day.Monday, Hour.H17_00, Hour.H19_00));

        // Act
        var morning = campus.MinutesOpenIn(Day.Monday, Shift.Morning);
        var afternoon = campus.MinutesOpenIn(Day.Monday, Shift.Afternoon);
        var evening = campus.MinutesOpenIn(Day.Monday, Shift.Evening);

        // Assert
        (morning, afternoon, evening).Should().Be((0, 60, 60));
    }

    [Test]
    public void Campus_MinutesOpenIn_Should_split_a_single_window_across_the_three_shifts()
    {
        // Arrange — 07h–23h cobre a manhã e a tarde inteiras e 5h da noite.
        var campus = NewCampus(Window(Day.Monday, Hour.H07_00, Hour.H23_00));

        // Act
        var morning = campus.MinutesOpenIn(Day.Monday, Shift.Morning);
        var afternoon = campus.MinutesOpenIn(Day.Monday, Shift.Afternoon);
        var evening = campus.MinutesOpenIn(Day.Monday, Shift.Evening);

        // Assert
        (morning, afternoon, evening).Should().Be((MorningMinutes, AfternoonMinutes, 300));
    }

    [Test]
    public void Campus_MinutesOpenIn_Should_ignore_the_part_of_the_window_before_the_first_shift()
    {
        // Arrange — abre à meia-noite, mas a manhã só começa às 06h.
        var campus = NewCampus(Window(Day.Monday, Hour.H00_00, Hour.H09_00));

        // Act
        var minutes = campus.MinutesOpenIn(Day.Monday, Shift.Morning);

        // Assert
        minutes.Should().Be(180);
    }

    #endregion

    #region Nível 6 — várias janelas no mesmo dia

    [Test]
    public void Campus_MinutesOpenIn_Should_sum_two_windows_of_the_same_shift()
    {
        // Arrange — 07h–09h e 10h–12h, com uma hora fechado no meio.
        var campus = NewCampus(
            Window(Day.Monday, Hour.H07_00, Hour.H09_00),
            Window(Day.Monday, Hour.H10_00, Hour.H12_00));

        // Act
        var minutes = campus.MinutesOpenIn(Day.Monday, Shift.Morning);

        // Assert
        minutes.Should().Be(240);
    }

    [Test]
    public void Campus_MinutesOpenIn_Should_sum_two_adjacent_windows_as_a_continuous_block()
    {
        // Arrange — 07h–09h emendado com 09h–12h dá a manhã inteira, sem buraco.
        var campus = NewCampus(
            Window(Day.Monday, Hour.H07_00, Hour.H09_00),
            Window(Day.Monday, Hour.H09_00, Hour.H12_00));

        // Act
        var minutes = campus.MinutesOpenIn(Day.Monday, Shift.Morning);

        // Assert
        minutes.Should().Be(MorningMinutes);
    }

    [Test]
    public void Campus_MinutesOpenIn_Should_sum_three_windows_of_the_same_shift()
    {
        // Arrange
        var campus = NewCampus(
            Window(Day.Monday, Hour.H07_00, Hour.H08_00),
            Window(Day.Monday, Hour.H09_00, Hour.H10_00),
            Window(Day.Monday, Hour.H11_00, Hour.H12_00));

        // Act
        var minutes = campus.MinutesOpenIn(Day.Monday, Shift.Morning);

        // Assert
        minutes.Should().Be(180);
    }

    [Test]
    public void Campus_MinutesOpenIn_Should_not_depend_on_the_order_of_the_windows()
    {
        // Arrange — as janelas do dia vêm fora de ordem cronológica.
        var campus = NewCampus(
            Window(Day.Monday, Hour.H10_00, Hour.H12_00),
            Window(Day.Monday, Hour.H07_00, Hour.H09_00));

        // Act
        var minutes = campus.MinutesOpenIn(Day.Monday, Shift.Morning);

        // Assert
        minutes.Should().Be(240);
    }

    [Test]
    public void Campus_MinutesOpenIn_Should_count_each_shift_when_the_day_has_a_window_per_shift()
    {
        // Arrange — uma hora aberta em cada turno.
        var campus = NewCampus(
            Window(Day.Monday, Hour.H07_00, Hour.H08_00),
            Window(Day.Monday, Hour.H13_00, Hour.H14_00),
            Window(Day.Monday, Hour.H19_00, Hour.H20_00));

        // Act
        var morning = campus.MinutesOpenIn(Day.Monday, Shift.Morning);
        var afternoon = campus.MinutesOpenIn(Day.Monday, Shift.Afternoon);
        var evening = campus.MinutesOpenIn(Day.Monday, Shift.Evening);

        // Assert
        (morning, afternoon, evening).Should().Be((60, 60, 60));
    }

    [Test]
    public void Campus_MinutesOpenIn_Should_count_the_lunch_break_as_closed_time()
    {
        // Arrange — manhã cheia, almoço fechado das 12h às 13h, tarde a partir das 13h.
        var campus = NewCampus(
            Window(Day.Monday, Hour.H07_00, Hour.H12_00),
            Window(Day.Monday, Hour.H13_00, Hour.H18_00));

        // Act
        var morning = campus.MinutesOpenIn(Day.Monday, Shift.Morning);
        var afternoon = campus.MinutesOpenIn(Day.Monday, Shift.Afternoon);

        // Assert
        (morning, afternoon).Should().Be((MorningMinutes, 300));
    }

    [Test]
    public void Campus_MinutesOpenIn_Should_sum_only_the_windows_that_touch_the_shift()
    {
        // Arrange — três janelas, mas só duas encostam na tarde.
        var campus = NewCampus(
            Window(Day.Monday, Hour.H07_00, Hour.H11_00),
            Window(Day.Monday, Hour.H11_00, Hour.H13_00),
            Window(Day.Monday, Hour.H16_00, Hour.H20_00));

        // Act
        var minutes = campus.MinutesOpenIn(Day.Monday, Shift.Afternoon);

        // Assert — 12h–13h (60) + 16h–18h (120).
        minutes.Should().Be(180);
    }

    #endregion

    #region Nível 7 — semana inteira e casos combinados

    [Test]
    public void Campus_MinutesOpenIn_Should_not_count_the_windows_of_the_other_days()
    {
        // Arrange — mesma janela em dois dias não vira o dobro num dia só.
        var campus = NewCampus(
            Window(Day.Monday, Hour.H07_00, Hour.H12_00),
            Window(Day.Tuesday, Hour.H07_00, Hour.H12_00));

        // Act
        var minutes = campus.MinutesOpenIn(Day.Monday, Shift.Morning);

        // Assert
        minutes.Should().Be(MorningMinutes);
    }

    [Test]
    public void Campus_MinutesOpenIn_Should_count_a_different_window_for_each_day()
    {
        // Arrange — segunda só de manhã, quarta só de noite.
        var campus = NewCampus(
            Window(Day.Monday, Hour.H07_00, Hour.H12_00),
            Window(Day.Wednesday, Hour.H18_00, Hour.H22_00));

        // Act
        var mondayMorning = campus.MinutesOpenIn(Day.Monday, Shift.Morning);
        var mondayEvening = campus.MinutesOpenIn(Day.Monday, Shift.Evening);
        var wednesdayMorning = campus.MinutesOpenIn(Day.Wednesday, Shift.Morning);
        var wednesdayEvening = campus.MinutesOpenIn(Day.Wednesday, Shift.Evening);

        // Assert
        (mondayMorning, mondayEvening).Should().Be((MorningMinutes, 0));
        (wednesdayMorning, wednesdayEvening).Should().Be((0, 240));
    }

    [Test]
    public void Campus_MinutesOpenIn_Should_open_only_on_saturday_when_it_is_the_only_day_with_a_window()
    {
        // Arrange
        var campus = NewCampus(Window(Day.Saturday, Hour.H08_00, Hour.H12_00));

        // Act
        var saturday = campus.MinutesOpenIn(Day.Saturday, Shift.Morning);
        var monday = campus.MinutesOpenIn(Day.Monday, Shift.Morning);

        // Assert
        (saturday, monday).Should().Be((240, 0));
    }

    [Test]
    public void Campus_MinutesOpenIn_Should_follow_the_default_opening_hours_on_every_weekday()
    {
        // Arrange — o padrão é 07h–22h de segunda a sexta.
        var campus = DefaultCampus();
        Day[] weekdays = [Day.Monday, Day.Tuesday, Day.Wednesday, Day.Thursday, Day.Friday];

        // Act
        var morning = weekdays.Select(d => campus.MinutesOpenIn(d, Shift.Morning));
        var afternoon = weekdays.Select(d => campus.MinutesOpenIn(d, Shift.Afternoon));
        var evening = weekdays.Select(d => campus.MinutesOpenIn(d, Shift.Evening));

        // Assert
        morning.Should().AllBeEquivalentTo(MorningMinutes);
        afternoon.Should().AllBeEquivalentTo(AfternoonMinutes);
        evening.Should().AllBeEquivalentTo(240);
    }

    [Test]
    public void Campus_MinutesOpenIn_Should_handle_a_full_week_with_a_different_grade_per_day()
    {
        // Arrange — semana realista: dias cheios, dia só de noite, sábado só de manhã e
        // um dia sem nenhuma janela.
        var campus = NewCampus(
            Window(Day.Monday, Hour.H07_00, Hour.H12_00),
            Window(Day.Monday, Hour.H13_00, Hour.H22_00),
            Window(Day.Tuesday, Hour.H18_00, Hour.H22_30),
            Window(Day.Wednesday, Hour.H07_00, Hour.H12_00),
            Window(Day.Wednesday, Hour.H13_00, Hour.H22_00),
            Window(Day.Friday, Hour.H10_00, Hour.H16_00),
            Window(Day.Saturday, Hour.H08_00, Hour.H12_00));

        // Act
        var monday = (
            campus.MinutesOpenIn(Day.Monday, Shift.Morning),
            campus.MinutesOpenIn(Day.Monday, Shift.Afternoon),
            campus.MinutesOpenIn(Day.Monday, Shift.Evening));
        var tuesday = (
            campus.MinutesOpenIn(Day.Tuesday, Shift.Morning),
            campus.MinutesOpenIn(Day.Tuesday, Shift.Afternoon),
            campus.MinutesOpenIn(Day.Tuesday, Shift.Evening));
        var thursday = (
            campus.MinutesOpenIn(Day.Thursday, Shift.Morning),
            campus.MinutesOpenIn(Day.Thursday, Shift.Afternoon),
            campus.MinutesOpenIn(Day.Thursday, Shift.Evening));
        var friday = (
            campus.MinutesOpenIn(Day.Friday, Shift.Morning),
            campus.MinutesOpenIn(Day.Friday, Shift.Afternoon),
            campus.MinutesOpenIn(Day.Friday, Shift.Evening));
        var saturday = (
            campus.MinutesOpenIn(Day.Saturday, Shift.Morning),
            campus.MinutesOpenIn(Day.Saturday, Shift.Afternoon),
            campus.MinutesOpenIn(Day.Saturday, Shift.Evening));

        // Assert
        monday.Should().Be((MorningMinutes, 300, 240));      // fecha 12h–13h e 22h
        tuesday.Should().Be((0, 0, 270));                    // só noite, até 22h30
        thursday.Should().Be((0, 0, 0));                     // dia sem janela
        friday.Should().Be((120, 240, 0));                   // 10h–16h
        saturday.Should().Be((240, 0, 0));                   // só manhã
    }

    [Test]
    public void Campus_MinutesOpenIn_Should_sum_overlapping_windows_of_the_same_day_twice()
    {
        // Arrange — 07h–10h e 09h–12h se sobrepõem em 1h, e a soma conta essa hora em
        // dobro (360 > os 300min da manhã). O cálculo depende de as janelas do dia não
        // se sobreporem, invariante garantida no UpdateCampusOpeningHours.
        var campus = NewCampus(
            Window(Day.Monday, Hour.H07_00, Hour.H10_00),
            Window(Day.Monday, Hour.H09_00, Hour.H12_00));

        // Act
        var minutes = campus.MinutesOpenIn(Day.Monday, Shift.Morning);

        // Assert
        minutes.Should().Be(360);
    }

    #endregion
}
