using System;
using System.Xml.Serialization;
using System.Collections.Generic;

namespace Cheezburger.SchemaManager.Structure
{
    [XmlRoot("schema", Namespace = "http://schemas.icanhascheezburger.com/db")]
    public class Schema : SchemaItem
    {
        [XmlAttribute("version")]
        public int Version = 0;

        [XmlArray("macros")]
        [XmlArrayItem("macro", typeof(Macro))]
        public Macro[] Macros;

        [XmlArray("views")]
        [XmlArrayItem("view", typeof(View))]
        public View[] Views;

        [XmlArray("functions")]
        [XmlArrayItem("function", typeof(UserFunction))]
        public UserFunction[] Functions;

        [XmlArray("tables")]
        [XmlArrayItem("table", typeof(Table))]
        public Table[] Tables;

        [XmlArray("procedures")]
        [XmlArrayItem("procedure", typeof(Procedure))]
        public Procedure[] Procedures;

        [XmlArray("includes")]
        [XmlArrayItem("include", typeof(string))]
        public string[] Includes;
    }
}
