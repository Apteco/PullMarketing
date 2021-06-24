using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using ApiPager.Core;
using ApiPager.Core.FilterExpression;
using ApiPager.Data.Linq;
using ApiPager.Data.SqlServer;
using Apteco.PullMarketing.Data.Models;
using Apteco.PullMarketing.Data.MSSQL.DataTier;
using Microsoft.Extensions.Logging;
using Field = Apteco.PullMarketing.Data.Models.Field;

namespace Apteco.PullMarketing.Data.MSSQL
{
	public class MSSQLService : IDataService
  {
    #region private fields
    private IMSSQLConnectionSettings connectionSettings;
		private ILogger<MSSQLService> logger;
    private IDbAccess dbAccess;
    #endregion

    #region public constructor
    public MSSQLService(IMSSQLConnectionSettings connectionSettings, ILogger<MSSQLService> logger)
		{
			this.connectionSettings = connectionSettings;
			this.logger = logger;
			this.dbAccess = new DbAccess(connectionSettings.ConnectionString, "30", "30", new Logger<DbAccess>(new LoggerFactory()));
		}
		#endregion

		#region public methods
		public async Task<bool> DeleteDataStore(string tableName)
    {
			using IDbConnection connection = dbAccess.Connect();
			try
			{
				logger.LogInformation($"Deleting table '{tableName}'");
        using IDbCommand cmd = dbAccess.CreateCommand(connection);
        cmd.CommandText =
          $@"DROP TABLE {tableName}";

        await dbAccess.ExecuteNonQuery(cmd);
        logger.LogInformation($"Table '{tableName}' was deleted");
        return true;
      }
			catch (Exception e)
			{
        logger.LogInformation($"Exception whilst deleting table '{tableName}': {e}");
			}

			logger.LogInformation($"Table '{tableName}' was not deleted");
      return false;
		}

		public async Task<bool> CreateDataStore(DataStore dataStore)
    {
      using IDbConnection connection = dbAccess.Connect();
      try
      {
        logger.LogInformation($"Creating table '{dataStore.Name}'");
        using IDbCommand cmd = dbAccess.CreateCommand(connection);
        cmd.CommandText =
          $@"CREATE TABLE [{dataStore.Name}] (" + Environment.NewLine +
          $@"  [{dataStore.PrimaryKeyFieldName}] [varchar](50) NOT NULL," + Environment.NewLine +
          $@"  [Key] [varchar](50) NOT NULL," + Environment.NewLine +
          $@"  [Value] [nvarchar](max) NULL," + Environment.NewLine +
          $@"CONSTRAINT [PK_{dataStore.Name}] PRIMARY KEY CLUSTERED ([{dataStore.PrimaryKeyFieldName}], [Key]))";

        await dbAccess.ExecuteNonQuery(cmd);
        logger.LogInformation($"Table '{dataStore.Name}' was created");
        return true;
      }
      catch (Exception e)
      {
        logger.LogInformation($"Exception whilst creating table '{dataStore.Name}': {e}");
      }

      logger.LogInformation($"Table '{dataStore.Name}' was not created");
      return false;
		}

		public async Task<IEnumerable<DataStore>> GetDataStores(FilterPageAndSortInfo filterPageAndSortInfo)
		{
      List<DataStore> dataStores = new List<DataStore>();

			using IDbConnection connection = dbAccess.Connect();
      try
      {
        logger.LogInformation($"Listing tables");
        using IDbCommand cmd = dbAccess.CreateCommand(connection);
        cmd.CommandText =
          $@"SELECT PrimaryKeyColumn.TABLE_NAME AS Name, PrimaryKeyColumn.COLUMN_NAME AS PrimaryKeyFieldName" + Environment.NewLine +
          $@"FROM INFORMATION_SCHEMA.COLUMNS AS PrimaryKeyColumn" + Environment.NewLine +
          $@"INNER JOIN INFORMATION_SCHEMA.COLUMNS AS KeyColumn ON KeyColumn.TABLE_NAME = PrimaryKeyColumn.TABLE_NAME AND KeyColumn.COLUMN_NAME = 'Key'" + Environment.NewLine +
          $@"INNER JOIN INFORMATION_SCHEMA.COLUMNS AS ValueColumn ON ValueColumn.TABLE_NAME = PrimaryKeyColumn.TABLE_NAME AND ValueColumn.COLUMN_NAME = 'Value'" + Environment.NewLine +
          $@"WHERE PrimaryKeyColumn.ORDINAL_POSITION = 1";

        using IDataReader rdr = await dbAccess.ExecuteReader(cmd);
        while (await dbAccess.Read(rdr))
          dataStores.Add(new DataStore
          {
            Name = DatabaseUtilities.ConvertToString(rdr["Name"]), 
            PrimaryKeyFieldName = DatabaseUtilities.ConvertToString(rdr["PrimaryKeyFieldName"])
          });
      
        logger.LogInformation($"Found {dataStores.Count:N0} tables");
      }
			catch (Exception e)
      {
        logger.LogInformation($"Exception whilst listing tables: {e}");
      }

  		return dataStores.Filter(filterPageAndSortInfo, new string[] { "Name", "PrimaryKeyFieldName" }, "Name");
		}

