namespace MockApi.Api.Common;

/// <summary>
/// Minimal Result&lt;T&gt; wrapper so the service layer can signal "not found" /
/// validation failures without throwing exceptions for flow control.
/// </summary>
public class Result<T>
{
    public bool IsSuccess { get; }

    public T? Value { get; }

    public string? Error { get; }

    private Result(bool isSuccess, T? value, string? error)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
    }

    public static Result<T> Success(T value) => new(true, value, error: null);

    public static Result<T> Failure(string error) => new(false, value: default, error);
}

public class Result
{
    public bool IsSuccess { get; }

    public string? Error { get; }

    private Result(bool isSuccess, string? error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Success() => new(true, error: null);

    public static Result Failure(string error) => new(false, error);
}
