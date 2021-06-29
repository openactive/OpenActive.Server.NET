using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Collections.Generic;

namespace OpenActive.Server.NET.OpenBookingHelper
{
    public static class ClientRegistrationSerializer
    {
        /// <summary>
        /// Serializer settings used when deserializing.
        /// </summary>
        private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings()
        {
            Converters = new List<JsonConverter>()
            {
                new StringEnumConverter()
            },
            NullValueHandling = NullValueHandling.Ignore,
            DefaultValueHandling = DefaultValueHandling.Ignore,
            DateParseHandling = DateParseHandling.DateTimeOffset,
            // The ASP.NET MVC framework defaults to 32(so 32 levels deep in the JSON structure)
            // to prevent stack overflow caused by malicious complex JSON requests.
            MaxDepth = 32
        };

        /// <summary>
        /// Returns the JSON representation of a ClientRegistrationModel.
        /// </summary>
        /// <returns>
        /// A <see cref="string" /> that represents the JSON representation of the ClientRegistrationModel.
        /// </returns>
        public static string Serialize<T>(T obj) where T : ClientRegistrationModel => JsonConvert.SerializeObject(obj, SerializerSettings);


        /// <summary>
        /// Returns a strongly typed model of the JSON representation provided.
        /// 
        /// Note this will return null if the deserialized JSON-LD class cannot be assigned to `T`.
        /// </summary>
        /// <typeparam name="T">ClientRegistrationModel to deserialize</typeparam>
        /// <param name="str">JSON string</param>
        /// <returns>Strongly typed ClientRegistrationModel</returns>
        public static T Deserialize<T>(string str) where T : ClientRegistrationModel => JsonConvert.DeserializeObject<T>(str, SerializerSettings);
    }

    public class ClientRegistrationModel
    {
        [JsonProperty(OidcConstants.RegistrationResponse.ClientId)]
        public string ClientId { get; set; }

        [JsonProperty(OidcConstants.ClientMetadata.ClientName)]
        public string ClientName { get; set; }

        [JsonProperty(OidcConstants.ClientMetadata.ClientUri)]
        public string ClientUri { get; set; }

        [JsonProperty(OidcConstants.ClientMetadata.LogoUri)]
        public string LogoUri { get; set; }

        [JsonProperty(OidcConstants.ClientMetadata.GrantTypes)]
        public IEnumerable<string> GrantTypes { get; set; }

        [JsonProperty(OidcConstants.ClientMetadata.RedirectUris)]
        public IEnumerable<string> RedirectUris { get; set; } = new List<string>();

        public string Scope { get; set; } = "openid profile email";
    }

    public class ClientRegistrationResponse : ClientRegistrationModel
    {
        [JsonProperty(OidcConstants.RegistrationResponse.ClientSecret)]
        public string ClientSecret { get; set; }
    }
}
