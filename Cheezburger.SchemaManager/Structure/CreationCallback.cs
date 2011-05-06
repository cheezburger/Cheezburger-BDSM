using System;
using System.Xml.Serialization;

namespace Cheezburger.SchemaManager.Structure
{
    public class CreationCallback
    {
        [XmlAttribute("method")] 
        public string Method;
        
        [XmlAttribute("type")] 
        public string Type;
    }
}