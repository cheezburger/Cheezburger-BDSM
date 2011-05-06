using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using NUnit.Framework;

namespace Cheezburger.SchemaManager.Tests
{
    [TestFixture]
    public class SchemaManagerTests
    {
        [Test]
        public void CanRunMigration()
        {
            using (var connection = GetDbConnection())
            {
                var schemaUpdater = new SchemaUpdater(GetType().Assembly, "Cheezburger.SchemaManager.Tests");

                connection.Open();
                schemaUpdater.Upgrade(connection, true, Log.Write);
                connection.Close();
            }

            AssertTableExists("Category"); 
        }

        private void AssertTableExists(string tableName)
        {
            using (var connection = GetDbConnection())
            {
                var command = connection.CreateCommand();
                command.CommandText = string.Format(@"IF EXISTS (SELECT * FROM sysobjects WHERE type = 'U' AND name = '{0}') SELECT 1", tableName);

                connection.Open();
                var result = command.ExecuteScalar();
                connection.Close();

                Assert.That(result, Is.EqualTo(1)); 
            }
        }

        private DbConnection GetDbConnection()
        {
            var connectionString = @"Data Source=.\SqlExpress;Initial Catalog=SchemaManagerTest;Integrated Security=SSPI";
            return new SqlConnection(connectionString);
        }
    }
}