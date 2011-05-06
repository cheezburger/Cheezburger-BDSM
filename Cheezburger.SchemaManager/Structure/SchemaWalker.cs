using System;
using System.Data;
using System.Linq;
using System.Data.Common;
using System.Data.SqlClient;
using System.Collections.Generic;

namespace Cheezburger.Common.Database.Structure
{
    public abstract class SchemaWalker
    {
        protected SchemaWalker(DbConnection connection)
        {
            SchemaConnection = connection;
        }

        protected DbConnection SchemaConnection { get; private set; }

        protected IEnumerable<string> WalkViews()
        {
            return from row in InternalWalk(SqlClientMetaDataCollectionNames.Views, null)
                   select ((string)row["TABLE_NAME"]).Trim();
        }

        protected IEnumerable<string> WalkTables()
        {
            return from row in InternalWalk(SqlClientMetaDataCollectionNames.Tables, null)
                   select ((string)row["TABLE_NAME"]).Trim();
        }

        protected IEnumerable<DataRow> WalkColumns(string tableName)
        {
            return InternalWalk(SqlClientMetaDataCollectionNames.Columns, "TABLE_NAME, ORDINAL_POSITION ASC", null, null, tableName);
        }

        protected IEnumerable<DataRow> WalkIndexes(string tableName)
        {
            return from row in InternalWalk(SqlClientMetaDataCollectionNames.Indexes, null, null, null, tableName)
                   where !row.IsNull("INDEX_NAME")
                   select row;
        }

        protected IEnumerable<string> WalkViewIndexes(string viewName)
        {
            return InternalWalkSql("SELECT sys.indexes.name FROM sys.views JOIN sys.indexes ON sys.views.object_id = sys.indexes.object_id WHERE sys.views.name = '" + viewName.Replace("'", "''") + "'", dr => dr.GetString(0)).ToList();
        }

        protected IEnumerable<DataRow> WalkIndexColumns(string tableName, string indexName)
        {
            return InternalWalk(SqlClientMetaDataCollectionNames.IndexColumns, "ORDINAL_POSITION ASC", null, null, tableName, indexName);
        }

        protected IEnumerable<DataRow> WalkStoredProcedures()
        {
            return InternalWalk(SqlClientMetaDataCollectionNames.Procedures, null);
        }

        protected IEnumerable<DataRow> WalkForeignKeys(string tableName)
        {
            return InternalWalk(SqlClientMetaDataCollectionNames.ForeignKeys, null, null, null, tableName);
        }

        protected IEnumerable<DataRow> WalkStoredProcedureParameters(string proc)
        {
            return InternalWalk("ProcedureParameters", "ORDINAL_POSITION", null, null, proc);
        }

        private IEnumerable<T> InternalWalkSql<T>(string sql, Func<IDataRecord, T> onRow)
        {
            var cmd = SchemaConnection.CreateCommand();
            cmd.CommandText = sql;
            cmd.CommandType = CommandType.Text;
            using (var reader = cmd.ExecuteReader())
                while (reader.Read())
                    yield return onRow(reader);
        }

        private IEnumerable<DataRow> InternalWalk(string collectionName, string sort, params string[] restrictions)
        {
            DataTable schema = SchemaConnection.GetSchema(collectionName, restrictions);
            DataView view = new DataView(schema);
            if (sort != null)
                view.Sort = sort;
            foreach (DataRowView row in view)
                yield return row.Row;
        }
    }
}
