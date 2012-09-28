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
            Mappings = new List<SchemaMapping>();
        }

        public ICollection<SchemaMapping> Mappings { get; private set; }

        public virtual void Upgrade(DbConnection connection, bool forceFullCheck, Action<string> log)
        {
            foreach (var db in Mappings)
            {
                if (log != null) log("Upgrading " + db.Name);

                var resolver = GetResolver(db);
                try
                {
                    Importer.Upgrade(resolver.Resolve(db.Name), connection, resolver.Resolve, forceFullCheck, log);
                }
                catch (SqlException sex)
                {
                    if (db.IgnoreFailure) continue;

                    Log.Write(sex);
                    throw;
                }
            }
        }

        public interface IResolver
        {
            Schema Resolve(string name);
        }

        public abstract class StreamResolverBase
        {
            protected string _path;

            public virtual Schema Resolve(string name)
            {
                var serializer = new XmlSerializer(typeof(Schema));
                Schema result;

                using (var stream = ResolveStream(name))
                {
                    if (stream == null)
                        throw new FileNotFoundException("Unable to resolve schema: " + name, name);

                    using (var reader = new StreamReader(stream))
                        result = (Schema)serializer.Deserialize(reader);
                }

                if (string.IsNullOrEmpty(result.Name))
                    result.Name = name.Substring(_path.Length);

                return result;
            }

            protected abstract Stream ResolveStream(string name);
        }

        public class EmbeddedResourceResolver : StreamResolverBase, IResolver
        {
            private readonly Assembly _assembly;
            private readonly string _ns;

            public EmbeddedResourceResolver(Assembly assembly, string path, string @namespace)
            {
                _assembly = assembly;
                _path = path;
                _ns = @namespace;
            }

            protected override Stream ResolveStream(string name)
            {
                return _assembly.GetManifestResourceStream(_ns + "." + name);
            }
        }

        public class FileResolver : StreamResolverBase , IResolver
        {
            public FileResolver(string path)
            {
                _path = path;
            }

            protected override Stream ResolveStream(string name)
            {
                return new FileStream(Path.Combine(_path, name), FileMode.Open);
            }
        }

        private IResolver GetResolver(SchemaMapping mapping)
        {
            var embeddedMapping = mapping as EmbeddedResourceSchemaMapping;
            if (embeddedMapping != null)
                return new EmbeddedResourceResolver(embeddedMapping.Assembly, embeddedMapping.Path, embeddedMapping.Namespace);

            var fileMapping = mapping as FileSchemaMapping;
            if (fileMapping != null)
                return new FileResolver(fileMapping.Path);

            return null;
        }
    }
}