using System;

namespace Apteco.PullMarketing.Data.MSSQL.DataTier
{
  public class SimulatedSqlDeadlockException : Exception
  {
    public SimulatedSqlDeadlockException()
    {
    }

    public SimulatedSqlDeadlockException(string message)
      : base(message)
    {
    }

    public SimulatedSqlDeadlockException(string message, Exception innerException)
      : base(message, innerException)
    {
    }
  }
}
