using Estud.Back.Domain.Classes;

namespace Estud.Tests.Domain;

public class ClassGradeUnitTests
{
    #region Cenários

    private static readonly List<(ClassNoteType NoteType, int Weight, decimal Note)> Works =
    [
        (ClassNoteType.N1, 100, 7.0M),
        (ClassNoteType.N2, 100, 8.0M),
        (ClassNoteType.N3, 100, 9.0M),
    ];

    private static readonly List<(ClassNoteType NoteType, int Weight, decimal Note)> FirstActivityOnly =
    [
        (ClassNoteType.N1, 100, 8.0M),
    ];

    private static readonly List<(ClassNoteType NoteType, int Weight, decimal Note)> FirstTwoActivitiesOnly =
    [
        (ClassNoteType.N1, 100, 6.0M),
        (ClassNoteType.N2, 100, 8.0M),
    ];

    private static readonly List<(ClassNoteType NoteType, int Weight, decimal Note)> FirstHalfWeightedActivityOnly =
    [
        (ClassNoteType.N1, 50, 6.0M),
    ];

    private static readonly List<(ClassNoteType NoteType, int Weight, decimal Note)> FirstNoteTypeCompletedOnly =
    [
        (ClassNoteType.N1, 30, 5.0M),
        (ClassNoteType.N1, 70, 8.0M),
    ];

    private static readonly List<(ClassNoteType NoteType, int Weight, decimal Note)> FirstNoteTypeCompletedAndSecondStarted =
    [
        (ClassNoteType.N1, 40, 10.0M),
        (ClassNoteType.N1, 60, 7.0M),
        (ClassNoteType.N2, 50, 8.0M),
    ];

    private static readonly List<(ClassNoteType NoteType, int Weight, decimal Note)> UnevenNotes =
    [
        (ClassNoteType.N1, 100, 9.0M),
        (ClassNoteType.N2, 100, 4.0M),
        (ClassNoteType.N3, 100, 7.0M),
    ];

    private static readonly List<(ClassNoteType NoteType, int Weight, decimal Note)> PerfectNotes =
    [
        (ClassNoteType.N1, 100, 10.0M),
        (ClassNoteType.N2, 100, 10.0M),
        (ClassNoteType.N3, 100, 10.0M),
    ];

    private static readonly List<(ClassNoteType NoteType, int Weight, decimal Note)> PerfectNotesWithSplitWeights =
    [
        (ClassNoteType.N1, 20, 10.0M),
        (ClassNoteType.N1, 80, 10.0M),
        (ClassNoteType.N2, 35, 10.0M),
        (ClassNoteType.N2, 65, 10.0M),
        (ClassNoteType.N3, 50, 10.0M),
        (ClassNoteType.N3, 50, 10.0M),
    ];

    private static readonly List<(ClassNoteType NoteType, int Weight, decimal Note)> ThirdNoteIsTheLowest =
    [
        (ClassNoteType.N1, 100, 9.0M),
        (ClassNoteType.N2, 100, 8.0M),
        (ClassNoteType.N3, 100, 5.0M),
    ];

    private static readonly List<(ClassNoteType NoteType, int Weight, decimal Note)> ThirdActivityOnly =
    [
        (ClassNoteType.N3, 100, 10.0M),
    ];

    private static readonly List<(ClassNoteType NoteType, int Weight, decimal Note)> FirstNoteTypePartiallySplit =
    [
        (ClassNoteType.N1, 30, 6.0M),
        (ClassNoteType.N1, 30, 9.0M),
    ];

    #endregion

    #region Turma completa

    [Test]
    public void ClassGrade_Average_Should_take_the_two_highest_notes_when_rule_is_best_two_of_three()
    {
        // Act
        var average = ClassGradeRule.BestTwoOfThree.Average(Works);

        // Assert — (8 + 9) / 2
        average.Should().Be(8.5M);
    }

    [Test]
    public void ClassGrade_Average_Should_ignore_the_third_note_when_rule_is_average_of_two()
    {
        // Act
        var average = ClassGradeRule.AverageOfTwo.Average(Works);

        // Assert — (7 + 8) / 2
        average.Should().Be(7.5M);
    }

    [Test]
    public void ClassGrade_Average_Should_take_all_the_notes_when_rule_is_average_of_three()
    {
        // Act
        var average = ClassGradeRule.AverageOfThree.Average(Works);

        // Assert — (7 + 8 + 9) / 3
        average.Should().Be(8.0M);
    }

