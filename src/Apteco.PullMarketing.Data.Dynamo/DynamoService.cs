using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amazon;
using ApiPager.Core;
using ApiPager.Data.Linq;
using Apteco.PullMarketing.Data.Models;
using Microsoft.Extensions.Logging;
using Record = Apteco.PullMarketing.Data.Models.Record;

namespace Apteco.PullMarketing.Data.Dynamo
{
	public class DynamoService : IDataService
	{
    #region private fields
    private IDynamoConnectionSettings connectionSettings;
    private ILogger<DynamoService> logger;
    #endregion

    #region public constructor
    public DynamoService(IDynamoConnectionSettings connectionSettings,  ILogger<DynamoService> logger)
		{
			this.connectionSettings = connectionSettings;
			this.logger = logger;
		}
    #endregion

    #region public methods
    public async Task<bool> DeleteDataStore(string tableName)
		{
			using (var client = Connect())
			{
			  try
			  {
			    logger.LogInformation("Deleting table " + tableName);
			    var response = await client.DeleteTableAsync(tableName);
			    if (response.TableDescription == null)
			      return false;

			    logger.LogInformation("Waiting for table " + tableName);
			    bool success = await WaitForTableToBeDeleted(client, tableName, connectionSettings.ModifyDataStoreTimeoutInSeconds);
			    if (success)
          {
            logger.LogInformation("Table '" + tableName + "' was deleted");
            return true;
          }
          else
			    {
			      logger.LogInformation("Table '" + tableName + "' was not deleted after " + connectionSettings.ModifyDataStoreTimeoutInSeconds + " seconds");
			      return false;
			    }
			  }
			  catch (ResourceNotFoundException)
			  {
			    return false;
			  }
			}
		}

		public async Task<bool> CreateDataStore(DataStore dataStore)
		{
			using (var client = Connect())
			{
				//Create the table
				var createTableRequest = new CreateTableRequest();
				createTableRequest.TableName = dataStore.Name;
				createTableRequest.BillingMode = BillingMode.PAY_PER_REQUEST;

				//Set the attribute field
				createTableRequest.AttributeDefinitions = new List<AttributeDefinition>();
				AttributeDefinition ad = new AttributeDefinition(dataStore.PrimaryKeyFieldName, ScalarAttributeType.S);
				createTableRequest.AttributeDefinitions.Add(ad);

				//Set up a hash key that points to the attribute
				createTableRequest.KeySchema = new List<KeySchemaElement>();
				createTableRequest.KeySchema.Add(new KeySchemaElement(dataStore.PrimaryKeyFieldName, KeyType.HASH));

			  logger.LogInformation("Creating table '" + dataStore.Name + "'");

				await client.CreateTableAsync(createTableRequest);

				bool success = await WaitForTableState(client, dataStore.Name, "ACTIVE", connectionSettings.ModifyDataStoreTimeoutInSeconds);

			  if (success)
			  {
			    logger.LogInformation("Table '" + dataStore.Name + "' is now 'ACTIVE'");
			    return true;
			  }
			  else
			  {
			    logger.LogInformation("Table '" + dataStore.Name + "' did not become 'ACTIVE' active "+ connectionSettings.ModifyDataStoreTimeoutInSeconds + " seconds");
			    return false;
			  }
      }
		}

	  public async Task<IEnumerable<DataStore>> GetDataStores(FilterPageAndSortInfo filterPageAndSortInfo)
	  {
	    using (var client = Connect())
	    {
        List<DataStore> dataStores = new List<DataStore>();
	      var tables = await client.ListTablesAsync();
	      foreach (string tableName in tables.TableNames)
	      {
	        DataStore dataStore = await GetDataStore(client, tableName);
          if (dataStore != null)
            dataStores.Add(dataStore);
        }

	      return dataStores.Filter(filterPageAndSortInfo, new string[] { "Name", "PrimaryKeyFieldName" }, "Name");
	    }
	  }

	  public async Task<DataStore> GetDataStore(string name)
	  {
	    using (var client = Connect())
	    {
       return await GetDataStore(client, name);
	    }
    }

