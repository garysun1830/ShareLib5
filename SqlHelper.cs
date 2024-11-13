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
using System.Configuration;
using System.IO;

namespace ShareLib5
{

    public class DbTypeValue
    {
        public object Value;
        public SqlDbType dbType;
        public string Name;
        public ParameterDirection Direction;

        public DbTypeValue(SqlDbType ADbType, object AValue, string AName, ParameterDirection ADirection)
        {
            dbType = ADbType;
            Value = AValue;
            Name = AName;
            Direction = ADirection;
        }
    }

    public class SqlHelper
    {
        private string _connectionName;
        private string _sql;
        private List<DbTypeValue> _paramList;
        private CommandType _cmdType;
        private SqlConnection _conn;
        private SqlCommand _command;
        private string _retParamName;
        private SqlDbType _retType;
        public static string ConnectionString = null;

        public SqlHelper()
        {
        }

        public SqlHelper(string ConnectionName, string Sql, CommandType Cmd, bool SqlName, params string[] Args)
        {
            if (string.IsNullOrEmpty(ConnectionName))
                ConnectionName = MyFunc.GetSessionData("DefaultConnection").ToString();

            _connectionName = ConnectionName;
            if (SqlName)
            {
                _sql = GetSqlText(Sql);
                if (_sql.Length == 0)
                {
                    _cmdType = CommandType.StoredProcedure;
                    _sql = Sql;
                    return;
                }
            }
            else
                _sql = Sql;
            _sql = _sql.Replace("\n", " ").Replace("\r", " ");
            if (Args != null)
                _sql = string.Format(_sql, Args);
            _cmdType = Cmd;
            _paramList = new List<DbTypeValue>();
            CreateParamList();
            if (IsStoredProc())
            {
                _cmdType = CommandType.StoredProcedure;
                _sql = Sql;
            }
        }

        public SqlHelper(string ConnectionName, string Sql, CommandType Cmd, bool SqlName)
            : this(ConnectionName, Sql, Cmd, SqlName, null)
        {
        }

        public SqlHelper(string ConnectionName, string Sql, CommandType Cmd)
            : this(ConnectionName, Sql, CommandType.Text, true)
        {
        }

        public SqlHelper(string Sql, bool SqlName)
            : this("", Sql, CommandType.Text, SqlName)
        {
        }

        public SqlHelper(string Sql, CommandType Cmd, bool SqlName)
            : this("", Sql, Cmd, SqlName)
        {
        }

        public SqlHelper(string Sql, CommandType Cmd)
            : this("", Sql, Cmd, true)
        {
        }

        public SqlHelper(string Sql)
            : this("", Sql, CommandType.Text)
        {
        }

        public SqlHelper(string Sql, int NoUse, bool SqlName, params string[] Args)
            : this("", Sql, CommandType.Text, SqlName, Args)
        {
        }

        public SqlHelper(string Sql, int NoUse, params string[] Args)
            : this(Sql, NoUse, true, Args)
        {
        }

        public static string GetSqlText(string Sql)
        {
            return MyFunc.GetWebconfigValue(Sql, "");
        }

        private static string GetSqlText1(string Sql)
        {
            string result = MyFunc.GetWebconfigValue(Sql, "");
            if (string.IsNullOrEmpty(result))
            {
                object o = MyFunc.GetWebconfigValue(Sql);
                if (o == null)
                    throw new Exception(string.Format("SQL '{0}' not defined", Sql));
            }
            return result;
        }

        private bool SetExistingValue(string Name, object Value)
        {
            if (_paramList != null)
                foreach (DbTypeValue tv in _paramList)
                    if (tv != null)
                        if (string.Compare(tv.Name.Replace("@", ""), Name, true) == 0)
                        {
                            tv.Value = Value;
                            return true;
                        }
            return false;
        }

        public void SetValue(string Name, object Value)
        {
            SetValue(Name, Value, ParameterDirection.Input);
        }

        public void SetValue(string Name, object Value, ParameterDirection Dir)
        {
            if (SetExistingValue(Name, Value))
                return;
            if (_paramList == null)
                _paramList = new List<DbTypeValue>();
            DbTypeValue tv = new DbTypeValue(ConvertToSqlDbType(Value.GetType()), Value, Name, Dir);
            _paramList.Add(tv);
        }

        public string Sql
        {
            set { _sql = value; }
            get { return _sql; }
        }

        public void SetValue(int Id, object Value)
        {
            if (_paramList != null)
                _paramList[Id].Value = Value;
        }

