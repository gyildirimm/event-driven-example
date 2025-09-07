using Shared.Kernel.Domain.DDD;

namespace OrderService.Domain.ValueObjects;

public sealed class Money : ValueObject
{
    public decimal Amount { get; }
    public Currency Currency { get; }

    public Money(decimal amount, Currency currency)
    {
        if (amount < 0)
            throw new ArgumentException("Amount cannot be negative", nameof(amount));

        Amount = Math.Round(amount, 2);
        Currency = currency ?? throw new ArgumentNullException(nameof(currency));
    }
    
    public Money(decimal amount, string currencyCode = "TRY")
        : this(amount, Currency.FromCode(currencyCode))
    {
    }

    public static Money Zero(Currency currency) => new(0, currency);
    public static Money Zero(string currencyCode = "TRY") => new(0, currencyCode);


    public static Money USD(decimal amount) => new(amount, Currency.USD);
    public static Money EUR(decimal amount) => new(amount, Currency.EUR);
    public static Money TRY(decimal amount) => new(amount, Currency.TRY);

    public static Money operator +(Money left, Money right)
    {
        if (!left.Currency.Equals(right.Currency))
            throw new InvalidOperationException($"Cannot add money with different currencies: {left.Currency.Code} and {right.Currency.Code}");

        return new Money(left.Amount + right.Amount, left.Currency);
    }

    public static Money operator -(Money left, Money right)
    {
        if (!left.Currency.Equals(right.Currency))
            throw new InvalidOperationException($"Cannot subtract money with different currencies: {left.Currency.Code} and {right.Currency.Code}");

        return new Money(left.Amount - right.Amount, left.Currency);
    }

    public static Money operator *(Money money, decimal multiplier)
    {
        return new Money(money.Amount * multiplier, money.Currency);
    }

    public static Money operator *(decimal multiplier, Money money)
    {
        return money * multiplier;
    }

    public static Money operator /(Money money, decimal divisor)
    {
        if (divisor == 0)
            throw new DivideByZeroException("Cannot divide money by zero");

        return new Money(money.Amount / divisor, money.Currency);
    }

    public static bool operator >(Money left, Money right)
    {
        if (!left.Currency.Equals(right.Currency))
            throw new InvalidOperationException($"Cannot compare money with different currencies: {left.Currency.Code} and {right.Currency.Code}");

        return left.Amount > right.Amount;
    }

    public static bool operator <(Money left, Money right)
    {
        if (!left.Currency.Equals(right.Currency))
            throw new InvalidOperationException($"Cannot compare money with different currencies: {left.Currency.Code} and {right.Currency.Code}");

        return left.Amount < right.Amount;
    }

    public static bool operator >=(Money left, Money right)
    {
        return left > right || left.Equals(right);
    }

    public static bool operator <=(Money left, Money right)
    {
        return left < right || left.Equals(right);
    }

    public override string ToString()
    {
        return $"{Amount:F2} {Currency.Code}";
    }

    // Explicit conversion to decimal
    public static explicit operator decimal(Money money)
    {
        return money.Amount;
    }

    // Currency conversion method (placeholder - would integrate with exchange rate service)
    public Money ConvertTo(Currency targetCurrency, decimal exchangeRate)
    {
        if (Currency.Equals(targetCurrency))
            return this;

        return new Money(Amount * exchangeRate, targetCurrency);
    }
    
    public bool HasSameCurrency(Money other)
    {
        return Currency.Equals(other.Currency);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }
}