    public async Task<UpsertResults> Upsert(Stream stream, UpsertDetails upsertDetails)
		{
			using (var client = Connect())
			{
				var tables = await client.ListTablesAsync();
				var tableNames = tables.TableNames;

				//Check that the table exists in Dynamo
				if (!tableNames.Contains(upsertDetails.TableName))
					throw new Exception("No table with name '" + upsertDetails.TableName + "' found");

				//Get current table details including provisioned throughput
				var tableDetails = await client.DescribeTableAsync(upsertDetails.TableName);
				var throughput = tableDetails.Table.ProvisionedThroughput;

			  logger.LogInformation("Provisioned write capacity for table : " + throughput.WriteCapacityUnits.ToString());

			  logger.LogInformation("Opening table '" + upsertDetails.TableName + "'");
				var table = Table.LoadTable(client, upsertDetails.TableName);

				//Check keys match
				if (table.Keys.ContainsKey(upsertDetails.PrimaryKeyFieldName))
				{
				  logger.LogInformation("Table contains key '" + upsertDetails.PrimaryKeyFieldName + "'");

					if (table.Keys[upsertDetails.PrimaryKeyFieldName].IsHash)
					{
					  logger.LogInformation("Key '" + upsertDetails.PrimaryKeyFieldName + "' is a hash key.");
					}
					else
					{
						string msg = "Specified key '" + upsertDetails.PrimaryKeyFieldName + "' is not a hash key";
					  logger.LogInformation(msg);
						throw new Exception(msg);
					}
				}
				else
				{
					string msg = "Could not find key with name '" + upsertDetails.PrimaryKeyFieldName + "'";
				  logger.LogInformation(msg);
					throw new Exception(msg);
				}

        logger.LogInformation("Starting updates");
			  UpsertResults results = RunUpdates(stream, upsertDetails.FileMetadata, throughput, table);
			  logger.LogInformation("Updates complete");

				return results;
			}
		}

	  public async Task<IEnumerable<Record>> GetRecords(string tableName, FilterPageAndSortInfo filterPageAndSortInfo)
	  {
	    using (var client = Connect())
	    {
	      Table table;
	      if (!Table.TryLoadTable(client, tableName, out table) || (table == null))
	        return null;

        List<Record> records = new List<Record>();

        ScanOperationConfig scanOperationConfig = new ScanOperationConfig();
	      scanOperationConfig.Limit = filterPageAndSortInfo.Count;

	      while (true)
	      {
	        Search results = table.Scan(scanOperationConfig);
	        List<Document> documents = await results.GetNextSetAsync();
	        foreach (Document document in documents.Take(filterPageAndSortInfo.Count - records.Count))
	        {
	          records.Add(CreateRecord(table.HashKeys[0], document.ToAttributeMap()));
	        }

	        if (records.Count >= filterPageAndSortInfo.Count || results.IsDone)
	        {
	          filterPageAndSortInfo.TotalCount = records.Count + results.Count;
	          break;
	        }
	        else
	        {
	          scanOperationConfig.PaginationToken = results.PaginationToken;
	        }
        }

        return records;
	    }
	  }

    public async Task<Record> GetRecord(string tableName, string primaryKeyValue)
	  {
	    using (var client = Connect())
	    {
	      PrimaryKeyValue key = await CreatePrimaryKeyValue(client, tableName, primaryKeyValue);

        var response = await client.GetItemAsync(tableName, key.CreateKeyValueMap());
	      if (!response.IsItemSet)
	        return null;

	      Record record = CreateRecord(key.KeyName, response.Item);
	      return record;
	    }
	  }

	  public async Task<bool> UpsertRecord(string tableName, Record record)
	  {
	    using (var client = Connect())
	    {
	      PrimaryKeyValue key = await CreatePrimaryKeyValue(client, tableName, record.Key);
        Dictionary<string, AttributeValueUpdate> attributes = new Dictionary<string, AttributeValueUpdate>();
	      foreach (Field field in record.Fields)
	      {
	        attributes[field.Key] = new AttributeValueUpdate(new AttributeValue(field.Value), AttributeAction.PUT);
	      }

	      await client.UpdateItemAsync(tableName, key.CreateKeyValueMap(), attributes);
	      return true;
	    }
    }

