using System;
namespace MalModernUi.Models
{
    public class VoiceResponse
    {
        public string RecognitionStatus { get; set; }
        public string DisplayText { get; set; }
        public int Offset {get; set;}
        public int Duration { get; set; }
    }
}