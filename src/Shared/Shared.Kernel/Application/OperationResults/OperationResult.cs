using System.Diagnostics;
using System.Text.Json.Serialization;

namespace Shared.Kernel.Application.OperationResults;

public class OperationResult : IOperationResult
{

    private string _message;
    public string Message
    {
        get
        {
            if (string.IsNullOrEmpty(this._message) && !this.IsSuccessful && Error != null)
            {
                return this.Error.Errors.FirstOrDefault() ?? string.Empty;
            }

            return _message;
        }
        protected set => this._message = value;
    }

    public bool IsSuccessful { get; protected set; }

    public ErrorResult Error { get; protected set; }

    [JsonIgnore]
    public int StatusCode { get; protected set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string TraceId 
    { 
        get
        {
            if (!this.IsSuccessful) { return Activity.Current?.TraceId.ToString() ?? string.Empty; }
            else return string.Empty;
        }
    }

    public static OperationResult Success(string message, int statusCode = 200)
    {
        return new OperationResult { Message = message, StatusCode = statusCode, IsSuccessful = true };
    }

    public static OperationResult Fail(string errorMessage, bool isShow = true, int statusCode = 400)
    {
        return new OperationResult
        {
            Message = errorMessage,
            StatusCode = statusCode,
            IsSuccessful = false,
            Error = new ErrorResult(errorMessage, isShow)
        };
    }

    public static OperationResult Fail(ErrorResult errorResult, string message = "", int statusCode = 400)
    {
        return new OperationResult
        {
            Message = message,
            StatusCode = statusCode,
            IsSuccessful = false,
            Error = errorResult
        };
    }
}

public class OperationResult<T> : OperationResult, IOperationResult<T>
{
    public T Data { get; private set; }

    public static OperationResult<T> Success(T data, string message = "", int statusCode = 200)
    {
        return new OperationResult<T> { Data = data, Message = message, IsSuccessful = true, StatusCode = statusCode };
    }

    public new static OperationResult<T> Success(string message, int statusCode = 200)
    {
        return new OperationResult<T> { Data = default!, Message = message, IsSuccessful = true, StatusCode = statusCode };
    }

    public new static OperationResult<T> Fail(ErrorResult errorResult, string message = "", int statusCode = 400)
    {
        return new OperationResult<T>
        {
            Message = message,
            Error = errorResult,
            StatusCode = statusCode,
            IsSuccessful = false
        };
    }

    public new static OperationResult<T> Fail(string errorMessage, bool isShow = true, int statusCode = 400)
    {
        var errorDto = new ErrorResult(errorMessage, isShow);

        return new OperationResult<T> { Error = errorDto, Message = errorMessage, StatusCode = statusCode, IsSuccessful = false };
    }

    public static OperationResult<T> Fail(T data, ErrorResult errorResult, string message = "", int statusCode = 400)
    {
        return new OperationResult<T>
        {
            Message = message,
            Error = errorResult,
            StatusCode = statusCode,
            IsSuccessful = false,
            Data = data
        };
    }
}