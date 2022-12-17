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
        if (_dict.TryGetValue(elem, out var value))
        {
            _dict[elem] = value + 1;
        }
        else
        {
            _dict[elem] = 1;
        }
    }

    public int Get(K elem)
    {
        return _dict.TryGetValue(elem, out var value) ? value : 0;
    }
}
