using System.Collections.Generic;
using System;
using System.Xml.Serialization;

namespace Cheezburger.SchemaManager.Structure
{
    public enum ProcedureParameterDirection
    {
        [XmlEnum("In")]
        In,
        [XmlEnum("Out")]
        Out,
    }
}