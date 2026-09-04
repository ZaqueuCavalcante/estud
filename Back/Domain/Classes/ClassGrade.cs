namespace Estud.Back.Domain.Classes;

public static class ClassGrade
{
    extension(ClassGradeRule rule)
    {
        public ClassNoteType[] NoteTypes => rule switch
        {
            ClassGradeRule.AverageOfTwo => [ClassNoteType.N1, ClassNoteType.N2],

            ClassGradeRule.BestTwoOfThree or ClassGradeRule.AverageOfThree or ClassGradeRule.AverageOrThird =>
                [ClassNoteType.N1, ClassNoteType.N2, ClassNoteType.N3],

            _ => throw new ArgumentOutOfRangeException(nameof(rule), rule, "Unknown ClassGradeRule!"),
        };

        public decimal Average(IEnumerable<(ClassNoteType NoteType, int Weight, decimal Note)> works)
        {
            var notes = Notes(works);

            decimal Note(ClassNoteType type) => notes.GetValueOrDefault(type);

            return rule switch
            {
                ClassGradeRule.BestTwoOfThree => rule.NoteTypes
                    .Select(Note).OrderDescending().Take(2).Average(),

                ClassGradeRule.AverageOfTwo => (Note(ClassNoteType.N1) + Note(ClassNoteType.N2)) / 2,

                ClassGradeRule.AverageOfThree =>
                    (Note(ClassNoteType.N1) + Note(ClassNoteType.N2) + Note(ClassNoteType.N3)) / 3,

                ClassGradeRule.AverageOrThird =>
                    Math.Max((Note(ClassNoteType.N1) + Note(ClassNoteType.N2)) / 2, Note(ClassNoteType.N3)),

                _ => throw new ArgumentOutOfRangeException(nameof(rule), rule, "Unknown ClassGradeRule!"),
            };
        }
    }

    private static Dictionary<ClassNoteType, decimal> Notes(IEnumerable<(ClassNoteType NoteType, int Weight, decimal Note)> works)
    {
        ArgumentNullException.ThrowIfNull(works);

        var groups = works.GroupBy(w => w.NoteType).ToList();

        foreach (var group in groups)
        {
            if (!group.Key.IsValid())
                throw new ArgumentOutOfRangeException(nameof(works), group.Key, "Unknown ClassNoteType!");

            foreach (var work in group)
            {
                if (work.Weight is < 0 or > 100)
                    throw new ArgumentOutOfRangeException(
                        nameof(works), work.Weight, $"Weight of a {group.Key} activity must be between 0 and 100!");

                if (work.Note is < 0 or > 10)
                    throw new ArgumentOutOfRangeException(
                        nameof(works), work.Note, $"Note of a {group.Key} work must be between 0 and 10!");
            }

            var weight = group.Sum(w => w.Weight);
            if (weight > 100)
                throw new ArgumentOutOfRangeException(
                    nameof(works), weight, $"Weights of the {group.Key} activities must not add up to more than 100!");
        }

        return groups.ToDictionary(g => g.Key, g => g.Sum(w => w.Note * w.Weight) / 100M);
    }
}
