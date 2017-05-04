namespace Apteco.PullMarketing.Models
{
  public enum ErrorMessageCodes
  {
    //DataStores errors start with 1XXX

    //Records errors start with 2XXX
    NoUpsertDetailsSpecified = 1001,
    NoPrimaryKeySpecified = 1002,
    MultiplePrimaryKeysSpecified = 1003,
  }
}
