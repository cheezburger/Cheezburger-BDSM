using System.Collections.Generic;
using System;
using System.Xml.Serialization;

namespace Cheezburger.Common.Database.Structure
{
    public enum ProcedureParameterDirection
    {
        [XmlEnum("In")]
        In,
        [XmlEnum("Out")]
        Out,
    }
}