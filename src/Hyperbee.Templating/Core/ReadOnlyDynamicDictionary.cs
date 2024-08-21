using Hyperbee.Templating.Compiler;
using Hyperbee.Templating.Text;

namespace Hyperbee.Templating.Core;

public class ReadOnlyTokenDictionary : TemplateDictionary
{
    private IReadOnlyDictionary<string, IMethodInvoker> Methods { get; }

    public ReadOnlyTokenDictionary( TemplateDictionary tokens, IReadOnlyDictionary<string, IMethodInvoker> methods = null )
        : base( tokens.Validator, tokens.Variables )
    {
        Methods = methods ?? new Dictionary<string, IMethodInvoker>( StringComparer.OrdinalIgnoreCase );
    }

    public ReadOnlyTokenDictionary( IDictionary<string,string> tokens, IReadOnlyDictionary<string, IMethodInvoker> methods = null )
        : base( TemplateHelper.ValidateKey, tokens )
    {
        Methods = methods ?? new Dictionary<string, IMethodInvoker>( StringComparer.OrdinalIgnoreCase );
    }

    public TType Value<TType>( string name ) where TType : IConvertible
    {
        return (TType) Convert.ChangeType( this[name], typeof(TType) );
    }

    public object InvokeMethod( string methodName, params object[] args )
    {
        if ( !Methods.TryGetValue( methodName, out var methodInvoker ) )
            throw new MissingMethodException( $"Failed to invoke method '{methodName}'." );

        return methodInvoker.Invoke( args );
    }
}
