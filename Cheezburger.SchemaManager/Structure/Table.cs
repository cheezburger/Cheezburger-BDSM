using System.Collections.Generic;
using System;
using System.Xml.Serialization;

namespace Cheezburger.Common.Database.Structure
{
    public class Table : SchemaItem
    {
        [XmlArray("columns")]
        [XmlArrayItem("column", typeof(Column))]
        public Column[] Columns;
        [XmlArray("indexes")]
        [XmlArrayItem("index", typeof(Index))]
        public Index[] Indexes;
    }
}