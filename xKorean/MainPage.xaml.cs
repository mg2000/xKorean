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
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.System;
using Windows.System.Profile;
using Windows.UI.Notifications;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
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
        public MainPage()
        {
            this.InitializeComponent();

            if (AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Xbox")
            {
                TitleBlock.FontSize = 30;
            }

            CacheFolderChecked += App_CacheFolderChecked;
            CheckCacheFolder();

            string sv = AnalyticsInfo.VersionInfo.DeviceFamilyVersion;
            ulong v = ulong.Parse(sv);
            ulong v1 = (v & 0xFFFF000000000000L) >> 48;
            ulong v2 = (v & 0x0000FFFF00000000L) >> 32;
            ulong v3 = (v & 0x00000000FFFF0000L) >> 16;
            ulong v4 = v & 0x000000000000FFFFL;

            Debug.WriteLine($"디바이스 정보 {AnalyticsInfo.VersionInfo.DeviceFamily}, {v1}.{v2}.{v3}.{v4}");

            
        }


        private async void CheckCacheFolder()
        {
            var settings = Settings.Instance;
            await settings.Load();

            if (settings.LoadValue("useDolbyAtmos") == "True")
                DolbyAtmosCheckBox.Visibility = Visibility.Visible;

            if (settings.LoadValue("useKeyboardMouse") == "True")
                ConsoleKeyboardMouseCheckBox.Visibility = Visibility.Visible;

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

        public double GamesViewItemHeight
        {
            get
            {
                return GameViewModel.ItemHeight;
            }
        }

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
//#               if DEBUG
//                var response = await httpClient.PostAsync(new Uri("https://xbox-korean-viewer-server-dev.herokuapp.com/last_modified_time"), new HttpStringContent("{}", Windows.Storage.Streams.UnicodeEncoding.Utf8, "application/json"));
//#               else
                var response = await httpClient.PostAsync(new Uri("https://xbox-korean-viewer-server2.herokuapp.com/last_modified_time"), new HttpStringContent("{}", Windows.Storage.Streams.UnicodeEncoding.Utf8, "application/json"));
//#               endif
                
                var str = response.Content.ReadAsStringAsync().GetResults();

                var settingMap = JsonConvert.DeserializeObject<Dictionary<string, string>>(str);

                var downloadedJsonFile = new FileInfo(ApplicationData.Current.LocalFolder.Path + "\\games.json");
                
                var settings = Settings.Instance;
                if (settingMap["lastModifiedTime"] == "")
                {
                    var dialog = new MessageDialog("현재 서버 정보를 최신 정보로 업데이트 중입니다. 잠시 후에 다시 시도해 주십시오.", "데이터 수신 오류");
                    await dialog.ShowAsync();
                    _isRefreshing = false;

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

                var dialog = new MessageDialog("서버에서 한글화 정보를 확인할 수 없습니다. 잠시 후 다시 시도해 주십시오.", "데이터 수신 오류");
                await dialog.ShowAsync();
            }
        }
        private async void UpateJsonData()
        {
            var httpClient = new HttpClient();

            try
            {
//#               if DEBUG
//                var response = await httpClient.PostAsync(new Uri("https://xbox-korean-viewer-server-dev.herokuapp.com/title_list"), new HttpStringContent("{}", Windows.Storage.Streams.UnicodeEncoding.Utf8, "application/json"));
                
//#               else
                var response = await httpClient.PostAsync(new Uri("https://xbox-korean-viewer-server2.herokuapp.com/title_list"), new HttpStringContent("{}", Windows.Storage.Streams.UnicodeEncoding.Utf8, "application/json"));
//#               endif

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

                var dialog = new MessageDialog("서버에서 한글화 정보를 다운로드할 수 없습니다.", "데이터 수신 오류");
                await dialog.ShowAsync();
            }
        }
        private async void ReadGamesFromJson(bool useCacheOnly = false)
        {
            var downloadedJsonFile = new FileInfo(ApplicationData.Current.LocalFolder.Path + "\\games.json");

            StorageFile jsonFile = null;
            if (downloadedJsonFile.Exists && downloadedJsonFile.Length > 0)
            {
                jsonFile = await StorageFile.GetFileFromPathAsync(ApplicationData.Current.LocalFolder.Path + "\\games.json");
            }
            else
            {
                if (useCacheOnly)
                {
                    var dialog = new MessageDialog("오전 1 ~ 8시까지는 서버 점검 시간입니다. 불편을 끼쳐드려 죄송합니다.", "데이터 수신 오류");
                    await dialog.ShowAsync();
                }
                else
                {
                    UpateJsonData();
                    return;
                }
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
            Games = games;

            if (OrderByNameAscendItem.IsChecked)
                Games = Games.OrderBy(g => g.KoreanName).ToList();
            else if (OrderByNameDescendItem.IsChecked)
                Games = Games.OrderByDescending(g => g.KoreanName).ToList();
            else if (OrderByReleaseAscendItem.IsChecked)
                Games = Games.OrderBy(g => g.ReleaseDate).ThenBy(g => g.KoreanName).ToList();
            else
                Games = Games.OrderByDescending(g => g.ReleaseDate).ThenBy(g => g.KoreanName).ToList();

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
        private async void GamesView_ItemClick(object sender, ItemClickEventArgs e)
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
                async void OpenStore()
                {
                    if ((e.ClickedItem as GameViewModel).StoreUri.ToLower().Contains("ko-kr") && !(e.ClickedItem as GameViewModel).StoreUri.Contains("marketplace"))
                    {
                        string baseUri = "ms-windows-store://pdp/?ProductId=" + (e.ClickedItem as GameViewModel).ID;
                        Uri storeUri = new Uri(baseUri);
                        await Launcher.LaunchUriAsync(storeUri);
                    }
                    else
                        await Launcher.LaunchUriAsync(new Uri((e.ClickedItem as GameViewModel).StoreUri.ToLower()));
                }

                var message = (e.ClickedItem as GameViewModel).Message;

                if (AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Xbox")
                {
                    if ((e.ClickedItem as GameViewModel).StoreUri.ToLower().Contains("marketplace"))
                    {
                        if (message != "")
                            message += "\r\n\r\n";

                        message += "이 게임은 360 마켓플레이스에서만 구매할 수 있습니다. PC에서 구매해 주십시오.";
                    }
                    else if (!(e.ClickedItem as GameViewModel).StoreUri.ToLower().Contains("ko-kr"))
                    {
                        if (message != "")
                            message += "\r\n\r\n";

                        message += "이 게임은 한국스토어에서는 구매할 수 없습니다. 지역이 한국으로 선택되어 있다면, 해외로 변경하시거나, PC에서 구매해 주십시오.";
                    }
                }

                if (message != "")
                {
                    var dialog = new MessageDialog(message, "스토어로 이동하시기 전에...");

                    if (AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Xbox")
                    {
                        if (!(e.ClickedItem as GameViewModel).StoreUri.ToLower().Contains("marketplace")) {
                            var okBtn = new UICommand("스토어로 이동");
                            okBtn.Invoked += async command => {
                                string baseUri = "ms-windows-store://pdp/?ProductId=" + (e.ClickedItem as GameViewModel).ID;
                                Uri storeUri = new Uri(baseUri);
                                await Launcher.LaunchUriAsync(storeUri);
                            };
                            dialog.Commands.Add(okBtn);
                        }
                        
                    }
                    else
                    {
                        var okBtn = new UICommand("스토어로 이동");
                        okBtn.Invoked += command => {
                            OpenStore();
                        };
                        dialog.Commands.Add(okBtn);

                        if (AnalyticsInfo.VersionInfo.DeviceFamily != "Windows.Xbox")
                        {
                            var linkIdx = message.IndexOf("http");
                            if (linkIdx >= 0)
                            {
                                var linkBtn = new UICommand("링크로 이동");
                                linkBtn.Invoked += async command =>
                                {
                                    await Windows.System.Launcher.LaunchUriAsync(new Uri(message.Substring(linkIdx)));
                                };
                                dialog.Commands.Add(linkBtn);
                            }
                        }
                    }


                    await dialog.ShowAsync();
                }
                else
                    OpenStore();
            }
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

            var gamesFilteredByDolbyAtmos = FilterByDolbyAtmos(gamesFilteredByDiscount);
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
                             where g.KoreanName.ToLower().Contains(text.ToLower().Trim())
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
                        GamesViewModel.Add(new GameViewModel(g));
                    }
                //}

                TitleBlock.Text = $"한국어화 타이틀 목록 ({games.Length}개)";

            }
            else
            {
                GamesViewModel.Clear();
                foreach (var game in Games)
                {
                    GamesViewModel.Add(new GameViewModel(game));
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

        private Game[] FilterByDolbyAtmos(Game[] gamesFilteredByDiscount)
        {
            Game[] filteredGames = gamesFilteredByDiscount;

            if (DolbyAtmosCheckBox != null && (bool)DolbyAtmosCheckBox.IsChecked)
                filteredGames = (from g in gamesFilteredByDiscount where g.DolbyAtmos == "O" select g).ToArray();

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
            Games = Games.OrderBy(g => g.KoreanName).ToList();

            SearchBox_TextChanged(SearchBox, null);

            await Settings.Instance.SetValue("orderType", "name_asc");
        }

        private async void OrderByNameDescendItem_Click(object sender, RoutedEventArgs e)
        {
            Games = Games.OrderByDescending(g => g.KoreanName).ToList();

            SearchBox_TextChanged(SearchBox, null);

            await Settings.Instance.SetValue("orderType", "name_desc");
        }

        private async void OrderByReleaseAscendItem_Click(object sender, RoutedEventArgs e)
        {
            Games = Games.OrderBy(g => g.ReleaseDate).ThenBy(g => g.KoreanName).ToList();

            SearchBox_TextChanged(SearchBox, null);

            await Settings.Instance.SetValue("orderType", "release_asc");
        }

        private async void OrderByReleaseDescendItem_Click(object sender, RoutedEventArgs e)
        {
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
            _isRefreshing = true;
            CheckUpdateTime();
            while (_isRefreshing)
            {
                await RefreshButtonIcon.Rotate(value: _angle, centerX: 10.0f, centerY: 10.0f, duration: 1000, delay: 0, easingType: EasingType.Default).StartAsync();
                _angle += 360;
            }
        }

        //private void InVaultTimeRadioButtons_SelectionChanged(object sender, SelectionChangedEventArgs e)
        //{
        //    SearchBox_TextChanged(SearchBox, null);
        //}

        private void TimingRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            SearchBox_TextChanged(SearchBox, null);
        }

        private void InVaultTimeRadioButtons_GotFocus(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("InVaultTimeRadioButtons_GotFocus");
        }

        private void InVaultTimeRadioButtons_ItemClick(object sender, ItemClickEventArgs e)
        {
            var item = e.ClickedItem as RadioButton;

            if (item != null)
            {
                item.IsChecked = true;
            }

        }

        private void CategoriesView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var item = e.ClickedItem as CheckBox;

            if (item != null)
            {
                item.IsChecked = !(item.IsChecked);
            }
        }

        private void StackPanel_GotFocus(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("StackPanel_GotFocus");

            if (!OrderBar.IsOpen)
            {
                OrderBar.Focus(FocusState.Programmatic);
                OrderBar.IsOpen = true;
            }
        }

        private void CategoryOGCheckBox_GotFocus(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("포커스가 왔다리");
        }

        private async void SettingButton_ClickAsync(object sender, RoutedEventArgs e)
        {
            var result = await new ExtraFilterDialog().ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                var settings = Settings.Instance;
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
            }
        }
    }
}
