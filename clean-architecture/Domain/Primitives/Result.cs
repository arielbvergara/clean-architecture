namespace Domain.Primitives;

public class Result<T, TE>
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public T? Value { get; }
    public TE? Error { get; }

    private Result(T value)
    {
        IsSuccess = true;
        Value = value;
        Error = default;
    }

    private Result(TE error)
    {
        IsSuccess = false;
        Value = default;
        Error = error;
    }

    public static Result<T, TE> Ok(T value) => new(value);
    public static Result<T, TE> Fail(TE error) => new(error);

    public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<TE, TResult> onFailure)
    {
        return IsSuccess ? onSuccess(Value!) : onFailure(Error!);
    }
}