		public async Task<DataStore> GetDataStore(string tableName)
		{
      using IDbConnection connection = dbAccess.Connect();
      try
      {
        logger.LogInformation($"Getting table '{tableName}'");
        using IDbCommand cmd = dbAccess.CreateCommand(connection);
        cmd.CommandText =
          $@"SELECT TABLE_NAME AS Name, COLUMN_NAME AS PrimaryKeyFieldName" + Environment.NewLine +
          $@"FROM INFORMATION_SCHEMA.COLUMNS" + Environment.NewLine +
          $@"WHERE ORDINAL_POSITION = 1" + Environment.NewLine +
          $@"AND TABLE_NAME = {dbAccess.AddParameter(cmd, "NAME", tableName)}";

        using IDataReader rdr = await dbAccess.ExecuteReader(cmd);
        if (await dbAccess.Read(rdr))
        {
          logger.LogInformation($"Table '{tableName}' was found");
          return new DataStore
          {
            Name = DatabaseUtilities.ConvertToString(rdr["Name"]),
            PrimaryKeyFieldName = DatabaseUtilities.ConvertToString(rdr["PrimaryKeyFieldName"])
          };
        }
      }
      catch (Exception e)
      {
        logger.LogInformation($"Exception whilst getting table '{tableName}': {e}");
      }

      logger.LogInformation($"Table '{tableName}' was not found");
      return null;
		}

		public async Task<UpsertResults> Upsert(Stream stream, UpsertDetails upsertDetails)
		{
			var dataStore = await GetDataStore(upsertDetails.TableName);
      if (dataStore == null)
        throw new Exception($"No table with name '{upsertDetails.TableName}' was found");

    	logger.LogInformation("Starting updates");
		  UpsertResults results = RunUpdates(stream, upsertDetails.FileMetadata, 4, dataStore);
			logger.LogInformation("Updates complete");

			return results;
		}

		public async Task<IEnumerable<Record>> GetRecords(string tableName, FilterPageAndSortInfo filterPageAndSortInfo)
		{
      var dataStore = await GetDataStore(tableName);
      if (dataStore == null)
        throw new Exception($"No table with name '{tableName}' was found");

      using IDbConnection connection = dbAccess.Connect();
      try
      {
        logger.LogInformation($"Getting records from '{tableName}'");
        using IDbCommand cmd = dbAccess.CreateCommand(connection);
        cmd.CommandText =
          $@"SELECT [{dataStore.PrimaryKeyFieldName}] AS [Key], [Key] AS FieldKey, [Value] AS FieldValue" + Environment.NewLine +
          $@"FROM [{dataStore.Name}]";

        using IDataReader rdr = cmd.ExecuteReader(filterPageAndSortInfo, new string[] {"Key"}, "Key");

        List<Record> records = new List<Record>();

        Record currentRecord = null;
        while (await dbAccess.Read(rdr))
        {
          string key = DatabaseUtilities.ConvertToString(rdr["Key"]);
          if (currentRecord?.Key != key)
          {
            if (currentRecord?.Key != null)
              records.Add(currentRecord);
            currentRecord = new Record
            {
              Key = key,
              Fields = new List<Field>()
            };
          }

          currentRecord?.Fields?.Add(new Field
          {
            Key = DatabaseUtilities.ConvertToString(rdr["FieldKey"]),
            Value = DatabaseUtilities.ConvertToString(rdr["FieldValue"])
          });
        }

        if (currentRecord?.Key != null)
          records.Add(currentRecord);

        logger.LogInformation($"Found '{records.Count:N0}' records");
        return records;
      }
      catch (Exception e)
      {
        logger.LogInformation($"Exception whilst getting records from '{tableName}': {e}");
      }

      return null;
		}

