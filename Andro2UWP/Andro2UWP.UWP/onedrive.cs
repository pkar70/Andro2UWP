using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
namespace p
{
    class od
    {

        private static Microsoft.OneDrive.Sdk.IOneDriveClient goOneDriveClnt;
        private static bool gInOneDriveCommand = false;
        private static bool gbLimitToAppFolder = true;
        // Private gOneDriveMutex As Threading.Mutex = New Threading.Mutex
        // If Not gOneDriveMutex.WaitOne(100) Then Return ...
        // nie Mutex, bo wszystko w tym samym wątku!

        // UWAGA OGOLNA:
        // wszystkie funkcje uzywaja Mutexa, wiec nie mogą się wzajemnie wywolywac!
        // jesliby miały, to trzeba rozdzielic na FUNKCJAInt oraz FUNKCJA,
        // tak by Private FUNKCJAInt nie miala weryfikacji Mutexa,
        // zas Public FUNKCJA - miała

        public static bool IsOneDriveOpened()
        {
            return goOneDriveClnt != null;
        }

        public async static Task<bool> OpenOneDrive(bool limitToAppFolder, bool bInteractive)
        {
            p.k.DebugOut("OpenOneDrive(limitToAppFolder=" + limitToAppFolder.ToString());
            if (gInOneDriveCommand)
            {
                p.k.DebugOut("OpenOneDrive: gInOneDriveCommand=true");
                return false;
            }
            gInOneDriveCommand = true;

            bool bRet = await OpenOneDriveInt(bInteractive);
            gbLimitToAppFolder = limitToAppFolder;
            gInOneDriveCommand = false;
            return bRet;
        }

        private async static Task<bool> OpenOneDriveInt(bool bInteractive)
        {
            p.k.DebugOut("OpenOneDriveInt");
            // https://github.com/OneDrive/onedrive-sample-photobrowser-uwp/blob/master/OneDrivePhotoBrowser/AccountSelection.xaml.cs
            // dla PC tu bedzie error, wiec zwróci FALSE

            // If gInOneDriveCommand Then Return False
            // gInOneDriveCommand = True

            bool bError = false;
            try
            {   // onedrive.appfolder
                string[] sScopes = new[] { "onedrive.readwrite", "offline_access" };
                const string oneDriveConsumerBaseUrl = "https://api.onedrive.com/v1.0";

                // inny sampel:
                // https://msdn.microsoft.com/en-us/magazine/mt632271.aspx
                // client = OneDriveClientExtensions.GetClientUsingOnlineIdAuthenticator(
                // _scopes);
                // var session = Await client.AuthenticateAsync();
                // Debug.WriteLine($"Token: {session.AccessToken}");

#if EmulacjaSdk
                // emulacja SDK (przekopiowany kod, zeby dokladniej sprawdzic gdzie jest error

                var onlineIdAuthProvider = new OnlineIdAuthenticationProvider(sScopes);
                await onlineIdAuthProvider.AuthenticateUserAsync();

                var authTask = onlineIdAuthProvider.RestoreMostRecentFromCacheOrAuthenticateUserAsync();
                // Await authTask

                //na razie pomijam: goOneDriveClnt = new Sdk.OneDriveClient(oneDriveConsumerBaseUrl, onlineIdAuthProvider);
                await authTask;     // tu jest w samplu - po moOneDriveClnt

#else

                var onlineIdAuthProvider = new Microsoft.OneDrive.Sdk.OnlineIdAuthenticationProvider(sScopes);
                p.k.DebugOut("OpenOneDriveInt: got onlineIdAuthProvider");

                // gdy poniższych dwu linii nie ma, i tak działa
                //await onlineIdAuthProvider.AuthenticateUserAsync();
                //p.k.DebugOut("OpenOneDriveInt: after AuthenticateUserAsync");

                Task authTask;
                if (bInteractive)
                    authTask = onlineIdAuthProvider.RestoreMostRecentFromCacheOrAuthenticateUserAsync();
                else
                    authTask = onlineIdAuthProvider.RestoreMostRecentFromCacheAsync();

                p.k.DebugOut("OpenOneDriveInt: after RestoreMostRecentFromCacheOrAuthenticateUserAsync");
                // Await authTask
                goOneDriveClnt = new Microsoft.OneDrive.Sdk.OneDriveClient(oneDriveConsumerBaseUrl, onlineIdAuthProvider);

                p.k.DebugOut("OpenOneDriveInt: got goOneDriveClnt");
                await authTask;     // tu jest w samplu - po moOneDriveClnt
                p.k.DebugOut("OpenOneDriveInt: after authTask");

#endif


            }
            catch (Exception ex)
            {
                p.k.DebugOut("OpenOneDriveInt catch: " + ex.Message );
                p.k.CrashMessageAdd("OpenOneDriveInt", ex, true);
                goOneDriveClnt = null;
                bError = true;
            }

            // gInOneDriveCommand = False
            return !bError;
        }

        // Public Async Function OpenCreateOneDriveFolder(sParentId As String, sName As String, bCreate As Boolean) As Task(Of String)
        // If Not IsOneDriveOpened() Then Return ""

        // If sName = "" Then Return ""

        // If Not gOneDriveMutex.WaitOne(100) Then Return ""

        // Dim oParent As Microsoft.OneDrive.Sdk.ItemRequest

        // If sParentId = "" Then
        // oParent = goOneDriveClnt.Drive.Root.Request
        // Else
        // oParent = goOneDriveClnt.Drive.Items(sParentId).Request
        // End If

        // Dim oLista As Microsoft.OneDrive.Sdk.Item = Await oParent.Expand("children").GetAsync

        // For Each oItem As Microsoft.OneDrive.Sdk.Item In oLista.Children.CurrentPage
        // If oItem.Name = sName Then
        // gInOneDriveCommand = false
        // Return oItem.Id
        // End If
        // Next

        // If Not bCreate Then Return ""

        // ' proba utworzenia katalogu
        // Dim oNew As Microsoft.OneDrive.Sdk.Item = New Microsoft.OneDrive.Sdk.Item
        // oNew.Name = sName
        // oNew.Folder = New Microsoft.OneDrive.Sdk.Folder

