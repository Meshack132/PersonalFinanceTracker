namespace PersonalFinanceTracker.Domain.Common;

/// <summary>
/// Represents the outcome of an operation that can succeed with a value or fail with an error.
/// Preferred over throwing exceptions for expected failure modes (validation, parsing, etc.).
/// </summary>
public readonly record struct Result<T>
{
    public T? Value { get; }
    public Error? Error { get; }
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;

    private Result(T value)
    {
        Value = value;
        Error = null;
        IsSuccess = true;
    }

    private Result(Error error)
    {
        Value = default;
        Error = error;
        IsSuccess = false;
    }

    public static Result<T> Success(T value) => new(value);
    public static Result<T> Failure(Error error) => new(error);
    public static Result<T> Failure(string code, string message) => new(new Error(code, message));

    /// <summary>Transform the success value; short-circuit on failure.</summary>
    public Result<TOut> Map<TOut>(Func<T, TOut> mapper) =>
        IsSuccess ? Result<TOut>.Success(mapper(Value!)) : Result<TOut>.Failure(Error!.Value);

    public static implicit operator Result<T>(T value) => Success(value);
    public static implicit operator Result<T>(Error error) => Failure(error);
}

public readonly record struct Error(string Code, string Message)
{
    public static readonly Error None = new(string.Empty, string.Empty);
    public override string ToString() => $"[{Code}] {Message}";
}

/// <summary>Common error codes used across the application.</summary>
public static class ErrorCodes
{
    public const string ParseFailure = "PARSE_FAILURE";
    public const string UnknownBank = "UNKNOWN_BANK";
    public const string InvalidAmount = "INVALID_AMOUNT";
    public const string InvalidDate = "INVALID_DATE";
    public const string EmptyFile = "EMPTY_FILE";
    public const string OcrFailure = "OCR_FAILURE";
    public const string ConfigMissing = "CONFIG_MISSING";
}