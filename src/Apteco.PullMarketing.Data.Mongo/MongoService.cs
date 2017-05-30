using System;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using ApiPager.Core;
using Apteco.PullMarketing.Data.Models;
using Microsoft.Extensions.Logging;

namespace Apteco.PullMarketing.Data.Mongo
{
	public class MongoService : IDataService
  {
    #region private fields
    private IMongoConnectionSettings connectionSettings;
		private ILogger<MongoService> logger;
    #endregion

    #region public constructor
    public MongoService(IMongoConnectionSettings connectionSettings, ILogger<MongoService> logger)
		{
			this.connectionSettings = connectionSettings;
			this.logger = logger;
		}
    #endregion

    #region public methods
    public Task<IEnumerable<DataStore>> GetDataStores(FilterPageAndSortInfo filterPageAndSortInfo)
    {
      throw new NotImplementedException();
    }

    public Task<DataStore> GetDataStore(string name)
    {
      throw new NotImplementedException();
    }

    public Task<bool> CreateDataStore(DataStore dataStore)
    {
      throw new NotImplementedException();
    }

    public Task<bool> DeleteDataStore(string tableName)
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

			  char encloser = (char)0;
			  if (upsertDetails.FileMetadata.Encloser > 0)
			    encloser = (char)upsertDetails.FileMetadata.Encloser;

        //Skip header if we have one, and update column offsets
			  if (upsertDetails.FileMetadata.Header)
			  {
			    string[] headers = (await sr.ReadLineAsync())?.Split(delimiter);
			    StringUtilities.RemoveEnclosers(headers, encloser);

          UpdateColumnOffsets(upsertDetails.FileMetadata, headers);
        }

        long records = 0;

				var collection = database.GetCollection<BsonDocument>(upsertDetails.TableName);

				//Read rows into Documents, send updates to Dynamo
				while (true)
				{
					var documents = await ReadDocuments(sr, upsertDetails.FileMetadata.Fields, upsertDetails.PrimaryKeyFieldName, delimiter, encloser, 1000);

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

    public Task<IEnumerable<Record>> GetRecords(string tableName, FilterPageAndSortInfo filterPageAndSortInfo)
    {
      throw new NotImplementedException();
    }

    public Task<Record> GetRecord(string tableName, string primaryKeyValue)
    {
      throw new NotImplementedException();
    }

    public Task<bool> UpsertRecord(string tableName, Record record)
    {
      throw new NotImplementedException();
    }

    public Task<bool> DeleteRecord(string tableName, string primaryKeyValue)
    {
      throw new NotImplementedException();
    }

    public Task<Field> GetRecordField(string tableName, string primaryKeyValue, string fieldName)
    {
      throw new NotImplementedException();
    }

    public Task<bool> UpsertRecordField(string tableName, string primaryKeyValue, Field field)
    {
      throw new NotImplementedException();
    }

    public Task<bool> DeleteRecordField(string tableName, string primaryKeyValue, string fieldName)
    {
      throw new NotImplementedException();
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

    private async Task<List<BsonDocument>> ReadDocuments(StreamReader sr, List<FieldMetadata> fields, string primaryKeyFieldName, char delimiter, char encloser, int records)
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
			  StringUtilities.RemoveEnclosers(items, encloser);

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