namespace Shared.Kernel.Application.OperationResults;

public class ErrorResult
{
    public List<string> Errors { get; private set; } = new List<string>();

    public bool IsShow { get; private set; }

    public ErrorResult(string error, bool isShow)
    {
        Errors.Add(error);
        IsShow = isShow;
    }

    public ErrorResult(List<string> errors, bool isShow)
    {
        Errors = errors;
        IsShow = isShow;
    }

    public static ErrorResult Create(string error, bool isShow = true, Exception? exception = null)
    {
        var response = new ErrorResult(error, isShow);

        if (exception != null)
            response.Errors.Add(exception.Message);

        return response;
    }

    public static ErrorResult Create(List<string> errors, bool isShow = true)
    {
        return new ErrorResult(errors, isShow);
    }
}