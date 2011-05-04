using System.Collections.Generic;
using System;
using System.Xml.Serialization;

namespace Cheezburger.Common.Database.Structure
{
    public enum IndexType
    {
        [XmlEnum("PrimaryKey")]
        PrimaryKey,
        [XmlEnum("Index")]
        Index,
        [XmlEnum("Unique")]
        Unique,
    }
}