using System;

namespace Apteco.PullMarketing.Data.MSSQL.DataTier
{
  public class DbAccessConnectionProperties
  {
    #region public constants
    private static readonly TimeSpan DefaultCommandTimeout = TimeSpan.FromSeconds(120);
    private static readonly TimeSpan DefaultConnectionTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan? DefaultPerformanceLoggingThreshold = null;
    private const int DefaultConnectionRetries = 3;
    #endregion

    #region public properties
    public string ConnectionUri { get; set; }
    public TimeSpan CommandTimeout { get; set; }
    public TimeSpan ConnectionTimeout { get; set; }
    public int ConnectionRetries { get; set; }
    public TimeSpan? PerformanceLoggingThreshold { get; set; }
    #endregion

    #region public constructors
    public DbAccessConnectionProperties(string connectionUri)
    {
      ConnectionUri = connectionUri;
      CommandTimeout = DefaultCommandTimeout;
      ConnectionTimeout = DefaultConnectionTimeout;
      ConnectionRetries = DefaultConnectionRetries;
      PerformanceLoggingThreshold = DefaultPerformanceLoggingThreshold;
    }

    public DbAccessConnectionProperties(string connectionUri, TimeSpan commandTimeout, TimeSpan connectionTimeout, int connectionRetries, TimeSpan? performanceLoggingThreshold)
    {
      ConnectionUri = connectionUri;
      CommandTimeout = commandTimeout;
      ConnectionTimeout = connectionTimeout;
      ConnectionRetries = connectionRetries;
      PerformanceLoggingThreshold = performanceLoggingThreshold;
    }
    #endregion

    #region public methods
    public static DbAccessConnectionProperties Create(string connectionUri, string commandTimeoutInSecondsString, string connectionTimeoutInSecondsString, string connectionRetriesString, string performanceLoggingThresholdInMillisString)
    {
      TimeSpan commandTimeout;
      if (int.TryParse(commandTimeoutInSecondsString, out int commandTimeoutInSeconds))
        commandTimeout = TimeSpan.FromSeconds(commandTimeoutInSeconds);
      else
        commandTimeout = DefaultCommandTimeout;

      TimeSpan connectionTimeout;
      if (int.TryParse(connectionTimeoutInSecondsString, out int connectionTimeoutInSeconds))
        connectionTimeout = TimeSpan.FromSeconds(connectionTimeoutInSeconds);
      else
        connectionTimeout = DefaultConnectionTimeout;

      if (!int.TryParse(connectionRetriesString, out int connectionRetries))
        connectionRetries = DefaultConnectionRetries;

      TimeSpan? performanceLoggingThreshold;
      if (int.TryParse(performanceLoggingThresholdInMillisString, out int performanceLoggingThresholdInMillis))
        performanceLoggingThreshold = TimeSpan.FromMilliseconds(performanceLoggingThresholdInMillis);
      else
        performanceLoggingThreshold = DefaultPerformanceLoggingThreshold;

      return new DbAccessConnectionProperties(connectionUri, commandTimeout, connectionTimeout, connectionRetries, performanceLoggingThreshold);
    }
    #endregion
  }
}