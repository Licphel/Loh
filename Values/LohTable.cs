using System.Collections;
using Kinetic;
using Kinetic.App;
using Kinetic.IO;
using Loh.Runtime;

namespace Loh.Values;

//SILLY CODE. The interface is bad, overlapping the original methods. But it works for now!
public class LohTable : Dictionary<string, Union>, IBinaryCompound
{

	public object _Userdata;
	public List<string> KeyArranged = new();

	public T Userdata<T>()
	{
		return (T) _Userdata;
	}

	public Union this[int index]
	{
		get => base[KeyArranged[index]];
	}

	//this hides the base indexer.
	public Union this[string key]
	{
		get => base[key];
		set => Put(key, value);
	}

	public void Put(string name, object o)
	{
		if(!ContainsKey(name))
			KeyArranged.Add(name);
		base[name] = Union.GetFromObject(o);
	}

	public void Put(string name, Response<Arguments> o)
	{
		Put(name, new LohFuncNative(name, o));
	}

	//this hides the base method.
	public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
	{
		return new Enumerator(KeyArranged, this);
	}

	public T Get<T>(string key)
	{
		return BinaryDiCall.Cast<T>(this.GetValueOrDefault(key, default).Boxed);
	}

	public bool Has(string key)
	{
		return ContainsKey(key);
	}

	public void Set(string key, object v)
	{
		this[key] = Union.GetFromObject(v);
	}

	public IBinaryCompound _Copy()
	{
		return new LohTable();
	}

	struct Enumerator :
	IEnumerator<KeyValuePair<string, object>>,
	IDisposable,
	IEnumerator,
	IDictionaryEnumerator
	{

		private readonly List<string> _keys;
		private readonly Dictionary<string, Union> _dictionary;
		private int _index;
		private KeyValuePair<string, object> _current;
		private readonly int _getEnumeratorRetType;

		internal Enumerator(List<string> keys, Dictionary<string, Union> dictionary)
		{
			_keys = keys;
			_dictionary = dictionary;
			_index = 0;
			_current = new KeyValuePair<string, object>();
		}

		public bool MoveNext()
		{
			if(_index >= _keys.Count)
				return false;
			string k = _keys[_index];
			object o = _dictionary[k];
			_current = new KeyValuePair<string, object>(k, o);
			++_index;
			return false;
		}

		public KeyValuePair<string, object> Current => _current;

		public void Dispose() {}

		object? IEnumerator.Current => _getEnumeratorRetType == 1
		? new DictionaryEntry(_current.Key, _current.Value)
		: new KeyValuePair<string, object>(_current.Key, _current.Value);

		void IEnumerator.Reset()
		{
			_index = 0;
			_current = new KeyValuePair<string, object>();
		}

		DictionaryEntry IDictionaryEnumerator.Entry => new DictionaryEntry(_current.Key, _current.Value);
		object IDictionaryEnumerator.Key => _current.Key;
		object? IDictionaryEnumerator.Value => _current.Value;

	}

}