    public async Task<bool> DeleteRecord(string tableName, string primaryKeyValue)
	  {
	    using (var client = Connect())
	    {
	      PrimaryKeyValue key = await CreatePrimaryKeyValue(client, tableName, primaryKeyValue);

	      await client.DeleteItemAsync(tableName, key.CreateKeyValueMap());
	      return true;
	    }
	  }

	  public async Task<Field> GetRecordField(string tableName, string primaryKeyValue, string fieldName)
	  {
	    Record record = await GetRecord(tableName, primaryKeyValue);
	    if (record?.Fields == null)
	      return null;

	    return record.Fields.FirstOrDefault(f => f.Key == fieldName);
	  }

	  public async Task<bool> UpsertRecordField(string tableName, string primaryKeyValue, Field field)
	  {
	    using (var client = Connect())
	    {
	      PrimaryKeyValue key = await CreatePrimaryKeyValue(client, tableName, primaryKeyValue);
	      Dictionary<string, AttributeValueUpdate> attributes = new Dictionary<string, AttributeValueUpdate>();
	      attributes[field.Key] = new AttributeValueUpdate(new AttributeValue(field.Value), AttributeAction.PUT);

	      await client.UpdateItemAsync(tableName, key.CreateKeyValueMap(), attributes);
	      return true;
	    }
    }

	  public async Task<bool> DeleteRecordField(string tableName, string primaryKeyValue, string fieldName)
	  {
	    using (var client = Connect())
	    {
	      PrimaryKeyValue key = await CreatePrimaryKeyValue(client, tableName, primaryKeyValue);
	      Dictionary<string, AttributeValueUpdate> attributes = new Dictionary<string, AttributeValueUpdate>();
        attributes[fieldName] = new AttributeValueUpdate(null, AttributeAction.DELETE);

	      await client.UpdateItemAsync(tableName, key.CreateKeyValueMap(), attributes);
	      return true;
	    }
    }
    #endregion

    #region private methods
    private async Task<PrimaryKeyValue> CreatePrimaryKeyValue(AmazonDynamoDBClient client, string tableName, string primaryKeyValue)
	  {
	    DataStore dataStore = await GetDataStore(client, tableName);

	    return new PrimaryKeyValue(tableName, dataStore.PrimaryKeyFieldName, primaryKeyValue);
	  }

    private async Task<DataStore> GetDataStore(AmazonDynamoDBClient client, string tableName)
	  {
	    try
	    {
	      var tableDetails = await client.DescribeTableAsync(tableName);
	      if (tableDetails == null)
	        return null;

	      var primaryKeySchema = tableDetails?.Table?.KeySchema?.FirstOrDefault(ks => ks.KeyType == KeyType.HASH);
	      return new DataStore()
	      {
	        Name = tableName,
	        PrimaryKeyFieldName = primaryKeySchema?.AttributeName
	      };
	    }
	    catch (ResourceNotFoundException)
	    {
	      return null;
	    }
	  }

	  private async Task<bool> WaitForTableToBeDeleted(AmazonDynamoDBClient client, string tableName, int timeoutInSeconds)
	  {
	    bool success = await WaitForStatusWithTimeout(async () =>
	      {
	        var tables = await client.ListTablesAsync();
	        var tableNames = tables.TableNames;

	        return !tableNames.Contains(tableName);
	      },
	      TimeSpan.FromSeconds(1),
	      TimeSpan.FromSeconds(timeoutInSeconds)
	    );

	    return success;
    }

    private async Task<bool> WaitForTableState(AmazonDynamoDBClient client, string tableName, string matchState, int timeoutInSeconds)
	  {
	    bool success = await WaitForStatusWithTimeout(async () =>
        {
          try
          {
            var res = await client.DescribeTableAsync(new DescribeTableRequest(tableName));
            return res.Table.TableStatus == matchState;
          }
          catch (ResourceNotFoundException)
          {
            return false;
          }
        },
	      TimeSpan.FromSeconds(1),
	      TimeSpan.FromSeconds(timeoutInSeconds)
	    );

	    return success;
	  }

