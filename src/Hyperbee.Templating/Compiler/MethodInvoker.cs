
namespace Hyperbee.Templating.Compiler;

public interface IMethodInvoker
{
    object Invoke( params object[] args );
}

public sealed class MethodInvoker( Func<object[], object> invoker ) : IMethodInvoker
{
    private readonly Func<object[], object> _invoker = invoker ?? throw new ArgumentNullException( nameof( invoker ) );

    public object Invoke( params object[] args ) => _invoker( args );
}