        // Dim oFolder As Microsoft.OneDrive.Sdk.Item
        // oFolder = Await goOneDriveClnt.Drive.Root.Children.Request().AddAsync(oNew)

        // gInOneDriveCommand = false
        // Return oFolder.Id

        // End Function

        public async static Task<bool> ReplaceOneDriveFileContent(string sFilePathname, string sTresc)
        {

            MemoryStream oStream = new MemoryStream();
            var oWrtr = new StreamWriter(oStream);
            oWrtr.WriteLine(sTresc);
            oWrtr.Flush();

            bool bRet = await ReplaceOneDriveFileContent(sFilePathname, oStream);

            oWrtr.Dispose();
            oWrtr = null;

            oStream.Dispose();
            oStream = null;

            return bRet;
        }


        private static Microsoft.OneDrive.Sdk.IItemRequestBuilder RootOrAppRoot()
        {
            //if (gbLimitToAppFolder)
            //    return goOneDriveClnt.Drive.Special.AppRoot;
            //else
            return goOneDriveClnt.Drive.Root;
        }

        public async static Task<bool> ReplaceOneDriveFileContent(string sFilePathname, Stream sTresc)
        {
            if (!IsOneDriveOpened())
                return false;     // gdy nie widac OneDrive
            if (gInOneDriveCommand)
                return false;
            gInOneDriveCommand = true;

            bool bError = false;

            sTresc.Seek(0, SeekOrigin.Begin);

            try
            {
                await RootOrAppRoot().ItemWithPath(sFilePathname).Content.Request().PutAsync<Microsoft.OneDrive.Sdk.Item>(sTresc);
            }
            catch (Exception ex)
            {
                bError = true;
            }

            gInOneDriveCommand = false;

            return !bError;
        }


        public async static Task<string> CopyFileToOneDrive(Windows.Storage.StorageFile oFile, string sFolderPath, bool bCanResetWifi)
        {
            // return: link (zeby mozna bylo bez OneDrive sie dostac do ostatniej ramki

            if (!IsOneDriveOpened())
                return "";

            if (gInOneDriveCommand)
                return "";

            gInOneDriveCommand = true;

            // oneDriveClient.Drive.Root.ItemWithPath("Apps/BicycleApp/ALUWP.db").Request().GetAsync();

            try
            {
                Stream oStream = await oFile.OpenStreamForReadAsync();
                if (!oStream.CanRead)
                {
                    p.k.CrashMessageAdd("@CopyFileToOneDrive", "not readable stream?");
                    return "";
                }

                Microsoft.OneDrive.Sdk.Item oItem = null;
                bool bError = false;

                string sOutFileName = sFolderPath + "/" + oFile.Name;

                try
                {
                    oItem = await RootOrAppRoot().ItemWithPath(sOutFileName).Content.Request().PutAsync<Microsoft.OneDrive.Sdk.Item>(oStream);   // (oRdr.BaseStream)
                }
                catch (Exception ex)
                {
                    p.k.CrashMessageAdd("@CopyFileToOneDrive while trying to copy file (try 1)", ex);
                    bError = true;
                }

                if (bError)
                {
                    // czasem nie kopiuje, jakby blokada i potrzeba reconnect?
                    return "";
                    //if (!bCanResetWifi || !await p.k.NetWiFiOffOn())
                    //{
                    //    p.k.CrashMessageAdd("cannot reconnect WiFi", "");
                    //    return "";
                    //}

                    //// If mbInDebug Then Debug.WriteLine("wifi reconnect OK")
                    //await Task.Delay(15 * 1000);     // 10 sekund na przywrócenie WiFi
                    //if (!await OpenOneDriveInt())
                    //{
                    //    p.k.CrashMessageAdd("cannot reconnect OneDrive", "");
                    //    bError = true;
                    //}
                    //else
                    //{
                    //    // tu sie zmieniało (przynajmniej czasem) na nie CanRead - tak błąd z PutAsync był
                    //    if (!oStream.CanSeek || !oStream.CanRead)
                    //    {
                    //        oStream.Dispose();
                    //        oStream = await oFile.OpenStreamForReadAsync();
                    //    }
                    //    else
                    //        oStream.Seek(0, SeekOrigin.Begin);

                    //    oItem = await goOneDriveClnt.Drive.Root.ItemWithPath(sOutFileName).Content.Request().PutAsync<Microsoft.OneDrive.Sdk.Item>(oStream);   // (oRdr.BaseStream)
                    //}
                }

                oStream.Dispose();
                oStream = null;

                string sLink = "";
                if (oItem != null)
                {
                    Microsoft.OneDrive.Sdk.Permission oLink = null;
                    oLink = await goOneDriveClnt.Drive.Items[oItem.Id].CreateLink("view").Request().PostAsync();
                    sLink = oLink.Link.ToString();
                    oLink = null;
                }
                oItem = null;
                gInOneDriveCommand = false;

                // ' próba - czy zmniejszy się zuzycie pamięci
                // gInOneDriveCommand = Nothing
                // Await OpenOneDriveInt()

                return sLink;
            }
            catch (Exception ex)
            {
                gInOneDriveCommand = false;
                return "";
            }
        }


