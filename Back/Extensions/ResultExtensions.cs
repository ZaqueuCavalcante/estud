namespace Estud.Back.Extensions;

public static class ResultExtensions
{
    extension<S, E>(OneOf<S, E> value)
    {
        public bool IsSuccess => value.IsT0;
        public S Success => value.IsSuccess
            ? value.AsT0
            : throw new InvalidOperationException($"{value.Error}");

        public bool IsError => value.IsT1;
        public E Error => value.IsError
            ? value.AsT1
            : throw new InvalidOperationException($"{value.Success}");

        public bool HasError(out E error, out S success) => value.TryPickT1(out error, out success);
    }

    extension<S, E>(Task<OneOf<S, E>> task)
    {
        public async Task<S> Success() => (await task).Success;
    }
}