		public async Task<Record> GetRecord(string tableName, string primaryKeyValue)
		{
      var dataStore = await GetDataStore(tableName);
      if (dataStore == null)
        throw new Exception($"No table with name '{tableName}' was found");

      using IDbConnection connection = dbAccess.Connect();
      try
      {
        logger.LogInformation($"Getting record from '{tableName}' with key '{primaryKeyValue}'");
        using IDbCommand cmd = dbAccess.CreateCommand(connection);
        cmd.CommandText =
          $@"SELECT *" + Environment.NewLine +
          $@"FROM [{dataStore.Name}]" + Environment.NewLine +
          $@"WHERE [{dataStore.PrimaryKeyFieldName}] = {dbAccess.AddParameter(cmd, "PRIMARYKEY", primaryKeyValue)}";
        using IDataReader rdr = await dbAccess.ExecuteReader(cmd);

        Record record = new Record
        {
          Key = primaryKeyValue, 
          Fields = new List<Field>()
        };
        while (await dbAccess.Read(rdr))
        {
          record.Fields.Add(new Field
          {
            Key = DatabaseUtilities.ConvertToString(rdr["Key"]),
            Value = DatabaseUtilities.ConvertToString(rdr["Value"])
          });
        }

        logger.LogInformation($"Found record from '{tableName}' with key '{primaryKeyValue}'");
        return record;
      }
      catch (Exception e)
      {
        logger.LogInformation($"Exception whilst getting record from '{tableName}' with key '{primaryKeyValue}': {e}");
      }

      return null;
		}

    public async Task<bool> UpsertRecord(string tableName, Record record)
		{
      if (record?.Key == null)
        throw new Exception($"No key value was specified");

      var dataStore = await GetDataStore(tableName);
      if (dataStore == null)
        throw new Exception($"No table with name '{tableName}' was found");

      using IDbConnection connection = dbAccess.Connect();
      try
      {
        logger.LogInformation($"Upserting record in '{tableName}' with key '{record.Key}'");
        using IDbCommand cmd = dbAccess.CreateCommand(connection);
        cmd.CommandText =
          $@"MERGE [{tableName}] AS Target" + Environment.NewLine +
          $@"USING (VALUES" + Environment.NewLine +
          GenerateMergeValues(record.Key, record.Fields, cmd) + Environment.NewLine +
          $@") AS Source ([{dataStore.PrimaryKeyFieldName}], [Key], [Value])" + Environment.NewLine +
          $@"ON Target.[{dataStore.PrimaryKeyFieldName}] = Source.[{dataStore.PrimaryKeyFieldName}] AND Target.[Key] = Source.[Key]" + Environment.NewLine +
          $@"WHEN MATCHED THEN" + Environment.NewLine +
          $@"UPDATE SET Target.[Value] = Source.[Value]" + Environment.NewLine +
          $@"WHEN NOT MATCHED BY TARGET THEN" + Environment.NewLine +
          $@"INSERT ([{dataStore.PrimaryKeyFieldName}], [Key], [Value])" + Environment.NewLine +
          $@"VALUES (Source.[{dataStore.PrimaryKeyFieldName}], Source.[Key], Source.[Value])";
/*
        if (removeNonMatches)
          cmd.CommandText += Environment.NewLine +
            $@"WHEN NOT MATCHED BY SOURCE AND Target.[{dataStore.PrimaryKeyFieldName}] = {dbAccess.AddParameter(cmd, "PRIMARYKEY", record.Key)} THEN" + Environment.NewLine +
            $@"DELETE";
*/
        cmd.CommandText += ";";

        int result = await dbAccess.ExecuteNonQuery(cmd);
        if (result > 0)
        {
          logger.LogInformation($"Record with key '{record.Key}' was upserted into table '{dataStore.Name}'");
          return true;
        }
      }
      catch (Exception e)
      {
        logger.LogInformation($"Exception whilst upserting records into '{tableName}' with key '{record.Key}': {e}");
      }

      logger.LogInformation($"Record with key '{record.Key}' was not upserted into table '{dataStore.Name}'");
      return false;
		}

    public async Task<bool> DeleteRecord(string tableName, string primaryKeyValue)
		{
      var dataStore = await GetDataStore(tableName);
      if (dataStore == null)
        throw new Exception($"No table with name '{tableName}' was found");

      using IDbConnection connection = dbAccess.Connect();
      try
      {
        logger.LogInformation($"Deleting record from '{tableName}' with key '{primaryKeyValue}'");
        using IDbCommand cmd = dbAccess.CreateCommand(connection);
        cmd.CommandText =
          $@"DELETE" + Environment.NewLine +
          $@"FROM [{dataStore.Name}]" + Environment.NewLine +
          $@"WHERE [{dataStore.PrimaryKeyFieldName}] = {dbAccess.AddParameter(cmd, "KEY", primaryKeyValue)}";
        int result = await dbAccess.ExecuteNonQuery(cmd);
        if (result > 0)
        {
          logger.LogInformation($"Record with key '{primaryKeyValue}' was deleted from table '{dataStore.Name}'");
          return true;
        }
      }
      catch (Exception e)
      {
        logger.LogInformation($"Exception whilst deleting record from '{tableName}' with key '{primaryKeyValue}': {e}");
      }

      logger.LogInformation($"Record with key '{primaryKeyValue}' was not deleted from table '{dataStore.Name}'");
      return false;
		}

