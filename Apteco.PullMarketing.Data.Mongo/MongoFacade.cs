using System;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Apteco.PullMarketing.Data.Mongo
{
	public class MongoFacade : IDataFacade
  {
    #region private fields
    private MongoConnectionSettings connectionSettings;
		private ILogger<MongoFacade> logger;
    #endregion

    #region public constructor
    public MongoFacade(MongoConnectionSettings connectionSettings, ILogger<MongoFacade> logger)
		{
			this.connectionSettings = connectionSettings;
			this.logger = logger;
		}
    #endregion

    #region public methods
    public async Task<List<DataStore>> GetDataStores()
    {
      throw new NotImplementedException();
    }

    public async Task<DataStore> GetDataStore(string name)
    {
      throw new NotImplementedException();
    }

    public Task<bool> CreateTable(string tableName, string primaryKeyFieldName, int timeoutInSeconds)
    {
      throw new NotImplementedException();
    }

    public Task<bool> DeleteTable(string tableName, int timeoutInSeconds)
    {
      throw new NotImplementedException();
    }

    public async Task<UpsertResults> Upsert(Stream stream, UpsertDetails upsertDetails)
		{
			var client = Connect();
			var database = client.GetDatabase(upsertDetails.TableName);
			
			//Open input file
			using (var sr = new StreamReader(stream, Encoding.GetEncoding(0)))
			{
			  char delimiter = '\t';
			  if (upsertDetails.FileMetadata.Delimiter > 0)
			    delimiter = (char)upsertDetails.FileMetadata.Delimiter;

			  //Skip header if we have one, and update column offsets
			  if (upsertDetails.FileMetadata.Header)
			    UpdateColumnOffsets(upsertDetails.FileMetadata, (await sr.ReadLineAsync())?.Split(delimiter));

				long records = 0;

				var collection = database.GetCollection<BsonDocument>(upsertDetails.TableName);

				//Read rows into Documents, send updates to Dynamo
				while (true)
				{
					var documents = await ReadDocuments(sr, upsertDetails.FileMetadata.Fields, upsertDetails.PrimaryKeyFieldName, delimiter, 1000);

					var options = new BulkWriteOptions();
					options.IsOrdered = false;

					if (documents != null)
					{
						var requests = new List<WriteModel<BsonDocument>>();

						foreach (var document in documents)
						{
							InsertOneModel<BsonDocument> insertRequest = new InsertOneModel<BsonDocument>(document);
							//updateRequest.IsUpsert = true;
							requests.Add(insertRequest);
						}

						var result = await collection.BulkWriteAsync(requests, options);

						//Check result
						records += result.InsertedCount;
					}
					else
					{
						break;
					}

					logger.LogInformation("Records added : " + records);
				}

			  return new UpsertResults() { NumberOfRecordsUpserted = records, NumberOfRecordsSkipped = 0 };
			}
    }
    #endregion

    #region private methods
    private void UpdateColumnOffsets(FileMetadata fileMetadata, string[] header)
    {
      if (header == null || !fileMetadata.MatchOnHeader)
        return;

      foreach (FieldMetadata field in fileMetadata.Fields)
        field.Offset = Array.IndexOf(header, field.HeaderName ?? field.ColumnName);
    }

    private async Task<List<BsonDocument>> ReadDocuments(StreamReader sr, List<FieldMetadata> fields, string primaryKeyFieldName, char delimiter, int records)
		{
			var documents = new List<BsonDocument>();

			int recordCount = 0;

			while (recordCount < records)
			{
				string line = await sr.ReadLineAsync();
				recordCount++;

				if (line == null)
					break;

				string[] items = line.Split(delimiter);

				if (items.Length == fields.Count)
				{
					var document = new BsonDocument();

					foreach (var field in fields)
					{
						int colOffset = field.Offset;
						string colName = field.ColumnName;

						if (colName == primaryKeyFieldName)
							document["_id"] = items[colOffset];
						else
							document[colName] = items[colOffset];
					}

					documents.Add(document);
				}
			}

			return documents;
		}
				
		private IMongoClient Connect()
		{
			var settings = new MongoClientSettings();
			settings.Server = new MongoServerAddress(connectionSettings.Hostname);
			return new MongoClient(settings);
		}
    #endregion
  }
}