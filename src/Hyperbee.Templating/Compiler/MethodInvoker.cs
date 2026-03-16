
namespace Hyperbee.Templating.Compiler;

/// <summary>Defines a method that can be invoked from within template expressions.</summary>
public interface IMethodInvoker
{
    /// <summary>Invokes the method with the specified arguments.</summary>
    /// <param name="args">The arguments to pass to the method.</param>
    /// <returns>The result of the method invocation.</returns>
    object Invoke( params object[] args );
}

/// <summary>Wraps a delegate as an <see cref="IMethodInvoker"/> for use in template expressions.</summary>
public sealed class MethodInvoker( Func<object[], object> invoker ) : IMethodInvoker
{
    private readonly Func<object[], object> _invoker = invoker ?? throw new ArgumentNullException( nameof( invoker ) );

    /// <inheritdoc />
    public object Invoke( params object[] args ) => _invoker( args );
}
