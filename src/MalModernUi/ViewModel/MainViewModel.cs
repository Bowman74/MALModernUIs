﻿using System.ComponentModel;
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
using System.Net.Http.Headers;
using System.Linq;

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

        #region "Demo 1"
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
                string content = $"[{{'op': 'add','path': '/fields/System.Title','from': null,'value': 'Test {itemType}'}}]";

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
#endregion

        #region "Demo 2"
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
            catch (Exception)
            {
                AddNewMessage("I'm sorry, I encountered a network error evaluating the voice input.", false);
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

#endregion

        #region "Demo 3"
        private Command _takePicture;
        public Command TakePicture
        {
            get
            {
                if (_takePicture == null)
                {
                    _takePicture = new Command(async () => await TakePictureAsync());
                }
                return _takePicture;
            }
        }

        private async Task TakePictureAsync()
        {
            var options = new Plugin.Media.Abstractions.StoreCameraMediaOptions();
            options.PhotoSize = Plugin.Media.Abstractions.PhotoSize.MaxWidthHeight;
            options.MaxWidthHeight = 1000;
            var photo = await Plugin.Media.CrossMedia.Current.TakePhotoAsync(options);

            if (photo != null)
            {
                using (var imageStream = photo.GetStream())
                {
                    await RecognizeImageWritingAsync(imageStream);
                }
            }
        }

        public async Task<string> RecognizeImageWritingAsync(Stream imageStream)
        {
            string imageResult = string.Empty;
            //HttpWebResponse response = null;
            try
            {
                // Send audio stream to Bing and deserialize the response
                var url = $"{Url.VisionEndpoint}/recognizeText?mode=Handwritten";

                var client = new HttpClient();
                client.DefaultRequestHeaders.Add(
                    "Ocp-Apim-Subscription-Key", Url.VisionKey);

                HttpResponseMessage response;
                byte[] b;
                string operationLocation = string.Empty;

                using (BinaryReader br = new BinaryReader(imageStream))
                {
                    b = br.ReadBytes((int)imageStream.Length);
                }
                using (ByteArrayContent content = new ByteArrayContent(b))
                {
                    string contentString = string.Empty;
                    content.Headers.ContentType =
                        new MediaTypeHeaderValue("application/octet-stream");

                    response = await client.PostAsync(url, content);
                    if (response.IsSuccessStatusCode)
                    {
                        operationLocation = response.Headers.GetValues("Operation-Location").FirstOrDefault();

                        if (operationLocation != string.Empty)
                        {
                            int i = 0;
                            do
                            {
                                System.Threading.Thread.Sleep(1000);
                                response = await client.GetAsync(operationLocation);
                                contentString = await response.Content.ReadAsStringAsync();
                                ++i;
                            }
                            while (i < 10 && contentString.IndexOf("\"status\":\"Succeeded\"", StringComparison.CurrentCulture) == -1);

                            if (i == 10 && contentString.IndexOf("\"status\":\"Succeeded\"", StringComparison.CurrentCulture) == -1)
                            {
                                AddNewMessage("Timeout error analyzing message", false);
                            }
                        }

                        var doResponse = JsonConvert.DeserializeObject<VisionResponse>(contentString);
                        string allLines = string.Empty;
                        foreach (var line in doResponse.RecognitionResult.Lines)
                        {
                            if (!string.IsNullOrEmpty(allLines))
                            {
                                allLines += " ";
                            }
                            allLines += line.Text;
                        }
                        if (!string.IsNullOrEmpty(allLines))
                        {
                            UserText = allLines;
                            await SendTextAsync();
                        }
                    }
                    else
                    {
                        // Display the JSON error data.
                        string errorString = await response.Content.ReadAsStringAsync();
                        AddNewMessage(errorString, false);
                    }
                }
            }
            catch (Exception)
            {
                AddNewMessage("I'm sorry, I encountered a network error evaluating the image.", false);
            }
            return imageResult;
        }
        #endregion "Demo 3"

        private Command _showAr;
        public Command ShowAr
        {
            get
            {
                if (_showAr == null)
                {
                    _showAr = new Command(async () => await ShowArViewAsync());
                }
                return _showAr;
            }
        }

        private async Task ShowArViewAsync()
        {
            await App.GetNavigationPage().Navigation.PushAsync(new ArView());
        }
    }
}