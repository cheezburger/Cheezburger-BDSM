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
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Data.SqlClient;
using System.Text.RegularExpressions;
using Cheezburger.SchemaManager.Extensions;

namespace Cheezburger.SchemaManager.Structure
{
    public class Exporter : SchemaWalker
    {
        public Exporter(DbConnection db)
            : base(db)
        {
        }

        public Schema CreateSchema()
        {
            return InternalCreateSchema();
        }

        private Schema InternalCreateSchema()
        {
            var views = from view in WalkViews() select CreateView(view);
            var tables = from tablename in WalkTables()
                         let table = CreateTable(tablename)
                         orderby table.Name
                         select table;
            var procs = from procname in WalkStoredProcedures()
                        let proc = CreateProcedure(procname)
                        where proc != null
                        select proc;
            var result = new Schema {LongerComment = @"
CAUTION - THIS IS FOR INFORMATIVE PURPOSES ONLY

The Exporter will generate a mostly correct version of a DB's
schema, but it fails to get everything.

    * Stored prodecures

The current schema system does not support:

    * Foreign keys
    * Altering columns
    * Altering indexes
    
  ", Version = 1, Tables = tables.ToArrayOrNull(), Views = views.ToArrayOrNull(), Procedures = procs.ToArrayOrNull()};
            return result;
        }

        private View CreateView(string name)
        {
            var view = new View { Name = name };

            var create = GetCreateStatement(name);
            view.Body = create.Substring(create.IndexOf(" AS ", StringComparison.InvariantCultureIgnoreCase) + 4).Trim();

            view.Indexes = (from row in WalkViewIndexes(name)
                            select CreateIndex(name, row)).ToArrayOrNull();

            return view;
        }

        private Procedure CreateProcedure(DataRow row)
        {
            var proc = new Procedure {Name = (string) row["ROUTINE_NAME"]};

            if (proc.Name.StartsWith("sp_") || proc.Name.StartsWith("fn_"))
                return null;

            proc.Parameters = (from rowParam in WalkStoredProcedureParameters(proc.Name)
                               select CreateParameter(rowParam)).ToArrayOrNull();

            var createStatement = GetCreateStatement(proc.Name);
            var m = Regex.Match(createStatement, @"\bAS\b");
            proc.Body = "\n" + createStatement.Substring(m.Index + m.Length).Trim() + "\n      ";

            return proc;
        }

        private static ProcedureParameter CreateParameter(DataRow rowParam)
        {
            var param = new ProcedureParameter();
            FillNameAndType(rowParam, param);
            param.Direction = (string)rowParam["PARAMETER_MODE"] == "OUT" ? ProcedureParameterDirection.Out : ProcedureParameterDirection.In;
            return param;
        }

        private Table CreateTable(string name)
        {
            var result = new Table
                {
                    Name = name,
                    Columns = (from row in WalkColumns(name)
                               select CreateColumn(row)).ToArray(),
                    Indexes = (from row in WalkIndexes(name)
                               select CreateIndex(name, (string) row["INDEX_NAME"])).ToArrayOrNull()
                };
            return result;
        }

        int _colid;
        private Index CreateIndex(string tableName, string indexName)
        {
            var index = new Index {Name = indexName};

            var isPriKey = QuerySysTable<bool>("is_primary_key", "indexes", "object_id = OBJECT_ID(@p1) AND [name] = @p2", tableName, index.Name);
            var isUnique = QuerySysTable<bool>("is_unique_constraint", "indexes", "object_id = OBJECT_ID(@p1) AND [name] = @p2", tableName, index.Name);
            var id = QuerySysTable<int>("index_id", "indexes", "object_id = OBJECT_ID(@p1) AND [name] = @p2", tableName, index.Name);

            if (isPriKey)
                index.Type = IndexType.PrimaryKey;
            else if (isUnique)
                index.Type = IndexType.Unique;

            _colid = 1;

            index.Columns = (from row in WalkIndexColumns(tableName, index.Name)
                             let col = row["COLUMN_NAME"]
                             let isdesc = QuerySysTable<bool>("is_descending_key", "index_columns", "object_id = OBJECT_ID(@p1) AND index_id = @p2 AND index_column_id = @p3", tableName, id.ToString(), (_colid++).ToString())
                             let sort = isdesc ? " DESC" : " ASC"
                             select col + (index.Type == IndexType.Index ? sort : "")).Join(", ");

            return index;
        }

        private Column CreateColumn(DataRow columnRow)
        {
            var tableName = (string)columnRow["TABLE_NAME"];
            var column = new Column();

            FillNameAndType(columnRow, column);

            if ((string)columnRow["IS_NULLABLE"] != "NO")
                column.Nullable = true;
            if (!columnRow.IsNull("COLUMN_DEFAULT"))
            {
                column.DefaultValue = (string)columnRow["COLUMN_DEFAULT"];
                while (column.DefaultValue[0] == '(' && column.DefaultValue[column.DefaultValue.Length - 1] == ')')
                    column.DefaultValue = column.DefaultValue.Substring(1, column.DefaultValue.Length - 2);
            }

            if (QuerySysTable<bool>("is_identity", "columns", "object_id = OBJECT_ID(@p1) AND [name] = @p2", tableName, column.Name))
                column.IsIdentity = "true";

            return column;
        }

        private static void FillNameAndType(DataRow row, NameWithType nwt)
        {
            nwt.Name = (string)row[nwt is Column ? "COLUMN_NAME" : "PARAMETER_NAME"];
            nwt.Type = (string)row["DATA_TYPE"];
            if (!row.IsNull("CHARACTER_MAXIMUM_LENGTH") && nwt.Type != "text")
                nwt.Length = row["CHARACTER_MAXIMUM_LENGTH"].ToString();
        }

        private T QuerySysTable<T>(string col, string table, string where, params string[] args)
        {
            var cmd = (SqlCommand)SchemaConnection.CreateCommand();
            cmd.CommandType = CommandType.Text;
            cmd.CommandText = string.Format("SELECT {0} FROM sys.{1} WHERE {2}", col, table, where);

            for (var i = 0; i < args.Length; i++)
            {
                cmd.Parameters.Add("@p" + (i + 1), SqlDbType.Char, args[i].Length).Value = args[i];
            }

            var openClose = SchemaConnection.State == ConnectionState.Closed;
            if (openClose) SchemaConnection.Open();

            try
            {
                return (T)cmd.ExecuteScalar();
            }
            finally
            {
                if (openClose)
                    SchemaConnection.Close();
            }
        }

        private string GetCreateStatement(string dbobject)
        {
            var cmd = (SqlCommand)SchemaConnection.CreateCommand();
            cmd.CommandType = CommandType.Text;
            cmd.CommandText = "SELECT OBJECT_DEFINITION(OBJECT_ID(@p1));";
            cmd.Parameters.Add("@p1", SqlDbType.Char, dbobject.Length).Value = dbobject;

            bool openClose = SchemaConnection.State == ConnectionState.Closed;
            if (openClose) SchemaConnection.Open();

            try
            {
                var create = cmd.ExecuteScalar();
                if (create is DBNull)
                    return string.Format("CREATE [{0}] AS FAILED TO IMPORT", dbobject);
                return (string)create;
            }
            finally
            {
                if (openClose)
                    SchemaConnection.Close();
            }
        }
    }
}
