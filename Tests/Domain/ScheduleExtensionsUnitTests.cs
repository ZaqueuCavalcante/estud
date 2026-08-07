using Estud.Back.Domain.Classes;

namespace Estud.Tests.Domain;

public class ScheduleExtensionsUnitTests
{
    #region Happy path

    [Test]
    public void ScheduleExtensions_ToSchedules_Should_return_empty_list_when_there_are_no_items()
    {
        // Arrange
        List<(Day Day, Hour Start, Hour End, int?, int?)> items = [];

        // Act
        var result = items.ToSchedules();

        // Assert
        result.ShouldBeSuccess();
        result.Success.Should().BeEmpty();
    }

    [Test]
    public void ScheduleExtensions_ToSchedules_Should_convert_a_single_schedule()
    {
        // Arrange — aula noturna de segunda, 19h–22h.
        List<(Day Day, Hour Start, Hour End, int?, int?)> items =
        [
            (Day.Monday, Hour.H19_00, Hour.H22_00, null, null),
        ];

        // Act
        var result = items.ToSchedules();

        // Assert
        result.ShouldBeSuccess();
        var schedule = result.Success.Should().ContainSingle().Which;
        schedule.Day.Should().Be(Day.Monday);
        schedule.Start.Should().Be(Hour.H19_00);
        schedule.End.Should().Be(Hour.H22_00);
        schedule.GetDiffInMinutes().Should().Be(180);
    }

    [Test]
    public void ScheduleExtensions_ToSchedules_Should_convert_a_night_class_in_three_days_of_the_week()
    {
        // Arrange — grade clássica de turma noturna: segunda, quarta e sexta.
        List<(Day Day, Hour Start, Hour End, int?, int?)> items =
        [
            (Day.Monday, Hour.H19_00, Hour.H22_00, null, null),
            (Day.Wednesday, Hour.H19_00, Hour.H22_00, null, null),
            (Day.Friday, Hour.H19_00, Hour.H22_00, null, null),
        ];

        // Act
        var result = items.ToSchedules();

        // Assert
        result.ShouldBeSuccess();
        result.Success.Should().HaveCount(3);
        result.Success.Should().OnlyContain(x => x.GetDiffInMinutes() == 180);
        result.Success.Select(x => (x.Day, x.Start, x.End, x.TeacherId, x.ClassroomId)).Should().Equal(items);
    }

    [Test]
    public void ScheduleExtensions_ToSchedules_Should_convert_two_blocks_in_the_same_day_separated_by_a_break()
    {
        // Arrange — dois blocos no mesmo dia com 15min de intervalo entre eles.
        List<(Day Day, Hour Start, Hour End, int?, int?)> items =
        [
            (Day.Tuesday, Hour.H07_00, Hour.H09_00, null, null),
            (Day.Tuesday, Hour.H09_15, Hour.H11_00, null, null),
        ];

        // Act
        var result = items.ToSchedules();

        // Assert
        result.ShouldBeSuccess();
        result.Success.Should().HaveCount(2);
        result.Success.Select(x => (x.Day, x.Start, x.End, x.TeacherId, x.ClassroomId)).Should().Equal(items);
    }

    [Test]
    public void ScheduleExtensions_ToSchedules_Should_convert_back_to_back_blocks_in_the_same_day()
    {
        // Arrange — o fim de um bloco é exatamente o início do outro: encostar não é conflitar.
        List<(Day Day, Hour Start, Hour End, int?, int?)> items =
        [
            (Day.Monday, Hour.H07_00, Hour.H09_00, null, null),
            (Day.Monday, Hour.H09_00, Hour.H11_00, null, null),
            (Day.Monday, Hour.H11_00, Hour.H12_00, null, null),
        ];

        // Act
        var result = items.ToSchedules();

        // Assert
        result.ShouldBeSuccess();
        result.Success.Should().HaveCount(3);
        result.Success.Select(x => x.GetDiffInMinutes()).Should().Equal(120, 120, 60);
    }

