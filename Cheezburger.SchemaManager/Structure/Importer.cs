using System;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;
using System.Text.RegularExpressions;
using Cheezburger.SchemaManager.Extensions;

namespace Cheezburger.Common.Database.Structure
{
    public enum CallbackAction
    {
        Before,
        After,
    }

    public class Importer : SchemaWalker
    {
        private Schema schema;
        private Microsoft.Practices.EnterpriseLibrary.Data.Database db;
        private readonly Func<string, Schema> _resolve;
        private readonly Action<string> log;

        private DbConnection transatedConnection;
        private DbTransaction transaction;
        private int version = -1;

        private List<string> existingTables = new List<string>();

        private Dictionary<string, List<string>> existingColumns =
            new Dictionary<string, List<string>>(StringComparer.InvariantCultureIgnoreCase);

        private Dictionary<string, List<string>> existingIndexes =
            new Dictionary<string, List<string>>(StringComparer.InvariantCultureIgnoreCase);

        private Dictionary<string, List<string>> existingForeignKeys =
            new Dictionary<string, List<string>>(StringComparer.InvariantCultureIgnoreCase);

        private List<string> existingViews = new List<string>();
        private List<string> existingProcedures = new List<string>();
        private List<string> finalUpdates = new List<string>();

        public delegate void DebugLogHandler(string format, params object[] args);

        public static DebugLogHandler DebugLog = (s, a) => { };

        private Importer(Schema schema, Microsoft.Practices.EnterpriseLibrary.Data.Database db,
                         Func<string, Schema> resolve, Action<string> log)
            : base(db)
        {
            this.schema = schema;
            this.db = db;
            _resolve = resolve;
            this.log = log ?? (s => { });
        }

        public static void Upgrade(Schema schema, 
                                   Microsoft.Practices.EnterpriseLibrary.Data.Database db, 
                                   Func<string, Schema> resolve, 
                                   bool forceFullUpdate, Action<string> log)
        {
            if (schema == null) throw new ArgumentNullException("schema");
            if (db == null) throw new ArgumentNullException("db");

            using (Importer importer = new Importer(schema, db, resolve, log))
            {
                importer.InternalUpgrade(forceFullUpdate);
            }
        }

        #region Utility functions
        private DbCommand CreateCommand(string commandText, object[] args)
        {
            DbCommand command = transatedConnection.CreateCommand();
            command.Transaction = transaction;
            command.CommandType = CommandType.Text;
            command.CommandText = commandText;
            command.CommandTimeout = (int)TimeSpan.FromDays(1).TotalSeconds;

            for (int i = 0; i < args.Length; i++)
            {
                DbParameter param = command.CreateParameter();
                param.ParameterName = "@p" + (i + 1);
                param.Value = args[i];
                command.Parameters.Add(param);
            }

            return command;
        }

        private object ExecuteScalar(string commandText, params object[] args)
        {
            object result;
            try
            {
                using (DbCommand command = CreateCommand(commandText, args))
                    result = command.ExecuteScalar();
            }
            catch (Exception ex)
            {
                throw new Exception("Error in command: " + commandText, ex);
            }
            if (result is DBNull)
                return null;
            return result;
        }

        private IDataReader ExecuteReader(string commandText, params object[] args)
        {
            try
            {
                using (DbCommand command = CreateCommand(commandText, args))
                    return command.ExecuteReader();
            }
            catch (Exception ex)
            {
                throw new Exception("Error in command: " + commandText, ex);
            }
        }

        private void ExecuteNonQuery(string commandText, params object[] args)
        {
            try
            {
                using (DbCommand command = CreateCommand(commandText, args))
                    command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw new Exception("Error in command:\n" + commandText, ex);
            }
        }

        private int GetVersionInfo()
        {
            object oVersion = null;
            try
            {
                oVersion = ExecuteScalar("SELECT VersionId FROM [Version] WHERE SchemaName = @p1;", schema.Name);
            }
            catch
            {
                Table version = new Table();
                version.Name = "Version";
                version.Columns = new Column[]
                    {
                        new Column("VersionId", "int", null),
                        new Column("SchemaName", "varchar", "250"),
                    };
                version.Indexes = new Index[]
                    {
                        new Index("PK_Version", IndexType.PrimaryKey, "SchemaName"),
                    };

                CreateTable(version, new Queue<SchemaItem>());
            }

            if (oVersion == null || oVersion is DBNull)
                return -1;
            else
                return Convert.ToInt32(oVersion);
        }

