// LeversonCarlos
// Xamarin.OneDrive.Connector

// https://github.com/LeversonCarlos/Xamarin.OneDrive.Connector

using Android.App;
using Android.Content;
using Microsoft.Identity.Client;
using System;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net;
using System.Threading;
using System.Linq;
using System.Runtime.Serialization;
using System.Collections.Generic;

namespace Uno.OneDrive
{

    #region "Xamarin.OneDrive.Connector"
    partial class Connector
    {

        public static void SetAuthenticationContinuationEventArgs(int requestCode, Result resultCode, Intent data)
        {
            Microsoft.Identity.Client.AuthenticationContinuationHelper.SetAuthenticationContinuationEventArgs(requestCode, resultCode, data);
        }

    }


    internal class Configs : IDisposable
    {

        public string ClientID { get; set; }
        public string[] Scopes { get; set; }

        internal string RedirectUri { get; set; }
        internal object UiParent { get; set; }

        public void Dispose()
        {
            this.UiParent = null;
        }

    }

    internal class DependencyImplementation : IDependency
    {
        internal Context _activity;
        internal string _redirectUrl;

        public void Initialize(Configs configs)
        {
            if (_activity != null)
            {
                configs.UiParent = _activity;
            }
            else
            {
                var mainActivity = Android.App.Application.Context; // Uno.UI.BaseActivity.Current; // Android.App.Application.Context; // Xamarin.Forms.Forms.Context as Forms.Platform.Android.FormsAppCompatActivity;
                configs.UiParent = mainActivity;
            }
            if (!string.IsNullOrEmpty(_redirectUrl))
            {
                configs.RedirectUri = _redirectUrl;
            }
            else
            {
                configs.RedirectUri = "https://login.microsoftonline.com/common/oauth2/nativeclient"; // $"msal{configs.ClientID}://auth";
            }
        }

        public async Task<AuthenticationResult> GetAuthResult(Microsoft.Identity.Client.IPublicClientApplication client, Configs configs)
        {

            Activity aktywnosc = Uno.UI.BaseActivity.Current; // (Activity)configs.UiParent;
            try
            {
                return await client
                   .AcquireTokenInteractive(configs.Scopes)
                   .WithParentActivityOrWindow(aktywnosc)
                   .ExecuteAsync();
            }
            catch (Exception) { throw; }
        }

    }

    partial class Connector
    {

        public static void Init(Context activity) { Init(activity, ""); }

        public static void Init(Context activity, string redirectUrl)
        {
            var dependency = (DependencyImplementation)Dependency.Current;
            dependency._activity = activity;
            dependency._redirectUrl = redirectUrl;
        }

    }


    internal interface IDependency
    {
        void Initialize(Configs configs);
        Task<Microsoft.Identity.Client.AuthenticationResult> GetAuthResult(Microsoft.Identity.Client.IPublicClientApplication client, Configs configs);
    }

    internal class Dependency
    {

        static Lazy<IDependency> implementation = new Lazy<IDependency>(() => CreateDependency(), LazyThreadSafetyMode.PublicationOnly);

        static IDependency CreateDependency()
        {
            // #pragma warning disable IDE0022 // Use expression body for methods
            return new DependencyImplementation();
            // #pragma warning restore IDE0022 // Use expression body for methods
        }

        public static IDependency Current
        {
            get
            {
                IDependency ret = implementation.Value;
                if (ret == null)
                {
                    throw new NotImplementedException("This functionality is not implemented in the portable version of this assembly.  You should reference the NuGet package from your main application project in order to reference the platform-specific implementation.");
                }
                return ret;
            }
        }

    }

    partial class Connector
    {

        public async Task<bool> ConnectAsync()
        {
            return await this.ConnectorAsync(ConnectorHandler.InnerConnectionConnect);
        }

        public async Task<bool> DisconnectAsync()
        {
            return await this.ConnectorAsync(ConnectorHandler.InnerConnectionDisconnect);
        }

        private async Task<bool> ConnectorAsync(string connectorState)
        {
            try
            {
                var httpParam = new StringContent(connectorState);
                var httpMessage = await this.PostAsync(ConnectorHandler.InnerConnectionPath, httpParam);
                if (httpMessage.IsSuccessStatusCode) { return true; }
                else
                {
                    var httpReason = httpMessage.ReasonPhrase;
                    var httpContent = string.Empty;
                    if (httpMessage.Content != null)
                    {
                        httpContent = await httpMessage.Content.ReadAsStringAsync();
                    }
                    throw new Exception($"{httpReason}\n{httpContent}");
                }
            }
            catch (Exception) { throw; }
        }

    }

