using System.ComponentModel;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;
using MalModernUi.Constants;
using MalModernUi.Models;
using Xamarin.Forms;

namespace MalModernUi.ViewModel
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public MainViewModel()
        {
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

        private string _AppReplies;
        public string AppReplies
        {
            get
            {
                return _AppReplies;
            }
            set
            {
                if (_AppReplies != value)
                {
                    _AppReplies = value;
                    PropertyIsChanged(nameof(AppReplies));
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
                AddNewReply("I'm sorry, I encountered a network error calling into LUIS.");
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
                        AddNewReply("I don't understand what you are asking for...");
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
                    AddNewReply($"I don't know how to create an item named {itemType}.");
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
                        AddNewReply($"I've added a new {itemType} with an id of {doResponse.id}.");
                    }
                }

            }
            catch (WebException)
            {
                AddNewReply("I'm sorry, I encountered a network error creating the new Dev Ops item.");
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
        private void AddNewReply(string reply)
        {
            AppReplies = AppReplies + "\n\r\n\r" + reply;
        }
    }
}