        private SqlDbType ConvertToSqlDbType(string DbType)
        {
            switch (DbType.ToLower())
            {
                case "char":
                case "nvarchar":
                case "text":
                    return SqlDbType.Char;
                case "integer":
                case "int":
                    return SqlDbType.Int;
                case "decimal":
                    return SqlDbType.Decimal;
                case "boolean":
                case "bit":
                    return SqlDbType.Bit;
                case "dbdate":
                case "datetime":
                    return SqlDbType.DateTime;
                case "currency":
                case "money":
                    return SqlDbType.Money;
                case "double":
                case "float":
                    return SqlDbType.Float;
                default:
                    throw new Exception(string.Format("Data type {0} not defined", DbType));
            }
        }

        private SqlDbType ConvertToSqlDbType(Type ValueType)
        {
            switch (ValueType.Name.ToLower())
            {
                case "string":
                    return SqlDbType.Char;
                case "int32":
                    return SqlDbType.Int;
                case "boolean":
                    return SqlDbType.Bit;
                case "datetime":
                    return SqlDbType.DateTime;
                case "decimal":
                    return SqlDbType.Decimal;
                case "double":
                case "float":
                    return SqlDbType.Float;
                default:
                    throw new Exception(string.Format("Data type {0} not defined", ValueType.Name));
            }
        }

        private ParameterDirection ConvertToDirection(string Direction)
        {
            switch (Direction.Trim().ToLower())
            {
                case "output":
                    return ParameterDirection.Output;
                case "input":
                    return ParameterDirection.Input;
                case "inputoutput":
                    return ParameterDirection.InputOutput;
                case "returnvalue":
                    return ParameterDirection.ReturnValue;
                default:
                    return ParameterDirection.Input;
            }
        }

        private void CreateParamList()
        {
            MatchCollection mm = Regex.Matches(_sql, "\\[@((?!\\]).)*\\]");
            foreach (Match m in mm)
            {
                Match m2 = Regex.Match(m.Value, "\\[@((?!,).)*,");
                if (!m2.Success)
                    continue;
                string name = m2.Value.Replace("[", "").Replace(",", "").Trim().ToLower();
                m2 = Regex.Match(m.Value, ",((?!\\]).)*\\]");
                if (!m2.Success)
                    continue;
                string dbtype = m2.Value;
                string dir = "";
                Match m3 = Regex.Match(m2.Value, ",((?!,).)*,");
                if (m3.Success)
                {
                    dbtype = m3.Value;
                    dir = m2.Value.Substring(m3.Index + m3.Length).Replace(",", "").Replace("]", "");
                }
                dbtype = dbtype.Replace(",", "").Replace("]", "").Trim().ToLower();
                _paramList.Add(new DbTypeValue(ConvertToSqlDbType(dbtype), null, name, ConvertToDirection(dir)));
            }
        }

        private SqlConnection CreateConnection()
        {
            string connstr = "";
            if (ConnectionString == null)
            {
                if (string.IsNullOrEmpty(_connectionName))
                    _connectionName = "DefaultConnection";
                connstr = WebConfigurationManager.ConnectionStrings[_connectionName].ConnectionString;
            }
            else
                connstr = ConnectionString;
            SqlConnection conn = new SqlConnection(connstr);
            if (conn == null)
                throw new Exception("Cannot create a connection to the database.");
            conn.Open();
            return conn;
        }

        private string RemoveDbType()
        {
            string sql = _sql;
            if (_cmdType == CommandType.StoredProcedure)
                sql = Regex.Replace(sql, "\\[.+", "");
            else
                sql = Regex.Replace(sql, ",[ ]*\\w+\\]", "").Replace("[@", "@");
            return sql.Trim();
        }

        private void SetDbParameter(SqlCommand Command, DbTypeValue TypeValue)
        {
            SqlParameter param = Command.Parameters.AddWithValue(TypeValue.Name, TypeValue.Value);
            param.Direction = TypeValue.Direction;
            param.SqlDbType = TypeValue.dbType;
        }

        private void SetDbCommand()
        {
            _command = new SqlCommand(RemoveDbType(), _conn);
            _command.CommandType = _cmdType;
            if (_paramList != null)
                foreach (DbTypeValue tv in _paramList)
                    if (tv != null)
                        SetDbParameter(_command, tv);
        }

        public bool IsStoredProc()
        {
            if (string.IsNullOrEmpty(_sql))
                return true;
            if (string.IsNullOrEmpty(_sql.Trim()))
                return true;
            return _cmdType == CommandType.StoredProcedure;
        }

        public DbTypeValue FindItemByName(string Name)
        {
            foreach (DbTypeValue v in _paramList)
                if (v.Name == Name)
                    return v;
            return null;
        }

