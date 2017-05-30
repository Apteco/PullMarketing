using System;
using System.Collections.Generic;
using System.Text;
using Amazon.DynamoDBv2.Model;

namespace Apteco.PullMarketing.Data.Dynamo
{
  public class PrimaryKeyValue
  {
    public string TableName { get; private set; }
    public string KeyName { get; private set; }
    public string Value { get; private set; }

    public PrimaryKeyValue(string tableName, string keyName, string value)
    {
      TableName = tableName;
      KeyName = keyName;
      Value = value;
    }

    public Dictionary<string, AttributeValue> CreateKeyValueMap()
    {
      Dictionary<string, AttributeValue> key = new Dictionary<string, AttributeValue>();
      key.Add(KeyName, new AttributeValue(Value));
      return key;
    }
  }
}
