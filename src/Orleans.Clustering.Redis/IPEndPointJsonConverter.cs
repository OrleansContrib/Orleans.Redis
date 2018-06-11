using System;
using Newtonsoft.Json;
using System.Net;
using Orleans.Runtime;

namespace Orleans.Clustering.Redis
{
    public class SiloAddressJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(SiloAddress).IsAssignableFrom(objectType);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var stored = (StoredSiloAddress)serializer.Deserialize(reader, typeof(StoredSiloAddress));
            return SiloAddress.New(stored.Endpoint, stored.Generation);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var addr = (SiloAddress)value;

            serializer.Serialize(writer, new StoredSiloAddress
            {
                Endpoint = addr.Endpoint,
                Generation = addr.Generation
            });
        }

        class StoredSiloAddress
        {
            public int Generation
            {
                get; set;
            }
            public IPEndPoint Endpoint { get; set; }
        }
    }
    public class IPEndPointJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(IPEndPoint).IsAssignableFrom(objectType);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            IPEndPoint result = (StoredEndpoint)serializer.Deserialize(reader, typeof(StoredEndpoint));
            return result;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            StoredEndpoint ep = (IPEndPoint)value;
            serializer.Serialize(writer, ep);
        }
    }
}