        private Type delegateType = typeof(Action<DbConnection, DbTransaction, CallbackAction, int>);
        Type[] argTypes = new Type[] { typeof(DbConnection), typeof(DbTransaction), typeof(CallbackAction), typeof(int) };
        private void RunCallback(SchemaItem schemaItem, CallbackAction actionName)
        {
            if (schemaItem.Callback == null)
                return;

            string callbackName = schemaItem.Callback.Method + "," + schemaItem.Callback.Type;

            var timer = new Stopwatch();
            timer.Start();
            try
            {
                var args = new object[] { transatedConnection, transaction, actionName, version };
                CallbackHelper.RunCallback(delegateType, callbackName, args, argTypes);
            }
            catch (Exception ex)
            {
                throw new Exception("Error in " + actionName + " callback", ex);
            }
            finally
            {
                timer.Stop();
                if (timer.ElapsedMilliseconds > 20)
                    DebugLog("Callback: {2} {0} {1}ms", callbackName, timer.ElapsedMilliseconds, actionName);
            }
        }
        #endregion

        private void InternalUpgrade(bool forceFullUpdate)
        {
            DbConnection connection = db.CreateConnection();
            connection.Open();
            using (transatedConnection = connection)
            {
                using (transaction = transatedConnection.BeginTransaction(IsolationLevel.Serializable))
                {
                    FillMetaData();

                    ProcessUpgrade(forceFullUpdate);

                    foreach (var update in finalUpdates)
                        ExecuteNonQuery(update);

                    transaction.Commit();
                }
            }
        }

        private void ProcessUpgrade(bool skipVersionCheck)
        {
            version = GetVersionInfo();

            var logIntro = string.Format("Upgrading{0}: {1} ({2} -> {3})", skipVersionCheck ? " With Force" : "", schema.Name, version, schema.Version);
            log(logIntro);
            DebugLog(logIntro);

            RunCallback(schema, CallbackAction.Before);
            if (skipVersionCheck || schema.Version > version)
            {
                var timer = new Stopwatch();
                timer.Start();
                CreateTables();
                timer.Stop();
                DebugLog("Tables: {0}ms", timer.ElapsedMilliseconds);
                timer.Reset();

                timer.Start();
                CreateViews();
                timer.Stop();
                DebugLog("CreateViews: {0}ms", timer.ElapsedMilliseconds);
                timer.Reset();

                timer.Start();
                CreateProcedures();
                timer.Stop();
                DebugLog("CreateProcedures: {0}ms", timer.ElapsedMilliseconds);
                timer.Reset();

                timer.Start();
                CreateFunctions();
                timer.Stop();
                DebugLog("CreateFunctions: {0}ms", timer.ElapsedMilliseconds);
                timer.Reset();

                timer.Start();
                UpdateVersion();
                timer.Stop();
                DebugLog("UpdateVersion: {0}ms", timer.ElapsedMilliseconds);
                timer.Reset();
            }
            ProcessIncludes(skipVersionCheck);
            RunCallback(schema, CallbackAction.After);
        }

        private void CreateFunctions()
        {
            if (schema.Functions == null)
            {
                return;
            }
            foreach (UserFunction function in schema.Functions)
            {
                CreateFunction(function);
            }
        }

        private void CreateFunction(UserFunction userFunction)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("CREATE FUNCTION ");
            sb.AppendFormat("[{0}]", userFunction.Name);

            if (userFunction.Parameters != null)
            {
                sb.Append("(");
                foreach (var param in userFunction.Parameters)
                {
                    if (param == userFunction.Parameters[0])
                        sb.AppendLine();
                    else
                        sb.AppendLine(",");

                    sb.AppendFormat("    {0} {1}{2}{3}",
                        param.Name,
                        GetTypeDescriptor(param),
                        !string.IsNullOrEmpty(param.DefaultValue) ? " = " + param.DefaultValue : "",
                        param.Direction == ProcedureParameterDirection.Out ? " OUT " : "");
                }
                sb.Append(")");
            }

