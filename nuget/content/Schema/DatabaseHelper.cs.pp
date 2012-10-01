using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using Cheezburger.SchemaManager;
using Cheezburger.SchemaManager.Structure;

namespace $rootnamespace$.Schema
{
    public class DatabaseHelper
    {
        public string CreateDatabase(string masterConnectionString, string name)
        {
            using (var connection = new SqlConnection(masterConnectionString))
            {
                connection.Open();
                var cmd = connection.CreateCommand();
                var escapedDbName = name.Replace("]", "]]");
                cmd.CommandText = "IF 0 = (SELECT COUNT(*) FROM sys.databases WHERE [name] = @name) CREATE DATABASE [" + escapedDbName + "]";
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.Add(new SqlParameter("@name", name));
                cmd.ExecuteNonQuery();

                cmd = connection.CreateCommand();
                cmd.CommandText = string.Format("ALTER DATABASE [{0}] SET ALLOW_SNAPSHOT_ISOLATION ON; ALTER DATABASE [{0}] SET READ_COMMITTED_SNAPSHOT ON;", escapedDbName);
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.Add(new SqlParameter("@name", name));
                cmd.ExecuteNonQuery();

                cmd = connection.CreateCommand();
                cmd.CommandText = string.Format("USE [{0}];", escapedDbName);
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.Add(new SqlParameter("@name", name));
                cmd.ExecuteNonQuery();

                cmd = connection.CreateCommand();
                cmd.CommandText = "IF (SELECT COUNT(*) FROM sys.database_principals WHERE [name] = 'mine_users') = 0 CREATE ROLE mine_users;;";
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.Add(new SqlParameter("@name", name));
                cmd.ExecuteNonQuery();
            }

            return CreateConnectionString(masterConnectionString, name);
        }

        private static string CreateConnectionString(string masterConnectionString, string name)
        {
            return new SqlConnectionStringBuilder(masterConnectionString) {InitialCatalog = name}.ConnectionString;
        }

        public void CleanupDatabase(string masterConnectionString, string name)
        {
            using (var connection = new SqlConnection(masterConnectionString))
            {
                SqlConnection.ClearPool(new SqlConnection(CreateConnectionString(masterConnectionString, name)));

                connection.Open();
                var cmd = connection.CreateCommand();
                cmd.CommandText = "DROP DATABASE [" + name.Replace("]", "]]") + "]";
                cmd.CommandType = CommandType.Text;
                cmd.ExecuteNonQuery();
            }
        }

        public void UpgradeDatabase(string connectionString, bool forceFullCheck = false, Action<string> log = null)
        {
            var updater = new SchemaUpdater();
            updater.Mappings.Add(new EmbeddedResourceSchemaMapping {Name = "Schema.xml", Assembly = GetType().Assembly, Namespace = "$rootnamespace$.Schema"});

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                updater.Upgrade(connection, forceFullCheck, log ?? Console.WriteLine);
            }
        }
    }
}
