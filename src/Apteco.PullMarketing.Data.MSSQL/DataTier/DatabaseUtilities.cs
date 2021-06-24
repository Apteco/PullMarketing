using System;
using System.Data.SqlTypes;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace Apteco.PullMarketing.Data.MSSQL.DataTier
{
  public class DatabaseUtilities
  {
    public static string ConvertToString(object o, string defaultValue)
    {
      try
      {
        return (o == DBNull.Value || o is DBNull || o == null) ? null : Convert.ToString(o);
      }
      catch
      {
        return defaultValue;
      }
    }

    public static string ConvertToString(object o)
    {
      return ConvertToString(o, null);
    }

    public static int ConvertToInt32(object o, int defaultValue)
    {
      if (o == DBNull.Value || o is DBNull || o == null)
        return defaultValue;

      try
      {
        return Convert.ToInt32(o);
      }
      catch
      {
        return defaultValue;
      }
    }

    public static int ConvertToInt32(object o)
    {
      return ConvertToInt32(o, 0);
    }


    public static double? ConvertToNullableDouble(object o)
    {
      if (o == DBNull.Value || o is DBNull || o == null)
        return null;

      try
      {
        return Convert.ToDouble(o);
      }
      catch
      {
        return null;
      }
    }

    public static int? ConvertToNullableInt32(object o)
    {
      if (o == DBNull.Value || o is DBNull || o == null)
        return null;

      try
      {
        return Convert.ToInt32(o);
      }
      catch
      {
        return null;
      }
    }

    public static long? ConvertToNullableInt64(object o)
    {
      if (o == DBNull.Value || o is DBNull || o == null)
        return null;

      try
      {
        return Convert.ToInt64(o);
      }
      catch
      {
        return null;
      }
    }

    public static T? ConvertToNullableEnum<T>(object o) where T : struct
    {
      if (o == DBNull.Value || o is DBNull || o == null)
        return null;

      if (Enum.TryParse(o.ToString(), true, out T value))
        return value;

      return null;
    }

    public static long ConvertToInt64(object o, long defaultValue)
    {
      if (o == DBNull.Value || o is DBNull || o == null)
        return defaultValue;

      try
      {
        return Convert.ToInt64(o);
      }
      catch
      {
        return defaultValue;
      }
    }

    public static long ConvertToInt64(object o)
    {
      return ConvertToInt64(o, 0);
    }

    public static Guid ConvertToGuid(object o, Guid defaultValue)
    {
      if (o == DBNull.Value || o is DBNull || o == null)
        return defaultValue;

      try
      {
        return Guid.Parse(o.ToString());
      }
      catch
      {
        return defaultValue;
      }
    }

    public static Guid ConvertToGuid(object o)
    {
      return ConvertToGuid(o, Guid.Empty);
    }

    public static double ConvertToDouble(object o, double defaultValue)
    {
      if (o == DBNull.Value || o is DBNull || o == null)
        return defaultValue;

      try
      {
        return Convert.ToDouble(o);
      }
      catch
      {
        return defaultValue;
      }
    }

    public static double ConvertToDouble(object o)
    {
      return ConvertToDouble(o, 0.0);
    }

    public static decimal ConvertToDecimal(object o, decimal defaultValue)
    {
      if (o == DBNull.Value || o is DBNull || o == null)
        return defaultValue;

      try
      {
        return Convert.ToDecimal(o);
      }
      catch
      {
        return defaultValue;
      }
    }

    public static decimal ConvertToDecimal(object o)
    {
      return ConvertToDecimal(o, 0.0M);
    }

    public static bool ConvertToBoolean(object o)
    {
      return ConvertToBoolean(o, false);
    }

    public static bool? ConvertToNullableBoolean(object o)
    {
      if (o == DBNull.Value || o is DBNull || o == null)
        return null;

      try
      {
        return Convert.ToBoolean(o);
      }
      catch
      {
        return null;
      }
    }

    public static bool ConvertToBoolean(object o, bool defaultValue)
    {
      if (o == DBNull.Value || o is DBNull || o == null)
        return defaultValue;

      try
      {
        return Convert.ToBoolean(o);
      }
      catch
      {
        try
        {
          return Convert.ToInt32(o) != 0;
        }
        catch
        {
          return defaultValue;
        }
      }
    }

    public static DateTime ConvertToDateTime(object o, DateTime defaultValue)
    {
      if (o == DBNull.Value || o is DBNull || o == null)
        return defaultValue;

      try
      {
        return Convert.ToDateTime(o);
      }
      catch
      {
        return defaultValue;
      }
    }

    public static DateTime ConvertToDateTime(object o)
    {
      return ConvertToDateTime(o, DateTime.MinValue);
    }

    public static DateTime? ConvertToNullableDateTime(object o)
    {
      if (o == DBNull.Value || o is DBNull || o == null)
        return null;

      try
      {
        return Convert.ToDateTime(o);
      }
      catch
      {
        return null;
      }
    }

    public static string GetDateTime(object o)
    {
      if (o == DBNull.Value || o is DBNull || o == null)
        return "";

      try
      {
        return Convert.ToString(o, new CultureInfo("en-GB", false));
      }
      catch
      {
        return "";
      }
    }

    public static object GetDateTime(string dateTimeStr)
    {
      if (string.IsNullOrEmpty(dateTimeStr))
        return Convert.DBNull;

      try
      {
        return Convert.ToDateTime(dateTimeStr, new CultureInfo("en-GB", false));
      }
      catch
      {
        return Convert.DBNull;
      }
    }

    public static string Decompress(SqlBytes sqlBytes)
    {
      using (var ds = new GZipStream(sqlBytes.Stream, CompressionMode.Decompress))
      {
        using (var sr = new StreamReader(ds, Encoding.Unicode))
        {
          return sr.ReadToEnd();
        }
      }
    }

    public static SqlBytes Compress(string xml)
    {
      using (var ms = new MemoryStream())
      {
        using (var ds = new GZipStream(ms, CompressionMode.Compress, true))
        {
          using (StreamWriter sw = new StreamWriter(ds, Encoding.Unicode))
          {
            sw.Write(xml);
          }
        }

        ms.Position = 0;
        return new SqlBytes(ms.ToArray());
      }
    }
  }
}
