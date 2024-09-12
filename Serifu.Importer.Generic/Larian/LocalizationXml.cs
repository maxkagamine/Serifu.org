// Copyright (c) Max Kagamine
//
// This program is free software: you can redistribute it and/or modify it under
// the terms of version 3 of the GNU Affero General Public License as published
// by the Free Software Foundation.
//
// This program is distributed in the hope that it will be useful, but WITHOUT
// ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
// FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more
// details.
//
// You should have received a copy of the GNU Affero General Public License
// along with this program. If not, see https://www.gnu.org/licenses/.

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

