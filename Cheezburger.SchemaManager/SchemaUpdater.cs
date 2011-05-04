using System.Reflection;
using Cheezburger.Common.Database.Structure;

namespace Cheezburger.SchemaManager
{
    public class SchemaUpdater : SchemaUpdaterBase
    {
        private readonly Assembly schemaAssembly;
        private readonly string schemaNamespace;

        public SchemaUpdater(Assembly schemaAssembly, string schemaNamespace)
        {
            this.schemaAssembly = schemaAssembly;
            this.schemaNamespace = schemaNamespace;
        }

        public override Assembly SchemaAssembly { get { return schemaAssembly; } }
        public override string SchemaNamespace { get { return schemaNamespace; } }

        protected override SchemaMapping[] Mappings
        {
            get
            {
                return new[]
                {
                    new SchemaMapping {Name = "Schema.xml"},
                };
            }
        }
    }
}