		public async Task<Field> GetRecordField(string tableName, string primaryKeyValue, string fieldName)
		{
      var dataStore = await GetDataStore(tableName);
      if (dataStore == null)
        throw new Exception($"No table with name '{tableName}' was found");

      using IDbConnection connection = dbAccess.Connect();
      try
      {
        logger.LogInformation($"Getting field '{fieldName}' from '{tableName}' with key '{primaryKeyValue}'");
        using IDbCommand cmd = dbAccess.CreateCommand(connection);
        cmd.CommandText =
          $@"SELECT [Value]" + Environment.NewLine +
          $@"FROM [{dataStore.Name}]" + Environment.NewLine +
          $@"WHERE [{dataStore.PrimaryKeyFieldName}] = {dbAccess.AddParameter(cmd, "PRIMARYKEY", primaryKeyValue)}" + Environment.NewLine +
          $@"AND [Key] = {dbAccess.AddParameter(cmd, "KEY", fieldName)}";
        string value = DatabaseUtilities.ConvertToString(await dbAccess.ExecuteScalar(cmd));
        if (value != null)
        {
          logger.LogInformation($"Found field '{fieldName}' from '{tableName}' with key '{primaryKeyValue}'");
          return new Field
          {
            Key = fieldName,
            Value = value
          };
        }
      }
      catch (Exception e)
      {
        logger.LogInformation($"Exception whilst getting field '{fieldName}' from '{tableName}' with key '{primaryKeyValue}': {e}");
      }

      logger.LogInformation($"Unable to find field '{fieldName}' from '{tableName}' with key '{primaryKeyValue}'");
      return null;
		}

		public async Task<bool> UpsertRecordField(string tableName, string primaryKeyValue, Field field)
		{
      if (primaryKeyValue == null)
        throw new Exception($"No primary key value was specified");

      if (field?.Key == null)
        throw new Exception($"No field key was specified");

      var dataStore = await GetDataStore(tableName);
      if (dataStore == null)
        throw new Exception($"No table with name '{tableName}' was found");

      using IDbConnection connection = dbAccess.Connect();
      try
      {
        logger.LogInformation($"Upserting field '{field.Key}' in '{tableName}' with key '{primaryKeyValue}'");
        using IDbCommand cmd = dbAccess.CreateCommand(connection);
        cmd.CommandText =
          $@"MERGE [{tableName}] AS Target" + Environment.NewLine +
          $@"USING (VALUES" + Environment.NewLine +
          GenerateMergeValues(primaryKeyValue, new List<Field> { field }, cmd) + Environment.NewLine +
          $@") AS Source ([{dataStore.PrimaryKeyFieldName}], [Key], [Value])" + Environment.NewLine +
          $@"ON Target.[{dataStore.PrimaryKeyFieldName}] = Source.[{dataStore.PrimaryKeyFieldName}] AND Target.[Key] = Source.[Key]" + Environment.NewLine +
          $@"WHEN MATCHED THEN" + Environment.NewLine +
          $@"UPDATE SET Target.[Value] = Source.[Value]" + Environment.NewLine +
          $@"WHEN NOT MATCHED BY TARGET THEN" + Environment.NewLine +
          $@"INSERT ([{dataStore.PrimaryKeyFieldName}], [Key], [Value])" + Environment.NewLine +
          $@"VALUES (Source.[{dataStore.PrimaryKeyFieldName}], Source.[Key], Source.[Value])";
        cmd.CommandText += ";";

        int result = await dbAccess.ExecuteNonQuery(cmd);
        if (result > 0)
        {
          logger.LogInformation($"Field '{field.Key}' for key '{primaryKeyValue}' was upserted into table '{dataStore.Name}'");
          return true;
        }
      }
      catch (Exception e)
      {
        logger.LogInformation($"Exception whilst upserting field '{field.Key}' into '{tableName}' with key '{primaryKeyValue}': {e}");
      }

      logger.LogInformation($"Field '{field.Key}' for key '{primaryKeyValue}' was not upserted into table '{dataStore.Name}'");
      return false;
		}