    public partial class Connector : HttpClient
    {
        const string BaseURL = "https://graph.microsoft.com/v1.0/";
        internal Configs mConfig = null;
        internal bool mWrite = false;
        public Connector(string clientID, params string[] scopes) : this(new Configs { ClientID = clientID, Scopes = scopes })
        { }

        internal Connector(Configs configs) : base(new ConnectorHandler(configs))
        {
            this.BaseAddress = new Uri(BaseURL);
            mConfig = configs;
            foreach (string scope in configs.Scopes)
            {
                if (scope.Contains("Write"))
                    mWrite = true;
            }
        }

    }
    partial class ConnectorHandler
    {

        public HttpResponseMessage CreateMessage(HttpStatusCode statusCode)
        {
            return this.CreateMessage(statusCode, string.Empty);
        }

        public HttpResponseMessage CreateMessage(HttpStatusCode statusCode, string content)
        {
            var responseMessage = new HttpResponseMessage(statusCode);
            if (!string.IsNullOrEmpty(content))
            {
                responseMessage.Content = new StringContent(content);
            }
            return responseMessage;
        }

    }

    internal partial class ConnectorHandler : DelegatingHandler
    {
        Token Token { get; set; }
        internal const string InnerConnectionPath = "Xamarin-OneDrive-Connector";
        internal const string InnerConnectionConnect = "CONNECT";
        internal const string InnerConnectionDisconnect = "DISCONNECT";

        public ConnectorHandler(Configs configs)
        {

            // VALIDATION
            if (configs == null) { throw new NullReferenceException("The configs parameter must be defined with the microsoft graph definitions"); }
            if (string.IsNullOrEmpty(configs.ClientID)) { throw new ArgumentNullException("Your microsoft graph client id must be informed"); }
            if (configs.Scopes == null || configs.Scopes.Length == 0) { throw new ArgumentNullException("Your microsoft graph required scopes must be informed"); }
            if (configs.Scopes.Count(x => !string.IsNullOrEmpty(x)) == 0) { throw new ArgumentNullException("Your microsoft graph required scopes must be informed"); }

            // INITIALIZATION
            Dependency.Current.Initialize(configs);
            this.Token = new Token(configs);
            this.InnerHandler = new HttpClientHandler();
        }

        protected async override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {

            var InnerConnectionResult = await this.InnerConnectionHandlerAsync(request, cancellationToken);
            if (InnerConnectionResult.StatusCode != HttpStatusCode.SeeOther) { return InnerConnectionResult; }

            if (!await this.Token.ConnectAsync()) { return this.CreateMessage(HttpStatusCode.Unauthorized, "The token connect method has failed"); }

            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("bearer", this.Token.CurrentToken);
            return await base.SendAsync(request, cancellationToken);

        }

