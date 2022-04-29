using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HelperUtilities.Helper
{
    public class JsonCustomDateConverter : IsoDateTimeConverter
    {
        public JsonCustomDateConverter(string format)
        {
            base.DateTimeFormat = format;
        }
    }
}
