using SqlKata;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace Apteco.PullMarketing.Data.MSSQL.DataTier
{
  public static class DbCommandUtilities
  {
    #region public methods
    public static async Task<int> ExecuteNonQueryAsync(this IDbCommand command)
    {
      if (command is SqlCommand)
        return await ((SqlCommand)command).ExecuteNonQueryAsync();

      return await Task.Run(() => command.ExecuteNonQuery());
    }

    public static async Task<object> ExecuteScalarAsync(this IDbCommand command)
    {
      if (command is SqlCommand)
        return await ((SqlCommand)command).ExecuteScalarAsync();

      return await Task.Run(() => command.ExecuteScalar());
    }

    public static async Task<IDataReader> ExecuteReaderAsync(this IDbCommand command)
    {
      if (command is SqlCommand)
        return await ((SqlCommand)command).ExecuteReaderAsync();

      return await Task.Run(() => command.ExecuteReader());
    }

    public static async Task<IDataReader> ExecuteReaderAsync(this IDbCommand command, CommandBehavior behavior)
    {
      if (command is SqlCommand)
        return await ((SqlCommand)command).ExecuteReaderAsync(behavior);

      return await Task.Run(() => command.ExecuteReader(behavior));
    }

    public static async Task<bool> ReadAsync(this IDataReader reader)
    {
      if (reader is SqlDataReader)
        return await ((SqlDataReader)reader).ReadAsync();

      return await Task.Run(() => reader.Read());
    }

    public static void AddSqlResult(this IDbCommand command, IDbAccess dbAccess, SqlResult sqlResult)
    {
      command.CommandText = sqlResult.Sql;
      for (int i = 0; i < sqlResult.Bindings.Count; i++)
        dbAccess.AddParameter(command, $"p{i}", sqlResult.Bindings[i]);
    }
    #endregion
  }
}