            sb.AppendFormat("RETURNS {0} TABLE ", userFunction.ReturnTable.Name);

            if (userFunction.ReturnTable.Columns != null)
            {
                sb.Append("(");
                foreach (var column in userFunction.ReturnTable.Columns)
                {
                    if (column == userFunction.ReturnTable.Columns[0])
                        sb.AppendLine();
                    else
                        sb.AppendLine(",");

                    sb.AppendFormat("    {0} {1}",
                                    column.Name,
                                    GetTypeDescriptor(column));
                }
                sb.Append(")");
            }

            sb.AppendLine(" AS  ");
            sb.AppendLine(" BEGIN ");
            sb.AppendLine(ReplaceMacros(userFunction.Body));
            sb.AppendLine(" RETURN  END ");

            string oldCreateStatement = (string)ExecuteScalar("SELECT OBJECT_DEFINITION(OBJECT_ID(@p1));", userFunction.Name);
            string createStatement = sb.ToString();

            if (!string.IsNullOrEmpty(oldCreateStatement))
            {
                if (oldCreateStatement == createStatement)
                {
                    return;
                }
                ExecuteNonQuery("DROP FUNCTION @p1", userFunction.Name);
            }

            log("Creating Procedure: " + userFunction.Name);

            ExecuteNonQuery(createStatement);

            ExecuteNonQuery("GRANT SELECT ON [" + userFunction.Name + "] TO mine_users");
            RunCallback(userFunction, CallbackAction.After);

        }

        private void ProcessIncludes(bool skipVersionCheck)
        {
            if (schema.Includes == null)
                return;

            int oldVersion = version;
            Schema oldSchema = schema;

            try
            {
                foreach (var name in schema.Includes)
                {
                    version = -1;
                    var timer = new Stopwatch();
                    timer.Start();
                    schema = _resolve(name);
                    timer.Stop();
                    DebugLog("Load Schema: {0} {1}ms", name, timer.ElapsedMilliseconds);
                    ProcessUpgrade(skipVersionCheck);
                }
            }
            finally
            {
                version = oldVersion;
                schema = oldSchema;
            }
        }

        private void UpdateVersion()
        {
            if (schema.Version <= 0 || schema.Name == null)
                throw new ArgumentException("Schema objects require a Version > 0 and a non-null Name.");

            if (version == -1)
                ExecuteNonQuery("INSERT INTO [Version] (VersionId, SchemaName) VALUES (@p1, @p2)", schema.Version, schema.Name);
            else
                ExecuteNonQuery("UPDATE [Version] SET VersionId = @p1 WHERE SchemaName = @p2", schema.Version, schema.Name);
        }

        private void FillMetaData()
        {
            var timer = new Stopwatch();
            timer.Start();

            foreach (string table in WalkTables())
            {
                existingTables.Add(table);

                existingColumns[table] = (from row in WalkColumns(table)
                                          select ((string)row["COLUMN_NAME"]).Trim()).ToList();

                existingIndexes[table] = (from row in WalkIndexes(table)
                                          select ((string)row["INDEX_NAME"]).Trim()).ToList();

                existingForeignKeys[table] = (from row in WalkForeignKeys(table)
                                              select ((string)row["CONSTRAINT_NAME"]).Trim()).ToList();
            }

            foreach (string name in WalkViews())
            {
                existingViews.Add(name);

                existingIndexes[name] = WalkViewIndexes(name).ToList();
            }

            existingProcedures.AddRange(from row in WalkStoredProcedures()
                                        select ((string)row["ROUTINE_NAME"]).Trim());

            timer.Stop();
            DebugLog("");
            DebugLog("FillMetaData: {0}ms", timer.ElapsedMilliseconds);
        }

        bool Exists(List<string> list, string name)
        {
            if (list == null)
                return false;
            return list.Contains(name.Trim(), StringComparer.InvariantCultureIgnoreCase);
        }

