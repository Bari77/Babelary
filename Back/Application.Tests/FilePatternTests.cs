using Core.Exceptions;
using Core.Projects;

namespace Application.Tests;

public class FilePatternTests
{
    [Fact]
    public void Folder_pattern_keeps_the_file_name_and_adds_the_locale_folder()
    {
        Assert.True(FilePattern.TryMatch(FilePattern.Folder, "en-US", "en-US", "en-US/home.json", out var stored));
        Assert.Equal("home.json", stored);
        Assert.Equal("fr-FR/home.json", FilePattern.ExportPath(FilePattern.Folder, "fr-FR", "en-US", stored));
    }

    [Fact]
    public void Locale_file_is_renamed_to_the_exported_language()
    {
        Assert.True(FilePattern.TryMatch(FilePattern.LocaleFile, "en-US", "en-US", "en-US.json", out var stored));
        Assert.Equal("en-US.json", stored);
        Assert.Equal("fr-FR.json", FilePattern.ExportPath(FilePattern.LocaleFile, "fr-FR", "en-US", stored));
    }

    [Fact]
    public void Name_can_sit_beside_the_locale()
    {
        const string pattern = "{name}.{locale}.json";
        Assert.True(FilePattern.TryMatch(pattern, "fr-FR", "en-US", "home.fr-FR.json", out var stored));
        Assert.Equal("home.json", stored);
        Assert.Equal("home.en-US.json", FilePattern.ExportPath(pattern, "en-US", "en-US", stored));
    }

    [Fact]
    public void A_single_file_pattern_leaves_another_stored_name_alone()
    {
        Assert.Equal("home.json", FilePattern.ExportPath(FilePattern.LocaleFile, "fr-FR", "en-US", "home.json"));
    }

    [Fact]
    public void Star_is_a_file_wildcard()
    {
        Assert.Equal("{locale}/{name}.json", FilePattern.Normalize("{locale}/*.json"));
        Assert.Equal("{locale}/{file}", FilePattern.Normalize("{locale}/*"));
    }

    [Theory]
    [InlineData("{name}.json")]
    [InlineData("{locale}/{locale}.json")]
    [InlineData("../{locale}.json")]
    [InlineData("{locale}/{name}.txt")]
    public void Invalid_patterns_are_refused(string pattern)
    {
        var error = Assert.Throws<BadRequestException>(() => FilePattern.Normalize(pattern));
        Assert.Equal("ERR_FILE_PATTERN_INVALID", error.Message);
    }
}
