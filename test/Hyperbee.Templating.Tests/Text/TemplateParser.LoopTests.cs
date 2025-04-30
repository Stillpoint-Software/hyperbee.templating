using Hyperbee.Templating.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hyperbee.Templating.Tests.Text;

[TestClass]
public class TemplateParserLoopTests
{
    [TestMethod]
    public void Should_honor_while_condition()
    {
        // arrange
        const string expression = "{{while x => int.Parse(x.counter) < 3}}{{counter}}{{counter:{{x => int.Parse(x.counter) + 1}}}}{{/while}}";

        const string template = $"count: {expression}.";

        // act
        var result = Template.Render( template, new() 
        {
            Variables =
            {
                ["counter"] = "0"
            }
        } );

        // assert
        const string expected = "count: 012.";

        Assert.AreEqual( expected, result );
    }

    [TestMethod]
    public void Should_honor_each_expression()
    {
        // arrange
        const string expression = "{{each n:x => x.list.Split( \",\" )}}World {{n}},{{/each}}";

        const string template = $"hello {expression}.";

        // act
        var result = Template.Render( template, new() 
        { 
            Variables = 
            { 
                ["list"] = "1,2,3" 
            } 
        } );

        // assert
        const string expected = "hello World 1,World 2,World 3,.";

        Assert.AreEqual( expected, result );
    }

    [TestMethod]
    public void Should_honor_each_expression_RegEx()
    {
        // arrange
        const string expression = "{{each n:x => x.Where( t => Regex.IsMatch( t.Key, \"people*\" ) ).Select( t => t.Value )}}hello {{n}}. {{/each}}";

        const string template = $"{expression}";

        // act
        var result = Template.Render( template, new() 
        {
            Variables =
            {
                ["people[0]"] = "John",
                ["people[1]"] = "Jane",
                ["people[2]"] = "Doe"
            }
        } );

        // assert
        const string expected = "hello John. hello Jane. hello Doe. ";

        Assert.AreEqual( expected, result );
    }

    [TestMethod]
    public void Should_honor_each_Key()
    {
        // arrange
        const string expression = "{{each n:x => x.Where( t => Regex.IsMatch( t.Key, \"people*\" ) ).Select( t => t.Value )}}hello {{n}}. {{/each}}";

        const string template = $"{expression}";

        // act
        var result = Template.Render( template, new() 
        {
            Variables =
            {
                ["people[0]"] = "John",
                ["people[1]"] = "Jane",
                ["people[2]"] = "Doe"
            }
        } );

        // assert
        const string expected = "hello John. hello Jane. hello Doe. ";

        Assert.AreEqual( expected, result );
    }
}
