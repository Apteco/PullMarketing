using System;

namespace Apteco.PullMarketing.Api.ModelBinding
{
  [AttributeUsage(AttributeTargets.Property)]
  public sealed class FromJsonAttribute : Attribute
  {
  }
}
