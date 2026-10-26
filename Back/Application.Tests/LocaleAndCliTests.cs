using Cli;
using Core.Locales;

namespace Application.Tests;

/// <summary>
/// Locale codes follow <c>xx-XX</c>.
/// </summary>
public class LocaleCodeTests
{
    [Theory]
    [InlineData("en-US")]
    [InlineData("fr-FR")]
    [InlineData("ko-KR")]
    public void Accepts_xx_XX(string code)
    {
        Assert.True(LocaleCode.IsValid(code));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("en")]
    [InlineData("EN-us")]
    [InlineData("fr_FR")]
    public void Rejects_anything_else(string? code)
    {
        Assert.False(LocaleCode.IsValid(code));
    }

    [Fact]
    public void Catalog_keeps_real_locales_only()
    {
        Assert.Contains("fr-FR", LocaleCatalog.All);
        Assert.Contains("en-US", LocaleCatalog.All);
        Assert.Contains("ko-KR", LocaleCatalog.All);
        Assert.DoesNotContain("zz-ZZ", LocaleCatalog.All);
        Assert.DoesNotContain("en", LocaleCatalog.All);
    }
}

/// <summary>
/// The CLI stops before any network call when the token is missing, and never prints it.
/// </summary>
public class CommandLineTests
{
    [Fact]
    public void Help_does_not_need_a_token()
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        var code = CommandLine.Run(["--help"], token: null, stdout, stderr);

        Assert.Equal(0, code);
        Assert.Contains("push sources", stdout.ToString());
    }

    [Fact]
    public void Missing_token_stops_before_a_network_command()
    {
        var stderr = new StringWriter();

        var code = CommandLine.Run(["status"], token: " ", stdout: new StringWriter(), stderr);

        Assert.Equal(1, code);
        Assert.Contains("BABELARY_TOKEN", stderr.ToString());
    }

    [Fact]
    public void Output_never_contains_the_token()
    {
        const string token = "secret-token-value";
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        var code = CommandLine.Run(["pull"], token, stdout, stderr);
        var output = stdout.ToString() + stderr.ToString();

        Assert.Equal(1, code);
        Assert.DoesNotContain(token, output);
    }
}