        public async static Task<string> SaveFileToOneDrive(Windows.Storage.StorageFile oFile, string sFolderPath, bool bCanResetWifi)
        {
            // return: link (zeby mozna bylo bez OneDrive sie dostac do ostatniej ramki

            if (!IsOneDriveOpened())
            {
                oFile = null/* TODO Change to default(_) if this is not a reference type */;
                return "";
            }

            if (gInOneDriveCommand)
            {
                oFile = null/* TODO Change to default(_) if this is not a reference type */;
                return "";
            }

            gInOneDriveCommand = true;

            // oneDriveClient.Drive.Root.ItemWithPath("Apps/BicycleApp/ALUWP.db").Request().GetAsync();

            try
            {
                Stream oStream = await oFile.OpenStreamForReadAsync();
                if (!oStream.CanRead)
                {
                    p.k.CrashMessageAdd("@CopyFileToOneDrive", "not readable stream?");
                    return "";
                }

                Microsoft.OneDrive.Sdk.Item oItem = null/* TODO Change to default(_) if this is not a reference type */;
                bool bError = false;

                string sOutFileName = sFolderPath + "/" + oFile.Name;

                try
                {
                    oItem = await RootOrAppRoot().ItemWithPath(sOutFileName).Content.Request().PutAsync<Microsoft.OneDrive.Sdk.Item>(oStream);   // (oRdr.BaseStream)
                }
                catch (Exception ex)
                {
                    p.k.CrashMessageAdd("@CopyFileToOneDrive while trying to copy file (try 1)", ex);
                    bError = true;
                }

                if (bError)
                {
                    // czasem nie kopiuje, jakby blokada i potrzeba reconnect?
                    return "";
                    //if (!bCanResetWifi || !await p.k.NetWiFiOffOn())
                    //{
                    //    p.k.CrashMessageAdd("cannot reconnect WiFi", "");
                    //    return "";
                    //}

                    //// If mbInDebug Then Debug.WriteLine("wifi reconnect OK")
                    //await Task.Delay(15 * 1000);     // 10 sekund na przywrócenie WiFi
                    //if (!await OpenOneDriveInt())
                    //{
                    //    p.k.CrashMessageAdd("cannot reconnect OneDrive", "");
                    //    bError = true;
                    //}
                    //else
                    //{
                    //    // tu sie zmieniało (przynajmniej czasem) na nie CanRead - tak błąd z PutAsync był
                    //    if (!oStream.CanSeek || !oStream.CanRead)
                    //    {
                    //        oStream.Dispose();
                    //        oStream = await oFile.OpenStreamForReadAsync();
                    //    }
                    //    else
                    //        oStream.Seek(0, SeekOrigin.Begin);

                    //    oItem = await goOneDriveClnt.Drive.Root.ItemWithPath(sOutFileName).Content.Request().PutAsync<Microsoft.OneDrive.Sdk.Item>(oStream);   // (oRdr.BaseStream)
                    //}
                }

                oStream.Dispose();
                oStream = null;

                string sLink = "";
                if (oItem != null)
                {
                    Microsoft.OneDrive.Sdk.Permission oLink = null/* TODO Change to default(_) if this is not a reference type */;
                    oLink = await goOneDriveClnt.Drive.Items[oItem.Id].CreateLink("view").Request().PostAsync();
                    sLink = oLink.Link.ToString();
                    oLink = null/* TODO Change to default(_) if this is not a reference type */;
                }
                oItem = null/* TODO Change to default(_) if this is not a reference type */;
                gInOneDriveCommand = false;

                // ' próba - czy zmniejszy się zuzycie pamięci
                // gInOneDriveCommand = Nothing
                // Await OpenOneDriveInt()

                return sLink;
            }
            catch (Exception ex)
            {
                gInOneDriveCommand = false;
                return "";
            }
        }


        public async static Task<string> ReadOneDriveTextFileId(string sFileId)
        {
            var stream = await GetOneDriveFileIdStream(sFileId);
            if (stream is null) return null;

            var streamRdr = new StreamReader(stream);
            string retVal = streamRdr.ReadToEnd();
            streamRdr.Dispose();
            if (stream != null) stream.Dispose();

            return retVal;
        }

        public async static Task<string> ReadOneDriveTextFile(string sPath)
        {
            var stream = await GetOneDriveFileStream(sPath);
            if (stream is null) return null;

            var streamRdr = new StreamReader(stream);
            string retVal = streamRdr.ReadToEnd();
            streamRdr.Dispose();
            if(stream != null) stream.Dispose();

            return retVal;
        }



        private async static Task<Stream> GetOneDriveFileStream(Microsoft.OneDrive.Sdk.IItemRequestBuilder oItemReq)
        {
            p.k.DebugOut("GetOneDriveFileStream(oItemReq");
            try
            {
                var oFile = await oItemReq.Request().GetAsync();
                if(oFile is null)
                {
                    p.k.DebugOut("GetOneDriveFileStream: oFile is null");
                    return null;
                }
                p.k.DebugOut("GetOneDriveFileStream: got oFile");
                Stream oStream = await oItemReq.Content.Request().GetAsync();
                p.k.DebugOut("GetOneDriveFileStream: got Stream");

                // Dim oRdr As BinaryReader = New BinaryReader(oStream)
                // oRdr.ReadBytes(1000)

                return oStream;
            }
            catch (Exception ex)
            {
                // ale plik może nie istnieć - przyjmujemy taką możliwość...
                if (ex.Message != "Item does not exist")
                    p.k.CrashMessageAdd("@GetOneDriveFileStream(oItemReq", ex);

                return null;
            }

        }

        public async static Task<Stream> GetOneDriveFileIdStream(string sFileId)
        {
            p.k.DebugOut("GetOneDriveFileIdStream(" + sFileId);
            // https://msdn.microsoft.com/en-us/magazine/mt632271.aspx
            if (!IsOneDriveOpened())
                return null;

            if (gInOneDriveCommand)
                return null;
            gInOneDriveCommand = true;

            try
            {
                Microsoft.OneDrive.Sdk.IItemRequestBuilder oItemReq;

                oItemReq = goOneDriveClnt.Drive.Items[sFileId];

                if (oItemReq == null)
                    return null;

                return await GetOneDriveFileStream(oItemReq);
            }
            finally
            {
                gInOneDriveCommand = false;
            }

        }

