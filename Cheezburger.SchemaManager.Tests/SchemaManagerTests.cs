using System.Data;
using Microsoft.Practices.EnterpriseLibrary.Data;
using Microsoft.Practices.EnterpriseLibrary.Data.Sql;
using NUnit.Framework;

namespace Cheezburger.SchemaManager.Tests
{
    [TestFixture]
    public class SchemaManagerTests
    {
        [Test]
        public void CanRunMigration()
        {
            var db = GetTestDatabase();
            var schemaUpdater = new SchemaUpdater(GetType().Assembly, "Cheezburger.SchemaManager.Tests");
            schemaUpdater.Upgrade(x => db, true, Log.Write);

            AssertTableExists("Category");
        }

        private void AssertTableExists(string tableName)
        {
            var db = GetTestDatabase();
            var sql = string.Format(@"IF EXISTS (SELECT * FROM sysobjects WHERE type = 'U' AND name = '{0}') SELECT 1", tableName);
            var result = db.ExecuteScalar(CommandType.Text, sql);

            Assert.That(result, Is.EqualTo(1));
        }

        private Database GetTestDatabase()
        {
            var connectionString = @"Data Source=.\SqlExpress;Initial Catalog=SchemaManagerTest;Integrated Security=SSPI";
            return new SqlDatabase(connectionString);
        }
    }
}