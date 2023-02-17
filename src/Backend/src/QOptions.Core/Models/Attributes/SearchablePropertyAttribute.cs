using System;

namespace QOptions.Core.Models.Attributes
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class SearchablePropertyAttribute : Attribute
    {
    }
}