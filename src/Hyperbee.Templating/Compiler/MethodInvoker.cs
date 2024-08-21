using System.Linq.Expressions;

namespace Hyperbee.Templating.Compiler;

public interface IMethodInvoker
{
    object Invoke( params object[] args );
}

public sealed class MethodInvoker : IMethodInvoker
{
    private readonly Func<object[], object> _invoker;

    public MethodInvoker( Delegate method )
    {
        ArgumentNullException.ThrowIfNull( method, nameof( method ) );

        _invoker = CreateInvoker( method );
    }

    private static Func<object[], object> CreateInvoker( Delegate method )
    {
        var parameters = method.Method.GetParameters();
        var arguments = new[] { Expression.Parameter( typeof( object[] ), "args" ) };
        var callArguments = new Expression[parameters.Length];

        for ( var i = 0; i < parameters.Length; i++ )
        {
            var index = Expression.Constant( i );
            var parameterType = parameters[i].ParameterType;

            var parameterAccessor = Expression.ArrayIndex( arguments[0], index );
            var parameterCast = Expression.Convert( parameterAccessor, parameterType );

            callArguments[i] = parameterCast;
        }

        var instance = Expression.Constant( method.Target );
        var methodCall = Expression.Call( instance, method.Method, callArguments );

        var lambda = Expression.Lambda<Func<object[], object>>(
            Expression.Convert( methodCall, typeof( object ) ), arguments );

        return lambda.Compile();
    }

    public object Invoke( params object[] args ) => _invoker( args );
}

// Factory for creating method invokers

public static class Method
{
    public static IMethodInvoker Create<TInput, TOutput>(
        Func<TInput, TOutput> method )
    {
        return new MethodInvoker( method );
    }

    public static IMethodInvoker Create<TInput1, TInput2, TOutput>(
        Func<TInput1, TInput2, TOutput> method )
    {
        return new MethodInvoker( method );
    }

    public static IMethodInvoker Create<TInput1, TInput2, TInput3, TOutput>(
        Func<TInput1, TInput2, TInput3, TOutput> method )
    {
        return new MethodInvoker( method );
    }

    public static IMethodInvoker Create<TInput1, TInput2, TInput3, TInput4, TOutput>(
        Func<TInput1, TInput2, TInput3, TInput4, TOutput> method )
    {
        return new MethodInvoker( method );
    }

    public static IMethodInvoker Create<TInput1, TInput2, TInput3, TInput4, TInput5, TOutput>(
        Func<TInput1, TInput2, TInput3, TInput4, TInput5, TOutput> method )
    {
        return new MethodInvoker( method );
    }

    public static IMethodInvoker Create<TInput1, TInput2, TInput3, TInput4, TInput5, TInput6, TOutput>(
        Func<TInput1, TInput2, TInput3, TInput4, TInput5, TInput6, TOutput> method )
    {
        return new MethodInvoker( method );
    }

    public static IMethodInvoker Create<TInput1, TInput2, TInput3, TInput4, TInput5, TInput6, TInput7, TOutput>(
        Func<TInput1, TInput2, TInput3, TInput4, TInput5, TInput6, TInput7, TOutput> method )
    {
        return new MethodInvoker( method );
    }

    public static IMethodInvoker Create<TInput1, TInput2, TInput3, TInput4, TInput5, TInput6, TInput7, TInput8, TOutput>(
        Func<TInput1, TInput2, TInput3, TInput4, TInput5, TInput6, TInput7, TInput8, TOutput> method )
    {
        return new MethodInvoker( method );
    }

    public static IMethodInvoker Create<TInput1, TInput2, TInput3, TInput4, TInput5, TInput6, TInput7, TInput8, TInput9, TOutput>(
        Func<TInput1, TInput2, TInput3, TInput4, TInput5, TInput6, TInput7, TInput8, TInput9, TOutput> method )
    {
        return new MethodInvoker( method );
    }
}
