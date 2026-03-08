using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using JetBrains.Annotations;

namespace Witcher3StringEditor.Xliff;

[PublicAPI]
public class XliffReader : IXliffReader
{
    private const string XliffNamespace = "urn:oasis:names:tc:xliff:document:{0}"; // XLIFF namespace
    private XElement? xElement; // XElement instance 
    private XliffInfo? xliffInfo; // XliffInfo instance

    /// <summary>
    ///   Reads XliffInfo from XLIFF file
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    /// <exception cref="InvalidDataException"></exception>
    public XliffInfo? ReadInfo(string path)
    {
        xElement = XElement.Load(path); // Load XLIFF file
        if (!Version.TryParse(xElement.Attribute("version")?.Value, out var version)) return null; // Parse XLIFF version
        if (version.Major > 2) throw new InvalidDataException("Invalid XLIFF version."); // Check XLIFF version
        var srcLang = xElement.Attribute(version.Major == 1 ? "source-language" : "srcLang")?.Value;
        var trgLang = xElement.Attribute(version.Major == 1 ? "target-language" : "trgLang")?.Value;
        if (string.IsNullOrWhiteSpace(srcLang) || string.IsNullOrWhiteSpace(trgLang)) // Check XLIFF language
            throw new InvalidDataException("Invalid XLIFF language."); // Throw exception if XLIFF language is invalid
        xliffInfo = new XliffInfo // Create XliffInfo instance
        {
            FilePath = path, // Set file path
            Version = version, // Set XLIFF version
            SourceLanguage = srcLang, // Set source language
            TargetLanguage = trgLang // Set target language
        };
        var translationUnits = GetTranslationUnits(); // Get translation units
        return xliffInfo with { Count = translationUnits.Count() }; // Return XliffInfo instance with count
    }
    
    /// <summary>
    ///   Reads XliffDocument from XLIFF file
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public XliffDocument? ReadDocument(string path)
    {
        xliffInfo = ReadInfo(path); // Read XliffInfo
        return xliffInfo != null ? ReadDocument(xliffInfo) : null; // Return XliffDocument instance
    }

    /// <summary>
    ///   Reads XliffDocument from XliffInfo
    /// </summary>
    public XliffDocument ReadDocument(XliffInfo info)
    {
        return new XliffDocument // Create XliffDocument instance
        {
            Info = info, // Set XliffInfo
            Translations = ReadTranslations() // Set translations
        };
    }

    /// <summary>
    ///   Reads translations from XLIFF file
    /// </summary>
    private Dictionary<string, string> ReadTranslations()
    {
        var translationUnits = GetTranslationUnits(); // Get translation units
        return ParseTranslationUnitsToDictionary(translationUnits); // Return translations
    }

    /// <summary>
    ///   Gets translation units from XLIFF file
    /// </summary>
    private IEnumerable<XElement> GetTranslationUnits()
    {
        if (xliffInfo == null) return []; // Check XliffInfo
        xElement ??= XElement.Load(xliffInfo!.FilePath); // Load XLIFF file
        using var xmlReader = xElement.CreateReader(); // Create XML reader
        var nsManager = new XmlNamespaceManager(xmlReader.NameTable); // Create namespace manager
        var ns = string.Format(XliffNamespace, xliffInfo.Version); // Create XLIFF namespace
        nsManager.AddNamespace("xliff", ns); // Add XLIFF namespace
        var xpath = xliffInfo.Version.Major == 1 ? "//xliff:trans-unit" : "//xliff:segment"; // Create XPath
        return xElement.XPathSelectElements(xpath, nsManager); // Get translation units
    }

    /// <summary>
    ///   Parses translation units to dictionary
    /// </summary>
    private static Dictionary<string, string> ParseTranslationUnitsToDictionary(IEnumerable<XElement> translationUnits)
    {
        return translationUnits // Parse translation units
            .Select(ParseTranslationUnit) // Filter null pairs
            .Where(pair => pair != null) // Filter null pairs
            .ToDictionary(pair => pair!.Value.Key, pair => pair!.Value.Value); // Return dictionary
    }

    /// <summary>
    ///   Parses translation unit to key-value pair
    /// </summary>
    private static KeyValuePair<string, string>? ParseTranslationUnit(XElement unitElement)
    {
        var textNodes = unitElement // Get text nodes
            .DescendantNodes() // Get descendant nodes
            .Where(node => node.NodeType == XmlNodeType.Element) // Filter element nodes
            .Cast<XElement>() // Cast to XElement
            .Select(element => element.Value) // Get text
            .ToList(); // To list
        if (textNodes.Count < 2) return null; // Check text nodes
        return new KeyValuePair<string, string>(textNodes[0], textNodes[1]); // Return key-value pair
    }
}