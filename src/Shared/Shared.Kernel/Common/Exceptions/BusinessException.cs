using Newtonsoft.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Shared.Kernel.Common.Exceptions;

public class BusinessException : Exception
{
    public BusinessException() { }

    public BusinessException(string? message)
        : base(message) { }

    public BusinessException(string? message, Exception? innerException)
        : base(message, innerException) { }

    public BusinessException(string? message, object? obj)
        : base(ModifyMessage(message, obj)) {  
    }

    private static string ModifyMessage(string? message, object? obj)
    {
        string errMessage = message ?? string.Empty;
        errMessage += "\n------------------------------------------ \n";
        if (obj is not null) errMessage += JsonSerializer.Serialize(obj);

        return errMessage;
    }
}