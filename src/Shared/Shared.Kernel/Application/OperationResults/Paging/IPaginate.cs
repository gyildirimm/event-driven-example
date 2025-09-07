namespace Shared.Kernel.Application.OperationResults.Paging;

public interface IPaginate<T>
{
    int From { get; }
    int Index { get; }
    int Size { get; }
    int Count { get; }
    int Pages { get; }
    IList<T> Items { get; }
    bool HasPrevious { get; }
    bool HasNext { get; }
}

internal class Paginate<TSource, TResult> : IPaginate<TResult>
{
    public Paginate(IEnumerable<TSource> source, Func<IEnumerable<TSource>, IEnumerable<TResult>> converter,
                    int index, int size, int from)
    {
        var enumerable = source as TSource[] ?? source.ToArray();

        if (from > index) throw new ArgumentException($"From: {from} > Index: {index}, must From <= Index");

        if (source is IQueryable<TSource> queryable)
        {
            Index = index;
            Size = size;
            From = from;
            Count = queryable.Count();
            Pages = (int)Math.Ceiling(Count / (double)Size);

            var items = queryable.Skip((Index - From) * Size).Take(Size).ToArray();

            Items = new List<TResult>(converter(items));
        }
        else
        {
            Index = index;
            Size = size;
            From = from;
            Count = enumerable.Count();
            Pages = (int)Math.Ceiling(Count / (double)Size);

            var items = enumerable.Skip((Index - From) * Size).Take(Size).ToArray();

            Items = new List<TResult>(converter(items));
        }
    }


    public Paginate(IPaginate<TSource> source, Func<IEnumerable<TSource>, IEnumerable<TResult>> converter)
    {
        Index = source.Index;
        Size = source.Size;
        From = source.From;
        Count = source.Count;
        Pages = source.Pages;

        Items = new List<TResult>(converter(source.Items));
    }

    public int Index { get; }

    public int Size { get; }

    public int Count { get; }

    public int Pages { get; }

    public int From { get; }

    public IList<TResult> Items { get; }

    public bool HasPrevious => Index - From > 0;

    public bool HasNext => Index - From + 1 < Pages;
}

public static class Paginate
{
    public static IPaginate<T> Empty<T>()
    {
        return new Paginate<T>();
    }

    public static IPaginate<TResult> From<TResult, TSource>(IPaginate<TSource> source,
                                                            Func<IEnumerable<TSource>, IEnumerable<TResult>> converter)
    {
        return new Paginate<TSource, TResult>(source, converter);
    }
}

public static class IPaginateExtensions
{
    /// <summary>
    /// IPaginate içindeki item'ları başka bir tipe dönüştürür
    /// </summary>
    public static IPaginate<TResult> Map<TSource, TResult>(this IPaginate<TSource> source,
                                                          Func<TSource, TResult> mapper)
    {
        return Paginate.From(source, items => items.Select(mapper));
    }

    /// <summary>
    /// IPaginate içindeki item'ları async olarak başka bir tipe dönüştürür
    /// </summary>
    public static async Task<IPaginate<TResult>> MapAsync<TSource, TResult>(this IPaginate<TSource> source,
                                                                           Func<TSource, Task<TResult>> mapperAsync)
    {
        var mappedItems = new List<TResult>();
        foreach (var item in source.Items)
        {
            var mapped = await mapperAsync(item);
            mappedItems.Add(mapped);
        }

        return Paginate.From(source, _ => mappedItems);
    }
}