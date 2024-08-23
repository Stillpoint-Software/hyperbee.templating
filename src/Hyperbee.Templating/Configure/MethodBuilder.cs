using Hyperbee.Templating.Compiler;

namespace Hyperbee.Templating.Configure;

public class MethodBuilder
{
    private readonly string _name;
    private readonly TemplateOptions _options;

    public MethodBuilder( string name, TemplateOptions options )
    {
        _name = name;
        _options = options;
    }

    public TemplateOptions Expression<TOutput>( Func<TOutput> func )
    {
        var invoker = new MethodInvoker( _ => func() );
        _options.Methods[_name] = invoker;
        return _options;
    }


    public TemplateOptions Expression<TInput, TOutput>( Func<TInput, TOutput> func )
    {
        var invoker = new MethodInvoker( args => func( (TInput) args[0] ) );
        _options.Methods[_name] = invoker;
        return _options;
    }

    public TemplateOptions Expression<TInput1, TInput2, TOutput>( Func<TInput1, TInput2, TOutput> func )
    {
        var invoker = new MethodInvoker( args => func( (TInput1) args[0], (TInput2) args[1] ) );
        _options.Methods[_name] = invoker;
        return _options;
    }

    public TemplateOptions Expression<TInput1, TInput2, TInput3, TOutput>( Func<TInput1, TInput2, TInput3, TOutput> func )
    {
        var invoker = new MethodInvoker( args => func( (TInput1) args[0], (TInput2) args[1], (TInput3) args[2] ) );
        _options.Methods[_name] = invoker;
        return _options;
    }

    public TemplateOptions Expression<TInput1, TInput2, TInput3, TInput4, TOutput>( Func<TInput1, TInput2, TInput3, TInput4, TOutput> func )
    {
        var invoker = new MethodInvoker( args => func( (TInput1) args[0], (TInput2) args[1], (TInput3) args[2], (TInput4) args[3] ) );
        _options.Methods[_name] = invoker;
        return _options;
    }

    public TemplateOptions Expression<TInput1, TInput2, TInput3, TInput4, TInput5, TOutput>( Func<TInput1, TInput2, TInput3, TInput4, TInput5, TOutput> func )
    {
        var invoker = new MethodInvoker( args => func( (TInput1) args[0], (TInput2) args[1], (TInput3) args[2], (TInput4) args[3], (TInput5) args[4] ) );
        _options.Methods[_name] = invoker;
        return _options;
    }

    public TemplateOptions Expression<TInput1, TInput2, TInput3, TInput4, TInput5, TInput6, TOutput>( Func<TInput1, TInput2, TInput3, TInput4, TInput5, TInput6, TOutput> func )
    {
        var invoker = new MethodInvoker( args => func( (TInput1) args[0], (TInput2) args[1], (TInput3) args[2], (TInput4) args[3], (TInput5) args[4], (TInput6) args[5] ) );
        _options.Methods[_name] = invoker;
        return _options;
    }

    public TemplateOptions Expression<TInput1, TInput2, TInput3, TInput4, TInput5, TInput6, TInput7, TOutput>( Func<TInput1, TInput2, TInput3, TInput4, TInput5, TInput6, TInput7, TOutput> func )
    {
        var invoker = new MethodInvoker( args => func( (TInput1) args[0], (TInput2) args[1], (TInput3) args[2], (TInput4) args[3], (TInput5) args[4], (TInput6) args[5], (TInput7) args[6] ) );
        _options.Methods[_name] = invoker;
        return _options;
    }

    public TemplateOptions Expression<TInput1, TInput2, TInput3, TInput4, TInput5, TInput6, TInput7, TInput8, TOutput>( Func<TInput1, TInput2, TInput3, TInput4, TInput5, TInput6, TInput7, TInput8, TOutput> func )
    {
        var invoker = new MethodInvoker( args => func( (TInput1) args[0], (TInput2) args[1], (TInput3) args[2], (TInput4) args[3], (TInput5) args[4], (TInput6) args[5], (TInput7) args[6], (TInput8) args[7] ) );
        _options.Methods[_name] = invoker;
        return _options;
    }

    public TemplateOptions Expression<TInput1, TInput2, TInput3, TInput4, TInput5, TInput6, TInput7, TInput8, TInput9, TOutput>( Func<TInput1, TInput2, TInput3, TInput4, TInput5, TInput6, TInput7, TInput8, TInput9, TOutput> func )
    {
        var invoker = new MethodInvoker( args => func( (TInput1) args[0], (TInput2) args[1], (TInput3) args[2], (TInput4) args[3], (TInput5) args[4], (TInput6) args[5], (TInput7) args[6], (TInput8) args[7], (TInput9) args[8] ) );
        _options.Methods[_name] = invoker;
        return _options;
    }
}
