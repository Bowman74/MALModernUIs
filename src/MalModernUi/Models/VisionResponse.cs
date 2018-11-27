using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace MalModernUi.Models
{
    public class VisionResponse
    {
        public VisionResponse()
        {
            RecognitionResult = new RecognitionResult();
        }

        public string Status { get; set; }

        [JsonProperty("recognitionResult")]
        public RecognitionResult RecognitionResult { get; set; } 
    }

    public class RecognitionResult
    {
        public RecognitionResult()
        {
            Lines = new List<RecognitionLine>();
        }

        [JsonProperty("lines")]
        public List<RecognitionLine> Lines { get; set; }
    }

    public class RecognitionLine
    {
        public RecognitionLine()
        {
            BoundingBox = new List<int>();
            Words = new List<RecognitionWord>();
        }

        public List<int> BoundingBox { get; set; }

        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("words")]
        public List<RecognitionWord> Words { get; set; }
    }

    public class RecognitionWord
    {
        public RecognitionWord()
        {
            BoundingBox = new List<int>();
        }

        public List<int> BoundingBox { get; set; }

        [JsonProperty("text")]
        public string Text { get; set; }
    }
}