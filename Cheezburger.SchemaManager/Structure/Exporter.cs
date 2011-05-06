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
        private Exporter(DbConnection connection) : base(connection)
        {
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
            Procedure proc = new Procedure();
            proc.Name = (string)row["ROUTINE_NAME"];

            if (proc.Name.StartsWith("sp_") || proc.Name.StartsWith("fn_"))
                return null;

            proc.Parameters = (from rowParam in WalkStoredProcedureParameters(proc.Name)
                               select CreateParameter(rowParam)).ToArrayOrNull();

            string createStatement = GetCreateStatement(proc.Name);
            Match m = Regex.Match(createStatement, @"\bAS\b");
            proc.Body = "\n" + createStatement.Substring(m.Index + m.Length).Trim() + "\n      ";

            return proc;
        }

        private ProcedureParameter CreateParameter(DataRow rowParam)
        {
            var param = new ProcedureParameter();
            FillNameAndType(rowParam, param);
            param.Direction = (string)rowParam["PARAMETER_MODE"] == "OUT" ? ProcedureParameterDirection.Out : ProcedureParameterDirection.In;
            return param;
        }

        private Table CreateTable(string name)
        {
            Table result = new Table();
            result.Name = name;
            result.Columns = (from row in WalkColumns(name)
                              select CreateColumn(row)).ToArray();
            result.Indexes = (from row in WalkIndexes(name)
                              select CreateIndex(name, (string)row["INDEX_NAME"])).ToArrayOrNull();
            return result;
        }

        int colid = 0;
        private Index CreateIndex(string tableName, string indexName)
        {
            Index index = new Index();
            index.Name = indexName;

            bool isPriKey = QuerySysTable<bool>("is_primary_key", "indexes", "object_id = OBJECT_ID(@p1) AND [name] = @p2", tableName, index.Name);
            bool isUnique = QuerySysTable<bool>("is_unique_constraint", "indexes", "object_id = OBJECT_ID(@p1) AND [name] = @p2", tableName, index.Name);
            int id = QuerySysTable<int>("index_id", "indexes", "object_id = OBJECT_ID(@p1) AND [name] = @p2", tableName, index.Name);

            if (isPriKey)
                index.Type = IndexType.PrimaryKey;
            else if (isUnique)
                index.Type = IndexType.Unique;

            colid = 1;

            index.Columns = (from row in WalkIndexColumns(tableName, index.Name)
                             let col = row["COLUMN_NAME"]
                             let isdesc = QuerySysTable<bool>("is_descending_key", "index_columns", "object_id = OBJECT_ID(@p1) AND index_id = @p2 AND index_column_id = @p3", tableName, id.ToString(), (colid++).ToString())
                             let sort = isdesc ? " DESC" : " ASC"
                             select col + (index.Type == IndexType.Index ? sort : "")).Join(", ");

            return index;
        }

        private Column CreateColumn(DataRow columnRow)
        {
            string tableName = (string)columnRow["TABLE_NAME"];
            Column column = new Column();

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
            SqlCommand cmd = (SqlCommand)SchemaConnection.CreateCommand();
            cmd.CommandType = CommandType.Text;
            cmd.CommandText = string.Format("SELECT {0} FROM sys.{1} WHERE {2}", col, table, where);

            for (int i = 0; i < args.Length; i++)
            {
                cmd.Parameters.Add("@p" + (i + 1), SqlDbType.Char, args[i].Length).Value = args[i];
            }

            return (T)cmd.ExecuteScalar();
        }

        private string GetCreateStatement(string dbobject)
        {
            SqlCommand cmd = (SqlCommand)SchemaConnection.CreateCommand();
            cmd.CommandType = CommandType.Text;
            cmd.CommandText = "SELECT OBJECT_DEFINITION(OBJECT_ID(@p1));";
            cmd.Parameters.Add("@p1", SqlDbType.Char, dbobject.Length).Value = dbobject;

            var create = cmd.ExecuteScalar();
            if (create is DBNull)
                return string.Format("CREATE [{0}] AS FAILED TO IMPORT", dbobject);
            return (string)create;
        }
    }
}