        private void CreateProcedures()
        {
            if (schema.Procedures == null)
                return;

            foreach (var proc in schema.Procedures)
                CreateProdcedure(proc, Exists(existingProcedures, proc.Name));
        }

        private void CreateProdcedure(Procedure proc, bool alterInsteadOfCreate)
        {
            StringBuilder sb = new StringBuilder("CREATE PROCEDURE ");
            sb.AppendFormat("[{0}]", proc.Name);

            if (proc.Parameters != null)
            {
                sb.Append("(");
                foreach (var param in proc.Parameters)
                {
                    if (param == proc.Parameters[0])
                        sb.AppendLine();
                    else
                        sb.AppendLine(",");

                    sb.AppendFormat("    {0} {1}{2}{3}",
                        param.Name,
                        GetTypeDescriptor(param),
                        !string.IsNullOrEmpty(param.DefaultValue) ? " = " + param.DefaultValue : "",
                        param.Direction == ProcedureParameterDirection.Out ? " OUT " : "");
                }
                sb.Append(")");
            }

            sb.AppendLine("AS");
            sb.AppendLine("BEGIN");
            sb.AppendLine(ReplaceMacros(proc.Body));
            sb.AppendLine("END");

            string oldCreateStatement = (string)ExecuteScalar("SELECT OBJECT_DEFINITION(OBJECT_ID(@p1));", proc.Name);
            string createStatement = sb.ToString();

            if (oldCreateStatement == createStatement)
                return;

            if (!alterInsteadOfCreate)
                log("Creating Procedure: " + proc.Name);
            else
                log("Updating Procedure: " + proc.Name);

            if (!alterInsteadOfCreate)
                RunCallback(proc, CallbackAction.Before);
            else
                createStatement = "ALTER" + createStatement.Substring("CREATE".Length);

            ExecuteNonQuery(createStatement);

            ExecuteNonQuery("GRANT EXECUTE, VIEW DEFINITION ON [" + proc.Name + "] TO mine_users");
            RunCallback(proc, CallbackAction.After);
        }

        private string ReplaceMacros(string text)
        {
            if (schema.Macros == null)
                return text;

            foreach (var macro in schema.Macros)
            {
                var key = Regex.Escape("$(" + macro.Name + ")");
                text = Regex.Replace(text, key, macro.Value, RegexOptions.IgnoreCase);
            }

            return text;
        }

        private void CreateTables()
        {
            if (schema.Tables == null)
                return;

            foreach (var table in schema.Tables)
            {
                Queue<SchemaItem> callbackQueue = new Queue<SchemaItem>();
                if (Exists(existingTables, table.Name))
                {
                    UpgradeTable(table, callbackQueue);
                }
                else
                {
                    CreateTable(table, callbackQueue);
                }

                foreach (var item in callbackQueue)
                    RunCallback(item, CallbackAction.After);
            }
        }

        private void CreateTable(Table table, Queue<SchemaItem> callbackQueue)
        {
            log("Creating Table: " + table.Name);
            RunCallback(table, CallbackAction.Before);
            callbackQueue.Enqueue(table);

            ExecuteNonQuery("CREATE TABLE [" + table.Name + "] ( [__JUST_CREATED] INT NULL )");
            UpgradeTable(table, callbackQueue);
            ExecuteNonQuery("ALTER TABLE [" + table.Name + "] DROP COLUMN [__JUST_CREATED]");
        }

