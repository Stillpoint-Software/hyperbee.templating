namespace Hyperbee.Templating.Compiler;

// we expect methods with the signature
//
// delegate IConvertible TokenExpression( ReadOnlyDynamicDictionary tokens );
//
// but we will declare the input argument as dynamic so that the user can
// use dot notation for member access, and so we can support user defined
// dynamic methods

public delegate IConvertible TokenExpression( dynamic tokens );
