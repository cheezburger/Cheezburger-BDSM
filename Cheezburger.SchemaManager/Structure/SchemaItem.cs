using System.Collections.Generic;
using System;
using System.Xml.Serialization;

namespace Cheezburger.Common.Database.Structure
{
    public abstract class SchemaItem
    {
        protected SchemaItem() { }
        protected SchemaItem(string name) { this.Name = name; }

        [XmlAttribute("comment")]
        public string Comment;
        [XmlElement("comment")]
        public string LongerComment;
        [XmlAttribute("name")]
        public string Name;

        [XmlElement("callback")]
        public CreationCallback Callback;
    }
}