namespace sernick.Utility;

public class Multiset<K> where K : notnull
{
    private readonly Dictionary<K, int> _dict;
    public Multiset()
    {
        _dict = new Dictionary<K, int>();
    }

    public void Add(K elem)
    {
        _dict.TryGetValue(elem, out var value);
        _dict[elem] = value + 1;
    }

    public int this[K elem] => _dict.TryGetValue(elem, out var value) ? value : 0;
}