        public async static Task<Stream> GetOneDriveFileStream(string sFilePath)
        {
            p.k.DebugOut("GetOneDriveFileStream(" + sFilePath);
            // https://msdn.microsoft.com/en-us/magazine/mt632271.aspx
            if (!IsOneDriveOpened())
                return null;

            if (gInOneDriveCommand)
                return null;

            gInOneDriveCommand = true;

            try
            {
                Microsoft.OneDrive.Sdk.IItemRequestBuilder oItemReq;

                sFilePath = sFilePath.Replace(@"\", "/");
                oItemReq = RootOrAppRoot().ItemWithPath(sFilePath);

                if (oItemReq == null)
                    return null;

                return await GetOneDriveFileStream(oItemReq);
            }
            finally
            {
                gInOneDriveCommand = false;
            }

        }

        public async static Task<List<string>> OneDriveGetAllChilds(string sPathname, bool bFolders, bool bFiles)
        {
            List<string> lNames = new List<string>();

            if (gInOneDriveCommand)
                return null;
            gInOneDriveCommand = true;

            Microsoft.OneDrive.Sdk.Item oPicLista = null;

            try
            {
                oPicLista = await RootOrAppRoot().ItemWithPath(sPathname).Request().Expand("children").GetAsync();
            }
            catch (Exception ex)
            {
                p.k.CrashMessageAdd("@OneDriveGetAllChilds - get first page", ex);
                return lNames;
            }

            // gdzieś robi Exception, wiec dokładniejsze kontrole/sprawdzanie
            if(oPicLista is null)
            {
                p.k.CrashMessageAdd("@OneDriveGetAllChilds - oPicLista null (first page)");
                return lNames;
            }

            if (oPicLista.Children is null)
            {
                p.k.CrashMessageAdd("@OneDriveGetAllChilds - oPicLista.Children null (first page)");
                return lNames;
            }

            if (oPicLista.Children.CurrentPage is null)
            {
                p.k.CrashMessageAdd("@OneDriveGetAllChilds - oPicLista.Children.CurrentPage null (first page)");
                return lNames;
            }

            try
            {
                // Dim oPicItem As Microsoft.OneDrive.Sdk.Item
                for (int iInd = 0; iInd <= oPicLista.Children.CurrentPage.Count - 1; iInd++)
                {
                    // For Each oPicItem As Microsoft.OneDrive.Sdk.Item In oPicLista.Children.CurrentPage
                    Microsoft.OneDrive.Sdk.Item oPicItem = oPicLista.Children.CurrentPage.ElementAt(iInd);
                    if (bFolders && oPicItem.Folder != null)
                        lNames.Add(oPicItem.Name);
                    if (bFiles && oPicItem.File != null)
                        lNames.Add(oPicItem.Name);
                    oPicItem = null;
                }
                oPicLista.Children.CurrentPage.Clear();  // to juz pewnie w ogole niepotrzebne
            }
            catch (Exception ex)
            {
                p.k.CrashMessageAdd("@OneDriveGetAllChilds - iterate first page (should never happen)", ex);
                return lNames;
            }

            if (oPicLista == null)
            {
                gInOneDriveCommand = false;
                return lNames;
            }

            // oPicLista.Children na pewno nie jest null
            if (oPicLista.Children.NextPageRequest == null)
            {
                oPicLista = null;
                gInOneDriveCommand = false;
                return lNames;
            }

            try
            {

                Microsoft.OneDrive.Sdk.IItemChildrenCollectionPage oPicNew = null;
                try
                {
                    oPicNew = await oPicLista.Children.NextPageRequest.GetAsync();
                    oPicLista = null; // juz niepotrzebne
                }
                catch (Exception ex)
                {
                    p.k.CrashMessageAdd("@OneDriveGetAllChilds - get second page", ex);
                }

                if (oPicNew == null)
                {
                    gInOneDriveCommand = false;
                    return lNames;
                }

                for (int iGuard = 1; iGuard <= 12000 / (double)200; iGuard++)   // itemow moze byc, przez itemów na stronę
                {
                    // Microsoft.OneDrive.Sdk.Item oPicItem;
                    for (int iFor = 0; iFor <= oPicNew.CurrentPage.Count - 1; iFor++)
                    {
                        // For Each oPicItem In oPicNew.CurrentPage
                        Microsoft.OneDrive.Sdk.Item oPicItem = oPicNew.CurrentPage.ElementAt(iFor);
                        if (bFolders && oPicItem.Folder != null)
                            lNames.Add(oPicItem.Name);
                        if (bFiles && oPicItem.File != null)
                            lNames.Add(oPicItem.Name);
                    }
                    // oPicItem = null;

                    if (oPicNew.NextPageRequest == null)
                    {
                        oPicNew = null;
                        gInOneDriveCommand = false;
                        return lNames;
                    }
                    try
                    {
                        oPicNew = await oPicNew.NextPageRequest.GetAsync();
                    }
                    catch (Exception ex)
                    {
                        p.k.CrashMessageAdd("@OneDriveGetAllChilds - page " + iGuard, ex);
                        break;
                    }
                }
                oPicNew = null;
            }
            catch (Exception ex)
            {
                p.k.CrashMessageAdd("@OneDriveGetAllChilds", ex);
            }

            gInOneDriveCommand = false;
            return lNames;
        }

        public async static Task<Collection<Microsoft.OneDrive.Sdk.Item>> OneDriveGetAllChildsSDK(string sPathname, bool bFolders, bool bFiles)
        {
            try
            {
                Collection<Microsoft.OneDrive.Sdk.Item> oItems = new Collection<Microsoft.OneDrive.Sdk.Item>();

                if (gInOneDriveCommand)
                    return oItems;
                gInOneDriveCommand = true;

                Microsoft.OneDrive.Sdk.Item oPicLista = await RootOrAppRoot().ItemWithPath(sPathname).Request().Expand("children").GetAsync();
                for (int iInd = 0; iInd <= oPicLista.Children.CurrentPage.Count - 1; iInd++)
                {
                    // For Each oPicItem As Microsoft.OneDrive.Sdk.Item In oPicLista.Children.CurrentPage
                    Microsoft.OneDrive.Sdk.Item oPicItem = oPicLista.Children.CurrentPage.ElementAt(iInd);
                    if (bFolders && oPicItem.Folder != null)
                        oItems.Add(oPicItem);
                    if (bFiles && oPicItem.File != null)
                        oItems.Add(oPicItem);
                    oPicItem = null;
                }

                if (oPicLista.Children.NextPageRequest == null)
                {
                    gInOneDriveCommand = false;
                    return oItems;
                }

                Microsoft.OneDrive.Sdk.IItemChildrenCollectionPage oPicNew = await oPicLista.Children.NextPageRequest.GetAsync();
                oPicLista = null; // juz niepotrzebne

                for (int iGuard = 1; iGuard <= 12000 / (double)200; iGuard++)   // itemow moze byc, przez itemów na stronę
                {
                    Microsoft.OneDrive.Sdk.Item oPicItem;
                    for (int iFor = 0; iFor <= oPicNew.CurrentPage.Count - 1; iFor++)
                    {
                        // For Each oPicItem In oPicNew.CurrentPage
                        oPicItem = oPicNew.CurrentPage.ElementAt(iFor);
                        if (bFolders && oPicItem.Folder != null)
                            oItems.Add(oPicItem);
                        if (bFiles && oPicItem.File != null)
                            oItems.Add(oPicItem);
                        oPicItem = null;
                    }
                    oPicItem = null;
                    if (oPicNew.NextPageRequest == null)
                        return oItems;
                    oPicNew = await oPicNew.NextPageRequest.GetAsync();
                }
                oPicNew = null;

                gInOneDriveCommand = false;

                return oItems;
            }
            catch (Exception ex)
            {
                gInOneDriveCommand = false;
                p.k.CrashMessageExit("@OneDriveGetAllChildsSDK", ex.Message);
                return null;
            }
        }


        /// <summary>
        /// Usuwa z podanego katalogu listę plików (filenames)
        /// Ret: -1 = error (nie ma OneDrive, lub InUse)
        /// 0: wszystkie usunął
        /// >0: ile plików się nie udało usunąć
        ///      </summary>
        public async static Task<int> UsunPlikiOneDrive(string sFolderPathname, List<string> lFilesToDel)
        {
            if (!IsOneDriveOpened())
                return -1;

            // If mbInUsunPlikiOneDrive Then Return   ' nie potrzebuje osobnego - nie wejdzie, bo jest w OneDrive w ogole
            if (gInOneDriveCommand)
                return -1;
            gInOneDriveCommand = true;

            // mbInUsunPlikiOneDrive = True

            int iCnt = 0;
            foreach (string sFileName in lFilesToDel)
            {
                // gdy nie ma sieci, przerwij - na wypadek jakby trwało Del, a zaczął robić fotkę i był error powodujący reset WiFi
                if (!p.k.NetIsIPavailable(false))
                    break;
                try
                { // usuwać mogę z jednego UWP, ale z drugiego też - i wtedy już nie ma plików  :)
                    await RootOrAppRoot().ItemWithPath(sFolderPathname + "/" + sFileName).Request().DeleteAsync();
                    iCnt += 1;
                }
                catch (Exception)
                {
                }

                p.k.ProgRingInc();
            }

            // mbInUsunPlikiOneDrive = False
            gInOneDriveCommand = false;
            return lFilesToDel.Count - iCnt;
        }
    }

#if false
    public class Sdk
    {
        public class OnlineIdAuthenticationProvider : MsaAuthenticationProvider
        {
            private const string onlineIdServiceTicketRequestType = "DELEGATION";
            private readonly int ticketExpirationTimeInMinutes = 60;
            private readonly Windows.Security.Authentication.OnlineId.OnlineIdAuthenticator authenticator;
            private readonly Windows.Security.Authentication.OnlineId.CredentialPromptType credentialPromptType;

