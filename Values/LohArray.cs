using System.Collections;
using KryptonM.IO;

namespace Loh.Values;

//SILLY CODE. The interface is bad, overlapping the original methods. But it works for now!
public class LohArray : List<Union>, IBinaryList
{

    //this hides the base indexer.
    public Union this[int index]
    {
        get => base[index];
        set
        {
            if(index >= Count)
            {
                if(index != Count)
                    for(int i = 0; i < index - Count; i++)
                        Add(Union.Null);
                Add(value);
            }
            else
            {
                base[index] = value;
            }
        }
    }

    //this hides the base method.
    public IEnumerator<object> GetEnumerator()
    {
        return new LohArrayEnumerator(this);
    }

    public T Get<T>(int i)
    {
        return BinaryDiCall.Cast<T>(this[i].Boxed);
    }

    public void Insert(object v)
    {
        Add(Union.GetFromObject(v));
    }

    public void Set(int i, object v)
    {
        this[i] = Union.GetFromObject(v);
    }

    public IBinaryList _Copy()
    {
        return new LohArray();
    }

}

internal struct LohArrayEnumerator : IEnumerator<object>, IDisposable, IEnumerator
{

    private readonly List<Union> _list;
    private int _index;
    private Union _current;

    internal LohArrayEnumerator(List<Union> list)
    {
        _list = list;
        _index = 0;
        _current = default;
    }

    public void Dispose()
    {
    }

    public bool MoveNext()
    {
        if(_index >= _list.Count)
            return false;
        _current = _list[_index];
        ++_index;
        return true;
    }

    private bool MoveNextRare()
    {
        _index = _list.Count + 1;
        _current = default;
        return false;
    }

    public object Current => _current.Boxed;

    object? IEnumerator.Current => Current;

    void IEnumerator.Reset()
    {
        _index = 0;
        _current = default;
    }

}