	  private async Task<bool> WaitForStatusWithTimeout(Func<Task<bool>> getStatusFunc, TimeSpan pollDelay, TimeSpan timeout)
	  {
      DateTime startTime = DateTime.Now;

	    while((DateTime.Now - startTime) < timeout)
	    {
	      bool success = await getStatusFunc();
	      if (success)
	        return true;

        var delayTask = Task.Delay(pollDelay);
	      delayTask.Wait();
	    }

	    return false;
	  }

    private UpsertResults RunUpdates(Stream stream, FileMetadata fileMetadata, ProvisionedThroughputDescription throughput, Table table)
		{
			//A thread can make ~50 update requests / second
			int numberOfTasks = (int)(throughput.WriteCapacityUnits / 50L);
			if (numberOfTasks < 1)
				numberOfTasks = 1;

			int queueSize = 50 * numberOfTasks;

			var lines = new BlockingCollection<string[]>(queueSize);

			var fileReaderTask = Task.Run(() =>
			{
				ReadInput(stream, fileMetadata, lines);
			});
			
			var updateTasks = new Task[numberOfTasks];
      UpsertResults[] taskResults = new UpsertResults[numberOfTasks];

      for (int i = 0; i < updateTasks.Length; i++)
      {
        int taskNumber = i;
				updateTasks[taskNumber] = Task.Run(() =>
				{
				  taskResults[taskNumber] = UpdateItems(fileMetadata, table, lines);
  			});
			}

			fileReaderTask.Wait();
			Task.WaitAll(updateTasks);

		  UpsertResults consolodatedResults = new UpsertResults();
		  for (int i = 0; i < updateTasks.Length; i++)
		  {
		    consolodatedResults.NumberOfRecordsUpserted += taskResults[i].NumberOfRecordsUpserted;
		    consolodatedResults.NumberOfRecordsSkipped += taskResults[i].NumberOfRecordsSkipped;
		  }
		  return consolodatedResults;
		}

    private UpsertResults UpdateItems(FileMetadata fileMetadata, Table table, BlockingCollection<string[]> lines)
		{
			long updates = 0;
			long errors = 0;

			//Read lines into Documents, send updates to Dynamo
			foreach (var line in lines.GetConsumingEnumerable())
			{
				var document = new Document();

				foreach (var fieldMetadata in fileMetadata.Fields)
				{
					var colName = fieldMetadata.ColumnName;
					var colOffset = fieldMetadata.Offset;
					if (colOffset >= 0 && colOffset < line.Length)
						document[colName] = line[colOffset];
				}

				try
				{
					while (true)
					{
						try
						{
							//AWSSDK already implements retries with an increasing delay
							Task task = table.UpdateItemAsync(document);
						  task.Wait();

							updates++;
							break;
						}
						catch (ProvisionedThroughputExceededException)
						{
							//Try again
						}
					}
				}
				catch (AmazonDynamoDBException)
				{
					//There was a problem updating this item, skip it
					errors++;
				}
			}
		  return new UpsertResults() {NumberOfRecordsUpserted = updates, NumberOfRecordsSkipped = errors};
		}

		private void UpdateColumnOffsets(FileMetadata fileMetadata, string[] header)
	  {
	    if (header == null || !fileMetadata.MatchOnHeader)
	      return;

	    foreach (FieldMetadata fieldMetadata in fileMetadata.Fields)
	      fieldMetadata.Offset = Array.IndexOf(header, fieldMetadata.HeaderName ?? fieldMetadata.ColumnName);
	  }

