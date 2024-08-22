using System.ComponentModel;
using Hyperbee.Templating.Compiler;
using Hyperbee.Templating.Text;

namespace Hyperbee.Templating.Configure;

public class TemplateConfig
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

    public TemplateConfig()
        : this(null)
    {
    }

    public TemplateConfig( IDictionary<string, string> source )
    {
        Methods = new Dictionary<string, IMethodInvoker>( StringComparer.OrdinalIgnoreCase );
        Tokens = source ?? new Dictionary<string, string>( StringComparer.OrdinalIgnoreCase );
    }

    public void AddToken( string key, string value ) => Tokens[key] = value;

    public MethodBuilder AddMethod( string name ) => new ( name, this );

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
                throw new InvalidEnumArgumentException( nameof(TokenStyle), (int) TokenStyle, typeof(TokenStyle) );
        }

        return (tokenLeft, tokenRight);
    }
}
