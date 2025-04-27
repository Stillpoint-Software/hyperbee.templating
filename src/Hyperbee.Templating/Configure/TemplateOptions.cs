using System.ComponentModel;
using System.Reflection;
using Hyperbee.Templating.Compiler;
using Hyperbee.Templating.Core;
using Hyperbee.Templating.Text;

namespace Hyperbee.Templating.Configure;

public class TemplateOptions
{
    public static TemplateOptions Create() => new();

    public IDictionary<string, IMethodInvoker> Methods { get; }
    public IDictionary<string, string> Variables { get; init; }

    public TokenStyle TokenStyle { get; set; } = TokenStyle.Default;
    public KeyValidator Validator { get; set; } = KeyHelper.ValidateKey;

    public bool IgnoreMissingTokens { get; set; }
    public bool SubstituteEnvironmentVariables { get; set; }
    public int MaxTokenDepth { get; set; } = 20;

    public ITokenExpressionProvider TokenExpressionProvider { get; set; } = new RoslynTokenExpressionProvider();
    public Action<TemplateParser, TemplateEventArgs> TokenHandler { get; set; } = null;

    public TemplateOptions()
        : this( null )
    {
    }

    public TemplateOptions( IDictionary<string, string> variables )
    {
        Methods = new Dictionary<string, IMethodInvoker>( StringComparer.OrdinalIgnoreCase );
        Variables = variables ?? new Dictionary<string, string>( StringComparer.OrdinalIgnoreCase );
    }

    public TemplateOptions AddVariable( string key, string value )
    {
        Variables[key] = value;
        return this;
    }

    public TemplateOptions AddVariables( IEnumerable<KeyValuePair<string, string>> variables )
    {
        foreach ( var (key, value) in variables )
        {
            if ( string.IsNullOrWhiteSpace( key ) || value == null )
                continue;

            Variables.Add( key, value );
        }

        return this;
    }

    public TemplateOptions AddVariables( object variableObject )
    {
        const BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.InvokeMethod | BindingFlags.IgnoreCase;

        ArgumentNullException.ThrowIfNull( variableObject );

        var type = variableObject.GetType();

        foreach ( var member in type.GetMembers( bindingFlags ) )
        {
            object value;

            switch ( member.MemberType )
            {
                case MemberTypes.Property:
                    value = ((PropertyInfo) member).GetValue( variableObject, null );
                    break;
                case MemberTypes.Field:
                    value = ((FieldInfo) member).GetValue( variableObject );
                    break;
                default:
                    continue;
            }

            var valueType = value?.GetType();

            if ( valueType != null && (valueType.IsPrimitive || valueType == typeof( string )) )
                AddVariable( member.Name, value.ToString() );
        }

        return this;
    }

    public MethodBuilder AddMethod( string name ) => new( name, this );

    public TemplateOptions SetIgnoreMissingTokens( bool ignoreMissingTokens )
    {
        IgnoreMissingTokens = ignoreMissingTokens;
        return this;
    }

    public TemplateOptions SetMaxTokenDepth( int maxTokenDepth )
    {
        MaxTokenDepth = maxTokenDepth;
        return this;
    }

    public TemplateOptions SetSubstituteEnvironmentVariables( bool substituteEnvironmentVariables )
    {
        SubstituteEnvironmentVariables = substituteEnvironmentVariables;
        return this;
    }

    public TemplateOptions SetTokenExpressionProvider( ITokenExpressionProvider expressionProvider )
    {
        TokenExpressionProvider = expressionProvider;
        return this;
    }

    public TemplateOptions SetTokenHandler( Action<TemplateParser, TemplateEventArgs> tokenHandler )
    {
        TokenHandler = tokenHandler;
        return this;
    }

    public TemplateOptions SetTokenStyle( TokenStyle tokenStyle )
    {
        TokenStyle = tokenStyle;
        return this;
    }

    public TemplateOptions SetValidator( KeyValidator validator )
    {
        Validator = validator;
        return this;
    }

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
