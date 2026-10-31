using Core.Exceptions;
using Core.Projects;

namespace Application.Tests;

/// <summary>
/// Angular catalogs keep dotted message ids, and both token styles are checked.
/// </summary>
public class CatalogTests
{
    [Fact]
    public void Angular_ids_stay_flat_including_a_shorter_key()
    {
        const string json = """
            {
              "locale": "en-US",
              "translations": {
                "core.breadcrumb.moderation": "Moderation",
                "core.breadcrumb.moderation.users": "Users {$PH}"
              }
            }
            """;

        var parsed = SourceDocument.Parse(json, allowFlatKeys: false);

        Assert.True(parsed.Angular);
        Assert.Equal("Moderation", parsed.Leaves["core.breadcrumb.moderation"]);
        Assert.Equal("Users {$PH}", parsed.Leaves["core.breadcrumb.moderation.users"]);

        using var written = System.Text.Json.JsonDocument.Parse(SourceDocument.Write(parsed.Leaves, "fr-FR"));
        Assert.Equal("fr-FR", written.RootElement.GetProperty("locale").GetString());
        Assert.Equal(2, written.RootElement.GetProperty("translations").EnumerateObject().Count());
    }

    [Fact]
    public void Nested_json_is_unchanged()
    {
        var parsed = SourceDocument.Parse("""{ "ACCOUNT": { "LOGIN": "User" } }""", allowFlatKeys: false);

        Assert.False(parsed.Angular);
        Assert.Equal("User", parsed.Leaves["ACCOUNT.LOGIN"]);
    }

    [Fact]
    public void Angular_placeholder_must_survive_the_translation()
    {
        Placeholders.RequireSame("Users {$PH}", "Utilisateurs {$PH}");

        var missing = Assert.Throws<BadRequestException>(() => Placeholders.RequireSame("Users {$PH}", "Utilisateurs"));

        Assert.Equal("ERR_PLACEHOLDER_MISSING", missing.Message);
        Assert.Equal("{$PH}", missing.Detail);
    }
}
