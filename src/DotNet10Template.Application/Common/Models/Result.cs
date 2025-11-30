namespace DotNet10Template.Application.Common.Models;

/// <summary>
/// Generic result wrapper for API responses
/// </summary>
public class Result<T>
{
    public bool Succeeded { get; set; }
    public T? Data { get; set; }
    public string? Message { get; set; }
    public List<string> Errors { get; set; } = new();

    public static Result<T> Success(T data, string? message = null)
    {
        return new Result<T>
        {
            Succeeded = true,
            Data = data,
            Message = message
        };
    }

    public static Result<T> Failure(string error)
    {
        return new Result<T>
        {
            Succeeded = false,
            Errors = new List<string> { error }
        };
    }

    public static Result<T> Failure(List<string> errors)
    {
        return new Result<T>
        {
            Succeeded = false,
            Errors = errors
        };
    }
}

public class Result : Result<object>
{
    public static Result Success(string? message = null)
    {
        return new Result
        {
            Succeeded = true,
            Message = message
        };
    }

    public new static Result Failure(string error)
    {
        return new Result
        {
            Succeeded = false,
            Errors = new List<string> { error }
        };
    }

    public new static Result Failure(List<string> errors)
    {
        return new Result
        {
            Succeeded = false,
            Errors = errors
        };
    }
}
