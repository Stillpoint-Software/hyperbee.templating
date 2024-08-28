using Hyperbee.Templating.Compiler;

namespace Hyperbee.Templating.Configure;

public class MethodBuilder( string name, TemplateOptions options )
{
    public TemplateOptions Expression<TOutput>( Func<TOutput> func )
    {
        return SetInvoker( _ => func() );
    }

    public TemplateOptions Expression<TInput, TOutput>( Func<TInput, TOutput> func )
    {
        return SetInvoker( args => func( (TInput) args[0] ) );
    }

    public TemplateOptions Expression<TInput1, TInput2, TOutput>( Func<TInput1, TInput2, TOutput> func )
    {
        return SetInvoker( args => func( (TInput1) args[0], (TInput2) args[1] ) );
    }

    public TemplateOptions Expression<TInput1, TInput2, TInput3, TOutput>(
        Func<TInput1, TInput2, TInput3, TOutput> func )
    {
        return SetInvoker( args => func(
            (TInput1) args[0], (TInput2) args[1], (TInput3) args[2]
        ) );
    }

    public TemplateOptions Expression<TInput1, TInput2, TInput3, TInput4, TOutput>(
        Func<TInput1, TInput2, TInput3, TInput4, TOutput> func )
    {
        return SetInvoker( args => func(
            (TInput1) args[0], (TInput2) args[1], (TInput3) args[2], (TInput4) args[3]
        ) );
    }

    public TemplateOptions Expression<TInput1, TInput2, TInput3, TInput4, TInput5, TOutput>(
        Func<TInput1, TInput2, TInput3, TInput4, TInput5, TOutput> func )
    {
        return SetInvoker( args => func(
            (TInput1) args[0], (TInput2) args[1], (TInput3) args[2], (TInput4) args[3],
            (TInput5) args[4]
        ) );
    }

    public TemplateOptions Expression<TInput1, TInput2, TInput3, TInput4, TInput5, TInput6, TOutput>(
        Func<TInput1, TInput2, TInput3, TInput4, TInput5, TInput6, TOutput> func )
    {
        return SetInvoker( args => func(
            (TInput1) args[0], (TInput2) args[1], (TInput3) args[2], (TInput4) args[3],
            (TInput5) args[4], (TInput6) args[5]
        ) );
    }

    public TemplateOptions Expression<TInput1, TInput2, TInput3, TInput4, TInput5, TInput6, TInput7, TOutput>(
        Func<TInput1, TInput2, TInput3, TInput4, TInput5, TInput6, TInput7, TOutput> func )
    {
        return SetInvoker( args => func(
            (TInput1) args[0], (TInput2) args[1], (TInput3) args[2], (TInput4) args[3],
            (TInput5) args[4], (TInput6) args[5], (TInput7) args[6]
        ) );
    }

    public TemplateOptions Expression<TInput1, TInput2, TInput3, TInput4, TInput5, TInput6, TInput7, TInput8, TOutput>(
        Func<TInput1, TInput2, TInput3, TInput4, TInput5, TInput6, TInput7, TInput8, TOutput> func )
    {
        return SetInvoker( args => func(
            (TInput1) args[0], (TInput2) args[1], (TInput3) args[2], (TInput4) args[3],
            (TInput5) args[4], (TInput6) args[5], (TInput7) args[6], (TInput8) args[7]
        ) );

    }

    public TemplateOptions Expression<TInput1, TInput2, TInput3, TInput4, TInput5, TInput6, TInput7, TInput8, TInput9, TOutput>(
        Func<TInput1, TInput2, TInput3, TInput4, TInput5, TInput6, TInput7, TInput8, TInput9, TOutput> func )
    {
        return SetInvoker( args => func(
            (TInput1) args[0], (TInput2) args[1], (TInput3) args[2], (TInput4) args[3],
            (TInput5) args[4], (TInput6) args[5], (TInput7) args[6], (TInput8) args[7],
            (TInput9) args[8]
        ) );
    }

    private TemplateOptions SetInvoker( Func<object[], object> invoker )
    {
        options.Methods[name] = new MethodInvoker( invoker );
        return options;
    }
}
