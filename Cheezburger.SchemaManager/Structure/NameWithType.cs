using System.Collections.Generic;
using System;
using System.Xml.Serialization;

namespace Cheezburger.Common.Database.Structure
{
    public class NameWithType : SchemaItem
    {
        public NameWithType() { }
        public NameWithType(string name, string type, string length)
            : base(name)
        {
            Name = name;
            Type = type;
            Length = length;
        }

        [XmlAttribute("type")]
        public string Type;
        [XmlAttribute("length", DataType = "integer")]
        public string Length;
        [XmlAttribute("default")]
        public string DefaultValue;
    }
}