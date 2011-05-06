using System;
using System.Xml.Serialization;

namespace Cheezburger.SchemaManager.Structure
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