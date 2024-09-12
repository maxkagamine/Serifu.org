using System.Diagnostics;
using System.Xml.Serialization;

namespace Serifu.Importer.Generic.Larian;

[Serializable]
[XmlRoot(ElementName = "contentList")]
public class LocalizationXml
{
    [XmlElement("content")]
    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    public required LocalizationXmlContent[] Content { get; set; }
}

[Serializable]
[DebuggerDisplay("{Value,nq}", Name = "{ContentUid,nq}")]
public class LocalizationXmlContent
{
    [XmlAttribute(AttributeName = "contentuid")]
    public required string ContentUid { get; set; }

    [XmlText]
    public required string Value { get; set; }
}