        private void UpgradeTable(Table table, Queue<SchemaItem> callbackQueue)
        {
            List<string> indexNames = existingIndexes.GetValueOrDefault(table.Name.Trim());
            List<string> columnNames = existingColumns.GetValueOrDefault(table.Name.Trim());

            foreach (var column in table.Columns)
            {
                if (Exists(columnNames, column.Name))
                {
                    try
                    {
                        // we can alter columns if they aren't in any 
                        bool alterable = column.DefaultValue == null && (column.IsIdentity == null || !bool.Parse(column.IsIdentity));

                        if (alterable && table.Indexes != null)
                            foreach (var index in table.Indexes)
                                if (alterable && Exists(indexNames, index.Name))
                                {
                                    alterable = index.Include != "*"
                                        && !Exists(SplitColumnList(index.Columns).ToList(), column.Name)
                                        && !Exists(SplitColumnList(index.Include).ToList(), column.Name);
                                }

                        if (alterable)
                            ExecuteNonQuery("ALTER TABLE [" + table.Name + "] ALTER COLUMN [" + column.Name + "] " +
                                GetTypeDescriptor(column) + (column.Nullable ? " NULL" : " NOT NULL"));
                    }
                    catch (Exception ex)
                    {
                        // Most of the time this means nothing or has to be fixed manually
                        log(string.Format("Warning: alter column {0}.{1} failed: {2}", table.Name, column.Name, ex.GetBaseException().Message));
                    }
                }
                else
                {
                    string renameFrom = null;
                    if (column.OldNames != null)
                        renameFrom = column.OldNames.Where(oldname => Exists(columnNames, oldname)).FirstOrDefault();
                    if (renameFrom != null)
                        RenameColumn(table, column, renameFrom, callbackQueue);
                    else
                        CreateColumn(table, column, callbackQueue);
                }

                if (!string.IsNullOrEmpty(column.References))
                    CreateForeignKey(table, column);
            }

            if (table.Indexes != null)
            {
                foreach (var index in table.Indexes)
                {
                    var exists = Exists(indexNames, index.Name);
                    if (exists && index.Include != "*")
                    {
                        // currently we don't upgrade exising indexes
                    }
                    else
                    {
                        CreateIndex(table, index, exists, callbackQueue);
                    }
                }
            }
        }

        private void RenameColumn(Table table, Column column, string renameFrom, Queue<SchemaItem> callbackQueue)
        {
            log("Renaming Column: " + renameFrom + " -> " + column.Name);

            ExecuteNonQuery("EXEC sp_rename @p1, @p2, 'COLUMN';",
                table.Name + "." + renameFrom,
                column.Name);
        }

        private void CreateForeignKey(Table table, Column column)
        {
            string keyname = string.Format("FK_{0}_{1}_{2}", table.Name, column.Name, column.References);
            List<string> fkeys = existingForeignKeys.GetValueOrDefault(table.Name.Trim());
            if (Exists(fkeys, keyname))
                return;

            log("Creating Foreign Key: " + keyname);

            existingForeignKeys.GetValue(table.Name, () => new List<string>()).Add(keyname);
            finalUpdates.Add(string.Format("ALTER TABLE [{0}] ADD CONSTRAINT [FK_{0}_{1}_{2}] FOREIGN KEY ([{1}]) REFERENCES [{2}];", table.Name, column.Name, column.References));
        }

        private void CreateColumn(Table table, Column column, Queue<SchemaItem> callbackQueue)
        {
            log("Creating Column: " + column.Name);
            RunCallback(column, CallbackAction.Before);
            callbackQueue.Enqueue(column);
            string typeDesc = GetColumnDescriptor(column);

            string query = string.Format("ALTER TABLE [{0}] ADD [{1}] {2}", table.Name, column.Name, typeDesc);
            ExecuteNonQuery(query);
        }

        private static string GetColumnDescriptor(Column column)
        {
            string typeDesc = GetTypeDescriptor(column);

            if (!column.Nullable)
                typeDesc += " NOT NULL";
            if (!string.IsNullOrEmpty(column.DefaultValue))
                typeDesc += " DEFAULT (" + column.DefaultValue + ")";

            bool identity;
            if (Boolean.TryParse(column.IsIdentity, out identity) && identity)
                typeDesc += " IDENTITY";
            return typeDesc;
        }

        internal static string CreateTableWithoutCallbacksOrIndexesOrReferencesString(Table table)
        {
            return string.Format("CREATE TABLE [{0}] ({1})", table.Name,
                                 (from c in table.Columns select c.Name + " " + GetColumnDescriptor(c)).Join(", "));
        }

        private static string GetTypeDescriptor(NameWithType nameWithType)
        {
            switch (nameWithType.Type.ToLower())
            {
                case "varchar[]":
                case "string[]":
                    return "varchar_array READONLY";
                case "int[]":
                    return "int_array READONLY";
                case "long[]":
                case "bigint[]":
                    return "bigint_array READONLY";
                default:
                    string typeDesc = nameWithType.Type;
                    if (!string.IsNullOrEmpty(nameWithType.Length))
                        typeDesc += "(" + nameWithType.Length + ")";
                    return typeDesc;
            }
        }

