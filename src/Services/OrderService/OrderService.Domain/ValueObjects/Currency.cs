using Shared.Kernel.Domain.DDD;

namespace OrderService.Domain.ValueObjects;

public sealed class Currency : ValueObject
{
    public static readonly Currency USD = new("USD");
    public static readonly Currency EUR = new("EUR");
    public static readonly Currency TRY = new("TRY");
    
    public string Code { get; init; }

    private Currency(string code)
    {
        Code = code;
    }

    public static Currency FromCode(string code)
    {
        return All.FirstOrDefault(x => x.Code == code) ?? throw new ArgumentException($"Code '{code}' is not a valid currency code.");
    }

    public static readonly IReadOnlyCollection<Currency> All = [TRY, USD, EUR];
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Code;
    }
}