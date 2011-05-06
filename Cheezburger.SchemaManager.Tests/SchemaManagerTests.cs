using System;
using System.Configuration;
using System.Data.Common;
using System.Data.SqlClient;
using System.Reflection;
using Cheezburger.SchemaManager.Structure;
using NUnit.Framework;

namespace Cheezburger.SchemaManager.Tests
{
    [TestFixture]
    public class SchemaManagerTests
    {
        [Test]
        public void CanRunMigration()
        {
            using (var connection = GetDbConnection("TestDb"))
            {
                var schemaUpdater = new SchemaUpdater
                                        {
                                            Assembly = Assembly.GetExecutingAssembly(), 
                                            Namespace = "Cheezburger.SchemaManager.Tests", 
                                            Mappings = { new SchemaMapping { Name = "Schema.xml" } }
                                        };

                connection.Open();
                schemaUpdater.Upgrade(connection, true, Log.Write);
                connection.Close();
            }

            AssertTableExists("Category"); 
        }

        private void AssertTableExists(string tableName)
        {
            using (var connection = GetDbConnection("TestDb"))
            {
                var command = connection.CreateCommand();
                command.CommandText = string.Format(@"IF EXISTS (SELECT * FROM sysobjects WHERE type = 'U' AND name = '{0}') SELECT 1", tableName);

                connection.Open();
                var result = command.ExecuteScalar();
                connection.Close();

                Assert.That(result, Is.EqualTo(1)); 
            }
        }

        private DbConnection GetDbConnection(string name)
        {
            var dbConfig = ConfigurationManager.ConnectionStrings[name];
            if (dbConfig == null) throw new InvalidOperationException("Unable to find 'TestDb' in the config file.");

            var dbProviderFactory = DbProviderFactories.GetFactory(dbConfig.ProviderName);
            var connection = dbProviderFactory.CreateConnection();
            connection.ConnectionString = dbConfig.ConnectionString;

            return connection;
        }
    }
}