        public void ExecSql()
        {
            using (_conn = CreateConnection())
            {
                SetDbCommand();
                if (!string.IsNullOrEmpty(_retParamName))
                {
                    SqlParameter param = _command.Parameters.AddWithValue(_retParamName, null);
                    param.Direction = ParameterDirection.ReturnValue;
                    param.SqlDbType = _retType;
                }
                _command.ExecuteNonQuery();
            }
        }

        public object ExecuteScalar()
        {
            using (_conn = CreateConnection())
            {
                SetDbCommand();
                return _command.ExecuteScalar();
            }
        }

        public DataSet FillDataset()
        {
            return FillDataset("");
        }

        public DataSet FillDataset(string TableName)
        {
            using (_conn = CreateConnection())
            {
                SetDbCommand();
                SqlDataAdapter adapter = new SqlDataAdapter();
                adapter.SelectCommand = _command;
                DataSet ds = new DataSet();
                if (string.IsNullOrEmpty(TableName))
                    adapter.Fill(ds);
                else
                    adapter.Fill(ds, TableName);
                return ds;
            }
        }

        public DataTable GetDataTable()
        {
            return GetDataTable("table");
        }

        public DataTable GetDataTable(string TableName)
        {
            DataSet ds = FillDataset(TableName);
            return ds.Tables[TableName];
        }

        public DataRow GetSingleRow()
        {
            DataTable table = GetDataTable();
            if (table == null)
                return null;
            if (table.Rows.Count == 0)
                return null;
            return table.Rows[0];
        }

        public static object GetRowValue(DataRow Row, string FieldName)
        {
            if (Row == null)
                return null;
            return Row[FieldName];
        }

        public static object GetRowValue(DataRow Row, int FieldIndex)
        {
            if (Row == null)
                return null;
            return Row[FieldIndex];
        }

        public static string GetRowValue(DataRow Row, string FieldName, string Default)
        {
            if (Row == null)
                return Default;
            return MyFunc.ConvertDbValue(GetRowValue(Row, FieldName), Default);
        }

        public static int GetRowValue(DataRow Row, string FieldName, int Default)
        {
            if (Row == null)
                return Default;
            return MyFunc.ConvertDbValue(GetRowValue(Row, FieldName), Default);
        }

        public static DateTime GetRowValue(DataRow Row, string FieldName, DateTime Default)
        {
            if (Row == null)
                return Default;
            return MyFunc.ConvertDbValue(GetRowValue(Row, FieldName), Default);
        }

        public static bool GetRowValue(DataRow Row, string FieldName, bool Default)
        {
            if (Row == null)
                return Default;
            return MyFunc.ConvertDbValue(GetRowValue(Row, FieldName), Default);
        }

        public static double GetRowValue(DataRow Row, string FieldName, double Default)
        {
            if (Row == null)
                return Default;
            return MyFunc.ConvertDbValue(GetRowValue(Row, FieldName), Default);
        }

        public static decimal GetRowValue(DataRow Row, string FieldName, decimal Default)
        {
            if (Row == null)
                return Default;
            return MyFunc.ConvertDbValue(GetRowValue(Row, FieldName), Default);
        }

        public static decimal GetRowValue(DataRow Row, int FieldIndex, decimal Default)
        {
            if (Row == null)
                return Default;
            return MyFunc.ConvertDbValue(GetRowValue(Row, FieldIndex), Default);
        }

        public static int GetRowValue(DataRow Row, int FieldIndex, int Default)
        {
            return MyFunc.ConvertDbValue(GetRowValue(Row, FieldIndex), Default);
        }

        public static bool GetRowValue(DataRow Row, int FieldIndex, bool Default)
        {
            return MyFunc.ConvertDbValue(GetRowValue(Row, FieldIndex), Default);
        }

        public static string GetRowValue(DataRow Row, int FieldIndex, string Default)
        {
            if (Row == null)
                return Default;
            return MyFunc.ConvertDbValue(GetRowValue(Row, FieldIndex), Default);
        }

        public object GetSingleValue()
        {
            object o = ExecuteScalar();
            if (o == null)
                return null;
            if (Convert.IsDBNull(o))
                return null;
            return o;
        }

        public string GetSingleValue(string Col, string Default)
        {
            DataRow row = GetSingleRow();
            if (row == null)
                return Default;
            return GetRowValue(row, Col, Default);
        }

        public int GetSingleValue(int Default)
        {
            return MyFunc.ConvertDbValue(GetSingleValue(), Default);
        }