        private async Task<HttpResponseMessage> InnerConnectionHandlerAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            try
            {
                if (!request.RequestUri.AbsolutePath.EndsWith(InnerConnectionPath)) { return this.CreateMessage(HttpStatusCode.SeeOther); }
                if (request.Method != HttpMethod.Post || request.Content == null) { return this.CreateMessage(HttpStatusCode.BadRequest, "Method must be POST and content must be CONNECT or DISCONNECT"); }

                var command = await request.Content.ReadAsStringAsync();
                if (command == InnerConnectionConnect)
                {
                    var result = await this.Token.ConnectAsync();
                    if (result) { return this.CreateMessage(HttpStatusCode.OK); }
                    else { return this.CreateMessage(HttpStatusCode.InternalServerError, "The token connect method has failed"); }
                }
                else if (command == InnerConnectionDisconnect)
                {
                    await this.Token.DisconnectAsync();
                    return this.CreateMessage(HttpStatusCode.OK);
                }
                else
                { return this.CreateMessage(HttpStatusCode.BadRequest, "Method must be POST and content must be CONNECT or DISCONNECT"); }

            }
            catch (Exception ex) { return this.CreateMessage(HttpStatusCode.InternalServerError, ex.ToString()); }
        }

    }
    partial class Token
    {

        internal async Task<bool> AcquireAsync()
        {
            try
            {
                this.AuthResult = await Dependency.Current.GetAuthResult(this.Client, this.Configs);
                return this.IsValid();
            }
            catch (Exception) { throw; }
        }

    }
    partial class Token
    {

        internal async Task<bool> ConnectAsync()
        {
            try
            {

                // TOKEN STILL VALID
                if (this.IsValid()) { return true; }

                // REFRESH AN EXPIRED TOKEN 
                if (await this.RefreshAsync()) { return true; }

                // ACQUIRE A NEW TOKEN 
                if (await this.AcquireAsync()) { return true; }

                // OTHERWISE
                return false;

            }
            catch (Exception) { throw; }
        }

        internal async Task DisconnectAsync()
        {
            try
            {
                var accounts = await this.Client.GetAccountsAsync();
                if (accounts != null && accounts.Count() != 0)
                {
                    foreach (var account in accounts)
                    { await this.Client.RemoveAsync(account); }
                }

            }
            catch (Exception) { throw; }
        }

    }
    partial class Token
    {
        AuthenticationResult AuthResult { get; set; }

        internal string CurrentToken
        {
            get
            {
                if (!this.IsValid()) { return string.Empty; }
                return this.AuthResult.AccessToken;
            }
        }

        internal bool IsValid()
        {
            if (this.AuthResult == null) { return false; }
            if (string.IsNullOrEmpty(this.AuthResult.AccessToken)) { return false; }
            return (this.AuthResult.ExpiresOn > DateTimeOffset.UtcNow.AddMinutes(5));
        }

    }
    partial class Token
    {

        internal async Task<bool> RefreshAsync()
        {
            try
            {
                var accounts = await this.Client.GetAccountsAsync();
                if (accounts != null && accounts.Count() != 0)
                {
                    var account = accounts.FirstOrDefault();
                    if (account != null)
                    {
                        this.AuthResult = await this.Client.AcquireTokenSilent(this.Configs.Scopes, account).ExecuteAsync();
                    }
                }
                return this.IsValid();
            }
            catch (Exception) { throw; }
        }

    }
    internal partial class Token : IDisposable
    {
        internal Configs Configs { get; private set; }
        IPublicClientApplication Client { get; set; }

        internal Token(Configs configs)
        {
            this.Configs = configs;
            var builder = PublicClientApplicationBuilder.Create(configs.ClientID);
            if (!string.IsNullOrEmpty(configs.RedirectUri)) { builder = builder.WithRedirectUri(configs.RedirectUri); }
            this.Client = builder.Build();
        }

        public void Dispose()
        {
            this.Client = null;
            this.AuthResult = null;
            this.Configs.Dispose();
            this.Configs = null;
        }

    }

    #endregion

    #region "Xamarin.OneDrive.Connector.Files"

    #region "structy"
    [DataContract]
    public class FileData
    {

        [DataMember(Name = "id")]
        public string id { get; set; }

        [DataMember(Name = "name")]
        public string FileName { get; set; }

        [DataMember(Name = "path")]
        public string FilePath { get; set; }

        [DataMember(Name = "createdDateTime")]
        internal string CreatedDateTimeText { get; set; }
        public DateTime? CreatedDateTime { get; set; }

        [DataMember(Name = "size")]
        public long? Size { get; set; }

        [DataMember(Name = "@microsoft.graph.downloadUrl")]
        internal string downloadUrl { get; set; }

        [DataMember(Name = "parentReference")]
        internal FileData parentReference { get; set; }
        public string parentID { get; set; }

        [DataMember(Name = "folder")]
        internal FolderData folderData { get; set; }

        [DataMember(Name = "file")]
        internal FileDetailsData fileData { get; set; }

    }

    [DataContract]
    internal class FileDetailsData
    {

        [DataMember(Name = "mimeType")]
        internal string mimeType { get; set; }

    }

    [DataContract]
    internal class FolderData
    {

        [DataMember(Name = "childCount")]
        internal int childCount { get; set; }

    }

    [DataContract]
    internal class SearchData
    {

        [DataMember(Name = "value")]
        public List<FileData> Files { get; set; }

        [DataMember(Name = "@odata.nextLink")]
        public string nextLink { get; set; }

    }
    #endregion
    #region "folders"
    partial class Connector
    {
        public async Task<List<FileData>> GetChildFoldersAsync()
        {
            try
            {
                var httpPath = $"me/drive/root/children";
                var folderList = await GetChildFoldersAsync(httpPath);
                folderList.ForEach(x => x.FilePath = $"/ {x.FileName}");
                return folderList;
            }
            catch (Exception) { throw; }
        }

        public async Task<List<FileData>> GetChildFoldersAsync(FileData folder)
        {
            // MOJ DODATEK upraszczajacy moje funkcje
            if (folder is null)
                return await GetChildFoldersAsync();

            try
            {
                var httpPath = $"me/drive/items/{folder.id}/children";
                var folderList = await GetChildFoldersAsync(httpPath);
                folderList.ForEach(x => x.FilePath = $"{folder.FilePath} / {x.FileName}");
                return folderList;
            }
            catch (Exception) { throw; }
        }

        private async Task<List<FileData>> GetChildFoldersAsync(string httpPath)
        {
            try
            {
                var folderList = new List<FileData>();
                httpPath += "?select=id,name,folder&$top=1000";

                while (!string.IsNullOrEmpty(httpPath))
                {

                    // REQUEST DATA FROM SERVER
                    var httpMessage = await GetAsync(httpPath);
                    if (!httpMessage.IsSuccessStatusCode)
                    { throw new Exception(await httpMessage.Content.ReadAsStringAsync()); }

                    // SERIALIZE AND STORE RESULT
                    var httpContent = await httpMessage.Content.ReadAsStreamAsync();
                    var serializer = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(SearchData));
                    var httpResult = (SearchData)serializer.ReadObject(httpContent);
                    var folders = httpResult.Files.Where(x => x.folderData != null).ToList();
                    folderList.AddRange(folders);

                    // CHECK IF THERE IS ANOTHER PAGE OF RESULTS
                    httpPath = httpResult.nextLink;
                    if (!string.IsNullOrEmpty(httpPath))
                    { httpPath = httpPath.Replace(BaseAddress.AbsoluteUri, string.Empty); }

                }

                // NORMALIZE FOLDER's PATHS
                foreach (var folder in folderList)
                {
                    if (string.IsNullOrEmpty(folder.FilePath))
                    { folder.FilePath = string.Empty; }

                    var sep = folder.FilePath.IndexOf(":");
                    if (sep != -1)
                    { folder.FilePath = folder.FilePath.Substring(sep + 1); }

                    folder.FilePath = Uri.UnescapeDataString(folder.FilePath);
                }

                // RESULT
                folderList = folderList
                   .OrderBy(x => x.FilePath)
                   .ThenBy(x => x.FileName)
                   .ToList();
                return folderList;

            }
            catch (Exception) { throw; }
        }


        public async Task<List<FileData>> GetChildFilesAsync(FileData folder)
        {
            try
            {
                var fileList = new List<FileData>();
                var httpPath = $"me/drive/items/{folder.id}/children";
                httpPath += "?select=id,name,createdDateTime,size,file&$top=1000";

                while (!string.IsNullOrEmpty(httpPath))
                {

                    // REQUEST DATA FROM SERVER
                    var httpMessage = await GetAsync(httpPath);
                    if (!httpMessage.IsSuccessStatusCode)
                    { throw new Exception(await httpMessage.Content.ReadAsStringAsync()); }

                    // SERIALIZE AND STORE RESULT
                    var httpContent = await httpMessage.Content.ReadAsStreamAsync();
                    var serializer = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(SearchData));
                    var httpResult = (SearchData)serializer.ReadObject(httpContent);
                    var files = httpResult.Files.Where(x => x.fileData != null).ToList();
                    fileList.AddRange(files);

                    // CHECK IF THERE IS ANOTHER PAGE OF RESULTS
                    httpPath = httpResult.nextLink;
                    if (!string.IsNullOrEmpty(httpPath))
                    { httpPath = httpPath.Replace(BaseAddress.AbsoluteUri, string.Empty); }

                }

                // NORMALIZE FILE's PATHS
                foreach (var file in fileList)
                {
                    file.parentID = folder.id;
                    file.FilePath = folder.FilePath;
                }

                // RESULT
                return fileList;

            }
            catch (Exception) { throw; }
        }
    }
    #endregion
    #region "Files"

    partial class Connector
    {
        public async Task<FileData> GetDetailsAsync()
        {
            var httpPath = $"me/drive/root";
            var folder = await GetDetailsAsync(httpPath);
            if (string.IsNullOrEmpty(folder.FilePath))
            { folder.FilePath = "/drive/root:"; }
            return folder;
        }

        public async Task<FileData> GetDetailsAsync(FileData folder)
        {
            var httpPath = $"me/drive/items/{folder.id}";
            return await GetDetailsAsync(httpPath);
        }

        private async Task<FileData> GetDetailsAsync(string httpPath)
        {
            try
            {
                httpPath += "?select=id,name,parentReference";

                // REQUEST DATA FROM SERVER
                var httpMessage = await GetAsync(httpPath);
                if (!httpMessage.IsSuccessStatusCode)
                { throw new Exception(await httpMessage.Content.ReadAsStringAsync()); }

                // SERIALIZE AND STORE RESULT
                var httpContent = await httpMessage.Content.ReadAsStreamAsync();
                var serializer = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(FileData));
                var httpResult = (FileData)serializer.ReadObject(httpContent);

                // RESULT
                if (httpResult.parentReference != null)
                { httpResult.FilePath = httpResult.parentReference.FilePath; }
                return httpResult;

            }
            catch (Exception) { throw; }
        }
    }

    #endregion

    #region "upload"
    partial class Connector
    {

        public async Task<FileData> UploadAsync(FileData file, System.IO.Stream content)
        {
            try
            {

                var httpPath = $"me/drive/items/{file.id}/content";
                if (string.IsNullOrEmpty(file.id))
                { httpPath = $"me/drive/items/{file.parentID}:/{file.FileName}:/content"; }
                var httpData = new System.Net.Http.StreamContent(content);
                var httpMessage = await PutAsync(httpPath, httpData);

                if (!httpMessage.IsSuccessStatusCode)
                { throw new Exception(await httpMessage.Content.ReadAsStringAsync()); }

                var httpContent = await httpMessage.Content.ReadAsStreamAsync();
                var serializer = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(FileData));
                var httpResult = (FileData)serializer.ReadObject(httpContent);

                return httpResult;

            }
            catch (Exception) { throw; }
        }

    }
    #endregion

    #endregion

    #region "moje dodatki"

    public partial class Connector
    {

        public async Task<FileData> GetThisAppFolderAsync()
        {
            var httpMessage = await GetAsync("drive/special/approot:/");
            if (!httpMessage.IsSuccessStatusCode)
            { throw new Exception(await httpMessage.Content.ReadAsStringAsync()); }

            // SERIALIZE AND STORE RESULT
            var httpContent = await httpMessage.Content.ReadAsStreamAsync();
            var serializer = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(FileData));
            var httpResult = (FileData)serializer.ReadObject(httpContent);

            // RESULT
            if (httpResult.parentReference != null)
            { httpResult.FilePath = httpResult.parentReference.FilePath; }
            return httpResult;
        }


        public async Task<FileData> CreateFolder(FileData parentFolder, string folderName)
        {
            // if cannot write, then don't even try
            if (!mWrite)
                return null;

            string httpPath;
            if (parentFolder is null)
            {
                // zakladamy folder w glownym (rzadko, albo w ogole nie uzywany bedzie to kod
                httpPath = "me/drive/root/children";
            }
            else
            {
                httpPath = "me/drive/items/" + parentFolder.id + "/children";
            }

            var jsonCommand = "{ \"name\": \"" + folderName + "\", \"folder\": { }, \"@microsoft.graph.conflictBehavior\": \"rename\" }";
            var httpData = new StringContent(jsonCommand, System.Text.Encoding.UTF8, "application/json");
            var httpMessage = await PutAsync(httpPath, httpData);

            if (!httpMessage.IsSuccessStatusCode)
            { throw new Exception(await httpMessage.Content.ReadAsStringAsync()); }

            var httpContent = await httpMessage.Content.ReadAsStreamAsync();
            var serializer = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(FileData));
            var httpResult = (FileData)serializer.ReadObject(httpContent);

            return httpResult;

        }

        public async Task<FileData> OpenOrCreateFolder(FileData parentFolder, string folderName)
        {
            if (parentFolder is null) return null;
            if (string.IsNullOrEmpty(parentFolder.id)) return null;
            if (string.IsNullOrEmpty(folderName)) return null;


            FileData thisLevel = null;
            foreach (var subfold in await GetChildFoldersAsync(parentFolder))
            {
                if (subfold.FileName == folderName)
                {
                    thisLevel = subfold;
                    break;
                }
            }

            if (thisLevel is null)
            {
                thisLevel = await CreateFolder(parentFolder, folderName);
            }

            return null;

        }


        public async Task<FileData> OpenOrCreateFolder(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath))
                return null;

            folderPath = folderPath.Replace("\\", "/"); // na wszelki wypadek
            string[] pathParts = folderPath.Split("/");

            FileData thisLevel = null;
            FileData parentLevel = null;

            for (int i = 0; i < pathParts.Count(); i++)
            {
                // jesli zapis jest /cos/cos, to pierwszy / jest do pominiecia, a i tak zaczynamy od root
                if (pathParts[i] == "")
                    break;

                thisLevel = await OpenOrCreateFolder(parentLevel, pathParts[i]);

                if (thisLevel is null)
                    return null;

                parentLevel = thisLevel;
            }

            return thisLevel;
        }

        public async Task<FileData> SaveFile(FileData parentFolder, string fileName, string fileContent)
        {
            // if cannot write, then don't even try
            if (!mWrite)
                return null;

            if (parentFolder is null)
                return null; // nie obslugujemy zapisu do glownego katalogu

            try
            {

                var httpPath = $"me/drive/items/{parentFolder.id}:/{fileName}:/content";

                var httpData = new System.Net.Http.StringContent(fileContent);
                var httpMessage = await PutAsync(httpPath, httpData);

                if (!httpMessage.IsSuccessStatusCode)
                {
                    System.Diagnostics.Debug.WriteLine("--------OneDrive error: \n" + await httpMessage.Content.ReadAsStringAsync());
                    return null;
                }

                var httpContent = await httpMessage.Content.ReadAsStreamAsync();
                var serializer = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(FileData));
                var httpResult = (FileData)serializer.ReadObject(httpContent);

                return httpResult;

            }
            catch 
            {
                return null;
            }
        }
        private async Task<string> ReadFile(string httpPath)
        {
            var httpMessage = await GetAsync(httpPath);

            if (!httpMessage.IsSuccessStatusCode)
            {
                System.Diagnostics.Debug.WriteLine("--------OneDrive error: \n" + await httpMessage.Content.ReadAsStringAsync());
                return null;
            }
            return await httpMessage.Content.ReadAsStringAsync();
        }

        public async Task<string> ReadFile(FileData file)
        {
            var httpPath = $"me/drive/items/{file.id}/content";
            return await ReadFile(httpPath);
        }

        public async Task<string> ReadFile(FileData parentFolder, string fileName)
        {
            if (parentFolder is null)
                return null; // nie obslugujemy zapisu do glownego katalogu

            var httpPath = $"me/drive/items/{parentFolder.id}:/{fileName}:/content";
            return await ReadFile(httpPath);
        }
        private async Task<System.IO.Stream> GetFileStream(string httpPath)
        {
            var httpMessage = await GetAsync(httpPath);

            if (!httpMessage.IsSuccessStatusCode)
            {
                System.Diagnostics.Debug.WriteLine("--------OneDrive error: \n" + await httpMessage.Content.ReadAsStringAsync());
                return null;
            }

            return await httpMessage.Content.ReadAsStreamAsync();
        }

        public async Task<System.IO.Stream> GetFileStream(FileData file)
        {
            var httpPath = $"me/drive/items/{file.id}/content";
            return await GetFileStream(httpPath);
        }

        public async Task<System.IO.Stream> GetFileStream(FileData parentFolder, string fileName)
        {
            if (parentFolder is null)
                return null; // nie obslugujemy zapisu do glownego katalogu

            var httpPath = $"me/drive/items/{parentFolder.id}:/{fileName}:/content";
            return await GetFileStream(httpPath);
        }

        public async Task<bool> DeleteFile(FileData file)
        {
            if (string.IsNullOrEmpty(file.id))
                throw new ArgumentException("OneDrive.DeleteFile called with null FileData.id");

            var httpMessage = await DeleteAsync("drive/items/" + file.id);
            return httpMessage.IsSuccessStatusCode;
        }

        public async Task<bool> DeleteFile(string httpFilePath)
        {
            if (string.IsNullOrEmpty(httpFilePath))
                throw new ArgumentException("OneDrive.DeleteFile called with null httpFilePath");

            if (!httpFilePath.StartsWith("/"))
                httpFilePath = "/" + httpFilePath;
            var httpMessage = await DeleteAsync("drive/root:" + httpFilePath);
            return httpMessage.IsSuccessStatusCode;
        }

        public async Task<bool> DeleteFile(FileData parentFolder, string fileName)
        {
            if (string.IsNullOrEmpty(parentFolder.id))
                throw new ArgumentException("OneDrive.DeleteFile called with null parentFolder.id");
            if (string.IsNullOrEmpty(fileName))
                throw new ArgumentException("OneDrive.DeleteFile called with null fileName");

            var httpMessage = await DeleteAsync("drive/items/" + parentFolder.id + ":/" + fileName);
            return httpMessage.IsSuccessStatusCode;
        }


    }
    #endregion
}   
