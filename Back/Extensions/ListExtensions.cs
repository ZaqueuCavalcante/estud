namespace Estud.Back.Extensions;

public static class ListExtensions
{
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