    [Test]
    public void ScheduleExtensions_ToSchedules_Should_convert_the_same_hours_in_every_day_of_the_week()
    {
        // Arrange — mesmo horário todo dia: o conflito é por dia, então nada colide.
        List<(Day Day, Hour Start, Hour End, int?, int?)> items =
        [
            (Day.Monday, Hour.H07_00, Hour.H12_00, null, null),
            (Day.Tuesday, Hour.H07_00, Hour.H12_00, null, null),
            (Day.Wednesday, Hour.H07_00, Hour.H12_00, null, null),
            (Day.Thursday, Hour.H07_00, Hour.H12_00, null, null),
            (Day.Friday, Hour.H07_00, Hour.H12_00, null, null),
            (Day.Saturday, Hour.H07_00, Hour.H12_00, null, null),
        ];

        // Act
        var result = items.ToSchedules();

        // Assert
        result.ShouldBeSuccess();
        result.Success.Should().HaveCount(6);
        result.Success.Select(x => x.Day).Should().Equal(
            Day.Monday, Day.Tuesday, Day.Wednesday, Day.Thursday, Day.Friday, Day.Saturday);
    }

    [Test]
    public void ScheduleExtensions_ToSchedules_Should_convert_a_full_week_grade()
    {
        // Arrange — grade cheia: manhã e tarde de segunda a sexta, 12 blocos.
        List<(Day Day, Hour Start, Hour End, int?, int?)> items =
        [
            (Day.Monday, Hour.H07_00, Hour.H08_45, null, null),
            (Day.Monday, Hour.H09_00, Hour.H10_45, null, null),
            (Day.Monday, Hour.H13_00, Hour.H14_45, null, null),
            (Day.Tuesday, Hour.H07_00, Hour.H08_45, null, null),
            (Day.Tuesday, Hour.H09_00, Hour.H10_45, null, null),
            (Day.Wednesday, Hour.H07_00, Hour.H08_45, null, null),
            (Day.Wednesday, Hour.H13_00, Hour.H14_45, null, null),
            (Day.Wednesday, Hour.H15_00, Hour.H16_45, null, null),
            (Day.Thursday, Hour.H08_00, Hour.H11_30, null, null),
            (Day.Friday, Hour.H07_00, Hour.H08_45, null, null),
            (Day.Friday, Hour.H09_00, Hour.H10_45, null, null),
            (Day.Saturday, Hour.H08_00, Hour.H12_00, null, null),
        ];

        // Act
        var result = items.ToSchedules();

        // Assert
        result.ShouldBeSuccess();
        result.Success.Should().HaveCount(12);
        result.Success.Select(x => (x.Day, x.Start, x.End, x.TeacherId, x.ClassroomId)).Should().Equal(items);
    }

    [Test]
    public void ScheduleExtensions_ToSchedules_Should_keep_the_original_order_when_items_are_unsorted()
    {
        // Arrange — a lista não é ordenada nem por dia nem por hora; a saída espelha a entrada.
        List<(Day Day, Hour Start, Hour End, int?, int?)> items =
        [
            (Day.Friday, Hour.H19_00, Hour.H22_00, null, null),
            (Day.Monday, Hour.H13_00, Hour.H15_00, null, null),
            (Day.Friday, Hour.H07_00, Hour.H09_00, null, null),
            (Day.Monday, Hour.H07_00, Hour.H09_00, null, null),
        ];

        // Act
        var result = items.ToSchedules();

        // Assert
        result.ShouldBeSuccess();
        result.Success.Select(x => (x.Day, x.Start, x.End, x.TeacherId, x.ClassroomId)).Should().Equal(items);
    }

    [Test]
    [TestCase(Hour.H07_00, Hour.H07_15, 15)]     // menor bloco possível na grade de 15min
    [TestCase(Hour.H00_00, Hour.H07_00, 420)]    // H00_00, valor de borda do enum, como início
    [TestCase(Hour.H07_00, Hour.H23_45, 1005)]   // maior janela possível dentro do dia
    [TestCase(Hour.H00_00, Hour.H23_45, 1425)]   // dia inteiro
    [TestCase(Hour.H13_15, Hour.H17_45, 270)]
    public void ScheduleExtensions_ToSchedules_Should_convert_edge_hour_ranges(Hour start, Hour end, int expectedDiff)
    {
        // Arrange
        List<(Day Day, Hour Start, Hour End, int?, int?)> items = [(Day.Thursday, start, end, null, null)];

        // Act
        var result = items.ToSchedules();

        // Assert
        result.ShouldBeSuccess();
        result.Success.Should().ContainSingle().Which.GetDiffInMinutes().Should().Be(expectedDiff);
    }

