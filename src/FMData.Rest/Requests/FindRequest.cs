using Newtonsoft.Json;
using System.Collections.Generic;

namespace FMData.Rest.Requests
{
    /// <summary>
    /// The object that contains the parameters to serialize for a find request.
    /// </summary>
    public partial class FindRequest<TRequestType> : RequestBase, IFindRequest<TRequestType>
    {
        /// <summary>
        /// The find request dictionary.
        /// </summary>
        [JsonProperty("query")]
        public IEnumerable<RequestQueryInstance<TRequestType>> Query { get { return _query; } }

        private List<RequestQueryInstance<TRequestType>> _query = new List<RequestQueryInstance<TRequestType>>();

        /// <summary>
        /// Maximum number of records to return for this request.
        /// </summary>
        [JsonProperty("limit")]
        public int Limit { get; set; } = 100; // default 

        /// <summary>
        /// The number of records to skip before returning records for this request.
        /// </summary>
        [JsonProperty("offset")]
        public int Offset { get; set; } = 1; // default

        /// <summary>
        /// The sort fields and directions for this request.
        /// </summary>
        [JsonProperty("sort")]
        public IEnumerable<ISort> Sort { get; set; }

        /// <summary>
        /// Determines if container data attributes are processed and loaded.
        /// </summary>
        public bool LoadContainerData { get; set; }

        /// <summary>
        /// Create a find request from Json
        /// </summary>
        /// <param name="json">The incomming Json data to deserialize.</param>
        /// <returns>An instance of the FindRequest object from the provided Json string.</returns>
        public static FindRequest<T> FromJson<T>(string json) => JsonConvert.DeserializeObject<FindRequest<T>>(json);

        /// <summary>
        /// JSON Convert the current object to a string for passing out to the API.
        /// </summary>
        /// <returns></returns>
        public override string SerializeRequest() => JsonConvert.SerializeObject(this,
            Formatting.None,
            new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Ignore,
                Converters = 
                { 
                    new FormatNumbersAsTextConverter(), 
                    new RequestQueryInstanceConverter<TRequestType>() 
                }
            });

        /// <summary>
        /// Add an instance to the query collection.
        /// </summary>
        /// <param name="query">The object to add to the query.</param>
        /// <param name="omit">Flag indicating if this instance represents a find or an omit.</param>
        public void AddQuery(TRequestType query, bool omit = false) => _query.Add(new RequestQueryInstance<TRequestType>(query, omit));
    }
}