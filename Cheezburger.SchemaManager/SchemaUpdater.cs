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