using System.ComponentModel;
using System.Reflection;
using Hyperbee.Templating.Compiler;
using Hyperbee.Templating.Text;

namespace Hyperbee.Templating.Configure;

public class TemplateOptions
{
    public IDictionary<string, IMethodInvoker> Methods { get; }
    public IDictionary<string, string> Tokens { get; init; }

    public TokenStyle TokenStyle { get; set; } = TokenStyle.Default;
    public KeyValidator Validator { get; set; } = TemplateHelper.ValidateKey;

    public bool IgnoreMissingTokens { get; set; } = false;
    public bool SubstituteEnvironmentVariables { get; set; } = false;
    public int MaxTokenDepth { get; set; } = 20;

    public ITokenExpressionProvider TokenExpressionProvider { get; set; } = new RoslynTokenExpressionProvider();
    public Action<TemplateParser, TemplateEventArgs> TokenHandler { get; set; } = null;

    public TemplateOptions()
        : this( null )
    {
    }

    public TemplateOptions( IDictionary<string, string> source )
    {
        Methods = new Dictionary<string, IMethodInvoker>( StringComparer.OrdinalIgnoreCase );
        Tokens = source ?? new Dictionary<string, string>( StringComparer.OrdinalIgnoreCase );
    }

    public TemplateOptions AddToken( string key, string value )
    {
        Tokens[key] = value;
        return this;
    }

    public TemplateOptions AddTokens( IEnumerable<KeyValuePair<string, string>> tokens )
    {
        foreach ( var (key, value) in tokens )
        {
            if ( string.IsNullOrWhiteSpace( key ) || value == null )
                continue;

            Tokens.Add( key, value );
        }

        return this;
    }

    public TemplateOptions AddTokens( object tokenObject )
    {
        const BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.InvokeMethod | BindingFlags.IgnoreCase;

        ArgumentNullException.ThrowIfNull( tokenObject );

        var type = tokenObject.GetType();

        foreach ( var member in type.GetMembers( bindingFlags ) )
        {
            object value;

            switch ( member.MemberType )
            {
                case MemberTypes.Property:
                    value = ((PropertyInfo) member).GetValue( tokenObject, null );
                    break;
                case MemberTypes.Field:
                    value = ((PropertyInfo) member).GetValue( tokenObject, null );
                    break;
                default:
                    continue;
            }

            var valueType = value?.GetType();

            if ( valueType != null && (valueType.IsPrimitive || valueType == typeof( string )) )
                AddToken( member.Name, value.ToString() );
        }

        return this;
    }

    public MethodBuilder AddMethod( string name ) => new( name, this );

    public (string TokenLeft, string TokenRight) TokenDelimiters()
    {
        string tokenLeft;
        string tokenRight;

        switch ( TokenStyle )
        {
            case TokenStyle.Default:
            case TokenStyle.DoubleBrace:
                tokenLeft = "{{";
                tokenRight = "}}";
                break;
            case TokenStyle.SingleBrace:
                tokenLeft = "{";
                tokenRight = "}";
                break;
            case TokenStyle.PoundBrace:
                tokenLeft = "#{";
                tokenRight = "}";
                break;
            case TokenStyle.DollarBrace:
                tokenLeft = "${";
                tokenRight = "}";
                break;
            default:
                throw new InvalidEnumArgumentException( nameof( TokenStyle ), (int) TokenStyle, typeof( TokenStyle ) );
        }

        return (tokenLeft, tokenRight);
    }
}
