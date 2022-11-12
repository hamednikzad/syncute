using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace SynCute.Core.Helpers;

public static class JsonHelper
{
    public enum CaseType
    {
        PascalCase,
        CamelCase
    }

    private static readonly JsonSerializerSettings CamelCaseSerializerSettings = new()
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver()
    };

    private class DecimalFormatConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return (objectType == typeof(decimal));
        }


        public override void WriteJson(JsonWriter writer, object value,
            JsonSerializer serializer)
        {
            writer.WriteValue(string.Format("{0:N2}", value));
        }

        public override bool CanRead
        {
            get { return false; }
        }

        public override object ReadJson(JsonReader reader, Type objectType,
            object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }

    public static string Serialize<T>(T input, CaseType caseType)
    {
        if (caseType == CaseType.CamelCase)
        {
            return JsonConvert.SerializeObject(input, new DecimalFormatConverter());
        }

        return JsonConvert.SerializeObject(input);
    }

    public static T? Deserialize<T>(string json)
    {
        return JsonConvert.DeserializeObject<T>(json);
    }
}