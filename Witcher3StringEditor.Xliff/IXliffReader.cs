namespace Witcher3StringEditor.Xliff;

public interface IXliffReader
{
    public XliffInfo ReadInfo(string path);
    
    public XliffDocument ReadDocument(string path);

    public XliffDocument ReadDocument(XliffInfo info);
}