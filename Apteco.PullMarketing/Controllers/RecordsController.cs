﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ApiPager.Core;
using ApiPager.Core.Models;
using Apteco.PullMarketing.Data;
using Microsoft.AspNetCore.Mvc;
using Apteco.PullMarketing.ModelBinding;
using Apteco.PullMarketing.Models;
using Apteco.PullMarketing.Models.Records;
using Apteco.PullMarketing.Services;

namespace Apteco.PullMarketing.Controllers
{
  /// <summary>
  /// An endpoint within the API to manipulate records and fields
  /// </summary>
  [Route("api/[controller]")]
  public class RecordsController : Controller
  {
    private IDataService dataService;

    public RecordsController(IDataService dataService)
    {
      this.dataService = dataService;
    }

    /// <summary>
    /// Returns the details of all the records
    /// </summary>
    /// <returns>Details for each defined data store</returns>
    /// <param name="dataStoreName">The name of the data store to get the records from</param>
    /// <response code="200">The list of all data stores</response>
    /// <response code="400">A bad request</response>
    [HttpGet("{dataStoreName}", Name = "GetRecords")]
    [ProducesResponseType(typeof(PagedResults<Record>), 200)]
    [ProducesResponseType(typeof(void), 400)]
    [CanFilterPageAndSort(new string[] { "Key" })]
    public async Task<IActionResult> GetRecords(string dataStoreName)
    {
      return new OkObjectResult(null);
    }

    /// <summary>
    /// Bulk upserts a set of record definied in a file provided as part of the POST data
    /// </summary>
    /// <returns>Result details for the bulk upsert</returns>
    /// <param name="dataStoreName">The name of the data store to bulk insert the records into</param>
    /// <param name="bulkUpsertRecordsDetailsWithFile">The details of the bulk upsert including the file and metadata</param>
    /// <response code="200">Result details for the bulk upsert</response>
    /// <response code="400">A bad request</response>
    [ProducesResponseType(typeof(BulkUpsertRecordResults), 200)]
    [ProducesResponseType(typeof(ErrorMessages), 400)]
    [HttpPost("{dataStoreName}/BulkUpsertRecords", Name = "BulkUpsertRecords")]
    [MultiPartFormDataWithFile("file", "The file to bulk upsert")]
    public async Task<IActionResult> BulkUpsertRecords(string dataStoreName, BulkUpsertRecordsDetailsWithFile bulkUpsertRecordsDetailsWithFile)
    {
      if (bulkUpsertRecordsDetailsWithFile == null || bulkUpsertRecordsDetailsWithFile.File == null)
        return BadRequest(new ErrorMessages(new ErrorMessage(ErrorMessageCodes.NoUpsertDetailsSpecified, "No bulk upsert details provided")));

      List<ErrorMessage> errors;
      UpsertDetails upsertDetails = CreateUpsertDetails(dataStoreName, bulkUpsertRecordsDetailsWithFile.BulkUpsertRecordsDetails, out errors);
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
    /// Bulk upserts a set of record definied in a file on a file system accessable by the API
    /// </summary>
    /// <returns>Result details for the bulk upsert</returns>
    /// <param name="dataStoreName">The name of the data store to bulk insert the records into</param>
    /// <param name="bulkUpsertRecordsFromFilePathDetails">The details of the bulk upsert</param>
    /// <response code="200">Result details for the bulk upsert</response>
    /// <response code="400">A bad request</response>
    [ProducesResponseType(typeof(BulkUpsertRecordResults), 200)]
    [ProducesResponseType(typeof(ErrorMessages), 400)]
    [HttpPost("{dataStoreName}/BulkUpsertRecordsFromFilePath", Name = "BulkUpsertRecordsFromFilePath")]
    public async Task<IActionResult> BulkUpsertRecordsFromFilePath(string dataStoreName, [FromBody]BulkUpsertRecordsFromFilePathDetails bulkUpsertRecordsFromFilePathDetails)
    {
      if (bulkUpsertRecordsFromFilePathDetails == null)
        return BadRequest(new ErrorMessages(new ErrorMessage(ErrorMessageCodes.NoUpsertDetailsSpecified, "No bulk upsert details provided")));

      List<ErrorMessage> errors;
      UpsertDetails upsertDetails = CreateUpsertDetails(dataStoreName, bulkUpsertRecordsFromFilePathDetails, out errors);
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
    [HttpGet("{dataStoreName}/{key}", Name = "GetRecord")]
    [ProducesResponseType(typeof(Record), 200)]
    [ProducesResponseType(typeof(void), 400)]
    [ProducesResponseType(typeof(void), 404)]
    public async Task<IActionResult> GetRecord(string dataStoreName, string key)
    {
      return new OkObjectResult(null);
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
    [HttpPut("{dataStoreName}/{key}", Name = "UpsertRecord")]
    [ProducesResponseType(typeof(Field), 201)]
    [ProducesResponseType(typeof(void), 400)]
    public async Task<IActionResult> UpsertRecord(string dataStoreName, string key, [FromBody]UpsertRecordDetails record)
    {
      return new CreatedResult((Uri)null, null);
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
    [HttpDelete("{dataStoreName}/{key}", Name="DeleteRecord")]
    [ProducesResponseType(typeof(void), 204)]
    [ProducesResponseType(typeof(void), 400)]
    [ProducesResponseType(typeof(void), 404)]
    public async Task<IActionResult> DeleteRecord(string dataStoreName, string key)
    {
      return new OkObjectResult(null);
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
    [HttpGet("{dataStoreName}/{key}/{fieldName}", Name = "GetRecordField")]
    [ProducesResponseType(typeof(Field), 200)]
    [ProducesResponseType(typeof(void), 400)]
    [ProducesResponseType(typeof(void), 404)]
    public async Task<IActionResult> GetRecordField(string dataStoreName, string key, string fieldName)
    {
      return new OkObjectResult(null);
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
    [HttpPut("{dataStoreName}/{key}/{fieldName}", Name = "UpsertRecordField")]
    [ProducesResponseType(typeof(Field), 201)]
    [ProducesResponseType(typeof(void), 400)]
    [ProducesResponseType(typeof(void), 404)]
    public async Task<IActionResult> UpsertRecordField(string dataStoreName, string key, string fieldName, [FromBody]string value)
    {
      return new CreatedResult((Uri)null, null);
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
    [HttpDelete("{dataStoreName}/{key}/{fieldName}", Name = "DeleteRecordField")]
    [ProducesResponseType(typeof(void), 204)]
    [ProducesResponseType(typeof(void), 400)]
    [ProducesResponseType(typeof(void), 404)]
    public async Task<IActionResult> DeleteRecordField(string dataStoreName, string key, string fieldName)
    {
      return NoContent();
    }

    private UpsertDetails CreateUpsertDetails(string dataStoreName, BulkUpsertRecordsDetails bulkUpsertRecordsDetails, out List<ErrorMessage> errors)
    {
      errors = new List<ErrorMessage>();

      UpsertDetails upsertDetails = new UpsertDetails();
      upsertDetails.TableName = dataStoreName;
      upsertDetails.FileMetadata = new FileMetadata();
      upsertDetails.FileMetadata.Delimiter = string.IsNullOrEmpty(bulkUpsertRecordsDetails.Delimiter)? 0 : bulkUpsertRecordsDetails.Delimiter[0];
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
  }
}
