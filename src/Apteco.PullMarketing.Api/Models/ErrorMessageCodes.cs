namespace Apteco.PullMarketing.Api.Models
{
  public enum ErrorMessageCodes
  {
    //General errors start with 1XXX
    GeneralInvalidParameters = 1001,

    //DataStores errors start with 2XXX
    NoDataStoreNameProvided = 2001,
    NoDataStoreDetailsProvided = 2002,

    //Records errors start with 3XXX
    NoUpsertDetailsSpecified = 3001,
    NoPrimaryKeySpecified = 3002,
    MultiplePrimaryKeysSpecified = 3003,
    NoFieldNameSpecified = 3004,
    NoFieldValueSpecified = 3005,
    NoUpsertRecordDetailsSpecified = 3006,
    InvalidFilterValuesSpecified = 3007,
  }
}
