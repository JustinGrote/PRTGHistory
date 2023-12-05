using System.Collections;
using System.Diagnostics.CodeAnalysis;

public static class EnumerableExtensions
{

  public static IEnumerable<T> Cached<T>(this IEnumerable<T> source)
  {
    return new CachedEnumerable<T>(source);
  }
}

class CachedEnumerable<T> : IEnumerable<T>
{
  readonly object gate = new();

  readonly List<T> cache = [];

  IEnumerator<T> enumerator;

  bool isCacheComplete;

  public CachedEnumerable(IEnumerable<T> source)
  {
    enumerator = source.GetEnumerator();
  }

  public IEnumerator<T> GetEnumerator()
  {
    lock (gate)
    {
      if (isCacheComplete)
        return cache.GetEnumerator();
    }
    return GetCacheBuildingEnumerator();
  }

  public IEnumerator<T> GetCacheBuildingEnumerator()
  {
    int index = 0;
    T? item;
    while (TryGetItem(index, out item))
    {
      yield return item ?? throw new NullReferenceException("This type does not support nulls being returned by the enumerator");
      index++;
    }
  }

  bool TryGetItem(int index, out T? item)
  {
    lock (gate)
    {
      if (!IsItemInCache(index))
      {
        // The iteration may have completed while waiting for the lock.
        if (isCacheComplete)
        {
          item = default;
          return false;
        }
        if (!enumerator.MoveNext())
        {
          item = default;
          isCacheComplete = true;
          enumerator.Dispose();
          return false;
        }
        cache.Add(enumerator.Current);
      }
      item = cache[index];
      return true;
    }
  }

  bool IsItemInCache(int index)
  {
    return index < cache.Count;
  }

  IEnumerator IEnumerable.GetEnumerator()
  {
    return GetEnumerator();
  }
}
