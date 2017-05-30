using System.Collections.Generic;
using Apteco.PullMarketing.Data.Dynamo;
using Apteco.PullMarketing.Data.Mongo;

namespace Apteco.PullMarketing.Console.Models
{
  public class UploadSpecification
  {
    #region public properties
    public DynamoConnectionSettings DynamoConnectionSettings { get; set; }
    public MongoConnectionSettings MongoConnectionSettings { get; set; }
    public string DataStore { get; set; }
    public BulkUpsertRecordsDetails BulkUpsertRecordsDetails { get; set; }
    #endregion

    #region public methods
    public static UploadSpecification CreateExampleDynamoDbUploadSpecFile()
    {
      UploadSpecification uploadSpec = new UploadSpecification();
      uploadSpec.DynamoConnectionSettings = new DynamoConnectionSettings()
      {
        RegionEndpoint = "eu-west-1",
        AccessKey = "ABCDEFGHIJKLMNOPQRST",
        SecretAccessKey = "sdfjkhASDGSDAKHasdgjklhs/sdagaljk",
        ModifyDataStoreTimeoutInSeconds = 30,
      };
      uploadSpec.DataStore = "People";
      uploadSpec.BulkUpsertRecordsDetails = CreateExampleBulkUpsertRecordDetails();
      return uploadSpec;
    }

    public static UploadSpecification CreateExampleMongoDbUploadSpecFile()
    {
      UploadSpecification uploadSpec = new UploadSpecification();
      uploadSpec.MongoConnectionSettings = new MongoConnectionSettings()
      {
        Hostname = "localhost"
      };
      uploadSpec.DataStore = "People";
      uploadSpec.BulkUpsertRecordsDetails = CreateExampleBulkUpsertRecordDetails();
      return uploadSpec;
    }
    #endregion

    #region private methods
    private static BulkUpsertRecordsDetails CreateExampleBulkUpsertRecordDetails()
    {
      BulkUpsertRecordsDetails bulkUpsertRecordsDetails = new BulkUpsertRecordsDetails();
      bulkUpsertRecordsDetails.Delimiter = ",";
      bulkUpsertRecordsDetails.Encoding = FileEncodings.UTF8;
      bulkUpsertRecordsDetails.FieldMappings = new List<FieldMapping>()
      {
        new FieldMapping() { SourceFileFieldName = "Person URN", DestinationRecordFieldName = "URN", IsPrimaryKeyField = true},
        new FieldMapping() { SourceFileFieldName = "Forename", DestinationRecordFieldName = "FirstName", IsPrimaryKeyField = false},
        new FieldMapping() { SourceFileFieldName = "Surname", DestinationRecordFieldName = "LastName", IsPrimaryKeyField = false},
        new FieldMapping() { SourceFileFieldName = "Email Address", DestinationRecordFieldName = "Email", IsPrimaryKeyField = false},
        new FieldMapping() { SourceFileFieldName = "Segment", DestinationRecordFieldName = "Segment", IsPrimaryKeyField = false},
      };
      return bulkUpsertRecordsDetails;
    }
    #endregion
  }
}