using System;
using Newtonsoft.Json;

namespace MalModernUi.Models
{
    public class NaturalLangaugeResponse
    {

        public string Query { get; set; }

        public Intent TopScoringIntent { get; set; }

        public Entity[] Entities { get; set; }

        public Sentiment SentimentAnalysis { get; set; }
    }

    public class Intent
    {
        [JsonProperty("intent")]
        public string IntentName { get; set; }
        public double Score { get; set; }
    }

    public class Entity
    {
        [JsonProperty("entity")]
        public string EntityName { get; set; }
        public string Type { get; set; }
        public int StartIndex { get; set; }
        public int EndIndex { get; set; }
        public Values Resolution { get; set; }
    }

    public class Values
    {
        [JsonProperty("values")]
        public string[] ValueArray { get; set; }
    }

    public class Sentiment
    {
        public string Label { get; set; }
        public double Score { get; set; }
    }
}