            public enum PromptType
            {
                PromptIfNeeded = Windows.Security.Authentication.OnlineId.CredentialPromptType.PromptIfNeeded,
                RetypeCredentials = Windows.Security.Authentication.OnlineId.CredentialPromptType.RetypeCredentials,
                DoNotPrompt = Windows.Security.Authentication.OnlineId.CredentialPromptType.DoNotPrompt
            }

            // USED
            public OnlineIdAuthenticationProvider(
                string[] scopes, PromptType promptType = PromptType.PromptIfNeeded)
                : base(null, null, scopes)
            {
                this.authenticator = new Windows.Security.Authentication.OnlineId.OnlineIdAuthenticator();
                this.credentialPromptType = (Windows.Security.Authentication.OnlineId.CredentialPromptType)promptType;
            }

            // USED
            public override async Task AuthenticateUserAsync(Microsoft.Graph.IHttpProvider httpProvider, string userName = null)
            {
                var authResult = await this.GetAuthenticationResultFromCacheAsync(userName, httpProvider);

                if (authResult == null)
                {
                    authResult = await this.GetAccountSessionAsync();

                    if (string.IsNullOrEmpty(authResult?.AccessToken))
                    {
                        throw new Microsoft.Graph.ServiceException(
                            new Microsoft.Graph.Error
                            {
                                //Code = Microsoft.OneDrive.Sdk.Authentication.OAuthConstants.ErrorCodes.AuthenticationFailure,
                                Code = "authenticationFailure",
                                Message = "Failed to retrieve a valid authentication token for the user."
                            });
                    }
                }

                this.CacheAuthResult(authResult);
            }

            /// <summary>
            /// Signs the current user out.
            /// </summary>
            public override async Task SignOutAsync()
            {
                if (this.IsAuthenticated)
                {
                    if (this.authenticator.CanSignOut)
                    {
                        await this.authenticator.SignOutUserAsync();
                    }

                    this.DeleteUserCredentialsFromCache(this.CurrentAccountSession);
                    this.CurrentAccountSession = null;
                }
            }

