namespace Estud.Back.Extensions;

public static class ListExtensions
{
    extension(List<decimal> notes)
    {
        public decimal GetAverageNote()
        {
            if (notes.Count <= 2) return 0;
            var average = notes.Select(x => x).OrderDescending().Take(2).Average();
            return Math.Round(average, 2);
        }
    }

    extension(List<Guid> selfs)
    {
        public bool IsSubsetOf(List<Guid> others)
        {
            HashSet<Guid> set = [];
            foreach (var self in selfs)
            {
                if (!set.Add(self)) return false;

                if (!others.Contains(self)) return false;
            }

            return true;
        }

        public bool IsEquivalentTo(List<Guid> others)
        {
            if (selfs.Count != others.Count) return false;

            return selfs.IsSubsetOf(others);
        }
    }

    extension(List<int> selfs)
    {
        public bool IsSubsetOf(List<int> others)
        {
            HashSet<int> set = [];
            foreach (var self in selfs)
            {
                if (!set.Add(self)) return false;

                if (!others.Contains(self)) return false;
            }

            return true;
        }
    }

    extension(IEnumerable<int> list)
    {
        public bool IsAllDistinct()
        {
            if (list is null) return true;

            var set = new HashSet<int>();
            foreach (var x in list)
            {
                if (!set.Add(x)) return false;
            }

            return true;
        }
    }

    extension(IEnumerable<string> list)
    {
        public bool IsAllDistinct()
        {
            if (list is null) return true;

            var set = new HashSet<string>();
            foreach (var x in list)
            {
                if (!set.Add(x)) return false;
            }

            return true;
        }
    }

    extension<T>(IEnumerable<T> source)
    {
        public T PickRandom()
        {
            return source.PickRandom(1).Single();
        }

        public IEnumerable<T> PickRandom(int count)
        {
            return source.Shuffle().Take(count);
        }

        public IEnumerable<T> Shuffle()
        {
            return source.OrderBy(x => Guid.CreateVersion7());
        }
    }
}
