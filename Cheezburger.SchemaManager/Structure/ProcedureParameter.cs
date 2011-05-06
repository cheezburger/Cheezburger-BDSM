using System.Collections.Generic;
using System;
using System.Xml.Serialization;

namespace Cheezburger.SchemaManager.Structure
{
    public class ProcedureParameter : NameWithType
    {
        [XmlAttribute("direction")]
        public ProcedureParameterDirection Direction = ProcedureParameterDirection.In;
    }
}