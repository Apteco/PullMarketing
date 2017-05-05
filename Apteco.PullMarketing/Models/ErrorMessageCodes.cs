namespace Apteco.PullMarketing.Models
{
  public enum ErrorMessageCodes
  {
    //DataStores errors start with 1XXX
    NoDataStoreNameProvided = 1001,
    NoDataStoreDetailsProvided = 1002,

    //Records errors start with 2XXX
    NoUpsertDetailsSpecified = 2001,
    NoPrimaryKeySpecified = 2002,
    MultiplePrimaryKeysSpecified = 2003,
  }
}