    [Test]
    [TestCase(Day.Monday, Hour.H07_00, Hour.H09_00, Day.Monday, Hour.H09_00, Hour.H11_00)]   // encostados
    [TestCase(Day.Monday, Hour.H09_00, Hour.H11_00, Day.Monday, Hour.H07_00, Hour.H09_00)]   // encostados, invertidos
    [TestCase(Day.Monday, Hour.H07_00, Hour.H09_00, Day.Tuesday, Hour.H07_00, Hour.H09_00)]  // idênticos em dias distintos
    [TestCase(Day.Monday, Hour.H07_00, Hour.H09_00, Day.Monday, Hour.H09_15, Hour.H11_00)]   // com intervalo
    [TestCase(Day.Saturday, Hour.H08_00, Hour.H12_00, Day.Saturday, Hour.H13_00, Hour.H17_00)] // manhã e tarde
    [TestCase(Day.Friday, Hour.H07_00, Hour.H07_15, Day.Friday, Hour.H07_15, Hour.H07_30)]   // dois blocos de 15min colados
    public void ScheduleExtensions_ToSchedules_Should_not_conflict_when_schedules_do_not_overlap(
        Day dayA, Hour startA, Hour endA,
        Day dayB, Hour startB, Hour endB)
    {
        // Arrange
        List<(Day Day, Hour Start, Hour End, int?, int?)> items = [(dayA, startA, endA, null, null), (dayB, startB, endB, null, null)];

        // Act
        var result = items.ToSchedules();

        // Assert
        result.ShouldBeSuccess();
        result.Success.Should().HaveCount(2);
    }

    #endregion

    #region Invalid day

    [Test]
    [TestCase((Day)6)]    // domingo não existe no enum
    [TestCase((Day)7)]
    [TestCase((Day)99)]
    [TestCase((Day)(-1))]
    public void ScheduleExtensions_ToSchedules_Should_not_convert_when_day_is_invalid(Day day)
    {
        // Arrange
        List<(Day Day, Hour Start, Hour End, int?, int?)> items = [(day, Hour.H07_00, Hour.H09_00, null, null)];

        // Act
        var result = items.ToSchedules();

        // Assert
        result.ShouldBeError(InvalidDay.I);
    }

    [Test]
    public void ScheduleExtensions_ToSchedules_Should_not_convert_when_an_invalid_day_is_in_the_middle_of_the_list()
    {
        // Arrange — só o item do meio é inválido; os das pontas são válidos.
        List<(Day Day, Hour Start, Hour End, int?, int?)> items =
        [
            (Day.Monday, Hour.H07_00, Hour.H09_00, null, null),
            ((Day)9, Hour.H07_00, Hour.H09_00, null, null),
            (Day.Friday, Hour.H07_00, Hour.H09_00, null, null),
        ];

        // Act
        var result = items.ToSchedules();

        // Assert
        result.ShouldBeError(InvalidDay.I);
    }

    #endregion

    #region Invalid hour

    [Test]
    [TestCase((Hour)705, Hour.H09_00)]    // 07:05 não é múltiplo de 15min
    [TestCase((Hour)701, Hour.H09_00)]
    [TestCase(Hour.H07_00, (Hour)910)]    // 09:10 não existe
    [TestCase((Hour)2400, Hour.H09_00)]   // 24:00 não existe
    [TestCase(Hour.H07_00, (Hour)2460)]
    [TestCase((Hour)1260, Hour.H14_00)]   // 12:60 não existe
    [TestCase((Hour)(-700), Hour.H09_00)]
    [TestCase((Hour)9999, Hour.H09_00)]
    public void ScheduleExtensions_ToSchedules_Should_not_convert_when_hour_is_invalid(Hour start, Hour end)
    {
        // Arrange
        List<(Day Day, Hour Start, Hour End, int?, int?)> items = [(Day.Monday, start, end, null, null)];

        // Act
        var result = items.ToSchedules();

        // Assert
        result.ShouldBeError(InvalidHour.I);
    }

