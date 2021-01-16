using Microsoft.Toolkit.Uwp.Notifications;
using Microsoft.Toolkit.Uwp.UI.Animations;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using Windows.Security.ExchangeActiveSyncProvisioning;
using Windows.Storage;
using Windows.System;
using Windows.System.Profile;
using Windows.UI.Notifications;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;
using Windows.Web.Http;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace xKorean
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private string mIconSize = "Normal";
        private string mGameNameDisplayLanguage = "Korean";

        private Dictionary<string, string> mMessageTemplateMap = new Dictionary<string, string>();
        private List<Game> mGameList = new List<Game>();
        public MainPage()
        {
            this.InitializeComponent();

            mMessageTemplateMap["remaster"] = "이 게임의 리마스터가 출시되었습니다: [name]";
            mMessageTemplateMap["onetitle"] = "이 게임의 엑스박스 원 버전이 출시되었습니다.";
            mMessageTemplateMap["collection"] = "이 게임의 합본이 출시되었습니다: [name]";
            mMessageTemplateMap["packageonly"] = "패키지 버전만 한국어를 지원합니다.";
            mMessageTemplateMap["merge"] = "이 게임은 새로운 에디션에 통합되어 더 이상 판매되지 않습니다: [name]";
            mMessageTemplateMap["usermode"] = "이 게임은 유저 모드를 설치하셔야 한국어가 지원됩니다.";
            mMessageTemplateMap["required"] = "이 게임은 다음 항목이 설치되어 있어야 이용할 수 있습니다: [name]";
            mMessageTemplateMap["menuonly"] = "이 게임은 메뉴만 한국어로 되어 있습니다.";
            mMessageTemplateMap["hasPrimary"] = "이 게임의 기본 에디션이 게임패스를 지원합니다";

            if (AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Xbox")
            {
                TitleBlock.FontSize = 30;

                mMessageTemplateMap["noRegion"] = "다음 지역으로 변경하신 후 스토어에서 구매하실 수 있습니다: [name]";
                mMessageTemplateMap["dlregiononly"] = "다음 지역으로 변경하신 후 스토어에서 다운로드하셔야 한국어가 지원됩니다: [name]";
                mMessageTemplateMap["360market"] = "360 마켓플레이스를 통해서만 구매하실 수 있습니다. 웹 브라우저를 이용해서 구매해 주십시오.";
            }
            else
            {
                mMessageTemplateMap["360market"] = "360 마켓플레이스를 통해서만 구매하실 수 있습니다.";
                mMessageTemplateMap["dlregiononly"] = "다음 지역의 스토어에서 다운로드 받아야 한국어가 지원됩니다: [name]";
            }

            CacheFolderChecked += App_CacheFolderChecked;
            CheckCacheFolder();

            EasClientDeviceInformation eas = new EasClientDeviceInformation();

            Debug.WriteLine($"디바이스 정보 {eas.SystemManufacturer}, {eas.SystemProductName}");
            Debug.WriteLine($"지역 정보: {Windows.System.UserProfile.GlobalizationPreferences.HomeGeographicRegion}");
            
        }


        private async void CheckCacheFolder()
        {
            var settings = Settings.Instance;
            await settings.Load();

            if (settings.LoadValue("usePlayAnywhere") == "True")
                PlayAnywhereCheckBox.Visibility = Visibility.Visible;

            if (settings.LoadValue("useDolbyAtmos") == "True")
                DolbyAtmosCheckBox.Visibility = Visibility.Visible;

            if (settings.LoadValue("useKeyboardMouse") == "True")
                ConsoleKeyboardMouseCheckBox.Visibility = Visibility.Visible;

            mIconSize = settings.LoadValue("iconSize");
            UpdateItemHeight();

            mGameNameDisplayLanguage = settings.LoadValue("gameNameDisplayLanguage");
            
            var orderType = settings.LoadValue("orderType");
            switch (orderType)
            {
                case "":
                case "name_asc":
                    OrderByNameAscendItem.IsChecked = true;
                    break;
                case "name_desc":
                    OrderByNameDescendItem.IsChecked = true;
                    break;
                case "release_asc":
                    OrderByReleaseAscendItem.IsChecked = true;
                    break;
                case "release_desc":
                    OrderByReleaseDescendItem.IsChecked = true;
                    break;

            }

            var applicationFolder = ApplicationData.Current.LocalFolder;
            App.CacheFolder = (await ApplicationData.Current.LocalFolder.TryGetItemAsync("ThumbnailCache")) as StorageFolder;
            if (App.CacheFolder == null)
            {
                App.CacheFolder = await applicationFolder.CreateFolderAsync("ThumbnailCache");
            }

            var setting = (await ApplicationData.Current.LocalFolder.TryGetItemAsync("ThumbnailCache")) as StorageFolder;

            CacheFolderChecked?.Invoke(null, null);
        }

        private event EventHandler CacheFolderChecked;
        private void App_CacheFolderChecked(object sender, EventArgs e)
        {
            CheckUpdateTime();
        }

        //public double GamesViewItemHeight
        //{
        //    get
        //    {
        //        return GameViewModel.ItemHeight;
        //    }
        //}

        private async void CheckUpdateTime()
        {
            var now = DateTime.Now;

            //if (1 <= now.Hour && now.Hour <= 8)
            //{
            //    ReadGamesFromJson(true);
            //    return;
            //}


            var httpClient = new HttpClient();

            try
            {
#               if DEBUG
                var response = await httpClient.PostAsync(new Uri("http://192.168.200.105:3000/last_modified_time"), new HttpStringContent("{}", Windows.Storage.Streams.UnicodeEncoding.Utf8, "application/json"));
#               else
                var response = await httpClient.PostAsync(new Uri("https://xbox-korean-viewer-server2.herokuapp.com/last_modified_time"), new HttpStringContent("{}", Windows.Storage.Streams.UnicodeEncoding.Utf8, "application/json"));
#               endif
                
                var str = response.Content.ReadAsStringAsync().GetResults();

                var settingMap = JsonConvert.DeserializeObject<Dictionary<string, string>>(str);

                var downloadedJsonFile = new FileInfo(ApplicationData.Current.LocalFolder.Path + "\\games.json");
                
                var settings = Settings.Instance;
                if (settingMap["lastModifiedTime"] == "")
                {
                    _isRefreshing = false;

                    if (LoadingPanel.Visibility == Visibility.Visible)
                        LoadingPanel.Visibility = Visibility.Collapsed;

                    var dialog = new MessageDialog("현재 서버 정보를 최신 정보로 업데이트 중입니다. 잠시 후에 다시 시도해 주십시오.", "데이터 수신 오류");
                    await dialog.ShowAsync();

                    return;
                }
                else if (settings.LoadValue("lastModifiedTime") != settingMap["lastModifiedTime"] || !downloadedJsonFile.Exists)
                {
                    await settings.SetValue("lastModifiedTime", settingMap["lastModifiedTime"]);

                    UpateJsonData();
                }
                else
                {
                    var content = new ToastContentBuilder()
                        .AddText("업데이트 완료", hintMaxLines: 1)
                        .AddText("이미 모든 정보가 최신입니다.")
                        .GetToastContent();

                    var notif = new ToastNotification(content.GetXml());

                    // And show it!
                    ToastNotificationManager.History.Clear();
                    ToastNotificationManager.CreateToastNotifier().Show(notif);
                    _isRefreshing = false;

                    ReadGamesFromJson();
                }
            }
            catch (Exception exception)
            {
                System.Diagnostics.Debug.WriteLine($"다운로드 에러: {exception.Message}");
                _isRefreshing = false;

                if (LoadingPanel.Visibility == Visibility.Visible)
                    LoadingPanel.Visibility = Visibility.Collapsed;

                var dialog = new MessageDialog("서버에서 한글화 정보를 확인할 수 없습니다. 잠시 후 다시 시도해 주십시오.", "데이터 수신 오류");
                await dialog.ShowAsync();
            }
        }

        private async void LoadMessageTemplate()
        {
            var downloadedJsonFile = new FileInfo(ApplicationData.Current.LocalFolder.Path + "\\messageTemplate.json");

            StorageFile templateFile = null;
            if (downloadedJsonFile.Exists && downloadedJsonFile.Length > 0)
            {
                templateFile = await StorageFile.GetFileFromPathAsync(ApplicationData.Current.LocalFolder.Path + "\\messageTemplate.json");
            }
            else
            {
                UpdateMessageTemplate();
                return;
            }

            var templateStr = await FileIO.ReadTextAsync(templateFile);
            var messageTemplateList = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(templateStr);

            mMessageTemplateMap.Clear();
            messageTemplateList.ForEach(item =>
            {
                mMessageTemplateMap.Add(item["code"], item["template"]);
            });

            ReadGamesFromJson();
        }

        private async void UpdateMessageTemplate()
        {
            var httpClient = new HttpClient();

            try
            {
#               if DEBUG
                var response = await httpClient.PostAsync(new Uri("http://192.168.200.105:3000/get_message_template"), new HttpStringContent("{}", Windows.Storage.Streams.UnicodeEncoding.Utf8, "application/json"));
#else
                var response = await httpClient.PostAsync(new Uri("https://xbox-korean-viewer-server2.herokuapp.com/get_message_template"), new HttpStringContent("{}", Windows.Storage.Streams.UnicodeEncoding.Utf8, "application/json"));
#endif

                var str = response.Content.ReadAsStringAsync().GetResults();

                var file = await ApplicationData.Current.LocalFolder.CreateFileAsync("messageTemplate.json", CreationCollisionOption.ReplaceExisting);

                await FileIO.WriteTextAsync(file, str);

                LoadMessageTemplate();
            }
            catch (Exception exception)
            {
                System.Diagnostics.Debug.WriteLine($"다운로드 에러: {exception.Message}");
                _isRefreshing = false;

                if (LoadingPanel.Visibility == Visibility.Visible)
                    LoadingPanel.Visibility = Visibility.Collapsed;

                var dialog = new MessageDialog("서버에서 데이터를 수신할 수 없습니다. 잠시 후 다시 시도해 주십시오.", "데이터 수신 오류");
                await dialog.ShowAsync();
            }
        }

        private async void UpateJsonData()
        {
            var httpClient = new HttpClient();

            try
            {
#               if DEBUG
                var response = await httpClient.PostAsync(new Uri("http://192.168.200.105:3000/title_list"), new HttpStringContent("{}", Windows.Storage.Streams.UnicodeEncoding.Utf8, "application/json"));
                
#               else
                var response = await httpClient.PostAsync(new Uri("https://xbox-korean-viewer-server2.herokuapp.com/title_list"), new HttpStringContent("{}", Windows.Storage.Streams.UnicodeEncoding.Utf8, "application/json"));
#               endif

                var str = response.Content.ReadAsStringAsync().GetResults();

                if (str == "[]")
                {
                    var dialog = new MessageDialog("서버에서 한글화 정보를 업데이트 중입니다. 잠시후에 다시 시도해 주십시오.", "정보 업데이트 중");
                    await dialog.ShowAsync();
                }
                else
                {
                    var file = await ApplicationData.Current.LocalFolder.CreateFileAsync("games.json", CreationCollisionOption.ReplaceExisting);

                    await FileIO.WriteTextAsync(file, str);

                    ReadGamesFromJson();
                }
            }
            catch (Exception exception)
            {
                System.Diagnostics.Debug.WriteLine($"다운로드 에러: {exception.Message}");
                _isRefreshing = false;

                if (LoadingPanel.Visibility == Visibility.Visible)
                    LoadingPanel.Visibility = Visibility.Collapsed;

                var dialog = new MessageDialog("서버에서 한글화 정보를 다운로드할 수 없습니다.", "데이터 수신 오류");
                await dialog.ShowAsync();
            }
        }
        private async void ReadGamesFromJson()
        {
            var downloadedJsonFile = new FileInfo(ApplicationData.Current.LocalFolder.Path + "\\games.json");

            StorageFile jsonFile = null;
            if (downloadedJsonFile.Exists && downloadedJsonFile.Length > 0)
            {
                jsonFile = await StorageFile.GetFileFromPathAsync(ApplicationData.Current.LocalFolder.Path + "\\games.json");
            }
            else
            {
                UpateJsonData();
                return;
            }


            var jsonString = await FileIO.ReadTextAsync(jsonFile);
            var games = JsonConvert.DeserializeObject<List<Game>>(jsonString);

            HashSet<string> genre = new HashSet<string>();

            //foreach (var game in games)
            //{
            //    foreach (var c in game.Categories)
            //    {
            //        genre.Add(c);
            //    }

            //    GamesViewModel.Add(new GameViewModel(game));
            //    if (game.MetaScore.Count == 0)
            //    {
            //        game.MetaScore.Add(Platform.Unknown, -1);
            //    }

            //}
            //List<string> orderedGenre = genre.OrderBy(g => g).ToList();
            //Categories.Clear();
            //foreach (var g in orderedGenre)
            //{
            //    Categories.Add(new CategorieViewModel(g));
            //}

            mGameList.Clear();
            mGameList.AddRange(games);

            Games = games;

            if (OrderByNameAscendItem.IsChecked)
            {
                if (mGameNameDisplayLanguage == "English")
                    Games = Games.OrderBy(g => g.Name).ToList();
                else
                    Games = Games.OrderBy(g => g.KoreanName).ToList();
            }
            else if (OrderByNameDescendItem.IsChecked)
            {
                if (mGameNameDisplayLanguage == "English")
                    Games = Games.OrderByDescending(g => g.Name).ToList();
                else
                    Games = Games.OrderByDescending(g => g.KoreanName).ToList();
            }
            else if (OrderByReleaseAscendItem.IsChecked)
            {
                if (mGameNameDisplayLanguage == "English")
                    Games = Games.OrderBy(g => g.ReleaseDate).ThenBy(g => g.Name).ToList();
                else
                    Games = Games.OrderBy(g => g.ReleaseDate).ThenBy(g => g.KoreanName).ToList();
            }
            else
            {
                if (mGameNameDisplayLanguage == "English")
                    Games = Games.OrderByDescending(g => g.ReleaseDate).ThenBy(g => g.Name).ToList();
                else
                    Games = Games.OrderByDescending(g => g.ReleaseDate).ThenBy(g => g.KoreanName).ToList();
            }

            LoadingPanel.Visibility = Visibility.Collapsed;

            SearchBox_TextChanged(SearchBox, null);
            _isRefreshing = false;
        }

        public List<Game> Games = new List<Game>();
        public ObservableCollection<GameViewModel> GamesViewModel { get; set; } = new ObservableCollection<GameViewModel>();

        public ObservableCollection<CategorieViewModel> Categories { set; get; } = new ObservableCollection<CategorieViewModel>();
        private void OrderByScoreAscendItem_Click(object sender, RoutedEventArgs e)
        {
            //Games = Games.OrderBy(g => g.MetaScore.First().Value).ToList();
            //SearchBox_TextChanged
            //GamesViewModel.Clear();
            //foreach(var g in Games)
            //{
            //    GamesViewModel.Add(new GameViewModel(g));
            //}
            SearchBox_TextChanged(SearchBox, null);
        }

        private void OrderByScoreDescendItem_Click(object sender, RoutedEventArgs e)
        {
            //Games = Games.OrderByDescending(g => g.MetaScore.First().Value).ToList();

            //GamesViewModel.Clear();
            //foreach (var g in Games)
            //{
            //    GamesViewModel.Add(new GameViewModel(g));
            //}
            SearchBox_TextChanged(SearchBox, null);
        }

        UIElement animatingElement;
        private void GamesView_ItemClick(object sender, ItemClickEventArgs e)
        {
            //var container = GamesView.ContainerFromItem(e.ClickedItem) as GridViewItem;
            //if (container != null)
            //{
            //    //find the image
            //    var root = (FrameworkElement)container.ContentTemplateRoot;
            //    var image = (UIElement)root.FindName("PosterImage");
            //    animatingElement = image;
            //    //prepare the animation
            //    ConnectedAnimationService.GetForCurrentView().PrepareToAnimate("ForwardConnectedAnimation", image);
            //}

            //ConnectedAnimationService.GetForCurrentView().PrepareToAnimate("ForwardConnectedAnimation", sender);

            //Frame.Navigate(typeof(GameDetailPage), e.ClickedItem);

            if (e.ClickedItem != null)
            {
                GoToStore((e.ClickedItem as GameViewModel).Game);
            }
        }

        private async void GoToStore(Game game)
        {
            string dlRegionCode = "";
            string oneStoreUrl = "";
            string store360Url = "";

            async void OpenStore(string storeUrl, string region = "")
            {
                if (AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Xbox")
                {
                    string baseUri = "ms-windows-store://pdp/?ProductId=" + game.ID;
                    Uri storeUri = new Uri(baseUri);
                    await Launcher.LaunchUriAsync(storeUri);
                }
                else
                {
                    if (region != "")
                    {
                        var startTag = "com/";
                        var startRegionIdx = storeUrl.IndexOf(startTag);

                        var endRegionIdx = -1;
                        if (startRegionIdx > 0)
                        {
                            startRegionIdx += startTag.Length;
                            endRegionIdx = storeUrl.IndexOf("/", startRegionIdx);
                        }

                        if (endRegionIdx > 0)
                        {
                            var storeUrlBuilder = new StringBuilder();
                            storeUrlBuilder.Append(storeUrl.Substring(0, startRegionIdx));
                            switch (region.ToLower())
                            {
                                case "kr":
                                    storeUrlBuilder.Append("ko-kr");
                                    break;
                                case "en":
                                    storeUrlBuilder.Append("en-us");
                                    break;
                                case "hk":
                                    storeUrlBuilder.Append("en-hk");
                                    break;
                                case "jp":
                                    storeUrlBuilder.Append("ja-jp");
                                    break;
                            }
                            storeUrlBuilder.Append(storeUrl.Substring(endRegionIdx));

                            storeUrl = storeUrlBuilder.ToString();
                        }
                    }

                    var storeLinkRegion = GetRegionCodeFromUrl(storeUrl).ToLower();

                    if (Windows.System.UserProfile.GlobalizationPreferences.HomeGeographicRegion.ToLower() == storeLinkRegion && !storeUrl.Contains("marketplace"))
                    {
                        string baseUri = "ms-windows-store://pdp/?ProductId=" + game.ID;
                        Uri storeUri = new Uri(baseUri);
                        await Launcher.LaunchUriAsync(storeUri);
                    }
                    else
                        await Launcher.LaunchUriAsync(new Uri(storeUrl));
                }
            }

            Game remasterGame = null;
            Game mergeGame = null;
            Game collectionGame = null;
            Game requiredGame = null;

            var messageArr = game.Message.Split("\n");
            var messageBuilder = new StringBuilder();

            if (AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Xbox")
            {
                var storeRegion = GetRegionCodeFromUrl(game.StoreLink);

                if (storeRegion.ToLower() != Windows.System.UserProfile.GlobalizationPreferences.HomeGeographicRegion.ToLower())
                {
                    var template = mMessageTemplateMap["noRegion"];
                    messageBuilder.Append("* ").Append(template.Replace("[name]", ConvertCodeToStr(storeRegion)));

                    if (game.Message.Trim() != "" || game.StoreLink.Contains("marketplace") || game.HasPrimary != "")
                        messageBuilder.Append("\r\n");
                }

                if (game.StoreLink.Contains("marketplace"))
                {
                    messageBuilder.Append("* ").Append(mMessageTemplateMap["360market"]);

                    if (game.Message.Trim() != "" || game.HasPrimary != "")
                        messageBuilder.Append("\r\n");
                }
            }

            if (game.HasPrimary != "")
            {
                messageBuilder.Append("* ").Append(mMessageTemplateMap["hasPrimary"]);

                if (game.Message.Trim() != "")
                    messageBuilder.Append("\r\n");
            }

            for (var i = 0; i < messageArr.Length; i++)
            {
                var parsePart = messageArr[i].Split("=");
                var code = parsePart[0].Trim().ToLower();
                if (mMessageTemplateMap.ContainsKey(code))
                {
                    var message = mMessageTemplateMap[code];
                    if (message.Contains("[name]") && parsePart.Length > 1)
                    {
                        var strValue = "";
                        switch (code)
                        {
                            case "dlregiononly":
                                if (Windows.System.UserProfile.GlobalizationPreferences.HomeGeographicRegion.ToLower() != parsePart[1].ToLower())
                                {
                                    strValue = ConvertCodeToStr(parsePart[1]);
                                    dlRegionCode = parsePart[1];
                                }
                                break;
                            case "required":
                                var requiredID = GetIDFromStoreUrl(parsePart[1]);
                                requiredGame = mGameList.FirstOrDefault(item => item.ID == requiredID);
                                if (requiredGame != null)
                                {
                                    if (mGameNameDisplayLanguage == "English")
                                        strValue = requiredGame.Name;
                                    else
                                        strValue = requiredGame.KoreanName;
                                }
                                break;
                            case "remaster":
                                var remasterID = GetIDFromStoreUrl(parsePart[1]);
                                remasterGame = mGameList.FirstOrDefault(item => item.ID == remasterID);
                                if (remasterGame != null)
                                {
                                    if (mGameNameDisplayLanguage == "English")
                                        strValue = remasterGame.Name;
                                    else
                                        strValue = remasterGame.KoreanName;
                                }
                                break;
                            case "collection":
                                var collectionID = GetIDFromStoreUrl(parsePart[1]);
                                collectionGame = mGameList.FirstOrDefault(item => item.ID == collectionID);
                                if (collectionGame != null)
                                {
                                    if (mGameNameDisplayLanguage == "English")
                                        strValue = collectionGame.Name;
                                    else
                                        strValue = collectionGame.KoreanName;
                                }
                                break;
                            case "merge":
                                var mergeID = GetIDFromStoreUrl(parsePart[1]);
                                mergeGame = mGameList.FirstOrDefault(item => item.ID == mergeID);
                                if (mergeGame != null)
                                {
                                    if (mGameNameDisplayLanguage == "English")
                                        strValue = mergeGame.Name;
                                    else
                                        strValue = mergeGame.KoreanName;
                                }
                                break;
                            default:
                                strValue = parsePart[1];
                                break;
                        }



                        message = message.Replace("[name]", strValue);
                    }

                    if ((code == "dlregiononly" && Windows.System.UserProfile.GlobalizationPreferences.HomeGeographicRegion.ToLower() != parsePart[1].ToLower()) || code != "dlregiononly")
                        messageBuilder.Append("* ").Append(message);
                }
                else if (code != "")
                    messageBuilder.Append("* ").Append(parsePart[0]);

                if (i < messageArr.Length - 1)
                    messageBuilder.Append("\r\n");

                if (code == "onetitle" && parsePart.Length > 1)
                    oneStoreUrl = parsePart[1];
                else if (code == "360market" && parsePart.Length > 1)
                    store360Url = parsePart[1];

            }

            if (messageBuilder.Length > 0)
            {
                Game oneGame = null;
                var dlRegionName = "";

                if (oneStoreUrl != "")
                {
                    var oneVerionID = GetIDFromStoreUrl(oneStoreUrl);
                    oneGame = mGameList.FirstOrDefault(item => item.ID == oneVerionID);
                }

                var messageDialog = new StoreInfoDialog(messageBuilder.ToString(), !game.StoreLink.Contains("marketplace"), requiredGame != null, oneGame != null, remasterGame != null, mergeGame != null, collectionGame != null, game.HasPrimary != "", store360Url != "", dlRegionName);
                await messageDialog.ShowAsync();

                switch(messageDialog.ChooseItem)
                {
                    case "store":
                        OpenStore(game.StoreLink);
                        break;
                    case "required":
                        GoToStore(requiredGame);
                        break;
                    case "oneStore":
                        GoToStore(oneGame);
                        break;
                    case "DLstore":
                        OpenStore(game.StoreLink, dlRegionCode);
                        break;
                    case "merge":
                        GoToStore(mergeGame);
                        break;
                    case "collection":
                        GoToStore(collectionGame);
                        break;
                    case "remaster":
                        GoToStore(remasterGame);
                        break;
                    case "defaultEdition":
                        string baseUri = "ms-windows-store://pdp/?ProductId=" + game.HasPrimary;
                        Uri storeUri = new Uri(baseUri);
                        await Launcher.LaunchUriAsync(storeUri);
                        break;
                    case "360market":
                        OpenStore(store360Url);
                        break;
                }
            }
            else
                OpenStore(game.StoreLink);
        }

        private string GetIDFromStoreUrl(string storeUrl)
        {
            var startStoreIdx = storeUrl.LastIndexOf('/');
            if (startStoreIdx > 0)
            {
                startStoreIdx++;

                char[] startTagList = { '?', '#' };
                var endStoreIdx = 99999;
                for (var i = 0; i < startTagList.Length; i++)
                {
                    var temp = storeUrl.IndexOf(startTagList[i]);
                    if (0 < temp && temp < endStoreIdx)
                        endStoreIdx = temp;
                }

                if (endStoreIdx == 99999)
                    return "";
                else
                    return storeUrl.Substring(startStoreIdx, endStoreIdx - startStoreIdx).ToUpper();
            }
            else
                return "";
        }

        private string ConvertCodeToStr(string regionCode)
        {
            switch (regionCode.ToLower())
            {
                case "kr":
                    return "한국";
                case "us":
                    return "미국";
                case "jp":
                    return "일본";
                case "hk":
                    return "홍콩";
                default:
                    return "";
            }
        }

        private string GetRegionCodeFromUrl(string storeUrl)
        {
            var startIdx = storeUrl.IndexOf("com/");
            if (startIdx > 0)
                startIdx = storeUrl.IndexOf("-", startIdx);

            var endIdx = -1;
            if (startIdx > 0)
            {
                startIdx++;
                endIdx = storeUrl.IndexOf("/", startIdx);
            }

            if (endIdx > 0)
                return storeUrl.Substring(startIdx, endIdx - startIdx);
            else
                return "";
        }

        private string GetStoreUrlFromRegionCode(string storeUrl, string regionCode)
        {
            var startTag = "com/";
            var startRegionIdx = storeUrl.IndexOf(startTag);

            var endRegionIdx = -1;
            if (startRegionIdx > 0)
            {
                startRegionIdx += startTag.Length;
                endRegionIdx = storeUrl.IndexOf("/", startRegionIdx);
            }

            if (endRegionIdx > 0)
            {
                var storeUrlBuilder = new StringBuilder();
                storeUrlBuilder.Append(storeUrl.Substring(0, startRegionIdx));
                switch (regionCode.ToLower())
                {
                    case "kr":
                        storeUrlBuilder.Append("ko-kr");
                        break;
                    case "en":
                        storeUrlBuilder.Append("en-us");
                        break;
                    case "hk":
                        storeUrlBuilder.Append("en-hk");
                        break;
                    case "jp":
                        storeUrlBuilder.Append("ja-jp");
                        break;
                }
                storeUrlBuilder.Append(storeUrl.Substring(endRegionIdx));

                return storeUrlBuilder.ToString();
            }
            else
                return "";
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            switch (AnalyticsInfo.VersionInfo.DeviceFamily)
            {
                case "Windows.Xbox":
                    GamesView.DesiredWidth = 160;
                    GamesView.ItemHeight = 240;
                    break;
            }


            if (animatingElement != null)
            {
                var anim = ConnectedAnimationService.GetForCurrentView().GetAnimation("BackwardConnectedAnimation");
                if (anim != null)
                {
                    anim.TryStart(animatingElement);
                }
                animatingElement = null;
            }
        }

        private async void AboutButton_ClickAsync(object sender, RoutedEventArgs e)
        {
            //switch (AnalyticsInfo.VersionInfo.DeviceFamily)
            //{
            //    case "Windows.Desktop":
            //        AboutTip.IsOpen = true;
            //        break;
            //}
            var aboutDialogue = new AboutDialog();
            var result = await aboutDialogue.ShowAsync();

        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            //SearchBox.TextChanged += SearchBox_TextChanged;
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox searchBlock = sender as TextBox;
            string text = searchBlock.Text;
            var gamesFilteredByCategories = FilterByCategorie(Games.ToArray());
            if (gamesFilteredByCategories == null)
            {
                gamesFilteredByCategories = Games.ToArray();
            }
            var gamesFilteredByTiming = FilterByTiming(gamesFilteredByCategories);
            if (gamesFilteredByTiming == null)
            {
                gamesFilteredByTiming = Games.ToArray();
            }
            var gamesFilteredByGamePass = FilterByGamePass(gamesFilteredByTiming);
            if (gamesFilteredByGamePass == null)
            {
                gamesFilteredByGamePass = Games.ToArray();
            }
            var gamesFilteredByDiscount = FilterByDiscount(gamesFilteredByGamePass);
            if (gamesFilteredByDiscount == null)
            {
                gamesFilteredByDiscount = Games.ToArray();
            }

            var gamesFilteredByPlayAnywhere = FilterByPlayAnywhere(gamesFilteredByDiscount);
            if (gamesFilteredByPlayAnywhere == null)
            {
                gamesFilteredByPlayAnywhere = Games.ToArray();
            }

            var gamesFilteredByDolbyAtmos = FilterByDolbyAtmos(gamesFilteredByPlayAnywhere);
            if (gamesFilteredByDolbyAtmos == null)
            {
                gamesFilteredByDolbyAtmos = Games.ToArray();
            }

            var gamesFilteredByUseKeyboardMouse = FilterByKeyboardMouse(gamesFilteredByDolbyAtmos);
            if (gamesFilteredByUseKeyboardMouse == null)
            {
                gamesFilteredByUseKeyboardMouse = Games.ToArray();
            }

            if (text.Trim() != string.Empty || gamesFilteredByUseKeyboardMouse != null)
            {
                if (gamesFilteredByUseKeyboardMouse == null)
                {
                    gamesFilteredByUseKeyboardMouse = Games.ToArray();
                }
                var games = (from g in gamesFilteredByUseKeyboardMouse
                             where g.KoreanName.ToLower().Contains(text.ToLower().Trim()) || g.Name.ToLower().Contains(text.ToLower().Trim())
                             select g).ToArray();


                //bool isViewModelChanged = false;
                //if (games.Length == GamesViewModel.Count)
                //{
                //    for (int i = 0; i < games.Length; ++i)
                //    {
                //        if (games[i].ID != GamesViewModel[i].ID)
                //        {
                //            isViewModelChanged = true;
                //            break;
                //        }
                //    }
                //}
                //else
                //{
                //    isViewModelChanged = true;
                //}

                //if (isViewModelChanged)
                //{
                    GamesViewModel.Clear();
                    foreach (var g in games)
                    {
                        GamesViewModel.Add(new GameViewModel(g, mGameNameDisplayLanguage, mIconSize));
                    }
                //}

                TitleBlock.Text = $"한국어화 타이틀 목록 ({games.Length}개)";

            }
            else
            {
                GamesViewModel.Clear();
                foreach (var game in Games)
                {
                    GamesViewModel.Add(new GameViewModel(game, mGameNameDisplayLanguage, mIconSize));
                }

                TitleBlock.Text = $"한국어화 타이틀 목록 ({Games.Count}개)";
            }
        }

        private Game[] FilterByTiming(Game[] gamesFilteredByCategories)
        {
            Game[] filteredGames = gamesFilteredByCategories;

            if (KoreanVoiceRadioButton != null && (bool)KoreanVoiceRadioButton.IsChecked)
            {
                filteredGames = (from g in gamesFilteredByCategories where g.Localize.Contains("음성") select g).ToArray();
            }
            else if (KoreanSubtitleRadioButton != null && (bool)KoreanSubtitleRadioButton.IsChecked)
            {
                filteredGames = (from g in gamesFilteredByCategories where g.Localize.Contains("자막") select g).ToArray();
            }

            return filteredGames;
        }

        private Game[] FilterByGamePass(Game[] gamesFilteredByTiming)
        {
            Game[] filteredGames = gamesFilteredByTiming;

            if (GamePassCheckBox != null && (bool)GamePassCheckBox.IsChecked)
                filteredGames = (from g in gamesFilteredByTiming where g.GamePassCloud == "O" || g.GamePassPC == "O" || g.GamePassConsole == "O" select g).ToArray();

            return filteredGames;
        }

        private Game[] FilterByDiscount(Game[] gamesFilteredByGamePass)
        {
            Game[] filteredGames = gamesFilteredByGamePass;

            if (DiscountCheckBox != null && (bool)DiscountCheckBox.IsChecked)
                filteredGames = (from g in gamesFilteredByGamePass where g.Discount != "" && !g.Discount.Contains("출시") select g).ToArray();

            return filteredGames;
        }

        private Game[] FilterByPlayAnywhere(Game[] gamesFilteredByDiscount)
        {
            Game[] filteredGames = gamesFilteredByDiscount;

            if (PlayAnywhereCheckBox != null && (bool)PlayAnywhereCheckBox.IsChecked)
                filteredGames = (from g in gamesFilteredByDiscount where g.PlayAnywhere == "O" select g).ToArray();

            return filteredGames;
        }

        private Game[] FilterByDolbyAtmos(Game[] gamesFilteredByPlayAnywhere)
        {
            Game[] filteredGames = gamesFilteredByPlayAnywhere;

            if (DolbyAtmosCheckBox != null && (bool)DolbyAtmosCheckBox.IsChecked)
                filteredGames = (from g in gamesFilteredByPlayAnywhere where g.DolbyAtmos == "O" select g).ToArray();

            return filteredGames;
        }

        private Game[] FilterByKeyboardMouse(Game[] gamesFilteredByDolbyAtmos)
        {
            Game[] filteredGames = gamesFilteredByDolbyAtmos;

            if (ConsoleKeyboardMouseCheckBox != null && (bool)ConsoleKeyboardMouseCheckBox.IsChecked)
                filteredGames = (from g in gamesFilteredByDolbyAtmos where g.ConsoleKeyboardMouse == "O" select g).ToArray();

            return filteredGames;
        }

        private void PosterImage_ImageOpened(object sender, RoutedEventArgs e)
        {
            Image image = sender as Image;

            if (image != null && image.Tag != null)
            {
                var match = (from g in GamesViewModel where g.ID == image.Tag.ToString() select g).ToList();
                if (match.Count != 0)
                {
                    match.First().IsImageLoaded = Visibility.Collapsed;
                }
            }
        }

        private void MenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {

        }

        private async void OrderByNameAscendItem_Click(object sender, RoutedEventArgs e)
        {
            if (mGameNameDisplayLanguage == "English")
                Games = Games.OrderBy(g => g.Name).ToList();
            else
                Games = Games.OrderBy(g => g.KoreanName).ToList();

            SearchBox_TextChanged(SearchBox, null);

            await Settings.Instance.SetValue("orderType", "name_asc");
        }

        private async void OrderByNameDescendItem_Click(object sender, RoutedEventArgs e)
        {
            if (mGameNameDisplayLanguage == "English")
                Games = Games.OrderByDescending(g => g.Name).ToList();
            else
                Games = Games.OrderByDescending(g => g.KoreanName).ToList();

            SearchBox_TextChanged(SearchBox, null);

            await Settings.Instance.SetValue("orderType", "name_desc");
        }

        private async void OrderByReleaseAscendItem_Click(object sender, RoutedEventArgs e)
        {
            if (mGameNameDisplayLanguage == "English")
                Games = Games.OrderBy(g => g.ReleaseDate).ThenBy(g => g.Name).ToList();
            else
                Games = Games.OrderBy(g => g.ReleaseDate).ThenBy(g => g.KoreanName).ToList();

            SearchBox_TextChanged(SearchBox, null);

            await Settings.Instance.SetValue("orderType", "release_asc");
        }

        private async void OrderByReleaseDescendItem_Click(object sender, RoutedEventArgs e)
        {
            if (mGameNameDisplayLanguage == "English")
                Games = Games.OrderBy(g => g.ReleaseDate).ThenBy(g => g.Name).ToList();
            else
                Games = Games.OrderByDescending(g => g.ReleaseDate).ThenBy(g => g.KoreanName).ToList();

            SearchBox_TextChanged(SearchBox, null);

            await Settings.Instance.SetValue("orderType", "release_desc");
        }

        /// <summary>
        /// 通过类别筛选
        /// </summary>
        /// <returns>若为null就是不用筛选，如果要筛选且一个都没有会返回一个空数组的！</returns>
        private Game[] FilterByCategorie(Game[] games)
        {
            //Game[] selectedGames = null;
            var selectGamesList = new List<Game>();

            HashSet<string> checkedcategories = new HashSet<string>();

            selectGamesList.AddRange((from g in games
                                      where ((bool)CategorySeriesXSCheckBox.IsChecked && g.SeriesXS == "O") || 
                                      ((bool)CategoryOneXEnhancedCheckBox.IsChecked && g.OneXEnhanced == "O") ||
                                      ((bool)CategoryOneCheckBox.IsChecked && g.OneS == "O") ||
                                      ((bool)CategoryX360CheckBox.IsChecked && g.X360 == "O") ||
                                      ((bool)CategoryOGCheckBox.IsChecked && g.OG == "O")
                                      select g).ToList());

            var isChecked = false;
            if ((bool)CategorySeriesXSCheckBox.IsChecked || (bool)CategoryOneXEnhancedCheckBox.IsChecked || (bool)CategoryOneCheckBox.IsChecked ||
                (bool)CategoryX360CheckBox.IsChecked || (bool)CategoryOGCheckBox.IsChecked)
                isChecked = true;

            if (isChecked == false)
                return (from g in games
                        select g).ToArray();
            else
                return selectGamesList.ToArray();
        }

        private void UpdateItemHeight()
        {
            if (mIconSize == "Small")
            {
                switch (AnalyticsInfo.VersionInfo.DeviceFamily)
                {
                    case "Windows.Xbox":
                        GamesView.ItemHeight = 190;
                        GamesView.Padding = new Thickness(0, 0, 0, 0);
                        GamesView.Margin = new Thickness(0, 5, 0, 0);
                        break;
                    default:
                        GamesView.ItemHeight = 239;
                        GamesView.Padding = new Thickness(20, 0, 20, 0);
                        GamesView.Margin = new Thickness(0, 10, 0, 0);
                        break;
                }
            }
            else
            {
                switch (AnalyticsInfo.VersionInfo.DeviceFamily)
                {
                    case "Windows.Xbox":
                        GamesView.ItemHeight = 205;
                        break;
                    default:
                        GamesView.ItemHeight = 303.75;
                        break;
                }

                GamesView.Padding = new Thickness(20, 0, 20, 0);
                GamesView.Margin = new Thickness(0, 10, 0, 0);
            }
        }

        private void CategorieCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            SearchBox_TextChanged(SearchBox, null);
        }

        private void CategorieCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            SearchBox_TextChanged(SearchBox, null);
        }

        private bool _isRefreshing = false;
        private float _angle = 360;
        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            if (mGameList.Count == 0 && LoadingPanel.Visibility == Visibility.Collapsed)
                LoadingPanel.Visibility = Visibility.Visible;

            _isRefreshing = true;
            CheckUpdateTime();
            while (_isRefreshing)
            {
                await RefreshButtonIcon.Rotate(value: _angle, centerX: 10.0f, centerY: 10.0f, duration: 1000, delay: 0, easingType: EasingType.Default).StartAsync();
                _angle += 360;
            }
        }

        private void TimingRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            SearchBox_TextChanged(SearchBox, null);
        }

        private async void SettingButton_ClickAsync(object sender, RoutedEventArgs e)
        {
            var result = await new ExtraFilterDialog().ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                var settings = Settings.Instance;
                if (settings.LoadValue("usePlayAnywhere") == "True")
                    PlayAnywhereCheckBox.Visibility = Visibility.Visible;
                else
                {
                    PlayAnywhereCheckBox.Visibility = Visibility.Collapsed;
                    PlayAnywhereCheckBox.IsChecked = false;
                }


                if (settings.LoadValue("useDolbyAtmos") == "True")
                    DolbyAtmosCheckBox.Visibility = Visibility.Visible;
                else
                {
                    DolbyAtmosCheckBox.Visibility = Visibility.Collapsed;
                    DolbyAtmosCheckBox.IsChecked = false;
                }

                if (settings.LoadValue("useKeyboardMouse") == "True")
                    ConsoleKeyboardMouseCheckBox.Visibility = Visibility.Visible;
                else
                {
                    ConsoleKeyboardMouseCheckBox.Visibility = Visibility.Collapsed;
                    ConsoleKeyboardMouseCheckBox.IsChecked = false;
                }

                mIconSize = settings.LoadValue("iconSize");
                mGameNameDisplayLanguage = settings.LoadValue("gameNameDisplayLanguage");

                for (int i = 0; i < GamesViewModel.Count; i++)
                {
                    GamesViewModel[i].GameNameDisplayLanguage = mGameNameDisplayLanguage;
                    GamesViewModel[i].IconSize = mIconSize;
                }

                UpdateItemHeight();
            }
        }

        private void SearchBox_GotFocus(object sender, RoutedEventArgs e)
        {
            (sender as TextBox).SelectAll();
        }

        private void Page_KeyDown(object sender, Windows.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            if (e.Handled)
                return;

            switch (e.Key)
            {
                case VirtualKey.GamepadMenu:
                    SearchBox.Focus(FocusState.Programmatic);
                    break;
                case VirtualKey.GamepadView:
                    CategorySeriesXSCheckBox.Focus(FocusState.Programmatic);
                    break;
            }
        }
    }
}
