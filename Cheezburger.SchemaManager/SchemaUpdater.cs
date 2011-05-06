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
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.IO;
using System.Reflection;
using System.Xml.Serialization;
using Cheezburger.SchemaManager.Structure;

namespace Cheezburger.SchemaManager
{
    public class SchemaUpdater
    {
        public SchemaUpdater()
        {
            Path = "";
            Mappings = new List<SchemaMapping>();
        }

        public Assembly Assembly { get; set; }
        public string Namespace { get; set; }
        public string Path { get; private set; }
        public Action<Schema> ResolveCallback { get; private set; }
        public ICollection<SchemaMapping> Mappings { get; private set; }

        public virtual void Upgrade(DbConnection connection, bool forceFullCheck, Action<string> log)
        {
            foreach (var db in Mappings)
            {
                if (log != null) log("Upgrading " + db);

                try
                {
                    Importer.Upgrade(Resolve(db.Name), connection, Resolve, forceFullCheck, log);
                }
                catch (SqlException sex)
                {
                    if (db.IgnoreFailure) continue;

                    Log.Write(sex);
                    throw;
                }
            }
        }

        public virtual Schema Resolve(string name)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(Schema));
            Schema result;

            using (var stream = ResolveStream(name))
            {
                if (stream == null)
                    throw new FileNotFoundException("Unable to resolve schema: " + name, name);

                using (StreamReader reader = new StreamReader(stream))
                    result = (Schema)serializer.Deserialize(reader);
            }

            if (string.IsNullOrEmpty(result.Name))
                result.Name = name.Substring(Path.Length);

            if (ResolveCallback != null)
                ResolveCallback(result);

            return result;
        }

        private Stream ResolveStream(string name)
        {
            return Assembly.GetManifestResourceStream(Namespace + "." + name);
        }

        public virtual string ResolveAndReadText(string name)
        {
            using (var stream = ResolveStream(name))
            {
                if (stream == null)
                    throw new FileNotFoundException("Unable to resolve script", name);

                using (StreamReader reader = new StreamReader(stream))
                    return reader.ReadToEnd();
            }
        }
    }
}