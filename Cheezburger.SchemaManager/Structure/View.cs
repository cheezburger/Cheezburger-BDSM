using System.Collections.Generic;
using System;
using System.Xml.Serialization;

namespace Cheezburger.SchemaManager.Structure
{
    public class View : SchemaItem
    {
        [XmlElement("body")]
        public string Body;
        [XmlArray("indexes")]
        [XmlArrayItem("index", typeof(Index))]
        public Index[] Indexes;
    }
}