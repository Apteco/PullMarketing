using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Apteco.PullMarketing.Data.MSSQL.DataTier
{
  public class DbAccess : IDbAccess
  {
    #region private constants
    private const int MaxRetries = 10;

    private const string RemovePasswordFromConnectionStringRegexPattern = "(password=)([^;]*)(;?)";
    #endregion

    #region public constants
    public const int SqlLockError = 1204;
    public const int SqlDeadlock = 1205;
    public const int SqlPermissionErrorNumber = 297;
    #endregion

    #region private fields
    private static Regex removePasswordFromConnectionStringRegex;

    private ILogger<DbAccess> logger;
    private string connectionURI = "";
    private string databaseName = "";
    private TimeSpan commandTimeout;
    private TimeSpan connectionTimeout;
    private int connectionRetries;
    private TimeSpan? performanceLoggingThreshold;
    private bool retryOnDeadlock = true;
    private static Random random = new Random();

    #endregion

    #region public properties

    public TimeSpan CommandTimeout
    {
      get { return commandTimeout; }
    }

    public TimeSpan ConnectionTimeout
    {
      get { return connectionTimeout; }
    }

    public TimeSpan? PerformanceLoggingThreshold
    {
      get { return performanceLoggingThreshold; }
    }

    public string ConnectionString
    {
      get { return connectionURI; }
    }

    public string DatabaseName
    {
      get
      {
        if (string.IsNullOrEmpty(databaseName))
          databaseName = GetDatabaseName();

        return databaseName;
      }
    }

    public string DateCommand
    {
      get { return "GETDATE()"; }
    }

    public string LengthCommand
    {
      get { return "LEN"; }
    }

    public string NewGuid
    {
      get
      {
        Guid guid = Guid.NewGuid();
        return guid.ToString();
      }
    }

    public bool RetryOnDeadlock
    {
      get { return retryOnDeadlock; }
      set { retryOnDeadlock = value; }
    }
    #endregion

    #region private properties

    private string ParameterPrefix
    {
      get { return "@"; }
    }

    #endregion

    #region public constructors
    static DbAccess()
    {
      removePasswordFromConnectionStringRegex = new Regex(RemovePasswordFromConnectionStringRegexPattern);
    }

    public DbAccess(string connectionURI, string commandTimeoutString, ILogger<DbAccess> logger)
      : this(DbAccessConnectionProperties.Create(connectionURI, commandTimeoutString, null, null, null), logger)
    {
    }

    public DbAccess(string connectionURI, string commandTimeoutString, string connectionTimeoutString, ILogger<DbAccess> logger)
      : this(DbAccessConnectionProperties.Create(connectionURI, commandTimeoutString, connectionTimeoutString, null, null), logger)
    {
    }

    public DbAccess(DbAccessConnectionProperties connectionProperties, ILogger<DbAccess> logger)
    {
      if (connectionProperties == null)
        throw new ArgumentNullException(nameof(connectionProperties));

      if (logger == null)
        throw new ArgumentNullException(nameof(logger));

      this.connectionURI = connectionProperties.ConnectionUri;
      this.commandTimeout = connectionProperties.CommandTimeout;
      this.connectionTimeout = connectionProperties.ConnectionTimeout;
      this.connectionRetries = connectionProperties.ConnectionRetries;
      this.performanceLoggingThreshold = connectionProperties.PerformanceLoggingThreshold;

      this.logger = logger;
    }

    #endregion

    #region public methods
    public static string GetStringsAsEscapedList(IEnumerable<string> items)
    {
      if (items == null)
        throw new ArgumentNullException(nameof(items));

      string listString = GetArrayAsList(items.Select(item => "'" + DbAccess.EscapeForUseInSql(item) + "'"));
      if (string.IsNullOrEmpty(listString))
        throw new ArgumentException("Can't get an empty string enumerable as an escaped list", nameof(items));

      return listString;
    }

    public static string GetArrayAsList<T>(IEnumerable<T> array)
    {
      if (array == null)
        throw new ArgumentNullException(nameof(array));

      StringBuilder list = new StringBuilder();
      foreach (T item in array)
      {
        if (list.Length > 0)
          list.Append(',');
        list.Append(item);
      }
      return list.ToString();
    }

    public string Encapsulate(string value)
    {
      return "[" + value + "]";
    }

    public static string RemovePasswordFromConnectionString(string s)
    {
      if (s == null)
        return null;

      return Regex.Replace(s, RemovePasswordFromConnectionStringRegexPattern, "$1XXX$3", RegexOptions.IgnoreCase);
    }

    private const string BcpDriverStr = "Driver={SQL Server Native Client 11.0}";

    /// <summary>
    /// Create a database connection and open it
    /// </summary>
    /// <returns>An open database connection</returns>
    public virtual IDbConnection Connect()
    {
      int retries = 0;
      do
      {
        try
        {
          return RawConnect();
        }
        catch (Exception e)
        {
          logger.LogDebug(e.ToString());
          if (retries >= connectionRetries)
            throw;
        }

        retries++;
        WaitRandomTimeShort(retries);
        WriteDebug("Retrying connection (retry " + retries + " of " + connectionRetries + ")");
      } while (true);
    }

    /// <summary>
    /// Create a database connection and open it
    /// </summary>
    /// <returns>An open database connection</returns>
    public virtual IDbConnection RawConnect()
    {
      IDbConnection dbConn = new SqlConnection(connectionURI);
      dbConn.Open();

      databaseName = dbConn.Database;

      return dbConn;
    }

    public IDbCommand CreateCommand(IDbConnection connection)
    {
      if (connection == null)
        throw new Exception("Can't CreateCommand - connection is null");

      IDbCommand command = connection.CreateCommand();
      command.CommandTimeout = (int)commandTimeout.TotalSeconds;
      return command;
    }

    public IDbCommand CreateCommand(IDbTransaction transaction)
    {
      if (transaction == null)
        throw new Exception("Can't CreateCommand - transaction is null");

      if (transaction.Connection == null)
        throw new Exception("Can't CreateCommand - connection associated with the given transaction is null");

      IDbCommand command = transaction.Connection.CreateCommand();
      command.CommandTimeout = (int)commandTimeout.TotalSeconds;
      command.Transaction = transaction;
      return command;
    }

    public static bool ConvertDBBoolean(object o)
    {
      try
      {
        return bool.Parse(o.ToString());
      }
      catch
      {
        try
        {
          return int.Parse(o.ToString()) != 0;
        }
        catch
        {
          return false;
        }
      }
    }

    public static int ConvertDBNumber(object o)
    {
      int n;
      if (o is DBNull)
      {
        n = 0;
      }
      else if (o is int)
      {
        //Access gives us back an Int
        n = (int)o;
      }
      else if (o is long)
      {
        n = (int)(long)o;
      }
      else
      {
        //SQL Server gives us back a Decimal
        decimal d = (decimal)o;
        n = (Int32)d;
      }
      return n;
    }

    public static double ConvertDBNumberDouble(object o)
    {
      double n;
      if (o is DBNull)
      {
        n = 0.0;
      }
      else if (o is double)
      {
        //Access gives us back an Int
        n = (double)o;
      }
      else if (o is float)
      {
        n = (float)o;
      }
      else
      {
        //SQL Server gives us back a Decimal
        decimal d = (decimal)o;
        n = (double)d;
      }
      return n;
    }

    public static string GetDataString(IDataReader dataReader, int fieldPosition)
    {
      if (dataReader == null)
        throw new Exception("Can't GetDataString - dataReader is null");

      string result = dataReader.GetString(fieldPosition);
      return result;
    }

    public static void SetParameterString(IDataParameter param, string value)
    {
      if (param == null)
        throw new Exception("Can't SetParameterString - param is null");

      param.Value = value;
    }

    public static string EscapeForUseInSql(string s)
    {
      return s == null ? null : s.Replace("'", "''");
    }

    public static List<string> GetParameterNamesFromSql(string sql)
    {
      HashSet<string> declaredParameters = new HashSet<string>();

      HashSet<string> parameterNames = new HashSet<string>();

      if (string.IsNullOrEmpty(sql))
        return parameterNames.ToList();

      int parameterNameStartIndex = -1;
      bool inQuotes = false;
      bool followsDeclareStatement = false;

      for (int i = 0; i < sql.Length; i++)
      {
        char c = sql[i];
        if (c == '\'')
        {
          if (inQuotes)
          {
            if ((i < sql.Length - 1) && (sql[i + 1] == '\''))
              i++;
            else
              inQuotes = false;
          }
          else
          {
            inQuotes = true;
          }
        }

        if (c == '@' && !inQuotes && parameterNameStartIndex == -1)
        {
          parameterNameStartIndex = i;
          followsDeclareStatement = LookBackForStringIgnoringCase(sql, i - 1, "DECLARE");
        }

        if (!char.IsLetterOrDigit(c) && c != '@' && c != '$' && c != '_' && c != '#')
        {
          if (parameterNameStartIndex > -1)
          {
            string parameterName = sql.Substring(parameterNameStartIndex + 1, i - parameterNameStartIndex - 1);
            if (followsDeclareStatement)
              declaredParameters.Add(parameterName);
            else
              parameterNames.Add(parameterName);
          }

          parameterNameStartIndex = -1;
        }
      }

      if (parameterNameStartIndex > -1)
      {
        string parameterName = sql.Substring(parameterNameStartIndex + 1, sql.Length - parameterNameStartIndex - 1);
        if (followsDeclareStatement)
          declaredParameters.Add(parameterName);
        else
          parameterNames.Add(parameterName);

      }

      parameterNames.RemoveWhere(p => declaredParameters.Contains(p));
      return parameterNames.ToList();
    }

    private static bool LookBackForStringIgnoringCase(string sql, int startIndex, string stringToSearchFor)
    {
      int firstNonWhitespaceCharacter = -1;
      int lastNonWhitespaceCharacter = 0;
      for (int i = startIndex; i >= 0; i--)
      {
        char c = sql[i];
        if (!char.IsWhiteSpace(c))
        {
          if (firstNonWhitespaceCharacter == -1)
            firstNonWhitespaceCharacter = i;
        }
        else
        {
          if (firstNonWhitespaceCharacter > -1)
          {
            lastNonWhitespaceCharacter = i + 1;
            break;
          }
        }
      }

      string foundString = sql.Substring(lastNonWhitespaceCharacter,
        (firstNonWhitespaceCharacter - lastNonWhitespaceCharacter) + 1);
      return string.Equals(foundString, stringToSearchFor, StringComparison.OrdinalIgnoreCase);
    }


    public List<string> GetParameterFields(IDbCommand command)
    {
      if (command == null)
        throw new Exception("Can't GetParameterFields - command is null");

      List<string> parameterFields = new List<string>();
      foreach (IDbDataParameter parameter in command.Parameters)
      {
        if ((parameter.ParameterName != null) && (parameter.ParameterName.Length > ParameterPrefix.Length))
          parameterFields.Add(parameter.ParameterName.Substring(ParameterPrefix.Length));
      }
      return parameterFields;
    }

    public void SetParameter(IDbCommand command, string field, object value)
    {
      if (command == null)
        throw new Exception("Can't SetParameter \"" + field + "\" - command is null");

      IDbDataParameter parameter = (IDbDataParameter)command.Parameters[ParameterPrefix + field];
      parameter.Value = value;
    }

    public void ClearParameterValues(IDbCommand command)
    {
      if (command == null)
        throw new Exception("Can't ClearParameterValues - command is null");

      foreach (IDbDataParameter parameter in command.Parameters)
      {
        parameter.Value = null;
      }
    }

    public void ClearParameters(IDbCommand command)
    {
      if (command == null)
        throw new Exception("Can't ClearParameters - command is null");

      command.Parameters.Clear();
    }

    public string AddParameter(IDbCommand command, string field, DbType type)
    {
      if (command == null)
        throw new Exception("Can't AddParameter \"" + field + "\" - command is null");

      IDbDataParameter parameter = command.CreateParameter();
      parameter.ParameterName = ParameterPrefix + field;
      parameter.DbType = type;
      command.Parameters.Add(parameter);
      return GetDBParameterPlaceholder(field);
    }

    public string AddParameter(IDbCommand command, string field, int size, DbType type)
    {
      if (command == null)
        throw new Exception("Can't AddParameter \"" + field + "\" of type " + type + " - command is null");

      IDbDataParameter parameter = command.CreateParameter();
      parameter.ParameterName = ParameterPrefix + field;
      parameter.DbType = type;
      parameter.Size = size;
      command.Parameters.Add(parameter);
      return GetDBParameterPlaceholder(field);
    }

    public string AddTextParameter(IDbCommand command, string field, int size)
    {
      if (command == null)
        throw new Exception("Can't AddTextParameter \"" + field + "\" - command is null");

      IDbDataParameter parameter = command.CreateParameter();
      parameter.ParameterName = ParameterPrefix + field;
      parameter.DbType = DbType.String;
      parameter.Size = size;
      command.Parameters.Add(parameter);
      return GetDBParameterPlaceholder(field);
    }

    public string AddParameter(IDbCommand command, string field, object value, DbType type)
    {
      return AddParameter(command, field, value, type, false);
    }

    public string AddParameter(IDbCommand command, string field, object value, DbType type, bool addAsLiteral)
    {
      if (command == null)
        throw new Exception("Can't AddParameter \"" + field + "\" of type " + type + " - command is null");

      if (addAsLiteral)
        return AddParameter(command, field, value, true);

      IDbDataParameter parameter = command.CreateParameter();
      parameter.ParameterName = ParameterPrefix + field;
      parameter.DbType = type;
      if (value == null)
        parameter.Value = DBNull.Value;
      else
        parameter.Value = value;
      command.Parameters.Add(parameter);
      return GetDBParameterPlaceholder(field);
    }

    public string AddParameter(IDbCommand command, string field, object value)
    {
      return AddParameter(command, field, value, false);
    }

    public string AddParameter(IDbCommand command, string field, object value, bool addAsLiteral)
    {
      if (value == null)
        throw new Exception("Can't add a null object using this method - don't know what type to make the parameter");

      if (value is string)
        return AddParameter(command, field, (string)value, addAsLiteral);
      else if (value is DateTime)
        return AddParameter(command, field, (DateTime)value, addAsLiteral);
      else if (value is DateTime?)
        return AddParameter(command, field, (DateTime?)value, addAsLiteral);
      else if (value is int)
        return AddParameter(command, field, (int)value, addAsLiteral);
      else if (value is int?)
        return AddParameter(command, field, (int?)value, addAsLiteral);
      else if (value is long)
        return AddParameter(command, field, (long)value, addAsLiteral);
      else if (value is long?)
        return AddParameter(command, field, (long?)value, addAsLiteral);
      else if (value is bool)
        return AddParameter(command, field, (bool)value, addAsLiteral);
      else if (value is double)
        return AddParameter(command, field, (double)value, addAsLiteral);
      else if (value is decimal)
        return AddParameter(command, field, (decimal)value, addAsLiteral);
      else if (value is Guid)
        return AddParameter(command, field, (Guid)value, addAsLiteral);
      else if (value is Guid?)
        return AddParameter(command, field, (Guid?)value, addAsLiteral);
      else if (value is DBNull)
        return AddParameter(command, field, (DBNull)value);
      else
        throw new Exception("Can't add a parameter for an object of type " + value.GetType());
    }

    public string AddParameter(IDbCommand command, string field, string value, bool addAsLiteral)
    {
      if (command == null)
        throw new Exception("Can't AddParameter \"" + field + "\" with value \"" + value + "\" - command is null");

      if (addAsLiteral)
      {
        return (value != null) ? "'" + value.Replace("'", "''") + "'" : "NULL";
      }

      IDbDataParameter parameter = command.CreateParameter();
      parameter.ParameterName = ParameterPrefix + field;
      parameter.DbType = DbType.String;
      if (value == null)
        parameter.Value = DBNull.Value;
      else
        parameter.Value = value;
      parameter.Size = value == null ? 0 : value.Length;
      command.Parameters.Add(parameter);
      return GetDBParameterPlaceholder(field);
    }

    public string AddParameter(IDbCommand command, string field, string value)
    {
      return AddParameter(command, field, value, false);
    }

    public string AddParameter(IDbCommand command, string field, DateTime value, bool addAsLiteral)
    {
      if (command == null)
        throw new Exception("Can't AddParameter \"" + field + "\" with value \"" + value + "\" - command is null");

      if (addAsLiteral)
        return "'" + value.ToString("s", CultureInfo.InvariantCulture) + "'";

      IDbDataParameter parameter = command.CreateParameter();
      parameter.ParameterName = ParameterPrefix + field;
      parameter.DbType = DbType.DateTime;
      parameter.Value = value;
      command.Parameters.Add(parameter);
      return GetDBParameterPlaceholder(field);
    }

    public string AddParameter(IDbCommand command, string field, DateTime value)
    {
      return AddParameter(command, field, value, false);
    }

    public string AddParameter(IDbCommand command, string field, DateTime? value, bool addAsLiteral)
    {
      if (command == null)
        throw new Exception("Can't AddParameter \"" + field + "\" with value \"" + value + "\" - command is null");

      if (addAsLiteral)
        return value.HasValue ? "'" + value.Value.ToString("s", CultureInfo.InvariantCulture) + "'" : "NULL";

      IDbDataParameter parameter = command.CreateParameter();
      parameter.ParameterName = ParameterPrefix + field;
      parameter.DbType = DbType.DateTime;
      if (value.HasValue)
        parameter.Value = value.Value;
      else
        parameter.Value = DBNull.Value;
      command.Parameters.Add(parameter);
      return GetDBParameterPlaceholder(field);
    }

    public string AddParameter(IDbCommand command, string field, DateTime? value)
    {
      return AddParameter(command, field, value, false);
    }

    public string AddParameter(IDbCommand command, string field, Guid value, bool addAsLiteral)
    {
      if (command == null)
        throw new Exception("Can't AddParameter \"" + field + "\" with value \"" + value + "\" - command is null");

      if (addAsLiteral)
        return "'" + value + "'";

      IDbDataParameter parameter = command.CreateParameter();
      parameter.ParameterName = ParameterPrefix + field;
      parameter.DbType = DbType.Guid;
      parameter.Value = value;
      command.Parameters.Add(parameter);
      return GetDBParameterPlaceholder(field);
    }

    public string AddParameter(IDbCommand command, string field, Guid value)
    {
      return AddParameter(command, field, value, false);
    }

    public string AddParameter(IDbCommand command, string field, Guid? value, bool addAsLiteral)
    {
      if (command == null)
        throw new Exception("Can't AddParameter \"" + field + "\" with value \"" + value + "\" - command is null");

      if (addAsLiteral)
        return value.HasValue ? "'" + value.Value + "'" : "NULL";

      IDbDataParameter parameter = command.CreateParameter();
      parameter.ParameterName = ParameterPrefix + field;
      parameter.DbType = DbType.Guid;
      if (value.HasValue)
        parameter.Value = value.Value;
      else
        parameter.Value = DBNull.Value;
      command.Parameters.Add(parameter);
      return GetDBParameterPlaceholder(field);
    }

    public string AddParameter(IDbCommand command, string field, Guid? value)
    {
      return AddParameter(command, field, value, false);
    }

    public string AddParameter(IDbCommand command, string field, int value, bool addAsLiteral)
    {
      if (command == null)
        throw new Exception("Can't AddParameter \"" + field + "\" with value \"" + value + "\" - command is null");

      if (addAsLiteral)
        return value.ToString();

      IDbDataParameter parameter = command.CreateParameter();
      parameter.ParameterName = ParameterPrefix + field;
      parameter.DbType = DbType.Int32;
      parameter.Value = value;
      command.Parameters.Add(parameter);
      return GetDBParameterPlaceholder(field);
    }

    public string AddParameter(IDbCommand command, string field, int value)
    {
      return AddParameter(command, field, value, false);
    }

    public string AddParameter(IDbCommand command, string field, int? value, bool addAsLiteral)
    {
      if (command == null)
        throw new Exception("Can't AddParameter \"" + field + "\" with value \"" + value + "\" - command is null");

      if (addAsLiteral)
        return value.HasValue ? value.ToString() : "NULL";

      IDbDataParameter parameter = command.CreateParameter();
      parameter.ParameterName = ParameterPrefix + field;
      parameter.DbType = DbType.Int32;
      if (value.HasValue)
        parameter.Value = value.Value;
      else
        parameter.Value = DBNull.Value;
      command.Parameters.Add(parameter);
      return GetDBParameterPlaceholder(field);
    }

    public string AddParameter(IDbCommand command, string field, int? value)
    {
      return AddParameter(command, field, value, false);
    }

    public string AddParameter(IDbCommand command, string field, long value, bool addAsLiteral)
    {
      if (command == null)
        throw new Exception("Can't AddParameter \"" + field + "\" with value \"" + value + "\" - command is null");

      if (addAsLiteral)
        return value.ToString();

      IDbDataParameter parameter = command.CreateParameter();
      parameter.ParameterName = ParameterPrefix + field;
      parameter.DbType = DbType.Int64;
      parameter.Value = value;
      command.Parameters.Add(parameter);
      return GetDBParameterPlaceholder(field);
    }

    public string AddParameter(IDbCommand command, string field, long value)
    {
      return AddParameter(command, field, value, false);
    }

    public string AddParameter(IDbCommand command, string field, ulong value) =>
      AddParameter(command, field, value, false);

    public string AddParameter(IDbCommand command, string field, ulong value, bool addAsLiteral)
    {
      if (command == null)
        throw new Exception("Can't AddParameter \"" + field + "\" with value \"" + value + "\" - command is null");

      if (addAsLiteral)
        return value.ToString();

      IDbDataParameter parameter = command.CreateParameter();
      parameter.ParameterName = ParameterPrefix + field;
      parameter.DbType = DbType.Int64;
      parameter.Value = (long)value;
      command.Parameters.Add(parameter);
      return GetDBParameterPlaceholder(field);
    }

    public string AddParameter(IDbCommand command, string field, long? value, bool addAsLiteral)
    {
      if (command == null)
        throw new Exception("Can't AddParameter \"" + field + "\" with value \"" + value + "\" - command is null");

      if (addAsLiteral)
        return value.HasValue ? value.ToString() : "NULL";

      IDbDataParameter parameter = command.CreateParameter();
      parameter.ParameterName = ParameterPrefix + field;
      parameter.DbType = DbType.Int32;
      if (value.HasValue)
        parameter.Value = value.Value;
      else
        parameter.Value = DBNull.Value;
      command.Parameters.Add(parameter);
      return GetDBParameterPlaceholder(field);
    }

    public string AddParameter(IDbCommand command, string field, long? value)
    {
      return AddParameter(command, field, value, false);
    }

    public string AddParameter(IDbCommand command, string field, DBNull value)
    {
      if (command == null)
        throw new Exception("Can't AddParameter \"" + field + "\" with value \"" + value + "\" - command is null");

      return "NULL";
    }

    public string AddParameter(IDbCommand command, string field, bool value, bool addAsLiteral)
    {
      if (command == null)
        throw new Exception("Can't AddParameter \"" + field + "\" with value \"" + value + "\" - command is null");

      if (addAsLiteral)
        return value ? "-1" : "0";

      IDbDataParameter parameter = command.CreateParameter();
      parameter.ParameterName = ParameterPrefix + field;
      parameter.DbType = DbType.Boolean;
      parameter.Value = value;
      command.Parameters.Add(parameter);
      return GetDBParameterPlaceholder(field);
    }

    public string AddParameter(IDbCommand command, string field, bool value)
    {
      return AddParameter(command, field, value, false);
    }

    public string AddParameter(IDbCommand command, string field, double value, bool addAsLiteral)
    {
      if (command == null)
        throw new Exception("Can't AddParameter \"" + field + "\" with value \"" + value + "\" - command is null");

      if (addAsLiteral)
        return value.ToString();

      IDbDataParameter parameter = command.CreateParameter();
      parameter.ParameterName = ParameterPrefix + field;
      parameter.DbType = DbType.Double;
      parameter.Value = value;
      command.Parameters.Add(parameter);
      return GetDBParameterPlaceholder(field);
    }

    public string AddParameter(IDbCommand command, string field, double value)
    {
      return AddParameter(command, field, value, false);
    }

    public string AddParameter(IDbCommand command, string field, decimal value, bool addAsLiteral)
    {
      if (command == null)
        throw new Exception("Can't AddParameter \"" + field + "\" with value \"" + value + "\" - command is null");

      if (addAsLiteral)
        return value.ToString();

      IDbDataParameter parameter = command.CreateParameter();
      parameter.ParameterName = ParameterPrefix + field;
      parameter.DbType = DbType.Decimal;
      parameter.Value = value;
      command.Parameters.Add(parameter);
      return GetDBParameterPlaceholder(field);
    }

    public string AddParameter(IDbCommand command, string field, decimal value)
    {
      return AddParameter(command, field, value, false);
    }

    public async Task<int> NativeExecuteNonQuery(IDbCommand command)
    {
      if (command == null)
        throw new Exception("Can't run NativeExecuteNonQuery - command is null");

      return await command.ExecuteNonQueryAsync();
    }

    public async Task<int> ExecuteNonQuery(IDbCommand command)
    {
      if (command == null)
        throw new Exception("Can't run ExecuteNonQuery - command is null");

      int retries = MaxRetries;
      while (true)
      {
        try
        {
          DateTime started = DateTime.UtcNow;
          int rowsAffected = await command.ExecuteNonQueryAsync();
          DateTime finished = DateTime.UtcNow;
          WritePerformanceInformation(command, started, finished);
          return rowsAffected;
        }
        catch (SqlException e)
        {
          if (retries > 0 && ShouldReRunQuery(e))
          {
            WriteDebug("Got Sql Exception " + e.Number + " on " + command.CommandText);
            LogSqlErrors(e.Errors);
            retries--;
            WaitRandomTimeShort(MaxRetries - retries);
            WriteDebug("Retrying " + command.CommandText + " (retry " + (MaxRetries - retries) + " of " + MaxRetries +
                       ")");
          }
          else
          {
            WriteDebug("Got Unhandled Sql Exception " + e + " on running " + command.CommandText);
            LogSqlErrors(e.Errors);
            throw;
          }
        }
        catch (Exception e)
        {
          WriteDebug("Got Exception " + e + " on running " + command.CommandText);
          throw;
        }
      }
    }

    public async Task<IDataReader> ExecuteReader(IDbCommand command)
    {
      return await ExecuteReader(command, CommandBehavior.Default);
    }

    public async Task<IDataReader> ExecuteReader(IDbCommand command, CommandBehavior behavior)
    {
      if (command == null)
        throw new Exception("Can't run ExecuteReader - command is null");

      int retries = MaxRetries;
      while (true)
      {
        try
        {
          DateTime started = DateTime.UtcNow;
          IDataReader dataReader = await command.ExecuteReaderAsync(behavior);
          DateTime finished = DateTime.UtcNow;
          WritePerformanceInformation(command, started, finished);
          return dataReader;
        }
        catch (SqlException e)
        {
          if (retries > 0 && ShouldReRunQuery(e))
          {
            WriteDebug("Got Sql Exception " + e.Number + " on " + command.CommandText);
            LogSqlErrors(e.Errors);
            retries--;
            WaitRandomTimeShort(MaxRetries - retries);
            WriteDebug("Retrying " + command.CommandText + " (retry " + (MaxRetries - retries) + " of " + MaxRetries +
                       ")");
          }
          else
          {
            WriteDebug("Got Unhandled Sql Exception " + e + " on running " + command.CommandText);
            LogSqlErrors(e.Errors);
            throw;
          }
        }
        catch (Exception e)
        {
          WriteDebug("Got Exception " + e + " on running " + command.CommandText);
          throw;
        }
      }
    }

    public async Task<object> ExecuteScalar(IDbCommand command)
    {
      if (command == null)
        throw new Exception("Can't run ExecuteScalar - command is null");

      int retries = MaxRetries;
      while (true)
      {
        try
        {
          DateTime started = DateTime.UtcNow;
          object obj = await command.ExecuteScalarAsync();
          DateTime finished = DateTime.UtcNow;
          WritePerformanceInformation(command, started, finished);
          return obj;
        }
        catch (SqlException e)
        {
          if (retries > 0 && ShouldReRunQuery(e))
          {
            WriteDebug("Got Sql Exception " + e.Number + " on " + command.CommandText);
            LogSqlErrors(e.Errors);
            retries--;
            WaitRandomTimeShort(MaxRetries - retries);
            WriteDebug("Retrying " + command.CommandText + " (retry " + (MaxRetries - retries) + " of " + MaxRetries +
                       ")");
          }
          else
            throw;
        }
        catch (Exception e)
        {
          WriteDebug("Got Exception " + e + " on running " + command.CommandText);
          throw;
        }
      }
    }

    public async Task<bool> Read(IDataReader reader)
    {
      if (reader == null)
        throw new Exception("Can't run Read - reader is null");

      int retries = MaxRetries;
      while (true)
      {
        try
        {
          return await reader.ReadAsync();
        }
        catch (SqlException e)
        {
          if (retries > 0 && ShouldReRunQuery(e))
          {
            WriteDebug("Got Sql Exception " + e.Number + " on sql read");
            LogSqlErrors(e.Errors);
            retries--;
            WaitRandomTimeShort(MaxRetries - retries);
            WriteDebug("Retrying sql read (retry " + (MaxRetries - retries) + " of " + MaxRetries + ")");
          }
          else
          {
            WriteDebug("Got Unhandled Sql Exception " + e + " on sql read");
            LogSqlErrors(e.Errors);
            throw;
          }
        }
        catch (Exception e)
        {
          WriteDebug("Got Exception " + e + " on sql read");
          throw;
        }
      }
    }

    public static bool IsDeadlock(Exception e)
    {
      SqlException sqlException = e as SqlException;
      if (sqlException != null)
        return sqlException.Number == SqlDeadlock;

      if (e is SimulatedSqlDeadlockException)
        return true;

      return false;
    }

    public static bool IsDeadlock(SqlError sqlError)
    {
      return sqlError.Number == SqlDeadlock;
    }

    public static bool IsPermissionError(Exception e)
    {
      SqlException sqlException = e as SqlException;
      if (sqlException != null)
        return sqlException.Number == SqlPermissionErrorNumber;

      return false;
    }

    public static bool IsPermissionError(SqlError sqlError)
    {
      return sqlError.Number == SqlPermissionErrorNumber;
    }

    #endregion

    #region public static methods

    public static string GetInvariantValue(object value)
    {
      if (value is double)
        return ((double)value).ToString(CultureInfo.InvariantCulture);
      else if (value is float)
        return ((float)value).ToString(CultureInfo.InvariantCulture);
      else if (value is DateTime)
        return ((DateTime)value).ToString("yyyyMMdd HH:mm:ss");
      else
        return value.ToString();
    }

    public static string GetSqlServerDateTimeString(DateTime dateTime)
    {
      return "Convert(datetime,'" + dateTime.ToString("yyyy-MM-dd HH:mm:ss") + "', 120)";
    }

    #endregion

    #region private methods

    private void WriteDebug(string text)
    {
      logger.LogDebug(text);
    }

    private void WritePerformanceInformation(IDbCommand command, DateTime started, DateTime finished)
    {
      if (!performanceLoggingThreshold.HasValue)
        return;

      TimeSpan duration = finished - started;
      if (duration > performanceLoggingThreshold)
      {
        WriteDebug(command.CommandText + " took " + duration + " to execute");
      }
    }

    private void LogSqlErrors(SqlErrorCollection errors)
    {
      foreach (SqlError error in errors)
      {
        logger.LogDebug("Got SqlError " + error.Message + " (" + error.Number + "), severity " + error.Class + " for exception");
      }
    }

    private bool ShouldReRunQuery(SqlException sqlException)
    {
      if (retryOnDeadlock && IsDeadlock(sqlException))
      {
        WriteDebug("Can retry on deadlock exception");
        return true;
      }
      if (sqlException.Number == SqlLockError)
      {
        WriteDebug("Can retry on sql lock exception");
        return true;
      }

      // 3/10/11 - Si and Adam think retrying something on a timeout is not a good idea.
      // - the only reason to retry on a timeout would be if the server is busy, but then retrying
      // is only going to add to the load!      
      //if (sqlException.Number == SqlTimeout || sqlException.ErrorCode == SqlTimeout)
      //  return true;

      foreach (SqlError sqlError in sqlException.Errors)
      {
        if (ShouldReRunQuery(sqlError))
          return true;
      }

      return false;
    }

    private bool ShouldReRunQuery(SqlError sqlError)
    {
      if (retryOnDeadlock && IsDeadlock(sqlError))
      {
        WriteDebug("Can retry on deadlock error");
        return true;
      }
      if (sqlError.Number == SqlLockError)
      {
        WriteDebug("Can retry on lock error");
        return true;
      }

      // 3/10/11 - Si and Adam think retrying something on a timeout is not a good idea.
      // - the only reason to retry on a timeout would be if the server is busy, but then retrying
      // is only going to add to the load! 
      //if (sqlError.Number == SqlTimeout)
      //  return true;

      return false;
    }

    private string GetDatabaseName()
    {
      try
      {
        using (IDbConnection connection = Connect())
        {
          return connection.Database;
        }
      }
      catch (Exception)
      {
        logger.LogWarning("Unable to connect to database " + connectionURI);
        return "";
      }
    }

    private string GetDBParameterPlaceholder(string parameter)
    {
      return "@" + parameter;
    }

    private static void WaitRandomTimeShort(int retryNumber)
    {
      Thread.Sleep((int)(random.NextDouble() * 1000) * retryNumber);
    }

    #endregion
  }
}