        public string GetSingleValue(string Default)
        {
            return MyFunc.ConvertDbValue(GetSingleValue(), Default);
        }

        public bool GetSingleValue(bool Default)
        {
            return MyFunc.ConvertDbValue(GetSingleValue(), Default);
        }

        public Decimal GetSingleValue(Decimal Default)
        {
            return MyFunc.ConvertDbValue(GetSingleValue(), Default);
        }

        public bool RecordExists()
        {
            return GetSingleValue(0) > 0;
        }

        public static bool SqlExists(string Sql)
        {
            return !string.IsNullOrEmpty(MyFunc.GetWebconfigValue(Sql, ""));
        }

        public static int GetNextId()
        {
            return GetNextId("TopId", "Id");
        }

        public static int GetNextId(string IdTable, string IdField)
        {
            string uf = MyFunc.GetWebconfigValue("UserFieldInTopId", "");
            return GetNextId(IdTable, IdField, uf, -1);
        }

        public object GetCommandValue(string Name)
        {
            foreach (SqlParameter item in _command.Parameters)
            {
                if (string.Compare(item.ParameterName, "@" + Name, true) == 0)
                    return item.Value;
            }
            return null;
        }

        public string GetCommandValue(string Name, string Default)
        {
            return MyFunc.ConvertDbValue(GetCommandValue(Name), Default);
        }

        public int GetCommandValue(string Name, int Default)
        {
            return MyFunc.ConvertDbValue(GetCommandValue(Name), Default);
        }

        public bool GetCommandValue(string Name, bool Default)
        {
            return MyFunc.ConvertDbValue(GetCommandValue(Name), Default);
        }

        public double GetCommandValue(string Name, double Default)
        {
            return MyFunc.ConvertDbValue(GetCommandValue(Name), Default);
        }

        public decimal GetCommandValue(string Name, decimal Default)
        {
            return MyFunc.ConvertDbValue(GetCommandValue(Name), Default);
        }

        public DateTime GetCommandValue(string Name, DateTime Default)
        {
            return MyFunc.ConvertDbValue(GetCommandValue(Name), Default);
        }

        private object GetCommandReturnValue()
        {
            foreach (SqlParameter item in _command.Parameters)
            {
                if (item.Direction == ParameterDirection.ReturnValue)
                    return item.Value;
            }
            return null;
        }

        public string GetReturnValue(string Default)
        {
            return MyFunc.ConvertDbValue(GetCommandReturnValue(), Default);
        }

        public int GetReturnValue(int Default)
        {
            return MyFunc.ConvertDbValue(GetCommandReturnValue(), Default);
        }

        public bool GetReturnValue(bool Default)
        {
            return MyFunc.ConvertDbValue(GetReturnValue(0) == 1, Default);
        }

        public DateTime GetReturnValue(DateTime Default)
        {
            return MyFunc.ConvertDbValue(GetCommandReturnValue(), Default);
        }

        public double GetReturnValue(double Default)
        {
            return MyFunc.ConvertDbValue(GetCommandReturnValue(), Default);
        }

        public decimal GetReturnValue(decimal Default)
        {
            return MyFunc.ConvertDbValue(GetCommandReturnValue(), Default);
        }

        public static int GetNextId(string IdTable, string IdField, string UserField, int UserId)
        {
            SqlHelper param = null;
            if (SqlExists("IncTopIdProc"))
            {
                param = new SqlHelper("IncTopIdProc", CommandType.StoredProcedure);
                param.SetValue("UserId", UserId);
                param.ExecSql();
                return param.GetCommandValue("NewId", 0);
            }
            else
            {
                if (!string.IsNullOrEmpty(UserField))
                    UserField = string.Format(" AND {0}={1}", UserField, UserId);
                param = new SqlHelper(string.Format("UPDATE {0} SET {1}={1}+1 WHERE 1=1 {2}", IdTable, IdField, UserField), false);
                param.ExecSql();
                param = new SqlHelper(string.Format("SELECT {1} FROM {0} WHERE 1=1 {2}", IdTable, IdField, UserField), false);
                return param.GetSingleValue(0);
            }
        }

        public void AddReturnParam(string ParamName, SqlDbType ParamType)
        {
            _retParamName = ParamName;
            _retType = ParamType;
        }

        public int AddRecordSP()
        {
            using (_conn = CreateConnection())
            {
                SetDbCommand();
                AddReturnParam("NewIndex", SqlDbType.Int);
                _command.ExecuteNonQuery();
            }
            return GetReturnValue(0);
        }