		private void ReadInput(Stream stream, FileMetadata fileMetadata, BlockingCollection<string[]> lines)
		{
			char delimiter = '\t';
			if (fileMetadata.Delimiter > 0)
				delimiter = (char)fileMetadata.Delimiter;

		  char encloser = (char)0;
      if (fileMetadata.Encloser > 0)
        encloser = (char)fileMetadata.Encloser;
      
      Encoding encoding = Encoding.UTF8;
			if (fileMetadata.Encoding.ToUpper() == "DEFAULT")
				encoding = Encoding.GetEncoding(0);
			else if (fileMetadata.Encoding.ToUpper() == "ASCII")
				encoding = Encoding.ASCII;
			else if (fileMetadata.Encoding.ToUpper() == "UTF8")
				encoding = Encoding.UTF8;

		  bool canCalculatePercentage = stream.CanSeek;

			//Open input file
			using (var sr = new StreamReader(stream, encoding))
			{
				//Skip header if we have one, and update column offsets
			  if (fileMetadata.Header)
			  {
			    string[] headers = sr.ReadLine()?.Split(delimiter);
			    StringUtilities.RemoveEnclosers(headers, encloser);

          UpdateColumnOffsets(fileMetadata, headers);
			  }

			  int maxColumnIndex = 0;
				foreach (FieldMetadata field in fileMetadata.Fields)
				  maxColumnIndex = Math.Max(field.Offset, maxColumnIndex);

				long progressPercent = 0;
				long recordsRead = 0;

				while (true)
				{
				  if (canCalculatePercentage)
				  {
				    long percent = (stream.Position * 100L) / stream.Length;
				    if (percent > progressPercent)
				    {
				      progressPercent = percent;
				      logger.LogInformation("Records read from input file : " + recordsRead + " (" + percent + "%)");
				    }
				  }
				  else
				  {
            if (recordsRead % 1000 == 0)
  				    logger.LogInformation("Records read from input file : " + recordsRead);
				  }

          var line = sr.ReadLine();

					if (line == null)
						break;

					var items = line.Split(delimiter);
  		    StringUtilities.RemoveEnclosers(items, encloser);

					if (items.Length > maxColumnIndex)
					{
						lines.Add(items);
					}
					else
					{
						logger.LogInformation("Record " + recordsRead + " only contains " + items.Length + " columns when " + maxColumnIndex + " are required");
					}

					recordsRead++;
				}
				logger.LogInformation("Records read from input file : " + recordsRead + " (100%)");
			}

			lines.CompleteAdding();
		}

	  private Record CreateRecord(string primaryKeyName, Dictionary<string, AttributeValue> attributesMap)
	  {
	    string key;
	    AttributeValue keyValue;
	    if (!attributesMap.TryGetValue(primaryKeyName, out keyValue))
	      key = null;
	    else
	      key = keyValue.S;

	    return new Record()
	    {
	      Key = key,
	      Fields = attributesMap
	        .Where(kvp => kvp.Key != primaryKeyName)
	        .Select(kvp => new Field() { Key = kvp.Key, Value = kvp.Value.S })
	        .ToList()
	    };
	  }

    private AmazonDynamoDBClient Connect()
		{
			var ddbConfig = new AmazonDynamoDBConfig();
			ddbConfig.ThrottleRetries = true;
      
      if (!string.IsNullOrEmpty(connectionSettings.ServiceUrl))
				ddbConfig.ServiceURL = connectionSettings.ServiceUrl;

		  if (!string.IsNullOrEmpty(connectionSettings.RegionEndpoint))
		  {
		    RegionEndpoint regionEndpoint = RegionEndpoint.GetBySystemName(connectionSettings.RegionEndpoint);
        if (regionEndpoint == null)
          throw new Exception($"Can't parse the specified RegionEndpoint ({connectionSettings.RegionEndpoint}) as a valid value");

        ddbConfig.RegionEndpoint = regionEndpoint;
      }

      AWSCredentials credentials = null;
			if (!string.IsNullOrEmpty(connectionSettings.AccessKey))
				credentials = new BasicAWSCredentials(connectionSettings.AccessKey, connectionSettings.SecretAccessKey);
			
			if (credentials == null)
				return new AmazonDynamoDBClient(ddbConfig);
			else
				return new AmazonDynamoDBClient(credentials, ddbConfig);
		}
    #endregion
  }
}