    [Test]
    public void ClassGrade_Average_Should_take_the_third_note_when_it_beats_the_average_of_the_first_two()
    {
        // Act
        var average = ClassGradeRule.AverageOrThird.Average(Works);

        // Assert — (7 + 8) / 2 = 7.5 contra N3 = 9
        average.Should().Be(9.0M);
    }

    [Test]
    public void ClassGrade_Average_Should_discard_the_lowest_note_when_notes_are_uneven()
    {
        // Act
        var average = ClassGradeRule.BestTwoOfThree.Average(UnevenNotes);

        // Assert — N2 é a menor, então (9 + 7) / 2
        average.Should().Be(8.0M);
    }

    [Test]
    public void ClassGrade_Average_Should_ignore_the_third_note_when_notes_are_uneven()
    {
        // Act
        var average = ClassGradeRule.AverageOfTwo.Average(UnevenNotes);

        // Assert — N3 fica de fora mesmo valendo mais que N2, então (9 + 4) / 2
        average.Should().Be(6.5M);
    }

    [Test]
    public void ClassGrade_Average_Should_take_all_the_notes_when_notes_are_uneven()
    {
        // Act
        var average = ClassGradeRule.AverageOfThree.Average(UnevenNotes);

        // Assert — (9 + 4 + 7) / 3 = 6.666..., que arredonda para 6.7
        average.Should().Be(20M / 3M);
    }

    [Test]
    public void ClassGrade_Average_Should_take_the_third_note_when_notes_are_uneven()
    {
        // Act
        var average = ClassGradeRule.AverageOrThird.Average(UnevenNotes);

        // Assert — (9 + 4) / 2 = 6.5 contra N3 = 7
        average.Should().Be(7.0M);
    }

    [Test]
    public void ClassGrade_Average_Should_discard_the_third_note_when_it_is_the_lowest()
    {
        // Act
        var average = ClassGradeRule.BestTwoOfThree.Average(ThirdNoteIsTheLowest);

        // Assert — (9 + 8) / 2
        average.Should().Be(8.5M);
    }

    [Test]
    public void ClassGrade_Average_Should_keep_the_average_when_the_third_note_does_not_beat_it()
    {
        // Act
        var average = ClassGradeRule.AverageOrThird.Average(ThirdNoteIsTheLowest);

        // Assert — (9 + 8) / 2 = 8.5 contra N3 = 5
        average.Should().Be(8.5M);
    }

    #endregion

    #region Nota máxima

    [Test]
    [TestCase(ClassGradeRule.BestTwoOfThree)]
    [TestCase(ClassGradeRule.AverageOfTwo)]
    [TestCase(ClassGradeRule.AverageOfThree)]
    [TestCase(ClassGradeRule.AverageOrThird)]
    public void ClassGrade_Average_Should_be_ten_when_the_student_gets_every_note_right(ClassGradeRule rule)
    {
        // Act
        var average = rule.Average(PerfectNotes);

        // Assert
        average.Should().Be(10.0M);
    }

    [Test]
    [TestCase(ClassGradeRule.BestTwoOfThree)]
    [TestCase(ClassGradeRule.AverageOfTwo)]
    [TestCase(ClassGradeRule.AverageOfThree)]
    [TestCase(ClassGradeRule.AverageOrThird)]
    public void ClassGrade_Average_Should_be_ten_when_the_student_gets_every_note_right_with_split_weights(ClassGradeRule rule)
    {
        // Act
        var average = rule.Average(PerfectNotesWithSplitWeights);

        // Assert — cada tipo fecha 100 de peso repartido, então todos valem 10
        average.Should().Be(10.0M);
    }

    #endregion

    #region Nota parcial — tipos de nota ainda sem atividade

    [Test]
    [TestCase(ClassGradeRule.BestTwoOfThree)]
    [TestCase(ClassGradeRule.AverageOfTwo)]
    [TestCase(ClassGradeRule.AverageOfThree)]
    [TestCase(ClassGradeRule.AverageOrThird)]
    public void ClassGrade_Average_Should_be_zero_when_there_is_no_work(ClassGradeRule rule)
    {
        // Arrange
        List<(ClassNoteType NoteType, int Weight, decimal Note)> works = [];

        // Act
        var average = rule.Average(works);

        // Assert
        average.Should().Be(0M);
    }

    [Test]
    [TestCase(ClassGradeRule.BestTwoOfThree)]
    [TestCase(ClassGradeRule.AverageOfTwo)]
    [TestCase(ClassGradeRule.AverageOrThird)]
    public void ClassGrade_Average_Should_count_the_note_types_without_activity_as_zero(ClassGradeRule rule)
    {
        // Act
        var average = rule.Average(FirstActivityOnly);

        // Assert — (8 + 0) / 2
        average.Should().Be(4.0M);
    }