        private void CreateIndex(Table table, Index index, bool update, Queue<SchemaItem> callbackQueue)
        {
            RunCallback(index, CallbackAction.Before);
            callbackQueue.Enqueue(index);
            switch (index.Type)
            {
                case IndexType.PrimaryKey:
                    CreateIndexPrimaryKey(table, index);
                    break;
                case IndexType.Index:
                    CreateIndexRegular(table.Name, table.Columns, update, index);
                    break;
                case IndexType.Unique:
                    CreateIndexUnique(table.Name, index);
                    break;
                default:
                    break;
            }
        }

        private void CreateIndex(View view, Index index)
        {
            RunCallback(index, CallbackAction.Before);
            switch (index.Type)
            {
                case IndexType.PrimaryKey:
                    CreateIndexPrimaryKey(view, index);
                    break;
                case IndexType.Index:
                    CreateIndexRegular(view.Name, null, false, index);
                    break;
                case IndexType.Unique:
                    CreateIndexUnique(view.Name, index);
                    break;
                default:
                    break;
            }
            RunCallback(index, CallbackAction.After);
        }

        private void CreateIndexPrimaryKey(Table table, Index index)
        {
            log("Creating Primary Key: " + table.Name + " (" + index.Columns + ")");
            ExecuteNonQuery(string.Format("ALTER TABLE [{0}] ADD CONSTRAINT [{1}] PRIMARY KEY ({2});", table.Name, index.Name, EscapeColumnList(false, index.Columns)));
        }

        private void CreateIndexPrimaryKey(View view, Index index)
        {
            log("Creating Primary Key: " + view.Name + " (" + index.Columns + ")");
            ExecuteNonQuery(string.Format("CREATE UNIQUE CLUSTERED INDEX {1} ON [{0}] ({2});", view.Name, index.Name, EscapeColumnList(false, index.Columns)));
        }

        private void CreateIndexRegular(string tableName, IEnumerable<Column> cols, bool update, Index index)
        {
            log("Creating Index: " + tableName + "." + index.Name);
            var include = "";
            if (!string.IsNullOrEmpty(index.Include))
            {
                if (index.Include != "*")
                    include = " INCLUDE (" + EscapeColumnList(false, index.Include) + ")";
                else if (cols == null)
                    throw new InvalidProgramException("includ=* only allowed on tables");
                else
                {
                    var ignored = SplitColumnList(index.Columns).ToList();
                    include = " INCLUDE (" + EscapeColumnList(false, from col in cols
                                                                     where !Exists(ignored, col.Name)
                                                                     orderby col.Name
                                                                     select col.Name) + ")";
                }
            }
            var query = string.Format("CREATE INDEX [{1}] ON [{0}]({2}){3}", tableName, index.Name, EscapeColumnList(true, index.Columns), include);
            if (update)
            {
                if (query == GenerateSchemaForIndex(tableName, index))
                    return;
                query += " WITH (DROP_EXISTING = ON)";
            }
            ExecuteNonQuery(query);
        }

