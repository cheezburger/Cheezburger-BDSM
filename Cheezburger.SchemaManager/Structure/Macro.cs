using System.Collections.Generic;
using System;
using System.Xml.Serialization;

namespace Cheezburger.Common.Database.Structure
{
    public class Macro : SchemaItem
    {
        [XmlElement("value")]
        public string Value;
    }
}