    [Test]
    public void ClassGrade_Average_Should_count_the_note_types_without_activity_as_zero_when_rule_is_average_of_three()
    {
        // Act
        var average = ClassGradeRule.AverageOfThree.Average(FirstActivityOnly);

        // Assert — (8 + 0 + 0) / 3 = 2.666..., que arredonda para 2.7
        average.Should().Be(8M / 3M);
    }

    [Test]
    [TestCase(ClassGradeRule.BestTwoOfThree)]
    [TestCase(ClassGradeRule.AverageOfTwo)]
    [TestCase(ClassGradeRule.AverageOrThird)]
    public void ClassGrade_Average_Should_count_the_third_note_type_without_activity_as_zero(ClassGradeRule rule)
    {
        // Act
        var average = rule.Average(FirstTwoActivitiesOnly);

        // Assert — (6 + 8) / 2
        average.Should().Be(7.0M);
    }

    [Test]
    public void ClassGrade_Average_Should_count_the_third_note_type_without_activity_as_zero_when_rule_is_average_of_three()
    {
        // Act
        var average = ClassGradeRule.AverageOfThree.Average(FirstTwoActivitiesOnly);

        // Assert — (6 + 8 + 0) / 3 = 4.666..., que arredonda para 4.7
        average.Should().Be(14M / 3M);
    }

    [Test]
    [TestCase(ClassGradeRule.BestTwoOfThree)]
    [TestCase(ClassGradeRule.AverageOfTwo)]
    [TestCase(ClassGradeRule.AverageOrThird)]
    public void ClassGrade_Average_Should_count_the_first_note_type_as_zero_when_only_the_second_was_released(ClassGradeRule rule)
    {
        // Arrange
        List<(ClassNoteType NoteType, int Weight, decimal Note)> works = [(ClassNoteType.N2, 100, 8.0M)];

        // Act
        var average = rule.Average(works);

        // Assert — (0 + 8) / 2
        average.Should().Be(4.0M);
    }

    [Test]
    public void ClassGrade_Average_Should_take_the_third_note_alone_when_it_is_the_only_one_released()
    {
        // Act
        var average = ClassGradeRule.AverageOrThird.Average(ThirdActivityOnly);

        // Assert — max(0, 10): a substitutiva sozinha já entrega a nota cheia
        average.Should().Be(10.0M);
    }

    [Test]
    public void ClassGrade_Average_Should_be_zero_when_the_only_note_released_is_the_ignored_one()
    {
        // Act
        var average = ClassGradeRule.AverageOfTwo.Average(ThirdActivityOnly);

        // Assert — a regra não usa N3, então o 10 do aluno não entra em lugar nenhum
        average.Should().Be(0M);
    }

    [Test]
    [TestCase(ClassGradeRule.BestTwoOfThree)]
    [TestCase(ClassGradeRule.AverageOfTwo)]
    [TestCase(ClassGradeRule.AverageOrThird)]
    public void ClassGrade_Average_Should_count_the_missing_weight_and_note_types_as_zero(ClassGradeRule rule)
    {
        // Act
        var average = rule.Average(FirstHalfWeightedActivityOnly);

        // Assert — N1 vale 3.0 (metade do peso), então (3 + 0) / 2
        average.Should().Be(1.5M);
    }

    [Test]
    public void ClassGrade_Average_Should_count_the_missing_weight_and_note_types_as_zero_when_rule_is_average_of_three()
    {
        // Act
        var average = ClassGradeRule.AverageOfThree.Average(FirstHalfWeightedActivityOnly);

        // Assert — N1 vale 3.0 (metade do peso), então (3 + 0 + 0) / 3
        average.Should().Be(1.0M);
    }

    [Test]
    [TestCase(ClassGradeRule.BestTwoOfThree)]
    [TestCase(ClassGradeRule.AverageOfTwo)]
    [TestCase(ClassGradeRule.AverageOrThird)]
    public void ClassGrade_Average_Should_mix_a_completed_note_type_with_a_partial_one(ClassGradeRule rule)
    {
        // Act
        var average = rule.Average(FirstNoteTypeCompletedAndSecondStarted);

        // Assert — N1 vale 8.2 e N2 vale 4.0 (metade do peso), então (8.2 + 4.0) / 2
        average.Should().Be(6.1M);
    }

