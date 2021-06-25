using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ApiPager.AspNetCore;
using ApiPager.Core;
using ApiPager.Core.Models;
using Apteco.PullMarketing.Data;
using Apteco.PullMarketing.Data.Models;
using Microsoft.AspNetCore.Mvc;
using Apteco.PullMarketing.Api.Models;
using Apteco.PullMarketing.Api.Models.Records;
using Apteco.PullMarketing.Api.Services;
using Microsoft.AspNetCore.Http;
using Record = Apteco.PullMarketing.Api.Models.Records.Record;
using Field = Apteco.PullMarketing.Api.Models.Records.Field;

namespace Apteco.PullMarketing.Api.Controllers
{
  /// <summary>
  /// An endpoint within the API to manipulate records and fields
  /// </summary>
  [Route("api/[controller]")]
  public class RecordsController : Controller
  {
    private IDataService dataService;
    private IRoutingService routingService;

    public RecordsController(IDataService dataService, IRoutingService routingService)
    {
      this.dataService = dataService;
      this.routingService = routingService;
    }

    /// <summary>
    /// Returns the details of all the records
    /// </summary>
    /// <returns>Details for each defined data store</returns>
    /// <param name="dataStoreName">The name of the data store to get the records from</param>
    /// <response code="200">The list of all data stores</response>
    /// <response code="400">A bad request</response>
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(PagedResults<Record>), 200)]
    [ProducesResponseType(typeof(void), 400)]
    [CanFilterPageAndSort(new string[] { "[Key]", "FieldKey" })]
    [HttpGet("{dataStoreName}", Name = "GetRecords")]
    public async Task<IActionResult> GetRecords(string dataStoreName)
    {
      if (string.IsNullOrEmpty(dataStoreName))
        return BadRequest(new ErrorMessages(new ErrorMessage(ErrorMessageCodes.NoDataStoreNameProvided, "No data store name provided")));

      FilterPageAndSortInfo filterPageAndSortInfo = HttpContext.GetFilterPageAndSortInfo();
      IEnumerable<Data.Models.Record> dataRecords = await dataService.GetRecords(dataStoreName, filterPageAndSortInfo);
      if (dataRecords == null)
        return BadRequest(new ErrorMessage(ErrorMessageCodes.InvalidFilterValuesSpecified, "No data returned from the data store"));

      PagedResults<Record> pagedResults = new PagedResults<Record>()
      {
        List = dataRecords.Select(r => new Record()
          {
            DataStoreName = dataStoreName,
            Key = r.Key,
            Fields = r.Fields.Select(f => new Field() { Key = f.Key, Value = f.Value }).ToList()
          }).ToList()
      };
      pagedResults.SetFilterPageAndSortInfo(filterPageAndSortInfo);

      return new OkObjectResult(pagedResults);
    }

    /// <summary>
    /// Bulk upserts a set of record defined in a file provided as part of the POST data
    /// </summary>
    /// <returns>Result details for the bulk upsert</returns>
    /// <param name="dataStoreName">The name of the data store to bulk insert the records into</param>
    /// <param name="bulkUpsertRecordsDetailsWithFile">The details of the bulk upsert including the file and metadata</param>
    /// <response code="200">Result details for the bulk upsert</response>
    /// <response code="400">A bad request</response>
    [Consumes("multipart/form-data")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(BulkUpsertRecordResults), 200)]
    [ProducesResponseType(typeof(ErrorMessages), 400)]
    [HttpPost("{dataStoreName}/BulkUpsertRecords", Name = "BulkUpsertRecords")]
    public async Task<IActionResult> BulkUpsertRecords(string dataStoreName, [FromForm] BulkUpsertRecordsDetailsWithFile bulkUpsertRecordsDetailsWithFile)
    {
      if (string.IsNullOrEmpty(dataStoreName))
        return BadRequest(new ErrorMessages(new ErrorMessage(ErrorMessageCodes.NoDataStoreNameProvided, "No data store name provided")));

      if (bulkUpsertRecordsDetailsWithFile == null || bulkUpsertRecordsDetailsWithFile.File == null)
        return BadRequest(new ErrorMessages(new ErrorMessage(ErrorMessageCodes.NoUpsertDetailsSpecified, "No bulk upsert details provided")));

      if (!ModelState.IsValid)
        return BadRequest(new ErrorMessages(new ErrorMessage(ErrorMessageCodes.GeneralInvalidParameters, "Invalid parameters provided")));

      UpsertDetails upsertDetails = CreateUpsertDetails(dataStoreName, CreateBulkUpsertRecordsDetails(bulkUpsertRecordsDetailsWithFile), out List<ErrorMessage> errors);
      if (errors.Count > 0)
        return BadRequest(new ErrorMessages(errors));

      UpsertResults upsertResults;
      using (Stream stream = bulkUpsertRecordsDetailsWithFile.File.OpenReadStream())
      {
        upsertResults = await dataService.Upsert(stream, upsertDetails);
      }

      BulkUpsertRecordResults results = new BulkUpsertRecordResults()
      {
        DataStoreName = dataStoreName,
        NumberOfRecordsUpserted = upsertResults.NumberOfRecordsUpserted,
        NumberOfRecordsSkipped = upsertResults.NumberOfRecordsSkipped
      };

      return new OkObjectResult(results);
    }

    /// <summary>
    /// Bulk upserts a set of records defined in a file on a file system accessible by the API
    /// </summary>
    /// <returns>Result details for the bulk upsert</returns>
    /// <param name="dataStoreName">The name of the data store to bulk insert the records into</param>
    /// <param name="bulkUpsertRecordsFromFilePathDetails">The details of the bulk upsert</param>
    /// <response code="200">Result details for the bulk upsert</response>
    /// <response code="400">A bad request</response>
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(BulkUpsertRecordResults), 200)]
    [ProducesResponseType(typeof(ErrorMessages), 400)]
    [HttpPost("{dataStoreName}/BulkUpsertRecordsFromFilePath", Name = "BulkUpsertRecordsFromFilePath")]
    public async Task<IActionResult> BulkUpsertRecordsFromFilePath(string dataStoreName, [FromBody]BulkUpsertRecordsFromFilePathDetails bulkUpsertRecordsFromFilePathDetails)
    {
      if (string.IsNullOrEmpty(dataStoreName))
        return BadRequest(new ErrorMessages(new ErrorMessage(ErrorMessageCodes.NoDataStoreNameProvided, "No data store name provided")));

      if (bulkUpsertRecordsFromFilePathDetails == null)
        return BadRequest(new ErrorMessages(new ErrorMessage(ErrorMessageCodes.NoUpsertDetailsSpecified, "No bulk upsert details provided")));

      if (!ModelState.IsValid)
        return BadRequest(new ErrorMessages(new ErrorMessage(ErrorMessageCodes.GeneralInvalidParameters, "Invalid parameters provided")));

      UpsertDetails upsertDetails = CreateUpsertDetails(dataStoreName, bulkUpsertRecordsFromFilePathDetails, out List<ErrorMessage> errors);
      if (errors.Count > 0)
        return BadRequest(new ErrorMessages(errors));

      UpsertResults upsertResults;
      using (FileStream fileStream = new FileStream(bulkUpsertRecordsFromFilePathDetails.FilePath, FileMode.Open, FileAccess.Read))
      {
        upsertResults = await dataService.Upsert(fileStream, upsertDetails);
      }

      BulkUpsertRecordResults results = new BulkUpsertRecordResults()
      {
        DataStoreName = dataStoreName,
        NumberOfRecordsUpserted = upsertResults.NumberOfRecordsUpserted,
        NumberOfRecordsSkipped = upsertResults.NumberOfRecordsSkipped
      };

      return new OkObjectResult(results);
    }

    /// <summary>
    /// Returns record detail for the specified key
    /// </summary>
    /// <returns>The details for the specified record</returns>
    /// <param name="dataStoreName">The name of the data store to get the record from</param>
    /// <param name="key">The key of the record to view</param>
    /// <response code="200">The record details</response>
    /// <response code="400">A bad request</response>
    /// <response code="404">The record couldn't be found</response>
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(Record), 200)]
    [ProducesResponseType(typeof(void), 400)]
    [ProducesResponseType(typeof(void), 404)]
    [HttpGet("{dataStoreName}/{key}", Name = "GetRecord")]
    public async Task<IActionResult> GetRecord(string dataStoreName, string key)
    {
      if (string.IsNullOrEmpty(dataStoreName))
        return BadRequest(new ErrorMessages(new ErrorMessage(ErrorMessageCodes.NoDataStoreNameProvided, "No data store name provided")));

      if (string.IsNullOrEmpty(key))
        return BadRequest(new ErrorMessages(new ErrorMessage(ErrorMessageCodes.NoPrimaryKeySpecified, "No key value provided")));

      if (!ModelState.IsValid)
        return BadRequest(new ErrorMessages(new ErrorMessage(ErrorMessageCodes.GeneralInvalidParameters, "Invalid parameters provided")));

      Data.Models.Record dataRecord = await dataService.GetRecord(dataStoreName, key);
      if (dataRecord == null)
        return NotFound();

      Record record = new Record()
      {
        DataStoreName = dataStoreName,
        Key = dataRecord.Key,
        Fields = dataRecord.Fields.Select(f => new Field() {Key = f.Key, Value = f.Value}).ToList()
      };

      return new OkObjectResult(record);
    }

    /// <summary>
    /// Upserts a record with the specified key
    /// </summary>
    /// <returns>The details of the upserted record</returns>
    /// <param name="dataStoreName">The name of the data store to upsert the record into</param>
    /// <param name="key">The key of the record to upsert</param>
    /// <param name="record">The details of the record to upsert</param>
    /// <response code="201">The upserted record details</response>
    /// <response code="400">A bad request</response>
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(Record), 201)]
    [ProducesResponseType(typeof(void), 400)]
    [HttpPut("{dataStoreName}/{key}", Name = "UpsertRecord")]
    public async Task<IActionResult> UpsertRecord(string dataStoreName, string key, [FromBody]UpsertRecordDetails record)
    {
      if (string.IsNullOrEmpty(dataStoreName))
        return BadRequest(new ErrorMessages(new ErrorMessage(ErrorMessageCodes.NoDataStoreNameProvided, "No data store name provided")));

      if (string.IsNullOrEmpty(key))
        return BadRequest(new ErrorMessages(new ErrorMessage(ErrorMessageCodes.NoPrimaryKeySpecified, "No key value provided")));

      if (record == null)
        return BadRequest(new ErrorMessages(new ErrorMessage(ErrorMessageCodes.NoUpsertRecordDetailsSpecified, "No record details provided")));

      if (!ModelState.IsValid)
        return BadRequest(new ErrorMessages(new ErrorMessage(ErrorMessageCodes.GeneralInvalidParameters, "Invalid parameters provided")));

      Data.Models.Record dataRecord = new Data.Models.Record()
      {
        Key = key,
        Fields = record.Fields.Select(f => new Data.Models.Field() {Key = f.Key, Value = f.Value}).ToList()
      };

      await dataService.UpsertRecord(dataStoreName, dataRecord);

      Uri absoluteSelfUri = routingService.GetAbsoluteRouteUrl(this, "GetRecord", new { dataStoreName, key });
      return new CreatedResult(absoluteSelfUri, new Record()
      {
        DataStoreName = dataStoreName,
        Key = key,
        Fields = record.Fields
      });
    }

    /// <summary>
    /// Deletes the specified record
    /// </summary>
    /// <returns>No content</returns>
    /// <param name="dataStoreName">The name of the data store to delete the record from</param>
    /// <param name="key">The key of the record to delete</param>
    /// <response code="204">The record was deleted successfully</response>
    /// <response code="400">A bad request</response>
    /// <response code="404">The record couldn't be found</response>
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(void), 204)]
    [ProducesResponseType(typeof(void), 400)]
    [ProducesResponseType(typeof(void), 404)]
    [HttpDelete("{dataStoreName}/{key}", Name = "DeleteRecord")]
    public async Task<IActionResult> DeleteRecord(string dataStoreName, string key)
    {
      if (string.IsNullOrEmpty(dataStoreName))
        return BadRequest(new ErrorMessages(new ErrorMessage(ErrorMessageCodes.NoDataStoreNameProvided, "No data store name provided")));

      if (string.IsNullOrEmpty(key))
        return BadRequest(new ErrorMessages(new ErrorMessage(ErrorMessageCodes.NoPrimaryKeySpecified, "No key value provided")));

      if (!ModelState.IsValid)
        return BadRequest(new ErrorMessages(new ErrorMessage(ErrorMessageCodes.GeneralInvalidParameters, "Invalid parameters provided")));

      bool success = await dataService.DeleteRecord(dataStoreName, key);
      if (!success)
        return NotFound();

      return NoContent();
    }

    /// <summary>
    /// Returns a particular field for the specified record
    /// </summary>
    /// <returns>The details of the field for the specified record</returns>
    /// <param name="dataStoreName">The name of the data store to get the record from</param>
    /// <param name="key">The key of the record to view the field for</param>
    /// <param name="fieldName">The name of the field to view for the specified record</param>
    /// <response code="200">The field details</response>
    /// <response code="400">A bad request</response>
    /// <response code="404">The record or the field name couldn't be found</response>
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(Field), 200)]
    [ProducesResponseType(typeof(void), 400)]
    [ProducesResponseType(typeof(void), 404)]
    [HttpGet("{dataStoreName}/{key}/{fieldName}", Name = "GetRecordField")]
    public async Task<IActionResult> GetRecordField(string dataStoreName, string key, string fieldName)
    {
      if (string.IsNullOrEmpty(dataStoreName))
        return BadRequest(new ErrorMessages(new ErrorMessage(ErrorMessageCodes.NoDataStoreNameProvided, "No data store name provided")));

      if (string.IsNullOrEmpty(key))
        return BadRequest(new ErrorMessages(new ErrorMessage(ErrorMessageCodes.NoPrimaryKeySpecified, "No key value provided")));

      if (string.IsNullOrEmpty(fieldName))
        return BadRequest(new ErrorMessages(new ErrorMessage(ErrorMessageCodes.NoFieldNameSpecified, "No field name provided")));

      if (!ModelState.IsValid)
        return BadRequest(new ErrorMessages(new ErrorMessage(ErrorMessageCodes.GeneralInvalidParameters, "Invalid parameters provided")));

      Data.Models.Field dataField = await dataService.GetRecordField(dataStoreName, key, fieldName);
      if (dataField == null)
        return NotFound();

      Field field = new Field()
      {
        Key = dataField.Key,
        Value = dataField.Value
      };

      return new OkObjectResult(field);
    }

    /// <summary>
    /// Upserts a particular field for the specified record
    /// </summary>
    /// <returns>The details of the upserted field</returns>
    /// <param name="dataStoreName">The name of the data store to containing the record to upsert into</param>
    /// <param name="key">The key of the record to upsert into</param>
    /// <param name="fieldName">The name of the field to upsert into the specified record</param>
    /// <param name="value">The value of the field to upsert into the specified record</param>
    /// <response code="201">The upserted field details</response>
    /// <response code="400">A bad request</response>
    /// <response code="404">The record couldn't be found</response>
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(Field), 201)]
    [ProducesResponseType(typeof(void), 400)]
    [ProducesResponseType(typeof(void), 404)]
    [HttpPut("{dataStoreName}/{key}/{fieldName}", Name = "UpsertRecordField")]
    public async Task<IActionResult> UpsertRecordField(string dataStoreName, string key, string fieldName, [FromBody]string value)
    {
      if (string.IsNullOrEmpty(dataStoreName))
        return BadRequest(new ErrorMessages(new ErrorMessage(ErrorMessageCodes.NoDataStoreNameProvided, "No data store name provided")));

      if (string.IsNullOrEmpty(key))
        return BadRequest(new ErrorMessages(new ErrorMessage(ErrorMessageCodes.NoPrimaryKeySpecified, "No key value provided")));

      if (string.IsNullOrEmpty(fieldName))
        return BadRequest(new ErrorMessages(new ErrorMessage(ErrorMessageCodes.NoFieldNameSpecified, "No field name provided")));

      if (value == null)
        return BadRequest(new ErrorMessages(new ErrorMessage(ErrorMessageCodes.NoFieldValueSpecified, "No field value provided")));
      
      if (!ModelState.IsValid)
        return BadRequest(new ErrorMessages(new ErrorMessage(ErrorMessageCodes.GeneralInvalidParameters, "Invalid parameters provided")));

      Data.Models.Field dataField = new Data.Models.Field()
      {
        Key = fieldName,
        Value = value
      };

      await dataService.UpsertRecordField(dataStoreName, key, dataField);

      Uri absoluteSelfUri = routingService.GetAbsoluteRouteUrl(this, "GetRecordField", new { dataStoreName, key, fieldName });
      return new CreatedResult(absoluteSelfUri, new Field()
      {
        Key = fieldName,
        Value = value
      });
    }

    /// <summary>
    /// Deletes the field for the specified record
    /// </summary>
    /// <returns>No content</returns>
    /// <param name="dataStoreName">The name of the data store to containing the record to delete the field from</param>
    /// <param name="key">The key of the record to delete the field from</param>
    /// <param name="fieldName">The name of the field to delete from the specified record</param>
    /// <response code="204">The field was deleted from the specified record successfully</response>
    /// <response code="400">A bad request</response>
    /// <response code="404">The record or the field name couldn't be found</response>
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(void), 204)]
    [ProducesResponseType(typeof(void), 400)]
    [ProducesResponseType(typeof(void), 404)]
    [HttpDelete("{dataStoreName}/{key}/{fieldName}", Name = "DeleteRecordField")]
    public async Task<IActionResult> DeleteRecordField(string dataStoreName, string key, string fieldName)
    {
      if (string.IsNullOrEmpty(dataStoreName))
        return BadRequest(new ErrorMessages(new ErrorMessage(ErrorMessageCodes.NoDataStoreNameProvided, "No data store name provided")));

      if (string.IsNullOrEmpty(key))
        return BadRequest(new ErrorMessages(new ErrorMessage(ErrorMessageCodes.NoPrimaryKeySpecified, "No key value provided")));

      if (string.IsNullOrEmpty(fieldName))
        return BadRequest(new ErrorMessages(new ErrorMessage(ErrorMessageCodes.NoFieldNameSpecified, "No field name provided")));

      if (!ModelState.IsValid)
        return BadRequest(new ErrorMessages(new ErrorMessage(ErrorMessageCodes.GeneralInvalidParameters, "Invalid parameters provided")));

      bool success = await dataService.DeleteRecordField(dataStoreName, key, fieldName);
      if (!success)
        return NotFound();

      return NoContent();
    }

    #region private methods
    private int ParseAsCharacter(string s)
    {
      if (string.IsNullOrWhiteSpace(s))
        return 0;

      switch (s.ToUpperInvariant().Trim())
      {
        case "NULL":
        case "NUL":
        case "NONE":
          return '\0';
        case "TAB":
          return '\t';
        case "SPACE":
          return ' ';
        case "COMMA":
          return ',';
        case "PIPE":
          return '|';
        default:
          return s[0];
      }
    }

    private BulkUpsertRecordsDetails CreateBulkUpsertRecordsDetails(BulkUpsertRecordsDetailsWithFile bulkUpsertRecordsDetailsWithFile)
    {
      BulkUpsertRecordsDetails bulkUpsertRecordsDetails = new BulkUpsertRecordsDetails();
      bulkUpsertRecordsDetails.Delimiter = bulkUpsertRecordsDetailsWithFile.Delimiter;
      bulkUpsertRecordsDetails.Encloser = bulkUpsertRecordsDetailsWithFile.Encloser;
      bulkUpsertRecordsDetails.Encoding = bulkUpsertRecordsDetailsWithFile.Encoding;
      bulkUpsertRecordsDetails.FieldMappings = new List<FieldMapping>();
      for (int index = 0; index < bulkUpsertRecordsDetailsWithFile.SourceFieldNames.Count; index++)
      {
        FieldMapping fieldMapping = new FieldMapping();
        fieldMapping.SourceFileFieldName = bulkUpsertRecordsDetailsWithFile.SourceFieldNames[index];
        fieldMapping.DestinationRecordFieldName = bulkUpsertRecordsDetailsWithFile.DestinationFieldNames[index];
        fieldMapping.IsPrimaryKeyField = fieldMapping.SourceFileFieldName == bulkUpsertRecordsDetailsWithFile.PrimaryKeyFieldName;
        bulkUpsertRecordsDetails.FieldMappings.Add(fieldMapping);
      }
      return bulkUpsertRecordsDetails;
    }

    private UpsertDetails CreateUpsertDetails(string dataStoreName, BulkUpsertRecordsDetails bulkUpsertRecordsDetails, out List<ErrorMessage> errors)
    {
      errors = new List<ErrorMessage>();

      UpsertDetails upsertDetails = new UpsertDetails();
      upsertDetails.TableName = dataStoreName;
      upsertDetails.FileMetadata = new FileMetadata();
      upsertDetails.FileMetadata.Delimiter = ParseAsCharacter(bulkUpsertRecordsDetails.Delimiter);
      upsertDetails.FileMetadata.Encloser = ParseAsCharacter(bulkUpsertRecordsDetails.Encloser);
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
            errors.Add(new ErrorMessage(ErrorMessageCodes.MultiplePrimaryKeysSpecified, "More than one primary key has been specified"));

          upsertDetails.PrimaryKeyFieldName = fieldMapping.DestinationRecordFieldName;
        }
      }

      if (upsertDetails.PrimaryKeyFieldName == null)
        errors.Add(new ErrorMessage(ErrorMessageCodes.MultiplePrimaryKeysSpecified, "No primary key has been specified"));

      if (errors.Count > 0)
        return null;

      return upsertDetails;
    }
    #endregion
  }
}