            internal async Task<Microsoft.OneDrive.Sdk.Authentication.AccountSession> GetAccountSessionAsync()
            {
                try
                {
                    var serviceTicketRequest = new Windows.Security.Authentication.OnlineId.OnlineIdServiceTicketRequest(string.Join(" ", this.scopes), onlineIdServiceTicketRequestType);
                    var ticketRequests = new List<Windows.Security.Authentication.OnlineId.OnlineIdServiceTicketRequest> { serviceTicketRequest };
                    var authenticationResponse = await this.authenticator.AuthenticateUserAsync(ticketRequests, credentialPromptType);

                    var ticket = authenticationResponse.Tickets.FirstOrDefault();

                    if (string.IsNullOrEmpty(ticket?.Value))
                    {
                        throw new Microsoft.Graph.ServiceException(
                            new Microsoft.Graph.Error
                            {
                                //Code = Microsoft.OneDrive.Sdk.Authentication.OAuthConstants.ErrorCodes.AuthenticationFailure,
                                Code = "authenticationFailure",
                                Message = string.Format(
                                    "Failed to retrieve a valid authentication token from OnlineIdAuthenticator for user {0}.",
                                    authenticationResponse.SignInName)
                            });
                    }

                    var accountSession = new Microsoft.OneDrive.Sdk.Authentication.AccountSession
                    {
                        AccessToken = string.IsNullOrEmpty(ticket.Value) ? null : ticket.Value,
                        ExpiresOnUtc = DateTimeOffset.UtcNow.AddMinutes(this.ticketExpirationTimeInMinutes),
                        ClientId = this.authenticator.ApplicationId.ToString(),
                        UserId = authenticationResponse.SafeCustomerId
                    };

                    return accountSession;
                }
                catch (TaskCanceledException taskCanceledException)
                {
                    throw new Microsoft.Graph.ServiceException(
                        new Microsoft.Graph.Error {
                            // Code = Microsoft.OneDrive.Sdk.Authentication.OAuthConstants.ErrorCodes.AuthenticationCancelled, 
                            Code = "authenticationCancelled",
                            Message = "Authentication was canceled."
                        }, taskCanceledException);
                }
                catch (Exception exception)
                {
                    throw new Microsoft.Graph.ServiceException(
                        new Microsoft.Graph.Error {
                            // Code = Microsoft.OneDrive.Sdk.Authentication.OAuthConstants.ErrorCodes.AuthenticationFailure,
                            Code = "authenticationFailure",
                            Message = exception.Message },
                        exception);
                }
            }

            internal override async Task<Microsoft.OneDrive.Sdk.Authentication.AccountSession> ProcessCachedAccountSessionAsync(Microsoft.OneDrive.Sdk.Authentication.AccountSession accountSession, Microsoft.Graph.IHttpProvider httpProvider)
            {
                if (accountSession != null)
                {
                    if (accountSession.ShouldRefresh) // Don't check 'CanRefresh' because this type can always refresh
                    {
                        accountSession = await this.GetAccountSessionAsync();

                        if (!string.IsNullOrEmpty(accountSession?.AccessToken))
                        {
                            return accountSession;
                        }
                    }
                    else
                    {
                        return accountSession;
                    }
                }

                return null;
            }
        }


        // ***********************************          MsaAuthenticationProvider       ***********************************

        public class MsaAuthenticationProvider // na razie NIET: : IAuthenticationProvider
        {
            internal readonly string clientId;
            internal string clientSecret;
            internal string returnUrl;
            internal string[] scopes;

            private Microsoft.OneDrive.Sdk.Authentication.OAuthHelper oAuthHelper;

            internal Microsoft.OneDrive.Sdk.Authentication.ICredentialVault credentialVault;
            internal Microsoft.OneDrive.Sdk.Authentication.IWebAuthenticationUi webAuthenticationUi;



            /// <summary>
            /// Constructs an <see cref="AuthenticationProvider"/>.
            /// </summary>
            public MsaAuthenticationProvider(string clientId, string returnUrl, string[] scopes)
                : this(clientId, returnUrl, scopes, /* credentialCache */ null, /* credentialVault */ null)
            {
            }

            /// <summary>
            /// Constructs an <see cref="AuthenticationProvider"/>.
            /// </summary>
            public MsaAuthenticationProvider(string clientId, string returnUrl, string[] scopes, Microsoft.OneDrive.Sdk.Authentication.ICredentialVault credentialVault)
                : this(clientId, returnUrl, scopes, /* credentialCache */ null, credentialVault)
            {
            }

            /// <summary>
            /// Constructs an <see cref="MsaAuthenticationProvider"/>.
            /// </summary>
            public MsaAuthenticationProvider(
                string clientId,
                string returnUrl,
                string[] scopes,
                Microsoft.OneDrive.Sdk.Authentication.CredentialCache credentialCache,
                Microsoft.OneDrive.Sdk.Authentication.ICredentialVault credentialVault)
                : this(clientId, returnUrl, scopes, credentialCache)
            {
                if (credentialVault != null)
                {
                    this.CredentialCache.BeforeAccess = cacheArgs =>
                    {
                        credentialVault.RetrieveCredentialCache(cacheArgs.CredentialCache);
                        cacheArgs.CredentialCache.HasStateChanged = false;
                    };
                    this.CredentialCache.AfterAccess = cacheArgs =>
                    {
                        if (cacheArgs.CredentialCache.HasStateChanged)
                        {
                            credentialVault.AddCredentialCacheToVault(cacheArgs.CredentialCache);
                        }
                    };
                }
            }

            /// <summary>
            /// Constructs an <see cref="MsaAuthenticationProvider"/>.
            /// </summary>
            public MsaAuthenticationProvider(
                string clientId,
                string returnUrl,
                string[] scopes,
                Microsoft.OneDrive.Sdk.Authentication.CredentialCache credentialCache)
            {
                this.clientId = clientId;
                this.clientSecret = null;

                this.returnUrl = string.IsNullOrEmpty(returnUrl)
                    ? Windows.Security.Authentication.Web.WebAuthenticationBroker.GetCurrentApplicationCallbackUri().ToString()
                    : returnUrl;

                this.scopes = scopes;

                this.CredentialCache = credentialCache ?? new Microsoft.OneDrive.Sdk.Authentication.CredentialCache();
                this.oAuthHelper = new Microsoft.OneDrive.Sdk.Authentication.OAuthHelper();
                this.webAuthenticationUi = new Microsoft.OneDrive.Sdk.Authentication.WebAuthenticationBrokerWebAuthenticationUi();
            }


            private Microsoft.OneDrive.Sdk.Authentication.CredentialCache CredentialCache { get; set; }