    public async Task<bool> DeleteRecordField(string tableName, string primaryKeyValue, string fieldName)
		{
      var dataStore = await GetDataStore(tableName);
      if (dataStore == null)
        throw new Exception($"No table with name '{tableName}' was found");

      using IDbConnection connection = dbAccess.Connect();
      try
      {
        logger.LogInformation($"Deleting field '{fieldName}' from '{tableName}' with key '{primaryKeyValue}'");
        using IDbCommand cmd = dbAccess.CreateCommand(connection);
        cmd.CommandText =
          $@"DELETE" + Environment.NewLine +
          $@"FROM [{dataStore.Name}]" + Environment.NewLine +
          $@"WHERE [{dataStore.PrimaryKeyFieldName}] = {dbAccess.AddParameter(cmd, "PRIMARYKEY", primaryKeyValue)}" + Environment.NewLine +
          $@"AND [Key] = {dbAccess.AddParameter(cmd, "KEY", fieldName)}";
        int result = await dbAccess.ExecuteNonQuery(cmd);
        if (result > 0)
        {
          logger.LogInformation($"Field '{fieldName}' with key '{primaryKeyValue}' was deleted from table '{dataStore.Name}'");
          return true;
        }
      }
      catch (Exception e)
      {
        logger.LogInformation($"Exception whilst deleting field '{fieldName}' from '{tableName}' with key '{primaryKeyValue}': {e}");
      }

      logger.LogInformation($"Field '{fieldName}' with key '{primaryKeyValue}' was not deleted from table '{dataStore.Name}'");
      return false;
		}
		#endregion

		#region private methods
    private string GenerateMergeValues(string primaryKeyValue, List<Field> fields, IDbCommand cmd)
    {
      StringBuilder mergeValues = new StringBuilder();
      for(int index=0; index < fields.Count; index++)
      {
        if (index > 0)
          mergeValues.AppendLine();
        mergeValues.Append($@"  (");
        mergeValues.Append(dbAccess.AddParameter(cmd, $@"PRIMARYKEY{index+1}", primaryKeyValue));
        mergeValues.Append(", ");
        mergeValues.Append(dbAccess.AddParameter(cmd, $@"KEY{index+1}", fields[index].Key));
        mergeValues.Append(", ");
        mergeValues.Append(dbAccess.AddParameter(cmd, $@"VALUE{index+1}", fields[index].Value));
        mergeValues.Append(")");
        if (index != fields.Count - 1)
          mergeValues.Append(",");
      }
      return mergeValues.ToString();
    }

		private UpsertResults RunUpdates(Stream stream, FileMetadata fileMetadata, int numberOfTasks, DataStore dataStore)
		{
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
				updateTasks[taskNumber] = Task.Run(async () =>
				{
					taskResults[taskNumber] = await UpdateItems(fileMetadata, dataStore, lines);
				});
			}

			fileReaderTask.Wait();
			Task.WaitAll(updateTasks);

			UpsertResults consolidatedResults = new UpsertResults();
			for (int i = 0; i < updateTasks.Length; i++)
			{
        if (taskResults[i] != null)
        {
          consolidatedResults.NumberOfRecordsUpserted += taskResults[i].NumberOfRecordsUpserted;
          consolidatedResults.NumberOfRecordsSkipped += taskResults[i].NumberOfRecordsSkipped;
        }
      }
			return consolidatedResults;
		}

		private async Task<UpsertResults> UpdateItems(FileMetadata fileMetadata, DataStore dataStore, BlockingCollection<string[]> lines)
		{
			long updates = 0;
			long errors = 0;

      foreach (var line in lines.GetConsumingEnumerable())
      {
        Record record = new Record { Fields = new List<Field>() };
        foreach (var fieldMetadata in fileMetadata.Fields)
        {
          var colName = fieldMetadata.ColumnName;
          var colOffset = fieldMetadata.Offset;
          if (colOffset >= 0 && colOffset < line.Length)
          {
            if (colName == dataStore.PrimaryKeyFieldName)
              record.Key = line[colOffset];
            else
              record.Fields.Add(new Field {Key = colName, Value = line[colOffset]});
          }
        }

        if (record.Key == null)
          errors++;
        else if (await UpsertRecord(dataStore.Name, record))
          updates++;
      }

      return new UpsertResults { NumberOfRecordsUpserted = updates, NumberOfRecordsSkipped = errors };
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
		#endregion
	}
}