namespace Apteco.PullMarketing.Data
{
  public static class StringUtilities
  {
    public static void RemoveEnclosers(string[] items, char encloser)
    {
      if (encloser == (char) 0)
        return;

      for (int i = 0; i < items.Length; i++)
      {
        items[i] = RemoveEncloser(items[i], encloser);
      }
    }

    private static string RemoveEncloser(string item, char encloser)
    {
      if (string.IsNullOrEmpty(item))
        return item;

      int itemLength = item.Length;
      bool startEncloser = item[0] == encloser;
      bool endEncloser = item[itemLength - 1] == encloser;

      if (startEncloser && endEncloser && itemLength > 1)
        return item.Substring(1, itemLength - 2);
      else if (startEncloser && !endEncloser)
        return item.Substring(1, itemLength - 1);
      else if (!startEncloser && endEncloser)
        return item.Substring(0, itemLength - 1);
      else
        return item;
    }
  }
}
