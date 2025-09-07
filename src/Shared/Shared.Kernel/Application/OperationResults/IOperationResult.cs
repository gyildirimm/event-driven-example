namespace Shared.Kernel.Application.OperationResults;

public interface IOperationResult
{
    public string Message { get; }
    public bool IsSuccessful { get; }
    public ErrorResult Error { get; }
    public int StatusCode { get; }

    public string TraceId { get; }
}

public interface IOperationResult<T> : IOperationResult
{
    public T Data { get; }
}