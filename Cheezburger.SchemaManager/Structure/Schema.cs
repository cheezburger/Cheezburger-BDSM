// Copyright (C) 2011 by Cheezburger, Inc.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Xml.Serialization;

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
