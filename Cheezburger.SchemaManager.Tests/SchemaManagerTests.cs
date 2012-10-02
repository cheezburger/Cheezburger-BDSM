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
using System.Configuration;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Reflection;
using Cheezburger.SchemaManager.Structure;
using NUnit.Framework;

namespace Cheezburger.SchemaManager.Tests
{
    [TestFixture]
    public class SchemaManagerTests
    {
        [Test]
        public void FakeTest()
        {
            Assert.Pass();
        }

        [Test, Ignore]
        public void CanRunMigration()
        {
            using (var connection = GetDbConnection("TestDb"))
            {
                var schemaUpdater = new SchemaUpdater
                                        {
                                            Mappings = { new EmbeddedResourceSchemaMapping { Name = "Schema.xml", Assembly = Assembly.GetExecutingAssembly(), Namespace = "Cheezburger.SchemaManager.Tests" } }
                                        };

                connection.Open();
                schemaUpdater.Upgrade(connection, true, Log.Write);
                connection.Close();
            }

            AssertTableExists("Category");
        }

        [Test, Ignore]
        public void CanRunFileMigration()
        {
            var dir = Directory.CreateDirectory("Schemas");
            var tempFilePaths = new[] {
                WriteEmbeddedFileToDisk("Cheezburger.SchemaManager.Tests.Schema.xml", "Schemas\\Schema.xml"),
                WriteEmbeddedFileToDisk("Cheezburger.SchemaManager.Tests.Tables.xml", "Schemas\\Tables.xml")
            };

            using (var connection = GetDbConnection("TestDb"))
            {
                var schemaUpdater = new SchemaUpdater
                {
                    Mappings = { new FileSchemaMapping { Name = "Schema.xml", Path = "Schemas"} }
                };

                connection.Open();
                schemaUpdater.Upgrade(connection, true, Log.Write);
                connection.Close();
            }

            AssertTableExists("Category");

            foreach (var path in tempFilePaths)
                File.Delete(path);
            dir.Delete(true);
        }

        private Stream GetStreamFromEmbeddedResource(string path)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            return (from a in assemblies
                    let stream = a.GetManifestResourceStream(path)
                    where stream != null
                    select stream).FirstOrDefault();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="embeddedPath"> </param>
        /// <param name="fileName"> </param>
        /// <returns>The path to the output file.</returns>
        private string WriteEmbeddedFileToDisk(string embeddedPath, string fileName)
        {
            string outputFile;
            using (var stream = GetStreamFromEmbeddedResource(embeddedPath))
            {
                outputFile = Path.Combine(Environment.CurrentDirectory, fileName);
                using (var fileStream = File.Create(outputFile))
                    stream.CopyTo(fileStream);
            }
            return outputFile;
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