    [Test]
    public void ClassGrade_Average_Should_mix_a_completed_note_type_with_a_partial_one_when_rule_is_average_of_three()
    {
        // Act
        var average = ClassGradeRule.AverageOfThree.Average(FirstNoteTypeCompletedAndSecondStarted);

        // Assert — (8.2 + 4.0 + 0) / 3 = 4.066..., que arredonda para 4.1
        average.Should().Be(12.2M / 3M);
    }

    #endregion

    #region Composição do peso dentro de um tipo de nota

    [Test]
    [TestCase(ClassGradeRule.BestTwoOfThree)]
    [TestCase(ClassGradeRule.AverageOfTwo)]
    [TestCase(ClassGradeRule.AverageOrThird)]
    public void ClassGrade_Average_Should_add_up_the_weights_inside_a_note_type(ClassGradeRule rule)
    {
        // Act
        var average = rule.Average(FirstNoteTypeCompletedOnly);

        // Assert — N1 vale 7.1 (5 × 30% + 8 × 70%), então (7.1 + 0) / 2
        average.Should().Be(3.55M);
    }

    [Test]
    public void ClassGrade_Average_Should_add_up_the_weights_inside_a_note_type_when_rule_is_average_of_three()
    {
        // Act
        var average = ClassGradeRule.AverageOfThree.Average(FirstNoteTypeCompletedOnly);

        // Assert — (7.1 + 0 + 0) / 3 = 2.366..., que arredonda para 2.4
        average.Should().Be(7.1M / 3M);
    }

    [Test]
    [TestCase(ClassGradeRule.BestTwoOfThree)]
    [TestCase(ClassGradeRule.AverageOfTwo)]
    [TestCase(ClassGradeRule.AverageOrThird)]
    public void ClassGrade_Average_Should_add_up_two_partial_weights_inside_a_note_type(ClassGradeRule rule)
    {
        // Act
        var average = rule.Average(FirstNoteTypePartiallySplit);

        // Assert — N1 vale 4.5 (6 × 30% + 9 × 30%), então (4.5 + 0) / 2
        average.Should().Be(2.25M);
    }

    [Test]
    public void ClassGrade_Average_Should_add_up_two_partial_weights_inside_a_note_type_when_rule_is_average_of_three()
    {
        // Act
        var average = ClassGradeRule.AverageOfThree.Average(FirstNoteTypePartiallySplit);

        // Assert — (4.5 + 0 + 0) / 3
        average.Should().Be(1.5M);
    }

    [Test]
    public void ClassGrade_Average_Should_count_the_weight_without_activity_as_zero_inside_a_note_type()
    {
        // Arrange — N1 só tem 50 dos 100 de peso criado
        List<(ClassNoteType NoteType, int Weight, decimal Note)> works =
        [
            (ClassNoteType.N1, 50, 8.0M),
            (ClassNoteType.N2, 100, 6.0M),
            (ClassNoteType.N3, 100, 8.0M),
        ];

        // Act
        var average = ClassGradeRule.AverageOfThree.Average(works);

        // Assert — N1 vale 4.0 e não 8.0, então (4 + 6 + 8) / 3
        average.Should().Be(6.0M);
    }

    [Test]
    public void ClassGrade_Average_Should_not_count_a_work_with_zero_weight()
    {
        // Arrange — a única atividade de N1 não vale nada na composição
        List<(ClassNoteType NoteType, int Weight, decimal Note)> works =
        [
            (ClassNoteType.N1, 0, 10.0M),
            (ClassNoteType.N2, 100, 7.0M),
            (ClassNoteType.N3, 100, 8.0M),
        ];

        // Act
        var average = ClassGradeRule.AverageOfThree.Average(works);

        // Assert — N1 fica zerada, então (0 + 7 + 8) / 3
        average.Should().Be(5.0M);
    }

    [Test]
    public void ClassGrade_Average_Should_keep_the_weight_of_a_work_with_zero_note()
    {
        // Arrange — 40 dos 100 de peso de N1 já existem e ainda estão sem nota
        List<(ClassNoteType NoteType, int Weight, decimal Note)> works =
        [
            (ClassNoteType.N1, 60, 10.0M),
            (ClassNoteType.N1, 40, 0.0M),
            (ClassNoteType.N2, 100, 8.0M),
            (ClassNoteType.N3, 100, 10.0M),
        ];

        // Act
        var average = ClassGradeRule.AverageOfThree.Average(works);

        // Assert — N1 cai de 10 para 6, então (6 + 8 + 10) / 3
        average.Should().Be(8.0M);
    }