            public Microsoft.OneDrive.Sdk.Authentication.AccountSession CurrentAccountSession { get; set; }

            /// <summary>
            /// Gets whether or not the current client is authenticated.
            /// </summary>
            public bool IsAuthenticated
            {
                get
                {
                    return this.CurrentAccountSession != null;
                }
            }

            /// <summary>
            /// Authenticates the provided request object.
            /// </summary>
            /// <param name="request">The <see cref="HttpRequestMessage"/> to authenticate.</param>
            /// <returns>The task to await.</returns>
            public async Task AuthenticateRequestAsync(Windows.Web.Http.HttpRequestMessage request)
            {
                var authResult = await this.ProcessCachedAccountSessionAsync(this.CurrentAccountSession).ConfigureAwait(false);

                if (authResult == null)
                {
                    throw new Microsoft.Graph.ServiceException(
                        new Microsoft.Graph.Error
                        {
                            // Code = Microsoft.OneDrive.Sdk.Authentication.OAuthConstants.ErrorCodes.AuthenticationFailure,
                            Code = "authenticationFailure",
                            Message = "Unable to retrieve a valid account session for the user. Please call AuthenticateUserAsync to prompt the user to re-authenticate."
                        });
                }

                if (!string.IsNullOrEmpty(authResult.AccessToken))
                {
                    var tokenTypeString = string.IsNullOrEmpty(authResult.AccessTokenType)
                        ? Microsoft.OneDrive.Sdk.Authentication.OAuthConstants.Headers.Bearer
                        : authResult.AccessTokenType;
                    // na razie NIET: request.Headers.Authorization = new Windows.Web.Http.Headers.AuthenticationHeaderValue(tokenTypeString, authResult.AccessToken);
                }
            }

            /// <summary>
            /// Signs the current user out.
            /// </summary>
            public virtual async Task SignOutAsync()
            {
                if (this.IsAuthenticated)
                {
                    await this.SignOutOfBrowserAsync();

                    this.DeleteUserCredentialsFromCache(this.CurrentAccountSession);
                    this.CurrentAccountSession = null;
                }
            }

            /// <summary>
            /// Get rid of any cookies in the browser
            /// </summary>
            /// <returns>Task for signout. When task is done, signout is complete.</returns>
            public async Task SignOutOfBrowserAsync()
            {
                if (this.webAuthenticationUi != null)
                {
                    var requestUri = new Uri(this.oAuthHelper.GetSignOutUrl(this.clientId, this.returnUrl));

                    try
                    {
                        await this.webAuthenticationUi.AuthenticateAsync(requestUri, new Uri(this.returnUrl)).ConfigureAwait(false);
                    }
                    catch (Microsoft.Graph.ServiceException serviceException)
                    {
                        // Sometimes WebAuthenticationBroker can throw authentication cancelled on the sign out call. We don't care
                        // about this so swallow the error.
                        // if (!serviceException.IsMatch(Microsoft.OneDrive.Sdk.Authentication.OAuthConstants.ErrorCodes.AuthenticationCancelled))
                        if (!serviceException.IsMatch("authenticationCancelled"))
                        {
                            throw;
                        }
                    }
                }
            }

            protected void CacheAuthResult(Microsoft.OneDrive.Sdk.Authentication.AccountSession accountSession)
            {
                this.CurrentAccountSession = accountSession;

                if (this.CredentialCache != null)
                {
                    // na razie NIET: this.CredentialCache.AddToCache(accountSession);
                }
            }

            protected void DeleteUserCredentialsFromCache(Microsoft.OneDrive.Sdk.Authentication.AccountSession accountSession)
            {
                if (this.CredentialCache != null)
                {
                    // na razie NIET: this.CredentialCache.DeleteFromCache(accountSession);
                }
            }

            
            
            // USED - na tym wylatuje z bledem 
            /// <summary>
            /// Retrieves the authentication token. Tries the to retrieve the most recently
            /// used credentials if available.
            /// </summary>
            /// <param name="userName">The login name of the user, if known.</param>
            /// <returns>The authentication token.</returns>
            public async Task RestoreMostRecentFromCacheOrAuthenticateUserAsync(string userName = null)
            {
                using (var httpProvider = new Microsoft.Graph.HttpProvider())
                {
                    await this.RestoreMostRecentFromCacheOrAuthenticateUserAsync(httpProvider, userName).ConfigureAwait(false);
                }
            }

            /// <summary>
            /// Retrieves the authentication token. Tries the to retrieve the most recently
            /// used credentials if available.
            /// </summary>
            /// <param name="httpProvider">HttpProvider for any web requests needed for authentication</param>
            /// <param name="userName">The login name of the user, if known.</param>
            /// <returns>The authentication token.</returns>
            public async Task RestoreMostRecentFromCacheOrAuthenticateUserAsync(Microsoft.Graph.IHttpProvider httpProvider, string userName = null)
            {
                var authResult = await this.GetMostRecentAuthenticationResultFromCacheAsync(httpProvider).ConfigureAwait(false);

                if (authResult == null)
                {
                    await this.AuthenticateUserAsync(httpProvider, userName);
                }
                else
                {
                    this.CacheAuthResult(authResult);
                }
            }

            /// <summary>
            /// Retrieves the authentication token. Retrieves the most recently
            /// used credentials if available, without showing the sign in UI if credentials are unavailable.
            /// </summary>
            /// <param name="userName">The login name of the user, if known.</param>
            /// <returns>The authentication token.</returns>
            public async Task<bool> RestoreMostRecentFromCacheAsync(string userName = null)
            {
                using (var httpProvider = new Microsoft.Graph.HttpProvider())
                {
                    return await this.RestoreMostRecentFromCacheAsync(httpProvider, userName).ConfigureAwait(false);
                }
            }