    [Test]
    public void ScheduleExtensions_ToSchedules_Should_not_convert_when_only_the_end_hour_is_invalid()
    {
        // Arrange
        List<(Day Day, Hour Start, Hour End, int?, int?)> items = [(Day.Monday, Hour.H07_00, (Hour)1150, null, null)];

        // Act
        var result = items.ToSchedules();

        // Assert
        result.ShouldBeError(InvalidHour.I);
    }

    #endregion

    #region Invalid schedule

    [Test]
    [TestCase(Hour.H07_00, Hour.H07_00)]   // início igual ao fim
    [TestCase(Hour.H00_00, Hour.H00_00)]
    [TestCase(Hour.H22_00, Hour.H19_00)]   // fim antes do início
    [TestCase(Hour.H09_00, Hour.H08_45)]   // invertido por um único slot de 15min
    [TestCase(Hour.H07_00, Hour.H00_00)]   // meia-noite como fim de uma aula da manhã
    [TestCase(Hour.H23_45, Hour.H07_00)]   // bloco que "vira o dia" não é suportado
    public void ScheduleExtensions_ToSchedules_Should_not_convert_when_the_range_is_invalid(Hour start, Hour end)
    {
        // Arrange
        List<(Day Day, Hour Start, Hour End, int?, int?)> items = [(Day.Wednesday, start, end, null, null)];

        // Act
        var result = items.ToSchedules();

        // Assert
        result.ShouldBeError(InvalidSchedule.I);
    }

    [Test]
    public void ScheduleExtensions_ToSchedules_Should_not_convert_when_one_item_of_a_valid_list_has_an_invalid_range()
    {
        // Arrange — grade válida com um único bloco invertido no meio.
        List<(Day Day, Hour Start, Hour End, int?, int?)> items =
        [
            (Day.Monday, Hour.H19_00, Hour.H22_00, null, null),
            (Day.Wednesday, Hour.H22_00, Hour.H19_00, null, null),
            (Day.Friday, Hour.H19_00, Hour.H22_00, null, null),
        ];

        // Act
        var result = items.ToSchedules();

        // Assert
        result.ShouldBeError(InvalidSchedule.I);
    }

    #endregion

    #region Error precedence

    [Test]
    public void ScheduleExtensions_ToSchedules_Should_return_the_error_of_the_first_invalid_item()
    {
        // Arrange — dia inválido antes de hora inválida: vence o primeiro da lista.
        List<(Day Day, Hour Start, Hour End, int?, int?)> items =
        [
            ((Day)42, Hour.H07_00, Hour.H09_00, null, null),
            (Day.Monday, (Hour)705, Hour.H09_00, null, null),
        ];

        // Act
        var result = items.ToSchedules();

        // Assert
        result.ShouldBeError(InvalidDay.I);
    }

    [Test]
    public void ScheduleExtensions_ToSchedules_Should_prefer_the_item_error_over_the_conflict()
    {
        // Arrange — os dois primeiros conflitam, mas há um item inválido depois:
        // a validação de todos os itens roda antes da checagem de conflito.
        List<(Day Day, Hour Start, Hour End, int?, int?)> items =
        [
            (Day.Monday, Hour.H07_00, Hour.H09_00, null, null),
            (Day.Monday, Hour.H08_00, Hour.H10_00, null, null),
            (Day.Friday, Hour.H07_00, (Hour)905, null, null),
        ];

        // Act
        var result = items.ToSchedules();

        // Assert
        result.ShouldBeError(InvalidHour.I);
    }

    #endregion

    #region Conflicting schedules

