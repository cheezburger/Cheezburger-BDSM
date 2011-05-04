using System.Collections.Generic;
using System;
using System.Xml.Serialization;

namespace Cheezburger.Common.Database.Structure
{
    public class Column : NameWithType
    {
        public Column() { }
        public Column(string name, string type, string length)
            : base(name, type, length)
        {
        }

        [XmlAttribute("nullable")]
        public bool Nullable;
        [XmlAttribute("isIdentity")]
        public string IsIdentity;
        [XmlAttribute("references")]
        public string References;

        [XmlArray("oldnames")]
        [XmlArrayItem("name", typeof(string))]
        public string[] OldNames;
    }
}