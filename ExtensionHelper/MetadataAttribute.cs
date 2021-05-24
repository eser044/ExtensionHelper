using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExtensionHelper
{
    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = true)]
    public class MetadataAttribute : Attribute
    {
        public MetadataAttribute(string description, string metaData = "")
        {
            Description = description;
            MetaData = metaData;
        }

        public string Description { get; set; }
        public string MetaData { get; set; }
    }
}