        private string GenerateSchemaForIndex(string tableName, Index index)
        {
            var query = string.Format(@"SELECT c.name cname, is_descending_key, is_included_column
FROM sys.tables t JOIN sys.indexes i ON t.object_id = i.object_id 
JOIN sys.index_columns ic ON i.index_id = ic.index_id AND t.object_id = ic.object_id
JOIN sys.columns c on ic.column_id = c.column_id AND t.object_id = c.object_id
WHERE t.name = '{0}' AND i.name = '{1}'
ORDER BY key_ordinal + is_included_column*10, c.name", tableName, index.Name);

            bool first = true;
            bool inIncludes = false;
            var sb = new StringBuilder("CREATE INDEX [" + index.Name + "] ON [" + tableName + "](");
            using (var reader = ExecuteReader(query))
            {
                while (reader.Read())
                {
                    var name = reader.GetString(0);
                    var order = "";
                    if (!reader.GetBoolean(2)) // is not include
                    {
                        if (!first)
                            sb.Append(", ");
                        else
                            first = false;
                        if (reader.GetBoolean(1)) // is desc
                            order = " DESC";
                        else
                            order = " ASC";
                    }
                    else
                    {
                        if (!inIncludes)
                        {
                            sb.Append(") INCLUDE (");
                            inIncludes = true;
                        }
                        else
                            sb.Append(", ");
                    }
                    sb.AppendFormat("[{0}]{1}", name, order);
                }
            }
            sb.Append(")");
            return sb.ToString();
        }

        private void CreateIndexUnique(string tableName, Index index)
        {
            log("Creating Index: " + tableName + "." + index.Name);
            ExecuteNonQuery(string.Format("ALTER TABLE [{0}] ADD CONSTRAINT [{1}] UNIQUE ({2});", tableName, index.Name, EscapeColumnList(false, index.Columns)));
        }

        private string[] SplitColumnList(string columnNames)
        {
            if (string.IsNullOrEmpty(columnNames))
                return new string[0];

            string[] columns = columnNames.Split(',');
            for (int i = 0; i < columns.Length; i++)
            {
                string column = columns[i].Trim();

                string order = "";
                if (column.EndsWith(" DESC", StringComparison.InvariantCultureIgnoreCase))
                    order = " DESC";
                else if (column.EndsWith(" ASC", StringComparison.InvariantCultureIgnoreCase))
                    order = " ASC";

                columns[i] = column.Substring(0, column.Length - order.Length).Trim();
            }
            return columns;
        }

        private string EscapeColumnList(bool forceOrder, string columnNames)
        {
            return EscapeColumnList(forceOrder, columnNames.Split(','));
        }

        private string EscapeColumnList(bool forceOrder, IEnumerable<string> columnNames)
        {
            var names = columnNames.Select(column =>
                                          {
                                              column = column.Trim();

                                              string order = "";
                                              if (column.EndsWith(" DESC", StringComparison.InvariantCultureIgnoreCase))
                                                  order = " DESC";
                                              else if (column.EndsWith(" ASC", StringComparison.InvariantCultureIgnoreCase))
                                                  order = " ASC";
                                              column = column.Substring(0, column.Length - order.Length);

                                              if (order.Length == 0 && forceOrder)
                                                  order = " ASC";

                                              return "[" + column.Trim() + "]" + order;
                                          });
            return names.Join(", ");
        }

        private void CreateViews()
        {
            if (schema.Views == null)
                return;

            foreach (var view in schema.Views)
            {
                var mustRebuildIndexes = CreateView(view, Exists(existingViews, view.Name));

                var indexNames = existingIndexes.GetValueOrDefault(view.Name) ?? new List<string>();
                if (view.Indexes != null)
                    foreach (var index in view.Indexes.Where(index => mustRebuildIndexes || !Exists(indexNames, index.Name)))
                        CreateIndex(view, index);
            }
        }

        private bool CreateView(View view, bool alterInsteadOfCreate)
        {
            StringBuilder sb = new StringBuilder("CREATE VIEW ");
            sb.AppendFormat("[dbo].[{0}]", view.Name);

            if (view.Indexes != null && view.Indexes.Length > 0)
                sb.Append(" WITH SCHEMABINDING");

            sb.AppendLine(" AS ");
            sb.AppendLine(view.Body.Trim());

            string oldCreateStatement = (string)ExecuteScalar("SELECT OBJECT_DEFINITION(OBJECT_ID(@p1));", view.Name);
            string createStatement = sb.ToString();

            if (oldCreateStatement == createStatement)
                return false;

            log("Creating View: " + view.Name);

            if (!alterInsteadOfCreate)
                RunCallback(view, CallbackAction.Before);
            else
                createStatement = "ALTER" + createStatement.Substring("CREATE".Length);

            ExecuteNonQuery(createStatement);

            ExecuteNonQuery("GRANT ALL, VIEW DEFINITION ON [" + view.Name + "] TO mine_users");
            RunCallback(view, CallbackAction.After);
            return true;
        }

       
    }
}
