using System;
using System.Xml.Serialization;

namespace Cheezburger.SchemaManager.Structure
{
    public class Column : NameWithType
    {
        [XmlAttribute("isIdentity")] 
        public string IsIdentity;
        
        [XmlAttribute("nullable")] 
        public bool Nullable;

        [XmlArray("oldnames")] 
        [XmlArrayItem("name", typeof (string))] 
        public string[] OldNames;
        
        [XmlAttribute("references")] 
        public string References;

        public Column()
        {
        }

        public Column(string name, string type, string length)
            : base(name, type, length)
        {
        }
    }
}