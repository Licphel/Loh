namespace Loh.Values;

public unsafe readonly struct Union
{

	public readonly float Number;
	public readonly bool Bool;
	public readonly object Objref;
	public readonly byte Type;

	public Union()
	{
		Type = 0;
	}

	public Union(double number)
	{
		Number = (float) number;
		Type = 1;
	}

	public Union(float number)
	{
		Number = number;
		Type = 1;
	}

	public Union(bool boolean)
	{
		Bool = boolean;
		Type = 2;
	}

	public Union(object uo)
	{
		if(uo == null)
		{
			Type = 0;
			return;
		}
		if(uo is Union ev)
		{
			Objref = ev.Objref;
			Type = 3;
			return;
		}
		Objref = uo;
		Type = 3;
	}

	public static Union GetFromObject(in object o)
	{
		if(o is Union ev) return ev;
		if(o is double d) return new Union(d);
		if(o is float f) return new Union(f);
		if(o is int i) return new Union(i);
		if(o is bool b) return new Union(b);
		if(o is null) return Null;
		return new Union(o);
	}

	public bool IsNull => Type == 0;
	public bool IsNumber => Type == 1;
	public bool IsBool => Type == 2;
	public bool IsObj => Type == 3;

	public bool AsBool
	{
		get
		{
			if(IsBool) return Bool;
			if(IsNumber)
			{
				if(Number == 1) return true;
				if(Number == 0) return false;
				throw new Exception($"Cannot dynamically cast {ToString()} to a boolean.");
			}
			return !IsNull;
		}
	}

	public int AsInt => (int) AsFloat;

	public float AsFloat
	{
		get
		{
			if(IsNumber) return Number;
			if(IsBool) return Bool ? 1 : 0;
			if(IsNull) return 0;
			throw new Exception($"Cannot dynamically cast {ToString()} to a number.");
		}
	}

	public string AsString
	{
		get
		{
			if(IsNumber) return Number.ToString();
			if(IsBool) return Bool ? "true" : "false";
			if(IsNull) return "null";
			return Objref.ToString();
		}
	}

	public object Boxed
	{
		get
		{
			if(IsNumber)
			{
				// We should be careful to distinguish if it is an int,
				// since we might soon change it into a binary value, where type is strict.
				// And why C# DOES NOT automatically cast floats to ints???
				if(float.IsInteger(Number))
					return (int) Number;
				return Number;
			}
			if(IsBool) return Bool;
			if(IsNull) return null;
			return Objref;
		}
	}

	public dynamic Dynamic => Boxed;

	public override string ToString()
	{
		return AsString;
	}

	public static readonly Union Null = new Union();
	public static readonly Union True = new Union(true);
	public static readonly Union False = new Union(false);

	public static bool operator ==(Union u1, Union u2)
	{
		if(u1.Type != u2.Type) return false;
		if(u1.IsNumber && u2.IsNumber)
		{
			double v = u1.Number - u2.Number;
			return v > -10E-6 && v < 10E-6;
		}
		if(u1.IsBool && u2.IsBool) return u1.Bool == u2.Bool;
		if(u1.IsObj && u2.IsObj) return u1.Objref == u2.Objref;
		return u1.IsNull && u2.IsNull;
	}

	public static bool operator !=(Union u1, Union u2)
	{
		return !(u1 == u2);
	}

}
