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
using Microsoft.Extensions.Logging;

namespace Apteco.PullMarketing.Data.Dynamo
{
	public class DynamoFacade : IDataFacade
	{
    #region private fields
    private DynamoConnectionSettings connectionSettings;
    private ILogger<DynamoFacade> logger;
    #endregion

    #region public constructor
    public DynamoFacade(DynamoConnectionSettings connectionSettings,  ILogger<DynamoFacade> logger)
		{
			this.connectionSettings = connectionSettings;
			this.logger = logger;
		}
    #endregion

    #region public methods
    public async Task<bool> DeleteTable(string tableName, int timeoutInSeconds)
		{
			//Connect
			using (var client = Connect())
			{
				//Delete the table
			  try
			  {
			    logger.LogInformation("Deleting table " + tableName);
			    var response = await client.DeleteTableAsync(tableName);
			    if (response.TableDescription == null)
			      return false;

			    logger.LogInformation("Waiting for table " + tableName);
			    bool success = await WaitForTableToBeDeleted(client, tableName, timeoutInSeconds);
			    if (success)
          {
            logger.LogInformation("Table '" + tableName + "' was deleted");
            return true;
          }
          else
			    {
			      logger.LogInformation("Table '" + tableName + "' was not deleted after " + timeoutInSeconds + " seconds");
			      return false;
			    }
			  }
			  catch (ResourceNotFoundException)
			  {
			    return false;
			  }
			}
		}

		public async Task<bool> CreateTable(string tableName, string primaryKeyFieldName, int timeoutInSeconds)
		{
			//Connect
			using (var client = Connect())
			{
				//Create the table
				var createTableRequest = new CreateTableRequest();
				createTableRequest.TableName = tableName;
				createTableRequest.ProvisionedThroughput = new ProvisionedThroughput(5, 1000);

				//Set the attribute field
				createTableRequest.AttributeDefinitions = new List<AttributeDefinition>();
				AttributeDefinition ad = new AttributeDefinition(primaryKeyFieldName, ScalarAttributeType.S);
				createTableRequest.AttributeDefinitions.Add(ad);

				//Set up a hash key that points to the attribute
				createTableRequest.KeySchema = new List<KeySchemaElement>();
				createTableRequest.KeySchema.Add(new KeySchemaElement(primaryKeyFieldName, KeyType.HASH));

			  logger.LogInformation("Creating table '" + tableName + "'");

				await client.CreateTableAsync(createTableRequest);

				bool success = await WaitForTableState(client, tableName, "ACTIVE", timeoutInSeconds);

			  if (success)
			  {
			    logger.LogInformation("Table '" + tableName + "' is now 'ACTIVE'");
			    return true;
			  }
			  else
			  {
			    logger.LogInformation("Table '" + tableName + "' did not become 'ACTIVE' active "+ timeoutInSeconds+" seconds");
			    return false;
			  }
      }
		}

	  public async Task<List<DataStore>> GetDataStores()
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

	      return dataStores;
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
			//Connect
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
    #endregion

    #region private methods
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

			var fileReaderTask = Task.Factory.StartNew(() =>
			{
				ReadInput(stream, fileMetadata, lines);
			});
			
			var updateTasks = new Task[numberOfTasks];
      UpsertResults[] taskResults = new UpsertResults[numberOfTasks];

      for (int i = 0; i < updateTasks.Length; i++)
      {
        int taskNumber = i;
				updateTasks[taskNumber] = Task.Factory.StartNew(() =>
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
				  UpdateColumnOffsets(fileMetadata, sr.ReadLine()?.Split(delimiter));

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

		private AmazonDynamoDBClient Connect()
		{
			var ddbConfig = new AmazonDynamoDBConfig();
			ddbConfig.ThrottleRetries = true;

			if (!string.IsNullOrEmpty(connectionSettings.ServiceUrl))
				ddbConfig.ServiceURL = connectionSettings.ServiceUrl;

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