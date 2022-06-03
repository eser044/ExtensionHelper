using Newtonsoft.Json.Converters;

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
