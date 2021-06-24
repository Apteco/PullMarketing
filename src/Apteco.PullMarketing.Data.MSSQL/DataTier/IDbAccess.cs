using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace Apteco.PullMarketing.Data.MSSQL.DataTier
{
  public interface IDbAccess
  {
    string ConnectionString { get; }

    string DatabaseName { get; }

    string DateCommand { get; }

    string LengthCommand { get; }

    string NewGuid { get; }

    TimeSpan CommandTimeout { get; }

    TimeSpan ConnectionTimeout { get; }

    bool RetryOnDeadlock { get; set; }

    TimeSpan? PerformanceLoggingThreshold { get; }

    string Encapsulate(string value);

    /// <summary>
    /// Create a database connection and open it
    /// </summary>
    /// <returns>An open database connection</returns>
    IDbConnection Connect();

    IDbCommand CreateCommand(IDbConnection connection);
    IDbCommand CreateCommand(IDbTransaction transaction);
    void SetParameter(IDbCommand command, string field, object value);
    void ClearParameters(IDbCommand command);
    void ClearParameterValues(IDbCommand command);
    List<string> GetParameterFields(IDbCommand command);
    string AddParameter(IDbCommand command, string field, DbType type);
    string AddParameter(IDbCommand command, string field, int size, DbType type);
    string AddTextParameter(IDbCommand command, string field, int size);
    string AddParameter(IDbCommand command, string field, object value, DbType type);
    string AddParameter(IDbCommand command, string field, object value, DbType type, bool addAsLiteral);
    string AddParameter(IDbCommand command, string field, object value);
    string AddParameter(IDbCommand command, string field, object value, bool addAsLiteral);
    string AddParameter(IDbCommand command, string field, string value);
    string AddParameter(IDbCommand command, string field, string value, bool addAsLiteral);
    string AddParameter(IDbCommand command, string field, DateTime value);
    string AddParameter(IDbCommand command, string field, DateTime value, bool addAsLiteral);
    string AddParameter(IDbCommand command, string field, DateTime? value);
    string AddParameter(IDbCommand command, string field, DateTime? value, bool addAsLiteral);
    string AddParameter(IDbCommand command, string field, int value);
    string AddParameter(IDbCommand command, string field, int value, bool addAsLiteral);
    string AddParameter(IDbCommand command, string field, int? value);
    string AddParameter(IDbCommand command, string field, int? value, bool addAsLiteral);
    string AddParameter(IDbCommand command, string field, long value);
    string AddParameter(IDbCommand command, string field, long value, bool addAsLiteral);
    string AddParameter(IDbCommand command, string field, ulong value);
    string AddParameter(IDbCommand command, string field, ulong value, bool addAsLiteral);
    string AddParameter(IDbCommand command, string field, long? value);
    string AddParameter(IDbCommand command, string field, long? value, bool addAsLiteral);
    string AddParameter(IDbCommand command, string field, bool value);
    string AddParameter(IDbCommand command, string field, bool value, bool addAsLiteral);
    string AddParameter(IDbCommand command, string field, double value);
    string AddParameter(IDbCommand command, string field, double value, bool addAsLiteral);
    string AddParameter(IDbCommand command, string field, decimal value);
    string AddParameter(IDbCommand command, string field, decimal value, bool addAsLiteral);
    string AddParameter(IDbCommand command, string field, Guid value);
    string AddParameter(IDbCommand command, string field, Guid value, bool addAsLiteral);
    string AddParameter(IDbCommand command, string field, Guid? value);
    string AddParameter(IDbCommand command, string field, Guid? value, bool addAsLiteral);

    Task<int> ExecuteNonQuery(IDbCommand command);
    Task<IDataReader> ExecuteReader(IDbCommand command);
    Task<IDataReader> ExecuteReader(IDbCommand command, CommandBehavior behavior);
    Task<object> ExecuteScalar(IDbCommand command);
    Task<bool> Read(IDataReader reader);
  }
}
