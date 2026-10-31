using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Core.Exceptions;

namespace Core.Projects;

/// <summary>
/// Reads a source JSON file into key paths. Nested objects become dotted paths.
/// </summary>
public static class SourceDocument
{
    /// <summary>Maximum length of a key path.</summary>
    public const int MaxPathLength = 400;

    /// <summary>
    /// Returns the string leaves in document order. A group such as <c>ACCOUNT</c> is not a key.
    /// An Angular catalog keeps each translation id whole, including dots.
    /// </summary>
    /// <param name="json">Raw JSON object.</param>
    public static Dictionary<string, string> Read(string? json) => Parse(json, allowFlatKeys: false).Leaves;

    /// <summary>
    /// Reads a translation file. Nested objects, flat dotted keys, and Angular catalogs all count.
    /// </summary>
    /// <param name="json">Raw JSON object.</param>
    public static Dictionary<string, string> ReadTranslations(string? json) => Parse(json, allowFlatKeys: true).Leaves;

    /// <summary>
    /// Reads a source or translation file and records whether it is an Angular catalog.
    /// </summary>
    /// <param name="json">Raw JSON object.</param>
    /// <param name="allowFlatKeys">When true, a dotted property name whose value is a string is one key.</param>
    public static ParsedDocument Parse(string? json, bool allowFlatKeys)
    {
        var document = Open(json);
        using (document)
        {
            var root = document.RootElement;
            if (IsAngular(root))
            {
                return new ParsedDocument(ReadAngular(root), Angular: true);
            }

            var leaves = new Dictionary<string, string>(StringComparer.Ordinal);
            Walk(root, "", leaves, allowFlatKeys);
            return new ParsedDocument(leaves, Angular: false);
        }
    }

    /// <summary>
    /// Rebuilds a JSON object from dotted paths. The first occurrence of each group fixes its order.
    /// </summary>
    /// <param name="leaves">String leaves in source order. Missing translations are empty strings.</param>
    /// <param name="angularLocale">When set, writes an Angular catalog for this locale instead of nested JSON.</param>
    public static string Write(IEnumerable<KeyValuePair<string, string>> leaves, string? angularLocale = null)
    {
        if (angularLocale is not null)
        {
            return WriteAngular(angularLocale, leaves);
        }

        var root = new Node();
        foreach (var (path, value) in leaves)
        {
            var parts = path.Split('.');
            var node = root;
            for (var index = 0; index < parts.Length - 1; index++)
            {
                node = node.Child(parts[index]);
            }

            var leaf = node.Child(parts[^1]);
            leaf.IsLeaf = true;
            leaf.Leaf = value;
        }

        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions
        {
            Indented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        }))
        {
            WriteNode(writer, root);
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static JsonDocument Open(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new BadRequestException("ERR_FILE_JSON_INVALID");
        }

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(json);
        }
        catch (JsonException)
        {
            throw new BadRequestException("ERR_FILE_JSON_INVALID");
        }

        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            document.Dispose();
            throw new BadRequestException("ERR_FILE_JSON_INVALID");
        }

        return document;
    }

    private static bool IsAngular(JsonElement root)
    {
        var locale = false;
        var translations = false;
        var count = 0;
        foreach (var property in root.EnumerateObject())
        {
            count++;
            if (property.NameEquals("locale") && property.Value.ValueKind == JsonValueKind.String)
            {
                locale = true;
            }
            else if (property.NameEquals("translations") && property.Value.ValueKind == JsonValueKind.Object)
            {
                translations = true;
            }
            else
            {
                return false;
            }
        }

        return count == 2 && locale && translations;
    }

    private static Dictionary<string, string> ReadAngular(JsonElement root)
    {
        var leaves = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var property in root.GetProperty("translations").EnumerateObject())
        {
            if (property.Name.Length == 0 || property.Name.Length > MaxPathLength)
            {
                throw new BadRequestException("ERR_FILE_KEY_INVALID");
            }

            if (property.Value.ValueKind != JsonValueKind.String)
            {
                throw new BadRequestException("ERR_FILE_LEAF_NOT_STRING");
            }

            leaves[property.Name] = property.Value.GetString() ?? "";
        }

        return leaves;
    }

    private static string WriteAngular(string locale, IEnumerable<KeyValuePair<string, string>> leaves)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions
        {
            Indented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        }))
        {
            writer.WriteStartObject();
            writer.WriteString("locale", locale);
            writer.WritePropertyName("translations");
            writer.WriteStartObject();
            foreach (var (path, value) in leaves)
            {
                writer.WriteString(path, value);
            }

            writer.WriteEndObject();
            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static void Walk(JsonElement element, string prefix, Dictionary<string, string> leaves, bool flatKeys)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (property.Name.Length == 0)
            {
                throw new BadRequestException("ERR_FILE_KEY_INVALID");
            }

            if (property.Name.Contains('.', StringComparison.Ordinal))
            {
                if (!flatKeys || property.Value.ValueKind != JsonValueKind.String)
                {
                    throw new BadRequestException("ERR_FILE_KEY_INVALID");
                }

                var flat = prefix.Length == 0 ? property.Name : prefix + "." + property.Name;
                RequirePath(flat);
                leaves[flat] = property.Value.GetString() ?? "";
                continue;
            }

            var path = prefix.Length == 0 ? property.Name : prefix + "." + property.Name;
            if (path.Length > MaxPathLength)
            {
                throw new BadRequestException("ERR_FILE_KEY_INVALID");
            }

            switch (property.Value.ValueKind)
            {
                case JsonValueKind.Object:
                    Walk(property.Value, path, leaves, flatKeys);
                    break;
                case JsonValueKind.String:
                    leaves[path] = property.Value.GetString() ?? "";
                    break;
                default:
                    throw new BadRequestException("ERR_FILE_LEAF_NOT_STRING");
            }
        }
    }

    private static void RequirePath(string path)
    {
        if (path.Length == 0 || path.Length > MaxPathLength || path.Split('.').Any(part => part.Length == 0))
        {
            throw new BadRequestException("ERR_FILE_KEY_INVALID");
        }
    }

    private static void WriteNode(Utf8JsonWriter writer, Node node)
    {
        if (node.IsLeaf)
        {
            writer.WriteStringValue(node.Leaf ?? "");
            return;
        }

        writer.WriteStartObject();
        foreach (var (name, child) in node.Children)
        {
            writer.WritePropertyName(name);
            WriteNode(writer, child);
        }

        writer.WriteEndObject();
    }

    private sealed class Node
    {
        public bool IsLeaf { get; set; }

        public string? Leaf { get; set; }

        public List<(string Name, Node Node)> Children { get; } = [];

        public Node Child(string name)
        {
            foreach (var (existing, node) in this.Children)
            {
                if (existing == name)
                {
                    return node;
                }
            }

            var created = new Node();
            this.Children.Add((name, created));
            return created;
        }
    }
}

/// <summary>Leaves of one JSON file, and whether that file is an Angular catalog.</summary>
/// <param name="Leaves">String values in document order.</param>
/// <param name="Angular">True when the file is <c>{ locale, translations }</c>.</param>
public readonly record struct ParsedDocument(Dictionary<string, string> Leaves, bool Angular);
