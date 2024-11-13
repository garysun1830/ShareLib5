using System;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Web;
using System.Data.OleDb;
using System.Data.SqlClient;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Collections.Generic;
using System.Data.Common;
using System.Web.Configuration;
using System.Text;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Configuration;
using System.IO;
using System.Xml;

namespace ShareLib5
{

    public class TableColumnDef
    {
        public string ColName;
        public string KeyName;
        public string ColType;
        public Int32 Size;
        public Int32 Decimals;

        public TableColumnDef(string AColName, string AColType, Int32 ASize, Int32 ADecimals, bool IsKey)
        {
            ColName = AColName;
            ColType = AColType;
            Size = ASize;
            Decimals = ADecimals;
            if (IsKey)
                KeyName = AColName;
        }

        public TableColumnDef(string AColName, string AColType, Int32 ASize, Int32 ADecimals)
            : this(AColName, AColType, ASize, ADecimals, false)
        {
        }

        public TableColumnDef(string AColName, string AColType, Int32 ASize)
            : this(AColName, AColType, ASize, 0)
        {
        }

        public TableColumnDef(string AColName, string AColType)
            : this(AColName, AColType, 0)
        {
        }
    }

    public static class DatabaseHelper
    {
        public static void ChangeColumnSize(string Table, string ColName, int Size)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("ALTER Table ");
            sb.Append(Table);
            sb.Append(" ALTER ");
            sb.Append(ColName);
            sb.Append("(");
            sb.Append(Size.ToString());
            sb.Append(")");
            SqlHelper param =new SqlHelper (sb.ToString(),false);
         param  . ExecSql();
        }

