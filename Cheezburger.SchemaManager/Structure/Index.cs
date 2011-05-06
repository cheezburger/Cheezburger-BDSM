using System;
using System.Xml.Serialization;

namespace Cheezburger.SchemaManager.Structure
{
    public class Index : SchemaItem
    {
        public Index() { }
        public Index(string name, IndexType type, string columns) : base(name)
        {
            this.Type = type;
            this.Columns = columns;
        }

        [XmlAttribute("type")]
        public IndexType Type = IndexType.Index;
        [XmlAttribute("columns")]
        public string Columns;
        [XmlAttribute("include")]
        public string Include;
    }
}