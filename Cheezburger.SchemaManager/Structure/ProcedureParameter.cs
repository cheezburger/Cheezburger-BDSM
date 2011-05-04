using System.Collections.Generic;
using System;
using System.Xml.Serialization;

namespace Cheezburger.Common.Database.Structure
{
    public class ProcedureParameter : NameWithType
    {
        [XmlAttribute("direction")]
        public ProcedureParameterDirection Direction = ProcedureParameterDirection.In;
    }
}