        public static void AddColumn(string Table, TableColumnDef ColDef)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("ALTER Table ");
            sb.Append(Table);
            sb.Append(" ADD ");
            sb.Append(ColDef.ColName);
            sb.Append(" ");
            sb.Append(ColDef.ColType);
            if (ColDef.Size > 0)
            {
                sb.Append("(");
                sb.Append(ColDef.Size.ToString());
                if (ColDef.Decimals > 0)
                {
                    sb.Append(".");
                    sb.Append(ColDef.Decimals.ToString());
                }
                sb.Append(")");
            }
            if (ColDef.KeyName == ColDef.ColName)
            {
                sb.Append(" ");
                sb.Append("PRIMARY KEY");
            }
            SqlHelper param =new SqlHelper (sb.ToString(),false);
       param .    ExecSql();
        }

        private static DbType ConvertDbType(string StringType)
        {
            switch (StringType.Trim().ToLower())
            {
                case "char":
                    return DbType.String;
                case "dbdate":
                    return DbType.Date;
                case "integer":
                    return DbType.Int32;
                case "decimal":
                    return DbType.Decimal;
                case "boolean":
                    return DbType.Boolean;
                case "datetime":
                    return DbType.DateTime;
                case "currency":
                    return DbType.Currency;
                case "double":
                    return DbType.Double;
                default:
                    throw new Exception(string.Format("Data type {0} not defined", StringType));
            }
        }

        private static DbTypeValue CreateTypeValue(DbType DbType, object Value, string Name, ParameterDirection Direction)
        {
            switch (DbType)
            {
                case DbType.Date:
                    return new DbTypeValue(SqlDbType.DateTime, Value, Name, Direction);
                case DbType.DateTime:
                    return new DbTypeValue(SqlDbType.DateTime, Value, Name, Direction);
                case DbType.String:
                    return new DbTypeValue(SqlDbType.Char, Value, Name, Direction);
                case DbType.Int32:
                    return new DbTypeValue(SqlDbType.Int, Value, Name, Direction);
                case DbType.Decimal:
                    return new DbTypeValue(SqlDbType.Decimal, Value, Name, Direction);
                case DbType.Boolean:
                    return new DbTypeValue(SqlDbType.Bit, Value, Name, Direction);
                case DbType.Currency:
                    return new DbTypeValue(SqlDbType.Money, Value, Name, Direction);
                case DbType.Double:
                    return new DbTypeValue(SqlDbType.Float, Value, Name, Direction);
                default:
                    throw new Exception(string.Format("Data type {0} not defined", DbType.ToString()));
            }
        }

        private static string RemoveNullField(string Sql, string NullField)
        {
            Sql = Sql.Replace('\r', ' ').Replace('\n', ' ');
            if (Sql.ToUpper().Contains("INSERT"))
                return RemoveNullFieldFromInsert(Sql, NullField);
            if (Sql.ToUpper().Contains("UPDATE"))
                return RemoveNullFieldFromUpdate(Sql, NullField);
            throw new Exception(string.Format("RemoveNullField not defined for {0}", Sql));
        }

        private static string RemoveNullFieldFromInsert(string Sql, string NullField)
        {
            string[] strs = Sql.Split(new char[] { ',', '(', ')' });
            foreach (string s in strs)
                if (s.Trim() == NullField)
                    Sql = Sql.Replace(s + ",", "").Replace("," + s, "");
            foreach (string s in strs)
                if (s.Trim() == "[@" + NullField)
                    Sql = Sql.Replace("@" + NullField, "");
            strs = Sql.Split(new char[] { '[', ')', '(' });
            foreach (string s in strs)
                if (!s.Contains("@") && s.Contains("]"))
                    Sql = Sql.Replace("[" + s.Remove(s.IndexOf(']')), "[");
            strs = Sql.Split(new char[] { ',', '(', ')' });
            foreach (string s in strs)
                if (s.Trim() == "[]")
                    Sql = Sql.Replace(s + ",", "").Replace("," + s, "");
            return Sql;
        }

        private static string RemoveNullFieldFromUpdate(string Sql, string NullField)
        {
            List<string> strs = GetSetParams(Sql);
            foreach (string s in strs)
                if (string.Compare(NullField, GetParamFieldName(s), true) == 0)
                    Sql = Sql.Replace(s, "");
            string[] strs1 = Sql.Split(new Char[] { ',' });
            StringBuilder sb = new StringBuilder();
            foreach (string s in strs1)
                if (s.Trim().Length > 0)
                {
                    sb.Append(s);
                    sb.Append(",");
                }
            Sql = sb.ToString();
            Sql = Sql.Remove(Sql.Length - 1, 1);
            return Sql;
        }

        public static string RemoveNullFields(string Sql, List<string> NullFields)
        {
            foreach (string s in NullFields)
                Sql = RemoveNullField(Sql, s);
            return Sql;
        }

        private static List<string> GetSetParams(string Sql)
        {
            int i = Sql.IndexOf("SET", StringComparison.OrdinalIgnoreCase);
            if (i != -1)
                Sql = Sql.Substring(i + 3);
            i = Sql.IndexOf("WHERE", StringComparison.OrdinalIgnoreCase);
            if (i != -1)
                Sql = Sql.Remove(i);
            List<string> strs = new List<string>();
            while (ExtractSetParam(strs, ref Sql)) ;
            return strs;
        }

        private static bool ExtractSetParam(List<string> Strs, ref string Sql)
        {
            Sql = Sql.Trim();
            int i = Sql.IndexOf(']');
            if (i == -1)
                return false;
            string s = Sql.Remove(i) + "]";
            Strs.Add(s.Trim());
            Sql = Sql.Substring(i).Trim();
            if (Sql.StartsWith("]"))
                Sql = Sql.Remove(0, 1).Trim();
            if (Sql.StartsWith(","))
                Sql = Sql.Remove(0, 1).Trim();
            return true;
        }

        private static string GetParamFieldName(string Param)
        {
            string[] strs = Param.Split(new Char[] { '=' });
            foreach (string s in strs)
            {
                if (strs.Length < 2)
                    return "";
                return strs[0].Trim();
            }
            return "";
        }

        public static void CreateTable(string TableName, ArrayList Columns)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("CREATE TABLE ");
            sb.Append(TableName);
            sb.Append(" (");
            foreach (TableColumnDef col in Columns)
            {
                sb.Append(col.ColName);
                sb.Append(" ");
                sb.Append(col.ColType);
                if (col.Size > 0)
                {
                    sb.Append("(");
                    sb.Append(col.Size.ToString());
                    if (col.Decimals > 0)
                    {
                        sb.Append(".");
                        sb.Append(col.Decimals.ToString());
                    }
                    sb.Append(")");
                }
                if (col.KeyName == col.ColName)
                {
                    sb.Append(" ");
                    sb.Append("PRIMARY KEY");
                }
                sb.Append(",");
            }
            string sql = sb.ToString();
            if (sql.EndsWith(","))
                sql = sql.TrimEnd(",".ToCharArray());
            sql += ")";
        new SqlHelper (sql,false) .   ExecSql();
        }

        public static DataTable CreateTable(string TableName, string KeyName, bool KeyAutoInc, string[] ColName, string[] ColType)
        {
            if (ColName.Length != ColType.Length)
                throw new Exception("Column name and type must match.");
            DataTable table = new DataTable(TableName);
            DataColumn id = new DataColumn();
            id.DataType = System.Type.GetType("System.Int32");
            id.ColumnName = KeyName;
            id.AutoIncrement = KeyAutoInc;
            table.Columns.Add(id);
            for (int i = 0; i < ColName.Length; i++)
            {
                DataColumn col = new DataColumn();
                col.DataType = System.Type.GetType(ColType[i]);
                col.ColumnName = ColName[i];
                if (col.DataType == System.Type.GetType("System.Int32"))
                    col.DefaultValue = 0;
                if (col.DataType == System.Type.GetType("System.String"))
                    col.DefaultValue = "";
                table.Columns.Add(col);
            }
            DataColumn[] keys = new DataColumn[1];
            keys[0] = id;
            table.PrimaryKey = keys;
            return table;
        }

    }

}
