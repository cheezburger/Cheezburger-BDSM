using System;
using System.Data.SqlClient;
using System.IO;
using System.Reflection;
using System.Xml.Serialization;
using Cheezburger.SchemaManager;

namespace Cheezburger.Common.Database.Structure
{
    public abstract class SchemaUpdaterBase
    {
        private bool? oldDatabaseBackedConfig;

        protected SchemaUpdaterBase()
        {
            SchemaPath = "";
        }

        public abstract Assembly SchemaAssembly { get; }
        public abstract string SchemaNamespace { get; }
        public string SchemaPath { get; protected set; }
        public Action<Schema> ResolveCallback { get; protected set; }

        protected class SchemaMapping
        {
            public string Name { get; set; }
            public string Connection { get; set; }
            public bool IgnoreFailure { get; set; }
        }

        protected abstract SchemaMapping[] Mappings { get; }

        public virtual void Upgrade(Func<string, Microsoft.Practices.EnterpriseLibrary.Data.Database> getdatabase, bool forceFullCheck, Action<string> log)
        {
            foreach (var db in Mappings)
            {
                if (log != null) log("Upgrading " + db);

                try
                {
                    Importer.Upgrade(Resolve(db.Name), getdatabase(db.Connection), Resolve, forceFullCheck, log);
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
                result.Name = name.Substring(SchemaPath.Length);

            if (ResolveCallback != null)
                ResolveCallback(result);

            return result;
        }

        private Stream ResolveStream(string name)
        {
            return SchemaAssembly.GetManifestResourceStream(SchemaNamespace + "." + name);
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