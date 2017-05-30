using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Apteco.PullMarketing.Console.Models;
using Apteco.PullMarketing.Data;
using Apteco.PullMarketing.Data.Dynamo;
using Apteco.PullMarketing.Data.Models;
using Apteco.PullMarketing.Data.Mongo;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace Apteco.PullMarketing.Console
{
  public class Uploader
  {
    #region private fields
    private LoggerFactory loggerFactory;
    #endregion

    #region public constructor
    public Uploader()
    {
      loggerFactory = new LoggerFactory();
      loggerFactory.AddProvider(new ConsoleLoggerProvider(new ConsoleLoggerSettings()));
    }
    #endregion

    #region public methods
    public string Upload(string filename, UploadSpecification spec)
    {
      IDataService dataService = GetDataService(spec);
      if (dataService == null)
        return "No data store connection settings provided";

      string errors;
      UpsertDetails upsertDetails = CreateUpsertDetails(spec.DataStore, spec.BulkUpsertRecordsDetails, out errors);
      if (upsertDetails == null)
        return errors;

      using (FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read))
      {
        Task<UpsertResults> results = dataService.Upsert(fs, upsertDetails);
        results.Wait();
        return results.Result.NumberOfRecordsUpserted.ToString("N0") + " records upserted, with "+ results.Result.NumberOfRecordsSkipped.ToString("N0") +" skipped";
      }
    }
    #endregion

    #region private methods
    private IDataService GetDataService(UploadSpecification spec)
    {
      if (spec.DynamoConnectionSettings != null)
      {
        return new DynamoService(spec.DynamoConnectionSettings, loggerFactory.CreateLogger<DynamoService>());
      }
      else if (spec.MongoConnectionSettings != null)
      {
        return new MongoService(spec.MongoConnectionSettings, loggerFactory.CreateLogger<MongoService>());
      }
      else
      {
        return null;
      }
    }

    private UpsertDetails CreateUpsertDetails(string dataStoreName, BulkUpsertRecordsDetails bulkUpsertRecordsDetails, out string errors)
    {
      List<string> errorsList = new List<string>();

      UpsertDetails upsertDetails = new UpsertDetails();
      upsertDetails.TableName = dataStoreName;
      upsertDetails.FileMetadata = new FileMetadata();
      upsertDetails.FileMetadata.Delimiter = string.IsNullOrEmpty(bulkUpsertRecordsDetails.Delimiter) ? 0 : bulkUpsertRecordsDetails.Delimiter[0];
      upsertDetails.FileMetadata.Encloser = string.IsNullOrEmpty(bulkUpsertRecordsDetails.Encloser) ? 0 : bulkUpsertRecordsDetails.Encloser[0];
      upsertDetails.FileMetadata.Encoding = bulkUpsertRecordsDetails.Encoding.ToString();
      upsertDetails.FileMetadata.Header = true;
      upsertDetails.FileMetadata.MatchOnHeader = true;
      upsertDetails.FileMetadata.Fields = new List<FieldMetadata>();
      foreach (FieldMapping fieldMapping in bulkUpsertRecordsDetails.FieldMappings)
      {
        upsertDetails.FileMetadata.Fields.Add(new FieldMetadata()
        {
          ColumnName = fieldMapping.DestinationRecordFieldName,
          HeaderName = fieldMapping.SourceFileFieldName
        });

        if (fieldMapping.IsPrimaryKeyField)
        {
          if (upsertDetails.PrimaryKeyFieldName != null)
            errorsList.Add("More than one primary key has been specified");

          upsertDetails.PrimaryKeyFieldName = fieldMapping.DestinationRecordFieldName;
        }
      }

      if (upsertDetails.PrimaryKeyFieldName == null)
        errorsList.Add("No primary key has been specified");

      if (errorsList.Count > 0)
      {
        errors = string.Join(Environment.NewLine, errorsList);
        return null;
      }

      errors = null;
      return upsertDetails;
    }
    #endregion
  }
}