    #endregion

    #region Empates

    [Test]
    public void ClassGrade_Average_Should_take_both_notes_when_the_two_highest_are_tied()
    {
        // Arrange
        List<(ClassNoteType NoteType, int Weight, decimal Note)> works =
        [
            (ClassNoteType.N1, 100, 8.0M),
            (ClassNoteType.N2, 100, 8.0M),
            (ClassNoteType.N3, 100, 3.0M),
        ];

        // Act
        var average = ClassGradeRule.BestTwoOfThree.Average(works);

        // Assert — (8 + 8) / 2
        average.Should().Be(8.0M);
    }

    [Test]
    public void ClassGrade_Average_Should_keep_the_value_when_the_third_note_ties_the_average()
    {
        // Arrange
        List<(ClassNoteType NoteType, int Weight, decimal Note)> works =
        [
            (ClassNoteType.N1, 100, 8.0M),
            (ClassNoteType.N2, 100, 6.0M),
            (ClassNoteType.N3, 100, 7.0M),
        ];

        // Act
        var average = ClassGradeRule.AverageOrThird.Average(works);

        // Assert — (8 + 6) / 2 = 7 empatado com N3 = 7
        average.Should().Be(7.0M);
    }

    #endregion

    #region Tipos de nota usados pela regra

    [Test]
    public void ClassGrade_NoteTypes_Should_not_include_the_third_note_type_when_rule_is_average_of_two()
    {
        // Act
        var noteTypes = ClassGradeRule.AverageOfTwo.NoteTypes;

        // Assert
        noteTypes.Should().Equal(ClassNoteType.N1, ClassNoteType.N2);
    }

    [Test]
    [TestCase(ClassGradeRule.BestTwoOfThree)]
    [TestCase(ClassGradeRule.AverageOfThree)]
    [TestCase(ClassGradeRule.AverageOrThird)]
    public void ClassGrade_NoteTypes_Should_include_the_three_note_types(ClassGradeRule rule)
    {
        // Act
        var noteTypes = rule.NoteTypes;

        // Assert
        noteTypes.Should().Equal(ClassNoteType.N1, ClassNoteType.N2, ClassNoteType.N3);
    }

    #endregion

    #region Entradas inválidas

    [Test]
    public void ClassGrade_Average_Should_throw_when_the_works_are_null()
    {
        // Act
        var act = () => ClassGradeRule.BestTwoOfThree.Average(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Test]
    [TestCase(-1)]
    [TestCase(101)]
    public void ClassGrade_Average_Should_throw_when_a_weight_is_out_of_range(int weight)
    {
        // Arrange
        List<(ClassNoteType NoteType, int Weight, decimal Note)> works = [(ClassNoteType.N1, weight, 8.0M)];

        // Act
        var act = () => ClassGradeRule.BestTwoOfThree.Average(works);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Test]
    [TestCase(-0.1)]
    [TestCase(10.1)]
    public void ClassGrade_Average_Should_throw_when_a_note_is_out_of_range(decimal note)
    {
        // Arrange
        List<(ClassNoteType NoteType, int Weight, decimal Note)> works = [(ClassNoteType.N1, 100, note)];

        // Act
        var act = () => ClassGradeRule.BestTwoOfThree.Average(works);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Test]
    public void ClassGrade_Average_Should_throw_when_the_weights_of_a_note_type_add_up_to_more_than_one_hundred()
    {
        // Arrange
        List<(ClassNoteType NoteType, int Weight, decimal Note)> works =
        [
            (ClassNoteType.N1, 60, 8.0M),
            (ClassNoteType.N1, 60, 8.0M),
        ];

        // Act
        var act = () => ClassGradeRule.BestTwoOfThree.Average(works);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Test]
    public void ClassGrade_Average_Should_throw_when_a_note_type_is_unknown()
    {
        // Arrange
        List<(ClassNoteType NoteType, int Weight, decimal Note)> works = [((ClassNoteType)99, 100, 8.0M)];

        // Act
        var act = () => ClassGradeRule.BestTwoOfThree.Average(works);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Test]
    public void ClassGrade_Average_Should_throw_when_the_rule_is_unknown()
    {
        // Act
        var act = () => ((ClassGradeRule)99).Average(Works);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Test]
    public void ClassGrade_NoteTypes_Should_throw_when_the_rule_is_unknown()
    {
        // Act
        var act = () => ((ClassGradeRule)69).NoteTypes;

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    #endregion
}
