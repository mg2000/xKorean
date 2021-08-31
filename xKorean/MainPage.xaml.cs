using Microsoft.Toolkit.Uwp.Notifications;
using Microsoft.Toolkit.Uwp.UI.Controls;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Security.Cryptography;
using Windows.Security.ExchangeActiveSyncProvisioning;
using Windows.Services.Store;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System;
using Windows.System.Profile;
using Windows.UI.Core;
using Windows.UI.Notifications;
using Windows.UI.Popups;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Documents;
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
		private string mGameNameDisplayLanguage = "Korean";

		private Dictionary<string, string> mMessageTemplateMap = new Dictionary<string, string>();
		private List<Game> mGameList = new List<Game>();

		private readonly BlockingCollection<object> mDialogQueue = new BlockingCollection<object>(1);
		private readonly BlockingCollection<object> mTipQueue = new BlockingCollection<object>(1);

		private byte[] mOneTitleHeader = null;
		private byte[] mSeriesXSHeader = null;
		private byte[] mWindowsHeader = null;

		private const string windowsTitlePath = "ms-appx:///Assets/windows_title.png";
		private const string oneTitlePath = "ms-appx:///Assets/xbox_one_title.png";
		private const string seriesTitlePath = "ms-appx:///Assets/xbox_series_xs_title.png";

		private List<Game> mExistGames = new List<Game>();
		private List<string> mNewGames = new List<string>();

		private string mEditionLanguage;

		private int mSelectedIdx = 0;

		public MainPage()
		{
			this.InitializeComponent();

			SystemNavigationManager.GetForCurrentView().BackRequested += (sender, e) =>
			{
				if (EditionPanelView.Visibility == Visibility.Visible)
				{
					EditionPanelView.Visibility = Visibility.Collapsed;
					(GamesView.ContainerFromIndex(mSelectedIdx) as GridViewItem).Focus(FocusState.Programmatic);
					e.Handled = true;
				}
				else if (InfoPanelView.Visibility == Visibility.Visible) {
					InfoPanelView.Visibility = Visibility.Collapsed;
					(GamesView.ContainerFromIndex(mSelectedIdx) as GridViewItem).Focus(FocusState.Programmatic);
					e.Handled = true;
				}
			};

			var localSettings = ApplicationData.Current.LocalSettings;

			if (localSettings.Values["seriesXS"] != null)
				CategorySeriesXSCheckBox.IsChecked = (bool)localSettings.Values["seriesXS"];

			if (localSettings.Values["oneXEnhanced"] != null)
				CategoryOneXEnhancedCheckBox.IsChecked = (bool)localSettings.Values["oneXEnhanced"];
			
			if (localSettings.Values["oneS"] != null)
				CategoryOneCheckBox.IsChecked = (bool)localSettings.Values["oneS"];

			if (localSettings.Values["x360"] != null)
				CategoryX360CheckBox.IsChecked = (bool)localSettings.Values["x360"];
			
			if (localSettings.Values["og"] != null)
				CategoryOGCheckBox.IsChecked = (bool)localSettings.Values["og"];

			if (localSettings.Values["windows"] != null)
				CategoryWindowsCheckBox.IsChecked = (bool)localSettings.Values["windows"];

			if (localSettings.Values["cloud"] != null)
				CategoryCloudCheckBox.IsChecked = (bool)localSettings.Values["cloud"];

			mMessageTemplateMap["remaster"] = "이 게임의 리마스터가 출시되었습니다: [name]";
			mMessageTemplateMap["onetitle"] = "이 게임의 엑스박스 원 버전이 출시되었습니다.";
			mMessageTemplateMap["packageonly"] = "패키지 버전만 한국어를 지원합니다.";
			mMessageTemplateMap["usermode"] = "이 게임은 유저 모드를 설치하셔야 한국어가 지원됩니다.";
			mMessageTemplateMap["menuonly"] = "이 게임은 메뉴만 한국어로 되어 있습니다.";

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
			
			EasClientDeviceInformation eas = new EasClientDeviceInformation();

			Debug.WriteLine($"디바이스 정보 {eas.SystemManufacturer}, {eas.SystemProductName}");
			Debug.WriteLine($"지역 정보: {Windows.System.UserProfile.GlobalizationPreferences.HomeGeographicRegion}");

			if (AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Xbox")
				CategoryWindowsCheckBox.Visibility = Visibility.Collapsed;

			LoadTitleImage(oneTitlePath);
		}

		private async void LoadTitleImage(string fileName)
		{
			var oneTitleFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri(fileName));
			using (IRandomAccessStream stream = await oneTitleFile.OpenAsync(FileAccessMode.Read))
			{
				// Create the decoder from the stream
				BitmapDecoder decoder = await BitmapDecoder.CreateAsync(stream);

				// Get the SoftwareBitmap representation of the file
				var softwareBitmap = await decoder.GetSoftwareBitmapAsync();

				using (var buffer = softwareBitmap.LockBuffer(BitmapBufferAccessMode.Read))
				{
					using (var reference = buffer.CreateReference())
					{
						unsafe
						{
							((IMemoryBufferByteAccess)reference).GetBuffer(out byte* dataInBytes, out uint capacity);
							byte[] titleBuffer = new byte[capacity];
							if (fileName == oneTitlePath)
								mOneTitleHeader = titleBuffer;
							else if (fileName == seriesTitlePath)
								mSeriesXSHeader = titleBuffer;
							else
								mWindowsHeader = titleBuffer;

							BitmapPlaneDescription bufferLayout = buffer.GetPlaneDescription(0);
							for (var i = 0; i < capacity; i++)
							{
								titleBuffer[i] = dataInBytes[bufferLayout.StartIndex + i];
							}
						}
					}
				}
			}

			if (fileName == oneTitlePath)
				LoadTitleImage(seriesTitlePath);
			else if (fileName == seriesTitlePath)
				LoadTitleImage(windowsTitlePath);
			else
				CheckCacheFolder();
		}


		private async void CheckCacheFolder()
		{
			var settings = Settings.Instance;
			await settings.Load();

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
		private async void App_CacheFolderChecked(object sender, EventArgs e)
		{
			//var updateManager = StoreContext.GetDefault();
			//var updates = await updateManager.GetAppAndOptionalStorePackageUpdatesAsync();

			//if (updates.Count > 0)
			//{
			//	var dialog = new MessageDialog("업데이트가 스토어에 등록되었습니다. 업데이트 후에 앱을 다시 실행해 주십시오.", "업데이트 있음");

			//	var updateButton = new UICommand("업데이트");
			//	updateButton.Invoked += async (command) =>
			//	{
			//		await Launcher.LaunchUriAsync(new Uri($"ms-windows-store://pdp/?productId=9P6ZT4WKCCCH"));
			//	};
			//	dialog.Commands.Add(updateButton);

			//	if (mDialogQueue.TryAdd(dialog, 500))
			//	{
			//		await dialog.ShowAsync();
			//		mDialogQueue.Take();
			//	}
			//}
			//else
				await CheckUpdateTime();
		}

		private async Task CheckUpdateTime()
		{
			var now = DateTime.Now;

			var httpClient = new HttpClient();

			try
			{
#if DEBUG
				//var response = await httpClient.PostAsync(new Uri("http://192.168.200.8:3000/last_modified_time"), new HttpStringContent("{}", Windows.Storage.Streams.UnicodeEncoding.Utf8, "application/json"));
				var response = await httpClient.PostAsync(new Uri("http://127.0.0.1:3000/last_modified_time"), new HttpStringContent("{}", Windows.Storage.Streams.UnicodeEncoding.Utf8, "application/json"));
#else
				var response = await httpClient.PostAsync(new Uri("https://xbox-korean-viewer-server2.herokuapp.com/last_modified_time"), new HttpStringContent("{}", Windows.Storage.Streams.UnicodeEncoding.Utf8, "application/json"));
#endif

				var str = response.Content.ReadAsStringAsync().GetResults();

				var settingMap = JsonConvert.DeserializeObject<Dictionary<string, string>>(str);

				var downloadedJsonFile = new FileInfo(ApplicationData.Current.LocalFolder.Path + "\\games.json");
				
				var settings = Settings.Instance;
				if (settingMap["lastModifiedTime"] == "")
				{
					if (LoadingPanel.Visibility == Visibility.Visible)
						LoadingPanel.Visibility = Visibility.Collapsed;

					var dialog = new MessageDialog("현재 서버 정보를 최신 정보로 업데이트 중입니다. 잠시 후에 다시 시도해 주십시오.", "데이터 수신 오류");
					if (mDialogQueue.TryAdd(dialog, 500))
					{
						await dialog.ShowAsync();
						mDialogQueue.Take();
					}

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

					if (mGameList.Count == 0)
						ReadGamesFromJson();
					else
					{
						LoadingPanel.Visibility = Visibility.Collapsed;
						GamesView.Visibility = Visibility.Visible;
					}
				}
			}
			catch (Exception exception)
			{
				Debug.WriteLine($"다운로드 에러: {exception.Message}");

				if (LoadingPanel.Visibility == Visibility.Visible)
					LoadingPanel.Visibility = Visibility.Collapsed;

				var dialog = new MessageDialog("서버에서 한국어 지원 정보를 확인할 수 없습니다. 잠시 후 다시 시도해 주십시오.", "데이터 수신 오류");
				if (mDialogQueue.TryAdd(dialog, 500))
				{
					await dialog.ShowAsync();
					mDialogQueue.Take();
				}
			}
		}

		private async void UpateJsonData()
		{
			var httpClient = new HttpClient();

			try
			{
				var client = new HttpClient();


#if DEBUG
				//var request = new HttpRequestMessage(HttpMethod.Post, new Uri("http://192.168.200.8:3000/title_list_zip"));
				var request = new HttpRequestMessage(HttpMethod.Post, new Uri("http://127.0.0.1:3000/title_list_zip"));
#else
				var request = new HttpRequestMessage(HttpMethod.Post, new Uri("https://xbox-korean-viewer-server2.herokuapp.com/title_list_zip"));
#endif

				await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
				{
					ProgressReady.Visibility = Visibility.Collapsed;
					ProgressDownload.Visibility = Visibility.Visible;
				});

				var progressCallback = new Progress<HttpProgress>(HttpProgressCallback);
				var tokenSource = new CancellationTokenSource();
				var response = await client.SendRequestAsync(request).AsTask(tokenSource.Token, progressCallback);

				var inputStream = await response.Content.ReadAsInputStreamAsync();
				var outputStream = new MemoryStream();

				await RandomAccessStream.CopyAndCloseAsync(inputStream, outputStream.AsOutputStream());


				var buffer = CryptographicBuffer.DecodeFromBase64String(Encoding.UTF8.GetString(outputStream.ToArray()));

				// 저장 전에 이전 데이터 가져오기
				if (Settings.Instance.LoadValue("ShowNewTitle") != "False")
				{
					var downloadedJsonFile = new FileInfo(ApplicationData.Current.LocalFolder.Path + "\\games.json");

					if (downloadedJsonFile.Exists)
					{
						var existFile = await ApplicationData.Current.LocalFolder.CreateFileAsync("games.json", CreationCollisionOption.OpenIfExists);

						if (downloadedJsonFile.Length > 0)
						{
							var existStr = await FileIO.ReadTextAsync(existFile);
							mExistGames = JsonConvert.DeserializeObject<List<Game>>(existStr).OrderBy(g => g.ID).ToList();
						}

						await existFile.DeleteAsync();
					}
				}

				var plainStream = new MemoryStream();
				using (var arc = new ZipArchive(buffer.AsStream()))
				{				
					foreach (var entry in arc.Entries)
					{
						entry.ExtractToFile(ApplicationData.Current.LocalFolder.Path + "\\games.json");						
					}
				}

				ReadGamesFromJson();
			}
			catch (Exception exception)
			{
				Debug.WriteLine($"다운로드 에러: {exception.Message}");
				Debug.WriteLine(exception.StackTrace);

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
			var games = JsonConvert.DeserializeObject<List<Game>>(jsonString).OrderBy(g => g.ID).ToList();

			if (AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Xbox") {
				for (var i = 0; i < games.Count; i++) {
					if (games[i].OG != "O" && games[i].X360 != "O" && games[i].OneS != "O" && games[i].SeriesXS != "O" && games[i].PC == "O")
					{
						games.RemoveAt(i);
						i--;
					}
				}
			}

			if (mExistGames.Count > 0)
			{
				mNewGames.Clear();
				foreach (Game game in games)
				{
					bool oldTitle = false;
					for (var i = 0; i < mExistGames.Count; i++)
					{
						if (game.ID == mExistGames[i].ID)
						{
							mExistGames.RemoveAt(i);
							oldTitle = true;
							break;
						}
						else if (game.ID.CompareTo(mExistGames[i].ID) < 0)
							break;
					}

					if (oldTitle == false)
					{
						if (mGameNameDisplayLanguage == "English")
							mNewGames.Add(game.Name);
						else
							mNewGames.Add(game.KoreanName);
					}
						
				}

				mExistGames.Clear();
			}

			HashSet<string> genre = new HashSet<string>();

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
			GamesView.Visibility = Visibility.Visible;

			SearchBox_TextChanged(SearchBox, null);

			if (mNewGames.Count > 0)
			{
				var dialog = new NewControlDialog(mNewGames);
				if (mDialogQueue.TryAdd(dialog, 500))
				{
					await dialog.ShowAsync();
					mDialogQueue.Take();
				}
			}
		}

		public List<Game> Games = new List<Game>();
		public ObservableCollection<GameViewModel> GamesViewModel { get; set; } = new ObservableCollection<GameViewModel>();
		public ObservableCollection<EditionViewModel> mEditionViewModel { get; set; } = new ObservableCollection<EditionViewModel>();

		UIElement animatingElement;
		private async void GamesView_ItemClick(object sender, ItemClickEventArgs e)
		{
			var gridView = sender as AdaptiveGridView;
			mSelectedIdx = gridView.Items.IndexOf(e.ClickedItem);

			if (e.ClickedItem != null)
			{
				var game = (e.ClickedItem as GameViewModel).Game;
				await CheckExtraInfo(game);
			}
		}

		private void ShowEditionPanel(Game game)
		{
			EditionPanelView.Visibility = Visibility.Visible;

			mEditionLanguage = GetLanguageCodeFromUrl(game.StoreLink);

			mEditionViewModel.Clear();

			if (game.IsAvailable)
			{
				mEditionViewModel.Add(new EditionViewModel
				{
					ID = game.ID,
					Name = mGameNameDisplayLanguage == "Korean" ? game.KoreanName : game.Name,
					Discount = game.Discount,
					SeriesXS = game.SeriesXS,
					OneS = game.OneS,
					PC = game.PC,
					IsGamePassPC = game.GamePassPC,
					IsGamePassConsole = game.GamePassConsole,
					IsGamePassCloud = game.GamePassCloud,
					GamePassNew = game.GamePassNew,
					GamePassEnd = game.GamePassEnd,
					ThumbnailUrl = game.Thumbnail,
					SeriesXSHeader = mSeriesXSHeader,
					OneSHeader = mOneTitleHeader,
					PCHeader = mWindowsHeader
				});
			}

			foreach (var bundle in game.Bundle)
			{
				mEditionViewModel.Add(new EditionViewModel
				{
					ID = bundle.ID,
					Name = bundle.Name,
					Discount = bundle.DiscountType,
					SeriesXS = bundle.SeriesXS,
					OneS = bundle.OneS,
					PC = bundle.PC,
					IsGamePassPC = bundle.GamePassPC,
					IsGamePassConsole = bundle.GamePassConsole,
					IsGamePassCloud = bundle.GamePassCloud,
					GamePassNew = bundle.GamePassNew,
					GamePassEnd = bundle.GamePassEnd,
					ThumbnailUrl = bundle.Thumbnail,
					SeriesXSHeader = mSeriesXSHeader,
					OneSHeader = mOneTitleHeader,
					PCHeader = mWindowsHeader
				});
			}

			if (AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Xbox")
			{
				EditionView.UpdateLayout();
				EditionView.Focus(FocusState.Programmatic);
			}
		}

		private async Task GoToStore(string language, string id)
		{
			if (AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Xbox" || language.ToLower().IndexOf(Windows.System.UserProfile.GlobalizationPreferences.HomeGeographicRegion.ToLower()) >= 0)
				await Launcher.LaunchUriAsync(new Uri($"ms-windows-store://pdp/?productId={id}"));
			else
			{
				await Launcher.LaunchUriAsync(new Uri($"https://www.microsoft.com/{language}/p/xkorean/{id}"));
			}
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
					return storeUrl.Substring(startStoreIdx).ToUpper();
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
				case "gb":
					return "영국";
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

		private string GetLanguageCodeFromUrl(string storeUrl)
		{
			var startIdx = storeUrl.IndexOf("com/");

			var endIdx = -1;
			if (startIdx > 0)
			{
				startIdx += "com/".Length;
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
					case "gb":
						storeUrlBuilder.Append("en-gb");
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
			var dialog = new AboutDialog();
			if (mDialogQueue.TryAdd(dialog, 500))
			{
				await dialog.ShowAsync();
				mDialogQueue.Take();
			}
		}

		private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			TextBox searchBlock = sender as TextBox;
			string text;
			if (searchBlock == null)
				text = "";
			else
				text = searchBlock.Text;

			var gamesFilteredByCategories = FilterByDevices(Games.ToArray());
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

			var gamesFilteredByLocalCoop = FilterByLocalCoop(gamesFilteredByUseKeyboardMouse);
			if (gamesFilteredByLocalCoop == null)
			{
				gamesFilteredByLocalCoop = Games.ToArray();
			}

			var gamesFilteredByOnlineCoop = FilterByOnlineCoop(gamesFilteredByLocalCoop);
			if (gamesFilteredByOnlineCoop == null)
			{
				gamesFilteredByOnlineCoop = Games.ToArray();
			}

			var gamesFilteredByFPS120 = FilterByFPS120(gamesFilteredByOnlineCoop);
			if (gamesFilteredByFPS120 == null)
			{
				gamesFilteredByFPS120 = Games.ToArray();
			}

			var gamesFilteredByFPSBoost = FilterByFPSBoost(gamesFilteredByFPS120);
			if (gamesFilteredByFPSBoost == null)
			{
				gamesFilteredByFPSBoost = Games.ToArray();
			}

			// 장르별 검색
			var gamesFilteredByCategory = FilterByCategory(gamesFilteredByFPSBoost);
			if (gamesFilteredByCategory == null)
			{
				gamesFilteredByCategory = Games.ToArray();
			}


			if (text.Trim() != string.Empty || gamesFilteredByCategory != null)
			{
				if (gamesFilteredByCategory == null)
				{
					gamesFilteredByCategory = Games.ToArray();
				}
				var games = (from g in gamesFilteredByCategory
							 where g.KoreanName.ToLower().Contains(text.ToLower().Trim()) || g.Name.ToLower().Contains(text.ToLower().Trim())
							 select g).ToArray();

				GamesViewModel.Clear();
				foreach (var g in games)
				{
					GamesViewModel.Add(new GameViewModel(g, mGameNameDisplayLanguage, mOneTitleHeader, mSeriesXSHeader, mWindowsHeader));
				}

				TitleBlock.Text = $"한국어 지원 타이틀 목록 ({games.Length:#,#0}개)";

			}
			else
			{
				GamesViewModel.Clear();
				foreach (var game in Games)
				{
					GamesViewModel.Add(new GameViewModel(game, mGameNameDisplayLanguage, mOneTitleHeader, mSeriesXSHeader, mWindowsHeader));
				}

				TitleBlock.Text = $"한국어 지원 타이틀 목록 ({Games.Count:#,#0}개)";
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
			List<Game> filteredGames = new List<Game>();

			if (GamePassCheckBox != null && (bool)GamePassCheckBox.IsChecked)
			{
				foreach (var game in gamesFilteredByTiming)
				{
					if (game.GamePassCloud == "O" || game.GamePassPC == "O" || game.GamePassConsole == "O")
					{
						filteredGames.Add(game);
					}
					else if (game.Bundle.Count > 0)
					{
						foreach (var bundle in game.Bundle)
						{
							if (bundle.GamePassCloud == "O" || bundle.GamePassPC == "O" || bundle.GamePassConsole == "O")
							{
								filteredGames.Add(game);
								break;
							}
						}
					}
				}
			
				return filteredGames.ToArray();
			}
			else
				return gamesFilteredByTiming;
		}

		private Game[] FilterByDiscount(Game[] gamesFilteredByGamePass)
		{
			List<Game> filteredGames = new List<Game>();


			if (DiscountCheckBox != null && (bool)DiscountCheckBox.IsChecked) {
				foreach (var game in gamesFilteredByGamePass) {
					if (game.Discount != "" && !game.Discount.Contains("출시") && !game.Discount.Contains("판매"))
						filteredGames.Add(game);
					else {
						foreach (var bundle in game.Bundle) {
							if (bundle.DiscountType != "" && !bundle.DiscountType.Contains("출시") && !bundle.DiscountType.Contains("판매"))
							{
								filteredGames.Add(game);
								break;
							}
						}
					}
				}

				return filteredGames.ToArray();
			}
			else
				return gamesFilteredByGamePass;
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

		private Game[] FilterByLocalCoop(Game[] gamesFilteredByKeyboardMouse)
		{
			Game[] filteredGames = gamesFilteredByKeyboardMouse;

			if (LocalCoopCheckBox != null && (bool)LocalCoopCheckBox.IsChecked)
				filteredGames = (from g in gamesFilteredByKeyboardMouse where g.LocalCoop == "O" select g).ToArray();

			return filteredGames;
		}

		private Game[] FilterByOnlineCoop(Game[] gamesFilteredByLocalCoop)
		{
			Game[] filteredGames = gamesFilteredByLocalCoop;

			if (OnlineCoopCheckBox != null && (bool)OnlineCoopCheckBox.IsChecked)
				filteredGames = (from g in gamesFilteredByLocalCoop where g.OnlineCoop == "O" select g).ToArray();

			return filteredGames;
		}

		private Game[] FilterByFPS120(Game[] gamesFilteredByOnlineCoop)
		{
			Game[] filteredGames = gamesFilteredByOnlineCoop;

			if (FPS120CheckBox != null && (bool)FPS120CheckBox.IsChecked)
				filteredGames = (from g in gamesFilteredByOnlineCoop where g.FPS120 == "O" select g).ToArray();

			return filteredGames;
		}

		private Game[] FilterByFPSBoost(Game[] gamesFilteredByFPS120)
		{
			Game[] filteredGames = gamesFilteredByFPS120;

			if (FPSBoostCheckBox != null && (bool)FPSBoostCheckBox.IsChecked)
			{
				filteredGames = (from g in gamesFilteredByFPS120 where g.FPSBoost == "O" select g).ToArray();
			}

			return filteredGames;
		}
		
		private void PosterImage_ImageOpened(object sender, RoutedEventArgs e)
		{
			var image = sender as Image;

			if (image != null && image.Tag != null)
			{
				var match = (from g in GamesViewModel where g.ID == image.Tag.ToString() select g).ToList();
				if (match.Count != 0)
				{
					match.First().IsImageLoaded = Visibility.Collapsed;
				}
			}
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
				Games = Games.OrderByDescending(g => g.ReleaseDate).ThenBy(g => g.Name).ToList();
			else
				Games = Games.OrderByDescending(g => g.ReleaseDate).ThenBy(g => g.KoreanName).ToList();

			SearchBox_TextChanged(SearchBox, null);

			await Settings.Instance.SetValue("orderType", "release_desc");
		}

		private Game[] FilterByDevices(Game[] games)
		{
			if (CategorySeriesXSCheckBox.IsChecked == true ||
				CategoryOneXEnhancedCheckBox.IsChecked == true ||
				CategoryOneCheckBox.IsChecked == true ||
				CategoryX360CheckBox.IsChecked == true ||
				CategoryOGCheckBox.IsChecked == true ||
				CategoryWindowsCheckBox.IsChecked == true ||
				CategoryCloudCheckBox.IsChecked == true ||
				CategoryCloudCheckBox.IsChecked == true) {
				var selectGamesList = new List<Game>();

				foreach (var game in games) {
					if ((CategorySeriesXSCheckBox.IsChecked == true && game.SeriesXS == "O") ||
						(CategoryOneXEnhancedCheckBox.IsChecked == true && game.OneXEnhanced == "O") ||
						(CategoryOneCheckBox.IsChecked == true && game.OneS == "O") ||
						(CategoryX360CheckBox.IsChecked == true && game.X360 == "O") ||
						(CategoryOGCheckBox.IsChecked == true && game.OG == "O") ||
						(CategoryWindowsCheckBox.IsChecked == true && game.PC == "O"))
						selectGamesList.Add(game);
					else if (CategoryCloudCheckBox.IsChecked == true)
					{
						if (game.GamePassCloud == "O")
							selectGamesList.Add(game);
						else
						{
							foreach (var bundle in game.Bundle)
							{
								if (bundle.GamePassCloud == "O")
								{
									selectGamesList.Add(game);
									break;
								}
							}
						}
					}
				}

				return selectGamesList.ToArray();
			}
			else
				return (from g in games
						select g).ToArray();
		}

		private Game[] FilterByCategory(Game[] games)
		{
			if (FamilyKidsCheckBox.IsChecked != true &&
				FightingCheckBox.IsChecked != true &&
				EducationalCheckBox.IsChecked != true &&
				RacingFlyingCheckBox.IsChecked != true &&
				RolePlayingCheckBox.IsChecked != true &&
				MultiplayCheckBox.IsChecked != true &&
				ShooterCheckBox.IsChecked != true &&
				SportsCheckBox.IsChecked != true &&
				SimulationCheckBox.IsChecked != true &&
				ActionAdventureCheckBox.IsChecked != true &&
				MusicCheckBox.IsChecked != true &&
				StrategyCheckBox.IsChecked != true &&
				CardBoardCheckBox.IsChecked != true &&
				ClassicsCheckBox.IsChecked != true &&
				PuzzleTriviaCheckBox.IsChecked != true &&
				PlatformerCheckBox.IsChecked != true &&
				CasinoCheckBox.IsChecked != true &&
				OtherCheckBox.IsChecked != true)
				return games;

			//Game[] selectedGames = null;
			var selectGamesList = new List<Game>();

			HashSet<string> checkedcategories = new HashSet<string>();

			selectGamesList.AddRange((from g in games
									  where (FamilyKidsCheckBox.IsChecked == true && g.Category == "family & kids") ||
										(FightingCheckBox.IsChecked == true && g.Category == "fighting") ||
										(EducationalCheckBox.IsChecked == true && g.Category == "educational") ||
										(RacingFlyingCheckBox.IsChecked == true && g.Category == "racing & flying") ||
										(RolePlayingCheckBox.IsChecked == true && g.Category == "role playing") ||
										(MultiplayCheckBox.IsChecked == true && g.Category == "multi-player online battle arena") ||
										(ShooterCheckBox.IsChecked == true && g.Category == "shooter") ||
										(SportsCheckBox.IsChecked == true && g.Category == "sports") ||
										(SimulationCheckBox.IsChecked == true && g.Category == "simulation") ||
										(ActionAdventureCheckBox.IsChecked == true && g.Category == "action & adventure") ||
										(MusicCheckBox.IsChecked == true && g.Category == "music") ||
										(StrategyCheckBox.IsChecked == true && g.Category == "strategy") ||
										(CardBoardCheckBox.IsChecked == true && g.Category == "card + board") ||
										(ClassicsCheckBox.IsChecked == true && g.Category == "classics") ||
										(PuzzleTriviaCheckBox.IsChecked == true && g.Category == "puzzle & trivia") ||
										(PlatformerCheckBox.IsChecked == true && g.Category == "platformer") ||
										(CasinoCheckBox.IsChecked == true && g.Category == "casino") ||
										(OtherCheckBox.IsChecked == true && g.Category == "other")
									  select g).ToList());

			return selectGamesList.ToArray();
		}

		private void UpdateItemHeight()
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
					GamesView.Padding = new Thickness(0, 0, 0, 0);
					GamesView.Margin = new Thickness(0, 10, 0, 0);
					break;
			}
		}

		private async Task CheckExtraInfo(Game game) {
			var tipBuilder = new StringBuilder();

			GotoStoreButton.Tag = game;
			Goto360Market.Visibility = Visibility.Collapsed;
			GotoRemaster.Visibility = Visibility.Collapsed;
			GotoOneTitle.Visibility = Visibility.Collapsed;

			var messageArr = game.Message.Split("\n");

			if (AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Xbox")
			{
				var storeRegion = GetRegionCodeFromUrl(game.StoreLink);

				if (storeRegion.ToLower() != Windows.System.UserProfile.GlobalizationPreferences.HomeGeographicRegion.ToLower())
				{
					var template = mMessageTemplateMap["noRegion"];
					tipBuilder.Append("* ").Append(template.Replace("[name]", ConvertCodeToStr(storeRegion)));

					if (game.Message.Trim() != "")
						tipBuilder.Append("\r\n");
				}
			}

			for (var i = 0; i < messageArr.Length; i++)
			{
				var parsePart = messageArr[i].Split("=");
				var code = parsePart[0].Trim().ToLower();
				if (mMessageTemplateMap.ContainsKey(code))
				{
					var message = mMessageTemplateMap[code];
					if (parsePart.Length > 1)
					{
						var strValue = "";
						switch (code)
						{
							case "360market":
								if (AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Desktop") {
									Goto360Market.Tag = parsePart[1];
									Goto360Market.Visibility = Visibility;
								}
								break;
							case "dlregiononly":
								if (Windows.System.UserProfile.GlobalizationPreferences.HomeGeographicRegion.ToLower() != parsePart[1].ToLower())
									strValue = ConvertCodeToStr(parsePart[1]);
								break;
							case "remaster":
								var remasterID = GetIDFromStoreUrl(parsePart[1]);
								var remasterGame = mGameList.FirstOrDefault(item => item.ID == remasterID);
								if (remasterGame != null)
								{
									if (mGameNameDisplayLanguage == "English")
										strValue = remasterGame.Name;
									else
										strValue = remasterGame.KoreanName;
								}

								var remasterMap = new Dictionary<string, string>();
								remasterMap["language"] = GetLanguageCodeFromUrl(parsePart[1]);
								remasterMap["id"] = remasterID;
								GotoRemaster.Tag = remasterMap;
								GotoRemaster.Visibility = Visibility.Visible;
								break;
							case "onetitle":
								var oneTitleID = GetIDFromStoreUrl(parsePart[1]);
								var oneTitleGame = mGameList.FirstOrDefault(item => item.ID == oneTitleID);
								if (oneTitleGame != null)
								{
									if (mGameNameDisplayLanguage == "English")
										strValue = oneTitleGame.Name;
									else
										strValue = oneTitleGame.KoreanName;
								}

								var oneTitleMap = new Dictionary<string, string>();
								oneTitleMap["language"] = GetLanguageCodeFromUrl(parsePart[1]);
								oneTitleMap["id"] = oneTitleID;
								GotoOneTitle.Tag = oneTitleMap;
								GotoOneTitle.Visibility = Visibility.Visible;
								break;
							default:
								strValue = parsePart[1];
								break;
						}

						message = message.Replace("[name]", strValue);
					}

					if ((code == "dlregiononly" && Windows.System.UserProfile.GlobalizationPreferences.HomeGeographicRegion.ToLower() != parsePart[1].ToLower()) || code != "dlregiononly")
						tipBuilder.Append("* ").Append(message);
				}
				else if (code != "")
					tipBuilder.Append("* ").Append(parsePart[0]);

				if (i < messageArr.Length - 1)
					tipBuilder.Append("\r\n");
			}

			if (tipBuilder.Length > 0)
			{
				InfoBlock.Inlines.Clear();
				InfoBlock.Inlines.Add(new Run() { Text = tipBuilder.ToString().Trim(), FontWeight = FontWeights.Bold });
				InfoPanelView.Visibility = Visibility.Visible;

				if (AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Xbox")
				{
					GotoStoreButton.Focus(FocusState.Programmatic);
				}
			}
			else
			{
				await CheckEditionPanel(game);
			}
		}

		private async Task CheckEditionPanel(Game game) {
			if (game.Bundle.Count == 0)
				await GoToStore(GetLanguageCodeFromUrl(game.StoreLink), game.ID);
			else
			{
				if (game.IsAvailable || game.Bundle.Count > 1)
					ShowEditionPanel(game);
				else
				{
					await GoToStore(GetLanguageCodeFromUrl(game.StoreLink), game.Bundle[0].ID);
				}
			}
		}


		private async Task OpenLink(Game game, LinkType linkType) {
			var messageArr = game.Message.Split("\n");

			for (var i = 0; i < messageArr.Length; i++)
			{
				if (messageArr[i].IndexOf("=") == -1)
					continue;

				var parsePart = messageArr[i].Split("=");
				var code = parsePart[0].Trim().ToLower();
				switch (code)
				{
					case "360market":
						if (linkType == LinkType.Market360)
						{
							await Launcher.LaunchUriAsync(new Uri(parsePart[1]));
							return;
						}
						else
							break;
					case "remaster":
						if (linkType == LinkType.RemasterTitle)
						{
							await GoToStore(GetLanguageCodeFromUrl(parsePart[1]), GetIDFromStoreUrl(parsePart[1]));
							return;
						}
						else
							break;
					case "onetitle":
						if (linkType == LinkType.OneTitle)
						{
							await GoToStore(GetLanguageCodeFromUrl(parsePart[1]), GetIDFromStoreUrl(parsePart[1]));
							return;
						}
						else
							break;
				}
			}
		}

		private async Task ShowErrorReportDialog(GameViewModel game) {
			var dialog = new ErrorReportDialog(game.Title, GetRegionCodeFromUrl(game.StoreUri).ToUpper());
			if (mDialogQueue.TryAdd(dialog, 500))
			{
				await dialog.ShowAsync();
				mDialogQueue.Take();
			}
		}

		private async Task PlayCloud(Game game) {
			if (game.GamePassCloud != "")
				await Launcher.LaunchUriAsync(new Uri($"https://www.xbox.com/ko-KR/play/launch/xKorean/{game.ID}"));
			else
			{
				foreach (var bundle in game.Bundle)
				{
					if (bundle.GamePassCloud != "")
					{
						await Launcher.LaunchUriAsync(new Uri($"https://www.xbox.com/ko-KR/play/launch/xKorean/{bundle.ID}"));
						break;
					}
				}
			}
			
		}

		private void HttpProgressCallback(HttpProgress progress) {
			if (progress.TotalBytesToReceive == null)
				return;

			ProgressDownload.Minimum = 0;
			ProgressDownload.Maximum = (double)progress.TotalBytesToReceive;
			ProgressDownload.Value = progress.BytesReceived;
		}

		private void CategorieCheckBox_Checked(object sender, RoutedEventArgs e)
		{
			SearchBox_TextChanged(SearchBox, null);
		}

		private void CategorieCheckBox_Unchecked(object sender, RoutedEventArgs e)
		{
			SearchBox_TextChanged(SearchBox, null);
		}

		private void UpdateCategoriesState() {
			var localSettings = ApplicationData.Current.LocalSettings;

			localSettings.Values["seriesXS"] = CategorySeriesXSCheckBox.IsChecked;
			localSettings.Values["oneXEnhanced"] = CategoryOneXEnhancedCheckBox.IsChecked;
			localSettings.Values["oneS"] = CategoryOneCheckBox.IsChecked;
			localSettings.Values["x360"] = CategoryX360CheckBox.IsChecked;
			localSettings.Values["og"] = CategoryOGCheckBox.IsChecked;
			localSettings.Values["windows"] = CategoryWindowsCheckBox.IsChecked;
			localSettings.Values["cloud"] = CategoryCloudCheckBox.IsChecked;
		}

		private async void RefreshButton_Click(object sender, RoutedEventArgs e)
		{
			if (LoadingPanel.Visibility == Visibility.Collapsed)
			{
				LoadingPanel.Visibility = Visibility.Visible;
				GamesView.Visibility = Visibility.Collapsed;
				EditionPanelView.Visibility = Visibility.Collapsed;
				InfoPanelView.Visibility = Visibility.Collapsed;

				ProgressReady.Visibility = Visibility.Visible;
				ProgressReady.Visibility = Visibility.Collapsed;
			}

			await CheckUpdateTime();
		}

		private void TimingRadioButton_Checked(object sender, RoutedEventArgs e)
		{
			SearchBox_TextChanged(SearchBox, null);
		}

		private async void SettingButton_ClickAsync(object sender, RoutedEventArgs e)
		{
			var dialog = new SettingDialog();
			if (mDialogQueue.TryAdd(dialog, 500))
			{
				var result = await dialog.ShowAsync();
				mDialogQueue.Take();

				if (result == ContentDialogResult.Primary)
				{
					var settings = Settings.Instance;

					mGameNameDisplayLanguage = settings.LoadValue("gameNameDisplayLanguage");

					for (int i = 0; i < GamesViewModel.Count; i++)
					{
						GamesViewModel[i].GameNameDisplayLanguage = mGameNameDisplayLanguage;
					}

					UpdateItemHeight();
				}
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
				case VirtualKey.GamepadRightShoulder:
					SearchBox.Focus(FocusState.Programmatic);
					break;
				case VirtualKey.GamepadLeftShoulder:
					DeviceFilterButton.Focus(FocusState.Programmatic);
					break;
			}
		}

		private async void MenuRunCloud_Click(object sender, RoutedEventArgs e)
		{
			var game = (e.OriginalSource as MenuFlyoutItem).DataContext as GameViewModel;

			await PlayCloud(game.Game);
		}

		private async void GamesView_KeyUp(object sender, Windows.UI.Xaml.Input.KeyRoutedEventArgs e)
		{
			if (AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Xbox")
			{
				var game = (e.OriginalSource as GridViewItem).Content as GameViewModel;
				if (e.Key == VirtualKey.GamepadMenu)
					await ShowErrorReportDialog(game);
				else if (e.Key == VirtualKey.GamepadView)
					await PlayCloud(game.Game);
				else if (e.Key == VirtualKey.GamepadX)
					await OpenLink(game.Game, LinkType.RemasterTitle);
				else if (e.Key == VirtualKey.GamepadY)
					await OpenLink(game.Game, LinkType.OneTitle);
			}
		}

		private async void MenuErrorReport_Click(object sender, RoutedEventArgs e)
		{
			var game = (e.OriginalSource as MenuFlyoutItem).DataContext as GameViewModel;

			await ShowErrorReportDialog(game);
		}

		private async void EditionView_ItemClick(object sender, ItemClickEventArgs e)
		{
			if (e.ClickedItem != null)
			{
				var bundle = e.ClickedItem as EditionViewModel;

				await GoToStore(mEditionLanguage, bundle.ID);
			}
		}

		private void CloseEditionView_Click(object sender, RoutedEventArgs e)
		{
			EditionPanelView.Visibility = Visibility.Collapsed;
			(GamesView.ContainerFromIndex(mSelectedIdx) as GridViewItem).Focus(FocusState.Programmatic);
			
		}

		private void EditionPosterImage_ImageOpened(object sender, RoutedEventArgs e)
		{
			Image image = sender as Image;

			if (image != null && image.Tag != null)
			{
				var match = (from g in mEditionViewModel where g.ID == image.Tag.ToString() select g).ToList();
				if (match.Count != 0)
				{
					match.First().IsImageLoaded = Visibility.Collapsed;
				}
			}
		}

		private async void Menu360Market_Click(object sender, RoutedEventArgs e)
		{
			await OpenLink(((sender as MenuFlyoutItem).DataContext as GameViewModel).Game, LinkType.Market360);
		}

		private async void MenuRemasterTitle_Click(object sender, RoutedEventArgs e)
		{
			await OpenLink(((sender as MenuFlyoutItem).DataContext as GameViewModel).Game, LinkType.RemasterTitle);
		}

		private async void MenuOneTitle_Click(object sender, RoutedEventArgs e)
		{
			await OpenLink(((sender as MenuFlyoutItem).DataContext as GameViewModel).Game, LinkType.OneTitle);
		}

		private enum LinkType
		{
			Market360,
			RemasterTitle,
			OneTitle
		}

		private void SearchBox_KeyUp(object sender, Windows.UI.Xaml.Input.KeyRoutedEventArgs e)
		{
			if (e.Key == VirtualKey.Enter || e.Key == VirtualKey.GamepadMenu)
				SearchBox_TextChanged(SearchBox, null);
		}

		private async void GotoStoreButton_Click(object sender, RoutedEventArgs e)
		{
			InfoPanelView.Visibility = Visibility.Collapsed;

			var gotoStoreButton = sender as Button;
			await CheckEditionPanel(gotoStoreButton.Tag as Game);
		}

		private async void Goto360Market_Click(object sender, RoutedEventArgs e)
		{
			InfoPanelView.Visibility = Visibility.Collapsed;

			var goto360market = sender as Button;
			await Launcher.LaunchUriAsync(new Uri(goto360market.Tag as string));
		}

		private async void GotoRemaster_Click(object sender, RoutedEventArgs e)
		{
			InfoPanelView.Visibility = Visibility.Collapsed;

			var gotoRemaster = sender as Button;
			var remasterMap = gotoRemaster.Tag as Dictionary<string, string>;
			await GoToStore(remasterMap["language"], remasterMap["id"]);
		}

		private async void GotoOneTitle_Click(object sender, RoutedEventArgs e)
		{
			InfoPanelView.Visibility = Visibility.Collapsed;

			var gotoOneTitle = sender as Button;
			var oneTitleMap = gotoOneTitle.Tag as Dictionary<string, string>;
			await GoToStore(oneTitleMap["language"], oneTitleMap["id"]);
		}

		private void CloseInfoPanel_Click(object sender, RoutedEventArgs e)
		{
			InfoPanelView.Visibility = Visibility.Collapsed;
			(GamesView.ContainerFromIndex(mSelectedIdx) as GridViewItem).Focus(FocusState.Programmatic);
		}

		private void Grid_PointerEntered(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
		{
			GameViewModel game;
			if (e.OriginalSource as Grid != null)
				game = (e.OriginalSource as Grid).DataContext as GameViewModel;
			else if (e.OriginalSource as Image != null)
				game = (e.OriginalSource as Image).DataContext as GameViewModel;
			else if ((e.OriginalSource as TextBlock) != null)
				game = (e.OriginalSource as TextBlock).DataContext as GameViewModel;
			else if ((e.OriginalSource as GridViewItem) != null)
				game = (e.OriginalSource as GridViewItem).Content as GameViewModel;
			else
				game = (e.OriginalSource as ListViewItemPresenter).DataContext as GameViewModel;

			var menu = (sender as FrameworkElement).ContextFlyout as MenuFlyout;

			menu.Items[0].Visibility = Visibility.Collapsed;
			if (game.Game.GamePassCloud != "")
				menu.Items[0].Visibility = Visibility.Visible;
			else
			{
				foreach (var bundle in game.Bundle)
				{
					if (bundle.GamePassCloud != "")
					{
						menu.Items[0].Visibility = Visibility.Visible;
						break;
					}
				}
			}
		}

		private void CategoryCheckBox_Click(object sender, RoutedEventArgs e)
		{
			UpdateCategoriesState();
			SearchBox_TextChanged(SearchBox, null);
		}
	}
}
