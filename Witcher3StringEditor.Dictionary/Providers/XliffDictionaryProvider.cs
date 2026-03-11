using System.Globalization;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

namespace Witcher3StringEditor.Dictionary.Providers;

public class XliffDictionaryProvider : IDictionaryProvider
{
    private const string XliffNamespace = "urn:oasis:names:tc:xliff:document:{0}"; // XLIFF namespace

    public DictionaryInfo GetDictionaryInfo(string filePath)
    {
        var xElement = XElement.Load(filePath); // Load XLIFF file
        var version = ParseAndValidateVersion(xElement); // Parse XLIFF version
        var (srcLang, trgLang) = ExtractAndValidateLanguages(xElement, version); // Extract and validate languages
        ValidateSourceLanguage(srcLang); // Validate source language
        var count = GetTranslationUnits(xElement, version).Count(); // Count translation units
        return new DictionaryInfo(filePath, version, new CultureInfo(srcLang), new CultureInfo(trgLang),
            count); // Return DictionaryInfo instance
    }

    public Dictionary<string, string> GetEntries(DictionaryInfo dictionary)
    {
        var xElement = XElement.Load(dictionary.Path); // Load XLIFF file
        var translationUnits = GetTranslationUnits(xElement, dictionary.Version); // Get translation units
        return ParseTranslationUnitsToDictionary(translationUnits); // Return translations
    }

    private static Dictionary<string, string> ParseTranslationUnitsToDictionary(IEnumerable<XElement> translationUnits)
    {
        return translationUnits 
            .Select(ParseTranslationUnit)
            .Where(pair => pair != null)
            .ToDictionary(pair => pair!.Value.Key, pair => pair!.Value.Value);
    }

    private static KeyValuePair<string, string>? ParseTranslationUnit(XElement unitElement)
    {
        var textNodes = unitElement
            .DescendantNodes()
            .Where(node => node.NodeType == XmlNodeType.Element)
            .Cast<XElement>()
            .Select(element => element.Value)
            .ToList(); // Get text nodes

        if (textNodes.Count < 2) return null; // Check text nodes

        return new KeyValuePair<string, string>(textNodes[0], textNodes[1]); // Return key-value pair
    }

    private static Version ParseAndValidateVersion(XElement xElement)
    {
        if (!Version.TryParse(xElement.Attribute("version")?.Value, out var version) ||
            version.Major > 2) // Parse XLIFF version
            throw new InvalidDataException("Invalid XLIFF version."); // Throw exception if XLIFF version is invalid

        return version; // Return XLIFF version
    }

    private static (string srcLang, string trgLang) ExtractAndValidateLanguages(XElement xElement, Version version)
    {
        var srcLang =
            xElement.Attribute(version.Major == 1 ? "source-language" : "srcLang")?.Value; // Extract source language
        var trgLang =
            xElement.Attribute(version.Major == 1 ? "target-language" : "trgLang")?.Value; // Extract target language

        if (string.IsNullOrWhiteSpace(srcLang) || string.IsNullOrWhiteSpace(trgLang)) // Check XLIFF language
            throw new InvalidDataException("Invalid XLIFF language."); // Throw exception if XLIFF language is invalid

        return (srcLang, trgLang); // Return source and target languages
    }

    private static void ValidateSourceLanguage(string srcLang) // Validate source language
    {
        var culture = CultureInfo.GetCultureInfo(srcLang); // Get culture info
        var enCulture = new CultureInfo("en"); // Create English culture

        if (!Equals(culture, enCulture) && !Equals(culture.Parent, enCulture)) // Check XLIFF language
            throw new InvalidDataException("Invalid XLIFF language."); // Throw exception if XLIFF language is invalid
    }

    private static IEnumerable<XElement> GetTranslationUnits(XElement xElement, Version version)
    {
        using var xmlReader = xElement.CreateReader(); // Create XML reader
        var nsManager = new XmlNamespaceManager(xmlReader.NameTable); // Create namespace manager
        var ns = string.Format(XliffNamespace, version); // Create XLIFF namespace
        nsManager.AddNamespace("xliff", ns); // Add XLIFF namespace

        var xpath = version.Major == 1 ? "//xliff:trans-unit" : "//xliff:segment"; // Create XPath
        return xElement.XPathSelectElements(xpath, nsManager); // Get translation units
    }
}