using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

namespace Apteco.PullMarketing.Api.Test.TestHelpers
{
  public class MutableMemoryConfigurationProvider : ConfigurationProvider, IEnumerable<KeyValuePair<string, string>>
  {
    #region private fields
    private readonly MutableMemoryConfigurationSource source;
    #endregion

    #region public constructor
    /// <summary>
    /// Initialize a new instance from the source.
    /// </summary>
    /// <param name="source">The source settings.</param>
    public MutableMemoryConfigurationProvider(MutableMemoryConfigurationSource source)
    {
      if (source == null)
        throw new ArgumentNullException(nameof(source));

      this.source = source;
      source.Changed += SourceOnChanged;

      Load();
    }
    #endregion

    #region public methods
    public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
    {
      return Data.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
      return GetEnumerator();
    }

    public override void Set(string key, string value)
    {
      base.Set(key, value);
      source[key] = value;
    }

    public override void Load()
    {
      Data.Clear();
      foreach (var pair in source)
      {
        Data.Add(pair.Key, pair.Value);
      }
      OnReload();
    }
    #endregion

    #region private methods
    private void SourceOnChanged(object sender, EventArgs eventArgs)
    {
      Load();
    }
    #endregion
  }
}
