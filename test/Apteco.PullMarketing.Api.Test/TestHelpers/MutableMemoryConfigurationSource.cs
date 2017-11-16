using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

namespace Apteco.PullMarketing.Api.Test.TestHelpers
{
  public class MutableMemoryConfigurationSource : IConfigurationSource, IEnumerable<KeyValuePair<string, string>>
  {
    #region private fields
    private Dictionary<string, string> data;
    #endregion

    #region public events
    public event EventHandler Changed;
    #endregion

    #region public properties
    public string this[string key]
    {
      get { return data[key]; }
      set
      {
        data[key] = value;
        OnChanged(EventArgs.Empty);
      }
    }
    #endregion

    #region public constructor
    public MutableMemoryConfigurationSource(IEnumerable<KeyValuePair<string, string>> initialData)
    {
      data = new Dictionary<string, string>();

      foreach (KeyValuePair<string, string> kvp in initialData)
        data[kvp.Key] = kvp.Value;
    }
    #endregion

    #region public methods
    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
      return new MutableMemoryConfigurationProvider(this);
    }

    public bool Remove(string key)
    {
      bool succeeded = data.Remove(key);
      OnChanged(EventArgs.Empty);
      return succeeded;
    }

    public bool TryGetValue(string key, out string value)
    {
      return data.TryGetValue(key, out value);
    }

    public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
    {
      return data.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
      return GetEnumerator();
    }
    #endregion

    #region protected methods
    protected virtual void OnChanged(EventArgs e)
    {
      Changed?.Invoke(this, e);
    }
    #endregion
  }
}