        public int AddOrUpdate()
        {
            using (_conn = CreateConnection())
            {
                SetDbCommand();
                AddReturnParam("NewIndex", SqlDbType.Int);
                _command.ExecuteNonQuery();
            }
            return GetReturnValue(0);
        }

        public int AddRecord(int Id)
        {
            _paramList[0].Value = Id;
            ExecSql();
            return Id;
        }

        public int AddRecord()
        {
            if (_cmdType == CommandType.StoredProcedure)
            {
                AddReturnParam("NewIndex", SqlDbType.Int);
                ExecSql();
                return GetReturnValue(0);
            }
            return AddRecord(GetNextId());
        }

        public int AddRecord(string TopTable)
        {
            return AddRecord(GetNextId(TopTable, "Id"));
        }

        public void GetRecordInfo(ArrayList InfoList)
        {
            ArrayList InfoList1 = new ArrayList(InfoList);
            using (_conn = CreateConnection())
            {
                SetDbCommand();
                using (SqlDataReader DataReader = _command.ExecuteReader())
                {
                    if (DataReader.Read())
                    {
                        for (int i = 0; i < InfoList1.Count; i++)
                            if (DataReader.IsDBNull(i))
                                InfoList[i] = null;
                            else
                            {
                                switch ((DbType)InfoList1[i])
                                {
                                    case DbType.String:
                                        InfoList[i] = DataReader.GetString(i);
                                        break;
                                    case DbType.Int32:
                                        InfoList[i] = DataReader.GetInt32(i);
                                        break;
                                    case DbType.Decimal:
                                        InfoList[i] = DataReader.GetDecimal(i);
                                        break;
                                    case DbType.Date:
                                        InfoList[i] = DataReader.GetDateTime(i);
                                        break;
                                    case DbType.Boolean:
                                        InfoList[i] = DataReader.GetBoolean(i);
                                        break;
                                    default:
                                        throw new Exception(string.Format("Data type {0} not defined", InfoList1[i].ToString()));
                                }
                            }
                    }
                    else
                        InfoList.Clear();
                    DataReader.Close();
                }
            }
        }

        public void FillDataList(DataList List)
        {
            using (_conn = CreateConnection())
            {
                SetDbCommand();
                using (SqlDataReader DataReader = _command.ExecuteReader())
                {
                    List.DataSource = DataReader;
                    List.DataBind();
                    DataReader.Close();
                }
            }
        }

        public void FillDropDownList(DropDownList List, string TextField, string ValueField)
        {
            List.DataTextField = TextField;
            List.DataValueField = ValueField;
            using (_conn = CreateConnection())
            {
                SetDbCommand();
                using (SqlDataReader DataReader = _command.ExecuteReader())
                {
                    List.DataSource = DataReader;
                    List.DataBind();
                    DataReader.Close();
                }
            }
        }

        public void FillArrayList(SqlHelper Param, ArrayList List, DbType DataType)
        {
            List.Clear();
            using (_conn = CreateConnection())
            {
                SetDbCommand();
                using (SqlDataReader DataReader = _command.ExecuteReader())
                {
                    while (DataReader.Read())
                    {
                        switch (DataType)
                        {
                            case DbType.String:
                                List.Add(DataReader.GetString(0));
                                break;
                            case DbType.Int32:
                                List.Add(DataReader.GetInt32(0));
                                break;
                            case DbType.Decimal:
                                List.Add(DataReader.GetDecimal(0));
                                break;
                            case DbType.Date:
                                List.Add(DataReader.GetDateTime(0));
                                break;
                            case DbType.Boolean:
                                List.Add(DataReader.GetBoolean(0));
                                break;
                            default:
                                throw new Exception(string.Format("Data type {0} not defined", DataType.ToString()));
                        }
                    }
                    DataReader.Close();
                }
            }
        }

        public int GetValues(string Field, int Default)
        {
            return MyFunc.ConvertDbValue(GetValues(Field, ""), Default);
        }

        public string GetValues(string Field, string Default)
        {
            DataTable table = GetDataTable("Table");
            if (table.Rows.Count == 0)
                return Default;
            return GetRowValue(table.Rows[0], Field, Default);
        }

        public bool GetValues(string Field, bool Default)
        {
            return MyFunc.ConvertDbValue(GetValues(Field, ""), Default);
        }

        public DataRowCollection GetDataRows()
        {
            DataTable table = GetDataTable();
            return table.Rows;
        }

        public List<object> GetFirstColValues()
        {
            DataRowCollection rows = GetDataRows();
            if (rows == null)
                return null;
            List<object> list = new List<object>();
            foreach (DataRow row in rows)
                list.Add(row[0]);
            return list;
        }

    }

}