    [Test]
    [TestCase(Hour.H07_00, Hour.H09_00, Hour.H07_00, Hour.H09_00)]   // duplicata exata
    [TestCase(Hour.H07_00, Hour.H09_00, Hour.H07_00, Hour.H12_00)]   // mesmo início, fim diferente
    [TestCase(Hour.H07_00, Hour.H09_00, Hour.H08_00, Hour.H09_00)]   // mesmo fim, início diferente
    [TestCase(Hour.H07_00, Hour.H09_00, Hour.H08_00, Hour.H10_00)]   // o segundo começa dentro do primeiro
    [TestCase(Hour.H08_00, Hour.H10_00, Hour.H07_00, Hour.H09_00)]   // o segundo termina dentro do primeiro
    [TestCase(Hour.H07_00, Hour.H12_00, Hour.H08_00, Hour.H09_00)]   // o segundo está contido no primeiro
    [TestCase(Hour.H08_00, Hour.H09_00, Hour.H07_00, Hour.H12_00)]   // o primeiro está contido no segundo
    [TestCase(Hour.H07_00, Hour.H09_00, Hour.H08_45, Hour.H11_00)]   // sobreposição de apenas 15min
    [TestCase(Hour.H07_15, Hour.H09_00, Hour.H07_00, Hour.H07_30)]   // sobreposição de 15min no início
    public void ScheduleExtensions_ToSchedules_Should_not_convert_when_two_schedules_conflict(
        Hour startA, Hour endA, Hour startB, Hour endB)
    {
        // Arrange
        List<(Day Day, Hour Start, Hour End, int?, int?)> items =
        [
            (Day.Monday, startA, endA, null, null),
            (Day.Monday, startB, endB, null, null),
        ];

        // Act
        var result = items.ToSchedules();

        // Assert
        result.ShouldBeError(ConflictingSchedules.I);
    }

    [Test]
    public void ScheduleExtensions_ToSchedules_Should_not_convert_when_the_conflict_is_between_non_adjacent_items()
    {
        // Arrange — o conflito é entre o primeiro e o último item da lista.
        List<(Day Day, Hour Start, Hour End, int?, int?)> items =
        [
            (Day.Monday, Hour.H07_00, Hour.H09_00, null, null),
            (Day.Tuesday, Hour.H07_00, Hour.H09_00, null, null),
            (Day.Wednesday, Hour.H07_00, Hour.H09_00, null, null),
            (Day.Monday, Hour.H08_00, Hour.H10_00, null, null),
        ];

        // Act
        var result = items.ToSchedules();

        // Assert
        result.ShouldBeError(ConflictingSchedules.I);
    }

    [Test]
    public void ScheduleExtensions_ToSchedules_Should_not_convert_when_the_conflict_is_between_the_last_two_items()
    {
        // Arrange — garante que o par final também é comparado.
        List<(Day Day, Hour Start, Hour End, int?, int?)> items =
        [
            (Day.Monday, Hour.H07_00, Hour.H09_00, null, null),
            (Day.Tuesday, Hour.H07_00, Hour.H09_00, null, null),
            (Day.Friday, Hour.H19_00, Hour.H22_00, null, null),
            (Day.Friday, Hour.H21_00, Hour.H23_00, null, null),
        ];

        // Act
        var result = items.ToSchedules();

        // Assert
        result.ShouldBeError(ConflictingSchedules.I);
    }

    [Test]
    public void ScheduleExtensions_ToSchedules_Should_not_convert_when_the_same_block_is_repeated_three_times()
    {
        // Arrange
        List<(Day Day, Hour Start, Hour End, int?, int?)> items =
        [
            (Day.Thursday, Hour.H13_00, Hour.H17_00, null, null),
            (Day.Thursday, Hour.H13_00, Hour.H17_00, null, null),
            (Day.Thursday, Hour.H13_00, Hour.H17_00, null, null),
        ];

        // Act
        var result = items.ToSchedules();

        // Assert
        result.ShouldBeError(ConflictingSchedules.I);
    }

    [Test]
    public void ScheduleExtensions_ToSchedules_Should_not_convert_when_a_full_week_grade_has_a_single_overlap()
    {
        // Arrange — grade cheia e realista, com um único bloco sobreposto na quarta.
        List<(Day Day, Hour Start, Hour End, int?, int?)> items =
        [
            (Day.Monday, Hour.H07_00, Hour.H08_45, null, null),
            (Day.Monday, Hour.H09_00, Hour.H10_45, null, null),
            (Day.Tuesday, Hour.H07_00, Hour.H08_45, null, null),
            (Day.Wednesday, Hour.H07_00, Hour.H10_45, null, null),
            (Day.Wednesday, Hour.H10_00, Hour.H11_45, null, null),
            (Day.Thursday, Hour.H13_00, Hour.H16_30, null, null),
            (Day.Friday, Hour.H19_00, Hour.H22_00, null, null),
        ];

        // Act
        var result = items.ToSchedules();

        // Assert
        result.ShouldBeError(ConflictingSchedules.I);
    }

    #endregion
}
