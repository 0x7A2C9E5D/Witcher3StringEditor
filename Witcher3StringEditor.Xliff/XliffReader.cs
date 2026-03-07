using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using JetBrains.Annotations;

namespace Witcher3StringEditor.Xliff;

[PublicAPI]
public class XliffReader : IXliffReader
{
    private const string XliffNamespace = "urn:oasis:names:tc:xliff:document:{0}";
    private XElement? xElement;
    private XliffInfo? xliffInfo;

    public XliffInfo? ReadInfo(string path)
    {
        xElement = XElement.Load(path);
        if (!Version.TryParse(xElement.Attribute("version")?.Value, out var version)) return null;
        if (version.Major > 2) throw new InvalidDataException("Invalid XLIFF version.");
        var srcLang = xElement.Attribute(version.Major == 1 ? "source-language" : "srcLang")?.Value;
        var trgLang = xElement.Attribute(version.Major == 1 ? "target-language" : "trgLang")?.Value;
        if (string.IsNullOrWhiteSpace(srcLang) || string.IsNullOrWhiteSpace(trgLang))
            throw new InvalidDataException("Invalid XLIFF language.");
        return new XliffInfo
        {
            FilePath = path,
            Version = version,
            SourceLanguage = srcLang,
            TargetLanguage = trgLang
        };
    }

    public XliffDocument? ReadDocument(string path)
    {
        xliffInfo = ReadInfo(path);
        return xliffInfo != null ? ReadDocument(xliffInfo) : null;
    }

    public XliffDocument ReadDocument(XliffInfo info)
    {
        return new XliffDocument
        {
            Info = info,
            Translations = ReadTranslations()
        };
    }

    private Dictionary<string, string>? ReadTranslations()
    {
        if (xliffInfo == null || xElement == null) return null;
        using var xmlReader = xElement.CreateReader();
        var nsManager = new XmlNamespaceManager(xmlReader.NameTable);
        var ns = string.Format(XliffNamespace, xliffInfo.Version);
        nsManager.AddNamespace("xliff", ns);

        var xpath = xliffInfo.Version.Major == 1 ? "//xliff:trans-unit" : "//xliff:segment";
        var translationUnits = xElement.XPathSelectElements(xpath, nsManager);

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
            .ToList();
        if (textNodes.Count < 2) return null;
        return new KeyValuePair<string, string>(textNodes[0], textNodes[1]);
    }
}