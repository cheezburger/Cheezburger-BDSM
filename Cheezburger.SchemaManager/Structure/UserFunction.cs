using System.Collections.Generic;
using System;
using System.Xml.Serialization;

namespace Cheezburger.Common.Database.Structure
{
    public class UserFunction : SchemaItem
    {
        [XmlArray("parameters")]
        [XmlArrayItem("parameter", typeof(ProcedureParameter))]
        public ProcedureParameter[] Parameters;
        [XmlElement("body")]
        public string Body;
        [XmlElement("table")]
        public Table ReturnTable;
    }
}