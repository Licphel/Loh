namespace Loh.Compile;

public enum CPriority
{

	None,
	Assign,	// let a = 1
	Or,		// or
	And,	// and
	Equal,	// == or !=
	Compare,// > or < or >= or <=
	Term,	// + or -
	Factor,	// * or
	Tight,	// ^ or %
	Unary,	// ! or -
	Call,	//obj.x or obj()
	Primary

}