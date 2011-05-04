using System.Collections.Generic;
using System;
using System.Xml.Serialization;

namespace Cheezburger.Common.Database.Structure
{
    public class CreationCallback
    {
        [XmlAttribute("type")]
        public string Type;
        [XmlAttribute("method")]
        public string Method;
    }
}