            /// <summary>
            /// Retrieves the authentication token. Retrieves the most recently
            /// used credentials if available, without showing the sign in UI if credentials are unavailable.
            /// </summary>
            /// <param name="httpProvider">HttpProvider for any web requests needed for authentication</param>
            /// <param name="userName">The login name of the user, if known.</param>
            /// <returns>The authentication token.</returns>
            public async Task<bool> RestoreMostRecentFromCacheAsync(Microsoft.Graph.IHttpProvider httpProvider, string userName = null)
            {
                var authResult = await this.GetMostRecentAuthenticationResultFromCacheAsync(httpProvider).ConfigureAwait(false);
                if (authResult != null)
                {
                    this.CacheAuthResult(authResult);
                }
                return authResult != null;
            }

            /// <summary>
            /// Retrieves the authentication token.
            /// </summary>
            /// <param name="userName">The login name of the user, if known.</param>
            /// <returns>The authentication token.</returns>
            public async Task AuthenticateUserAsync(string userName = null)
            {
                using (var httpProvider = new Microsoft.Graph.HttpProvider())
                {
                    await this.AuthenticateUserAsync(httpProvider, userName).ConfigureAwait(false);
                }
            }

            /// <summary>
            /// Retrieves the authentication token.
            /// </summary>
            /// <param name="httpProvider">HttpProvider for any web requests needed for authentication</param>
            /// <param name="userName">The login name of the user, if known.</param>
            /// <returns>The authentication token.</returns>
            public virtual async Task AuthenticateUserAsync(Microsoft.Graph.IHttpProvider httpProvider, string userName = null)
            {
                var authResult = await this.GetAuthenticationResultFromCacheAsync(userName, httpProvider).ConfigureAwait(false);

                if (authResult == null)
                {
                    // Log the user in if we haven't already pulled their credentials from the cache.
                    var code = await this.oAuthHelper.GetAuthorizationCodeAsync(
                        this.clientId,
                        this.returnUrl,
                        this.scopes,
                        this.webAuthenticationUi,
                        userName).ConfigureAwait(false);

                    if (!string.IsNullOrEmpty(code))
                    {
                        authResult = await this.oAuthHelper.RedeemAuthorizationCodeAsync(
                            code,
                            this.clientId,
                            this.clientSecret,
                            this.returnUrl,
                            this.scopes,
                            httpProvider).ConfigureAwait(false);
                    }

                    if (authResult == null || string.IsNullOrEmpty(authResult.AccessToken))
                    {
                        throw new Microsoft.Graph.ServiceException(
                            new Microsoft.Graph.Error
                            {
                                // Code = Microsoft.OneDrive.Sdk.Authentication.OAuthConstants.ErrorCodes.AuthenticationFailure,
                                Code = "authenticationFailure",
                                Message = "Failed to retrieve a valid authentication token for the user."
                            });
                    }
                }

                this.CacheAuthResult(authResult);
            }

            internal async Task<Microsoft.OneDrive.Sdk.Authentication.AccountSession> GetAuthenticationResultFromCacheAsync(string userId, Microsoft.Graph.IHttpProvider httpProvider)
            {
                var accountSession = await this.ProcessCachedAccountSessionAsync(this.CurrentAccountSession, httpProvider).ConfigureAwait(false);

                if (accountSession != null)
                {
                    return accountSession;
                }

                if (string.IsNullOrEmpty(userId) && this.CurrentAccountSession != null)
                {
                    userId = this.CurrentAccountSession.UserId;
                }

                Microsoft.OneDrive.Sdk.Authentication.AccountSession cacheResult = null; // this.CredentialCache.GetResultFromCache(
                //this.clientId,
                //userId);

                var processedResult = await this.ProcessCachedAccountSessionAsync(cacheResult, httpProvider).ConfigureAwait(false);

                if (processedResult == null && cacheResult != null)
                {
                    // na razie NIET: this.CredentialCache.DeleteFromCache(cacheResult);
                    this.CurrentAccountSession = null;

                    return null;
                }

                return processedResult;
            }

            internal async Task<Microsoft.OneDrive.Sdk.Authentication.AccountSession> GetMostRecentAuthenticationResultFromCacheAsync(Microsoft.Graph.IHttpProvider httpProvider)
            {
                Microsoft.OneDrive.Sdk.Authentication.AccountSession cacheResult = null;//na razie NIET this.CredentialCache.GetMostRecentlyUsedResultFromCache();

                var processedResult = await this.ProcessCachedAccountSessionAsync(cacheResult, httpProvider).ConfigureAwait(false);

                if (processedResult == null && cacheResult != null)
                {
                    // na razie NIET:  this.CredentialCache.DeleteFromCache(cacheResult);
                    this.CurrentAccountSession = null;

                    return null;
                }

                return processedResult;
            }

            internal async Task<Microsoft.OneDrive.Sdk.Authentication.AccountSession> ProcessCachedAccountSessionAsync(Microsoft.OneDrive.Sdk.Authentication.AccountSession accountSession)
            {
                using (var httpProvider = new Microsoft.Graph.HttpProvider())
                {
                    var processedAccountSession = await this.ProcessCachedAccountSessionAsync(accountSession, httpProvider).ConfigureAwait(false);
                    return processedAccountSession;
                }
            }

            internal virtual async Task<Microsoft.OneDrive.Sdk.Authentication.AccountSession> ProcessCachedAccountSessionAsync(Microsoft.OneDrive.Sdk.Authentication.AccountSession accountSession, Microsoft.Graph.IHttpProvider httpProvider)
            {
                if (accountSession != null)
                {
                    var shouldRefresh = accountSession.ShouldRefresh;

                    // If we don't have an access token or it's expiring see if we can refresh the access token.
                    if (shouldRefresh && accountSession.CanRefresh)
                    {
                        accountSession = await this.oAuthHelper.RedeemRefreshTokenAsync(
                            accountSession.RefreshToken,
                            this.clientId,
                            this.clientSecret,
                            this.returnUrl,
                            this.scopes,
                            httpProvider).ConfigureAwait(false);

                        if (accountSession != null && !string.IsNullOrEmpty(accountSession.AccessToken))
                        {
                            return accountSession;
                        }
                    }
                    else if (!shouldRefresh)
                    {
                        return accountSession;
                    }
                }

                return null;
            }

        }
    }
#endif
}
