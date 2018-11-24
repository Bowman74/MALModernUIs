using System.ComponentModel;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;
using MalModernUi.Constants;
using MalModernUi.Models;
using Xamarin.Forms;
using System.Collections.ObjectModel;
using Plugin.AudioRecorder;
using System;
using System.Net.Http;
using System.IO;

namespace MalModernUi.ViewModel
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private AudioRecorderService _recorder;

        public MainViewModel()
        {
            AppConversation = new ObservableCollection<ConversationItem>();
            if (_recorder == null)
            {
                _recorder = new AudioRecorderService
                {
                    PreferredSampleRate = 16000,
                    StopRecordingOnSilence = true, //will stop recording after 2 seconds (default)
                    StopRecordingAfterTimeout = true,  //stop recording after a max timeout (defined below)
                    TotalAudioTimeout = TimeSpan.FromSeconds(15) //audio will stop recording after 15 seconds
                };
            }
            _recorder.AudioInputReceived += Done_Recording;
        }

        private async void Done_Recording(object sender, string audioFile)
        {
            //var token = await FetchTokenAsync();
            await RecognizeSpeechAsync(audioFile);

        }

        private Command _sendText;
        public Command SendText
        {
            get
            {
                if (_sendText == null)
                {
                    _sendText = new Command(async () => await SendTextAsync());
            }
                return _sendText;
            }
        }

        private Command _toggleRecording;
        public Command ToggleRecording
        {
            get
            {
                if (_toggleRecording == null)
                {
                    _toggleRecording = new Command(async () => await ToggleRecordingAsync());
                }
                return _toggleRecording;
            }
        }

        private bool _isRecording;
        public bool IsRecording
        {
            get { return _isRecording; }
            private set
            {
                if (_isRecording != value)
                {
                    _isRecording = value;
                    PropertyIsChanged(nameof(IsRecording));
                }
            }
        }

        private async Task ToggleRecordingAsync()
        {
            if (IsRecording)
            {
                IsRecording = false;
                if (_recorder.IsRecording)
                {
                    await _recorder.StopRecording();
                }
            }
            else
            {
                IsRecording = true;
                await _recorder.StartRecording();
            }
        }

        private string _UserText;
        public string UserText 
        { 
            get
            {
                return _UserText;
            }
            set
            {
                if (_UserText != value)
                {
                    _UserText = value;
                    PropertyIsChanged(nameof(UserText));
                }
            }
        }

        private ObservableCollection<ConversationItem> _AppConversation;
        public ObservableCollection<ConversationItem> AppConversation
        {
            get
            {
                return _AppConversation;
            }
            private set
            {
                if (_AppConversation != value)
                {
                    _AppConversation = value;
                    PropertyIsChanged(nameof(AppConversation));
                }
            }
        }

        private async Task SendTextAsync()
        {
            if (string.IsNullOrEmpty(UserText)) return;

            try
            {
                var requestText = UserText;
                UserText = string.Empty;

                AddNewMessage(requestText, true);
                var url = $"{Url.NaturalLanguageEndpoint}?subscription-key={Url.NaturalLanguageKey}&timezoneOffset=-360&q={HttpUtility.UrlEncode(requestText)}";

                var request = WebRequest.Create(url);
                HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync();
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    await ProcessNaturalLanguageResponse(response);
                }
            }
            catch (WebException)
            {
                AddNewMessage("I'm sorry, I encountered a network error calling into LUIS.", false);
            }
        }

        private async Task ProcessNaturalLanguageResponse(HttpWebResponse response)
        {
            var encoding = ASCIIEncoding.ASCII;
            NaturalLangaugeResponse nlResponse = null;
            using (var reader = new System.IO.StreamReader(response.GetResponseStream(), encoding))
            {
                string responseText = reader.ReadToEnd();
                nlResponse = JsonConvert.DeserializeObject<NaturalLangaugeResponse>(responseText);
            }

            if (nlResponse != null && nlResponse.TopScoringIntent != null)
            {
                switch (nlResponse.TopScoringIntent.IntentName)
                {
                    case "Create New":
                        if (nlResponse.Entities.Length >= 1)
                        {
                            var itemNumber = await CreateDevOpsItem(nlResponse.Entities[0].Resolution.ValueArray[0]);
                        }
                        break;
                    default:
                        AddNewMessage("I don't understand what you are asking for...", false);
                        break;
                }
            }
        }

        private void PropertyIsChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private async Task<int> CreateDevOpsItem(string itemType)
        {
            int returnValue = 0;

            try
            {
                var vstsItemName = GetVSTSItemName(itemType);
                if (string.IsNullOrEmpty(vstsItemName))
                {
                    AddNewMessage($"I don't know how to create an item named {itemType}.", false);
                }
                var url = $"{Url.VsDevOpsEnpoint}/_apis/wit/workitems/${vstsItemName}?api-version=4.1";
                string content= $"[{{'op': 'add','path': '/fields/System.Title','from': null,'value': 'Test {itemType}'}}]";

                var request = WebRequest.Create(url);
                request.Headers.Add("Authorization", $"Basic {Url.VsDevOpsKey}");
                request.Method = "POST";
                request.ContentType = "application/json-patch+json";

                ASCIIEncoding encoding1 = new ASCIIEncoding();
                byte[] contentBytes = encoding1.GetBytes(content);
                var reqStream = request.GetRequestStream();
                await reqStream.WriteAsync(contentBytes, 0, contentBytes.Length);
                reqStream.Close();

                HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync();
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var encoding = ASCIIEncoding.ASCII;

                    using (var reader = new System.IO.StreamReader(response.GetResponseStream(), encoding))
                    {
                        string responseText = reader.ReadToEnd();
                        var doResponse = JsonConvert.DeserializeObject<DevOpsResponse>(responseText);
                        AddNewMessage($"I've added a new {itemType} with an id of {doResponse.id}.", false);
                    }
                }

            }
            catch (WebException)
            {
                AddNewMessage("I'm sorry, I encountered a network error creating the new Dev Ops item.", false);
            }

            return returnValue;
        }

        private string GetVSTSItemName(string entityName)
        {
            var returnValue = "";
            switch (entityName.ToUpper())
            {
                case "FEATURE":
                    {
                        returnValue = "Feature";
                        break;
                    }
                case "EPIC":
                    {
                        returnValue = "Epic";
                        break;
                    }
                case "TEST CASE":
                    {
                        returnValue = "Test Case";
                        break;
                    }
                case "TASK":
                    {
                        returnValue = "Task";
                        break;
                    }
                case "USER STORY":
                    {
                        returnValue = "User Story";
                        break;
                    }
                case "BUG":
                    {
                        returnValue = "Bug";
                        break;
                    }
            }
            return returnValue;
        }

        private void AddNewMessage(string message, bool client)
        {
            AppConversation.Add(new ConversationItem
            {
                Message = message,
                ClientMessage = client
            });
        }

        private async Task<string> FetchTokenAsync()
        {
            UriBuilder uriBuilder = new UriBuilder(Url.TokenServiceEndpoint);

            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", Url.SpeechKey);

            var result = await httpClient.PostAsync(uriBuilder.Uri.AbsoluteUri, null);
            return await result.Content.ReadAsStringAsync();    
        }

        public async Task<string> RecognizeSpeechAsync(string fileName)
        {
            string speechResult = string.Empty;
            try
            {
                // Read audio file to a stream
                using (var fileStream = File.Open(fileName, FileMode.Open))
                {
                    // Send audio stream to Bing and deserialize the response
                    var url = $"{Url.SpeechEndpoint}?language=en-US&format=simple";

                    var request = (HttpWebRequest)WebRequest.Create(url);
                    request.Method = "POST";
                    request.ContentType = "audio/wav; codec=audio/pcm; samplerate=16000";
                    //request.Headers.Add("Authorization", $"Bearer {accessToken}");
                    request.Headers.Add("Ocp-Apim-Subscription-Key", Url.SpeechKey);
                    //request.Host = "westus.stt.speech.microsoft.com";

                    //var audioBytes = ReadFully(fileStream);

                    var reqStream = request.GetRequestStream();
                    await fileStream.CopyToAsync(reqStream);
                    //await reqStream.WriteAsync(audioBytes, 0, audioBytes.Length);
                    reqStream.Flush();
                    reqStream.Close();

                    HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync();
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        var encoding = ASCIIEncoding.ASCII;

                        using (var reader = new System.IO.StreamReader(response.GetResponseStream(), encoding))
                        {
                            string responseText = reader.ReadToEnd();
                            var doResponse = JsonConvert.DeserializeObject<VoiceResponse>(responseText);
                            UserText = doResponse.DisplayText;
                            await SendTextAsync();
                        }
                    }
                }
            }
            catch (Exception e)
            {
                var a = e;
            }
            return speechResult;
        }

        public static byte[] ReadFully(Stream input)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                input.CopyTo(ms);
                return ms.ToArray();
            }
        }
    }
}