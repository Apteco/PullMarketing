using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
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
    public async Task DeleteTable(string tableName)
		{
			//Connect
			using (var client = Connect())
			{
				//Delete the table
				try
				{
					logger.LogInformation("Deleting table " + tableName);
					await client.DeleteTableAsync(tableName);

				  logger.LogInformation("Waiting for table " + tableName);

					//Wait for deletion
					while (true)
					{
						var tables = await client.ListTablesAsync();
						var tableNames = tables.TableNames;

						if (!tableNames.Contains(tableName))
						{
						  logger.LogInformation("Deleted table " + tableName);
							break;
						}
					}
				}
				catch (ResourceNotFoundException) {}
			}
		}

		public async Task CreateTable(string tableName, string primaryKeyFieldName)
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

				var response = await client.CreateTableAsync(createTableRequest);

				await WaitForTableState(client, tableName, "ACTIVE");

			  logger.LogInformation("Table '" + tableName + "' is now 'ACTIVE'");
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
    private async Task WaitForTableState(AmazonDynamoDBClient client, string tableName, string matchState)
	  {
	    string status = null;

	    // Let us wait until table has the match state. Call DescribeTable.
	    do
	    {
	      logger.LogInformation("Waiting for table '" + tableName + "' to become 'ACTIVE'");

	      try
	      {
	        var res = await client.DescribeTableAsync(new DescribeTableRequest(tableName));
	        status = res.Table.TableStatus;
	      }
	      catch (ResourceNotFoundException)
	      {
	        // DescribeTable is eventually consistent.  So we handle the potential exception.
	      }

	      var delayTask = Task.Delay(1000);
	      delayTask.Wait();
	    }
	    while (status != matchState);
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