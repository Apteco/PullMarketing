using System;

namespace Apteco.PullMarketing.Swagger
{
  [AttributeUsage(AttributeTargets.Method)]
  public sealed class MultiPartFormDataWithFileAttribute : Attribute
  {
    #region public properties
    public string Name { get; private set; }

    public string Description { get; private set; }
    #endregion

    #region public constructor
    public MultiPartFormDataWithFileAttribute(string name, string description)
    {
      Name = name;
      Description = description;
    }
    #endregion
  }
}
