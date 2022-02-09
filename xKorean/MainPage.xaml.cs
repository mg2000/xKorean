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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Security.ExchangeActiveSyncProvisioning;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System;
using Windows.System.Profile;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Notifications;
using Windows.UI.Popups;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Documents;
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
		private string mGameNameDisplayLanguage = "Korean";

		private Dictionary<string, string> mMessageTemplateMap = new Dictionary<string, string>();
		private List<Game> mGameList = new List<Game>();

		private readonly BlockingCollection<object> mDialogQueue = new BlockingCollection<object>(1);
		private readonly BlockingCollection<object> mTipQueue = new BlockingCollection<object>(1);

		private byte[] mOneTitleHeader = null;
		private byte[] mSeriesXSHeader = null;
		private byte[] mPlayAnywhereHeader = null;
		private byte[] mPlayAnywhereSeriesHeader = null;
		private byte[] mWindowsHeader = null;

		private const string windowsTitlePath = "ms-appx:///Assets/windows_title.png";
		private const string oneTitlePath = "ms-appx:///Assets/xbox_one_title.png";
		private const string seriesTitlePath = "ms-appx:///Assets/xbox_series_xs_title.png";
		private const string playAnywherePath = "ms-appx:///Assets/xbox_playanywhere_title.png";
		private const string playAnywhereSeriesPath = "ms-appx:///Assets/xbox_playanywhere_xs_title.png";

		private List<Game> mExistGames = new List<Game>();
		private List<string> mNewGames = new List<string>();

		private string mEditionLanguage;

		private int mSelectedIdx = 0;

		private string mDeviceID = "";
		private int mRecommendCount = 5;

		private bool mShowRecommendTag = false;
		private bool mShowDiscount = true;
		private bool mShowGamepass = true;
		private bool mShowName = true;
		private bool mShowReleaseTime = false;

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

			UpdateDeviceFilterButton();

			if (localSettings.Values["recommendPriority"] != null)
				RecommendCheckBox.IsChecked = (bool)localSettings.Values["recommendPriority"];

			if (localSettings.Values["showRecommendTag"] != null)
				mShowRecommendTag = (bool)localSettings.Values["showRecommendTag"];
			else
				mShowRecommendTag = false;

			if (localSettings.Values["showDiscount"] != null)
				mShowDiscount = (bool)localSettings.Values["showDiscount"];
			else
				mShowDiscount = true;

			if (localSettings.Values["showGamepass"] != null)
				mShowGamepass = (bool)localSettings.Values["showGamepass"];
			else
				mShowGamepass = true;

			if (localSettings.Values["showName"] != null)
				mShowName = (bool)localSettings.Values["showName"];
			else
				mShowName = true;

			if (localSettings.Values["showReleaseTime"] != null)
				mShowReleaseTime = (bool)localSettings.Values["showReleaseTime"];
			else
				mShowReleaseTime = false;

			mMessageTemplateMap["packageonly"] = "패키지 버전만 한국어를 지원합니다.";
			mMessageTemplateMap["usermode"] = "이 게임은 유저 모드를 설치하셔야 한국어가 지원됩니다.";
			mMessageTemplateMap["windowsmod"] = "이 게임은 윈도우에서 한글 패치를 설치하셔야 한국어가 지원됩니다.";

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
			mDeviceID = eas.Id.ToString().ToUpper();

			Debug.WriteLine($"디바이스 정보 {mDeviceID}");
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
							else if (fileName == playAnywherePath)
								mPlayAnywhereHeader = titleBuffer;
							else if (fileName == playAnywhereSeriesPath)
								mPlayAnywhereSeriesHeader = titleBuffer;
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
				LoadTitleImage(playAnywherePath);
			else if (fileName == playAnywherePath)
				LoadTitleImage(playAnywhereSeriesPath);
			else if (fileName == playAnywhereSeriesPath)
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
			if (mGameNameDisplayLanguage == "")
				mGameNameDisplayLanguage = "Korean";

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
				if (LoadingPanel.Visibility == Visibility.Visible)
					LoadingPanel.Visibility = Visibility.Collapsed;

				var additionalMessage = "";
				if (exception.Message != null && exception.Message.Trim() != "")
					additionalMessage = $"\r\n\r\n{exception.Message.Trim()}";

				var dialog = new MessageDialog($"서버에서 한국어 지원 정보를 확인할 수 없습니다. 잠시 후 다시 시도해 주십시오.{additionalMessage}", "데이터 수신 오류");
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

				var inputStream = (await response.Content.ReadAsInputStreamAsync()).AsStreamForRead();
				var decompressor = new GZipStream(inputStream, CompressionMode.Decompress);
				var decompressStream = new MemoryStream();
				decompressor.CopyTo(decompressStream);
				decompressStream.Seek(0, SeekOrigin.Begin);

				var streamReader = new StreamReader(decompressStream);
				var str = streamReader.ReadToEnd();

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

				File.WriteAllText(ApplicationData.Current.LocalFolder.Path + "\\games.json", str);

				ReadGamesFromJson();
			}
			catch (Exception exception)
			{
				if (LoadingPanel.Visibility == Visibility.Visible)
					LoadingPanel.Visibility = Visibility.Collapsed;

				var additionalMessage = "";
				if (exception.Message != null && exception.Message.Trim() != "")
					additionalMessage = $"\r\n\r\n{exception.Message.Trim()}";

				var dialog = new MessageDialog($"서버에서 한글화 정보를 다운로드할 수 없습니다.{additionalMessage}", "데이터 수신 오류");
				if (mDialogQueue.TryAdd(dialog, 500))
				{
					await dialog.ShowAsync();
					mDialogQueue.Take();
				}
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
					if (games[i].OG != "O" && games[i].X360 != "O" && games[i].OneS != "O" && games[i].SeriesXS != "O" && games[i].PC == "O" || games[i].Message.ToLower().IndexOf("windowsmod") >= 0)
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
					if (AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Xbox" && game.PC == "O" && game.PlayAnywhere != "O")
						continue;

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

			mGameList.Clear();
			mGameList.AddRange(games);

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
					Discount = game.Discount == "곧 출시" || (mShowReleaseTime && game.Discount != "출시 예정" && game.Discount.Contains(" 출시")) ? Utils.GetReleaseStr(game.ReleaseDate) : game.Discount,
					SeriesXS = game.SeriesXS,
					OneS = game.OneS,
					PC = game.PC,
					PlayAnywhere = game.PlayAnywhere,
					IsGamePassPC = game.GamePassPC,
					IsGamePassConsole = game.GamePassConsole,
					IsGamePassCloud = game.GamePassCloud,
					GamePassNew = game.GamePassNew,
					GamePassEnd = game.GamePassEnd,
					ThumbnailUrl = game.Thumbnail,
					SeriesXSHeader = mSeriesXSHeader,
					OneSHeader = mOneTitleHeader,
					PlayAnywhereSeriesHeader = mPlayAnywhereSeriesHeader,
					PlayAnywhereHeader = mPlayAnywhereHeader,
					PCHeader = mWindowsHeader,
					ShowDiscount = mShowDiscount,
					ShowGamePass = mShowGamepass,
					ShowName = mShowName
				});
			}

			foreach (var bundle in game.Bundle)
			{
				mEditionViewModel.Add(new EditionViewModel
				{
					ID = bundle.ID,
					Name = bundle.Name,
					Discount = bundle.DiscountType == "곧 출시" || (mShowReleaseTime && bundle.DiscountType != "출시 예정" && bundle.DiscountType.Contains(" 출시")) ? Utils.GetReleaseStr(bundle.ReleaseDate) : bundle.DiscountType,
					SeriesXS = bundle.SeriesXS,
					OneS = bundle.OneS,
					PC = bundle.PC,
					PlayAnywhere = game.PlayAnywhere,
					IsGamePassPC = bundle.GamePassPC,
					IsGamePassConsole = bundle.GamePassConsole,
					IsGamePassCloud = bundle.GamePassCloud,
					GamePassNew = bundle.GamePassNew,
					GamePassEnd = bundle.GamePassEnd,
					ThumbnailUrl = bundle.Thumbnail,
					SeriesXSHeader = mSeriesXSHeader,
					OneSHeader = mOneTitleHeader,
					PlayAnywhereSeriesHeader = mPlayAnywhereSeriesHeader,
					PlayAnywhereHeader = mPlayAnywhereHeader,
					PCHeader = mWindowsHeader,
					ShowDiscount = mShowDiscount,
					ShowGamePass = mShowGamepass,
					ShowName = mShowName
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
			if (AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Xbox") // || language.ToLower().IndexOf(Windows.System.UserProfile.GlobalizationPreferences.HomeGeographicRegion.ToLower()) >= 0)
				await Launcher.LaunchUriAsync(new Uri($"ms-windows-store://pdp/?productId={id}"));
			else
			{
				//await Launcher.LaunchUriAsync(new Uri($"https://www.xbox.com/{language}/games/store/xkorean/{id}"));
				await Launcher.LaunchUriAsync(new Uri($"https://www.microsoft.com/{language}/p/xkorean/{id}"));
				//await Launcher.LaunchUriAsync(new Uri($"msxbox://game/?productId={id}"));
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
			List<Game> SortList(List<Game> unSortedGames) {
				var sortedList = new List<Game>();
				if (RecommendCheckBox != null && RecommendCheckBox.IsChecked == true)
				{
					var recommendUnsortedCount = 0;

					var recommendedList = unSortedGames.FindAll(g => g.ShowRecommend == true).OrderByDescending(g => g.Recommend);

					if (OrderByNameAscendItem.IsChecked == true)
					{
						if (mGameNameDisplayLanguage == "English")
							recommendedList = recommendedList.ThenBy(g => g.Name);
						else
							recommendedList = recommendedList.ThenBy(g => g.KoreanName);
					}
					else if (OrderByNameDescendItem.IsChecked == true)
					{
						if (mGameNameDisplayLanguage == "English")
							recommendedList = recommendedList.ThenByDescending(g => g.Name);
						else
							recommendedList = recommendedList.ThenByDescending(g => g.KoreanName);
					}
					else if (OrderByReleaseAscendItem.IsChecked == true)
					{
						if (mGameNameDisplayLanguage == "English")
							recommendedList = recommendedList.ThenBy(g => g.ReleaseDate).ThenBy(g => g.Name);
						else
							recommendedList = recommendedList.ThenBy(g => g.ReleaseDate).ThenBy(g => g.KoreanName);
					}
					else
					{
						if (mGameNameDisplayLanguage == "English")
							recommendedList = recommendedList.ThenByDescending(g => g.ReleaseDate).ThenBy(g => g.Name);
						else
							recommendedList = recommendedList.ThenByDescending(g => g.ReleaseDate).ThenBy(g => g.KoreanName);
					}

					foreach (var recommendGame in recommendedList)
					{
						sortedList.Add(recommendGame);
						unSortedGames.Remove(recommendGame);
						recommendUnsortedCount++;

						if (recommendUnsortedCount >= 10)
							break;
					}
				}

				if (OrderByNameAscendItem.IsChecked == true)
				{
					if (mGameNameDisplayLanguage == "English")
					{
						sortedList.AddRange(unSortedGames.OrderBy(g => g.Name).ToList());
					}
					else
						sortedList.AddRange(unSortedGames.OrderBy(g => g.KoreanName).ToList());
				}
				else if (OrderByNameDescendItem.IsChecked == true)
				{
					if (mGameNameDisplayLanguage == "English")
						sortedList.AddRange(unSortedGames.OrderByDescending(g => g.Name).ToList());
					else
						sortedList.AddRange(unSortedGames.OrderByDescending(g => g.KoreanName).ToList());
				}
				else if (OrderByReleaseAscendItem.IsChecked == true)
				{
					if (mGameNameDisplayLanguage == "English")
						sortedList.AddRange(unSortedGames.OrderBy(g => g.ReleaseDate).ThenBy(g => g.Name).ToList());
					else
						sortedList.AddRange(unSortedGames.OrderBy(g => g.ReleaseDate).ThenBy(g => g.KoreanName).ToList());
				}
				else
				{
					if (mGameNameDisplayLanguage == "English")
						sortedList.AddRange(unSortedGames.OrderByDescending(g => g.ReleaseDate).ThenBy(g => g.Name).ToList());
					else
						sortedList.AddRange(unSortedGames.OrderByDescending(g => g.ReleaseDate).ThenBy(g => g.KoreanName).ToList());
				}

				return sortedList;
			}

			TextBox searchBlock = sender as TextBox;
			string text;
			if (searchBlock == null)
				text = "";
			else
				text = searchBlock.Text;

			var gamesFilteredByDevices = FilterByDevices(mGameList);
			if (gamesFilteredByDevices == null)
			{
				gamesFilteredByDevices = mGameList;
			}

			var recommendCount = 0;
			foreach (var recommendGame in gamesFilteredByDevices.FindAll(g => g.Recommend > 0).OrderByDescending(g => g.Recommend))
			{
				if (recommendCount < 10)
				{
					recommendGame.ShowRecommend = true;
					recommendCount++;
				}
				else
					recommendGame.ShowRecommend = false;
			}

			for (var i = 0; i < gamesFilteredByDevices.Count; i++)
            {
				// 한국어 지원 범위 필터링
				if ((KoreanVoiceRadioButton != null && (bool)KoreanVoiceRadioButton.IsChecked && !gamesFilteredByDevices[i].Localize.Contains("음성")) ||
					(KoreanSubtitleRadioButton != null && (bool)KoreanSubtitleRadioButton.IsChecked && !gamesFilteredByDevices[i].Localize.Contains("자막")))
				{
					gamesFilteredByDevices.RemoveAt(i);
					i--;
					continue;
				}

				// 게임패스 필터링
				if (GamePassCheckBox != null && (bool)GamePassCheckBox.IsChecked)
				{
					if (gamesFilteredByDevices[i].GamePassCloud == "" && gamesFilteredByDevices[i].GamePassPC == "" && gamesFilteredByDevices[i].GamePassConsole == "")
					{
						if (gamesFilteredByDevices[i].Bundle.Count > 0)
                        {
							var gamePass = false;
							foreach (var bundle in gamesFilteredByDevices[i].Bundle)
							{
								
								if (bundle.GamePassCloud == "O" || bundle.GamePassPC == "O" || bundle.GamePassConsole == "O")
								{
									gamePass = true;
									break;
								}
							}

							if (!gamePass)
                            {
								gamesFilteredByDevices.RemoveAt(i);
								i--;
								continue;
							}
						}
						else
                        {
							gamesFilteredByDevices.RemoveAt(i);
							i--;
							continue;
						}
					}
				}

				// 게임패스 필터링
				if (GamePassExcludeCheckBox.IsChecked == true)
				{
					if (gamesFilteredByDevices[i].GamePassCloud == "" && gamesFilteredByDevices[i].GamePassPC == "" && gamesFilteredByDevices[i].GamePassConsole == "")
					{
						if (gamesFilteredByDevices[i].Bundle.Count > 0)
						{
							var gamePass = false;
							foreach (var bundle in gamesFilteredByDevices[i].Bundle)
							{

								if (bundle.GamePassCloud == "O" || bundle.GamePassPC == "O" || bundle.GamePassConsole == "O")
								{
									gamePass = true;
									break;
								}
							}

							if (gamePass)
							{
								gamesFilteredByDevices.RemoveAt(i);
								i--;
								continue;
							}
						}
					}
					else
					{
						gamesFilteredByDevices.RemoveAt(i);
						i--;
						continue;
					}
				}

				if (DiscountCheckBox != null && (bool)DiscountCheckBox.IsChecked)
				{
					if (gamesFilteredByDevices[i].Discount.Contains("출시"))
					{
						gamesFilteredByDevices.RemoveAt(i);
						i--;
						continue;
					}

					if ((gamesFilteredByDevices[i].Discount == "" || gamesFilteredByDevices[i].Discount == "판매 중지" || gamesFilteredByDevices[i].Discount.Contains("무료")) && gamesFilteredByDevices[i].Bundle.Count == 0)
					{
						gamesFilteredByDevices.RemoveAt(i);
						i--;
						continue;
					}

					if (!gamesFilteredByDevices[i].Discount.Contains("할인") && gamesFilteredByDevices[i].Bundle.Count > 0)
                    {
						var discount = false;
						foreach (var bundle in gamesFilteredByDevices[i].Bundle)
                        {
							if (bundle.DiscountType.Contains("할인"))
                            {
								discount = true;
								break;
                            }
                        }

						if (!discount)
						{
							gamesFilteredByDevices.RemoveAt(i);
							i--;
							continue;
						}
					}
				}

				if (PlayAnywhereCheckBox != null && (bool)PlayAnywhereCheckBox.IsChecked && gamesFilteredByDevices[i].PlayAnywhere == "" ||
					DolbyAtmosCheckBox != null && (bool)DolbyAtmosCheckBox.IsChecked && gamesFilteredByDevices[i].DolbyAtmos == "" ||
					ConsoleKeyboardMouseCheckBox != null && (bool)ConsoleKeyboardMouseCheckBox.IsChecked && gamesFilteredByDevices[i].ConsoleKeyboardMouse == "" ||
					LocalCoopCheckBox != null && (bool)LocalCoopCheckBox.IsChecked && gamesFilteredByDevices[i].LocalCoop == "" ||
					OnlineCoopCheckBox != null && (bool)OnlineCoopCheckBox.IsChecked && gamesFilteredByDevices[i].OnlineCoop == "" ||
					FPS120CheckBox != null && (bool)FPS120CheckBox.IsChecked && gamesFilteredByDevices[i].FPS120 == "" ||
					FPSBoostCheckBox != null && (bool)FPSBoostCheckBox.IsChecked && gamesFilteredByDevices[i].FPSBoost == "" ||
					F2PCheckBox != null && (bool)F2PCheckBox.IsChecked && !gamesFilteredByDevices[i].Discount.Contains("무료")) {
					gamesFilteredByDevices.RemoveAt(i);
					i--;
					continue;
				}

				if (FamilyKidsCheckBox.IsChecked == true ||
					FightingCheckBox.IsChecked == true ||
					EducationalCheckBox.IsChecked == true ||
					RacingFlyingCheckBox.IsChecked == true ||
					RolePlayingCheckBox.IsChecked == true ||
					MultiplayCheckBox.IsChecked == true ||
					ShooterCheckBox.IsChecked == true ||
					SportsCheckBox.IsChecked == true ||
					SimulationCheckBox.IsChecked == true ||
					ActionAdventureCheckBox.IsChecked == true ||
					MusicCheckBox.IsChecked == true ||
					StrategyCheckBox.IsChecked == true ||
					CardBoardCheckBox.IsChecked == true ||
					ClassicsCheckBox.IsChecked == true ||
					PuzzleTriviaCheckBox.IsChecked == true ||
					PlatformerCheckBox.IsChecked == true ||
					CasinoCheckBox.IsChecked == true ||
					OtherCheckBox.IsChecked == true)
				{
					var hasCateogry = false;
					foreach (var category in gamesFilteredByDevices[i].Categories)
					{
						if ((FamilyKidsCheckBox.IsChecked == true && category == "family & kids") ||
							(FightingCheckBox.IsChecked == true && category == "fighting") ||
							(EducationalCheckBox.IsChecked == true && category == "educational") ||
							(RacingFlyingCheckBox.IsChecked == true && category == "racing & flying") ||
							(RolePlayingCheckBox.IsChecked == true && category == "role playing") ||
							(MultiplayCheckBox.IsChecked == true && category == "multi-player online battle arena") ||
							(ShooterCheckBox.IsChecked == true && category == "shooter") ||
							(SportsCheckBox.IsChecked == true && category == "sports") ||
							(SimulationCheckBox.IsChecked == true && category == "simulation") ||
							(ActionAdventureCheckBox.IsChecked == true && category == "action & adventure") ||
							(MusicCheckBox.IsChecked == true && category == "music") ||
							(StrategyCheckBox.IsChecked == true && category == "strategy") ||
							(CardBoardCheckBox.IsChecked == true && category == "card + board") ||
							(ClassicsCheckBox.IsChecked == true && category == "classics") ||
							(PuzzleTriviaCheckBox.IsChecked == true && category == "puzzle & trivia") ||
							(PlatformerCheckBox.IsChecked == true && category == "platformer") ||
							(CasinoCheckBox.IsChecked == true && category == "casino") ||
							(OtherCheckBox.IsChecked == true && category == "other"))
						{
							hasCateogry = true;
							break;
						}
					}

					if (!hasCateogry)
                    {
						gamesFilteredByDevices.RemoveAt(i);
						i--;
						continue;
					}
				}

				if (text.Trim() != "" && 
					!gamesFilteredByDevices[i].KoreanName.ToLower().Contains(text.ToLower().Trim()) &&
					!gamesFilteredByDevices[i].Name.ToLower().Contains(text.ToLower().Trim()))
				{
					gamesFilteredByDevices.RemoveAt(i);
					i--;
					continue;
				}
			}

			if (GamePassCheckBox.IsChecked == true ||
				DiscountCheckBox.IsChecked == true ||
				PlayAnywhereCheckBox.IsChecked == true ||
				DolbyAtmosCheckBox.IsChecked == true ||
				ConsoleKeyboardMouseCheckBox.IsChecked == true ||
				LocalCoopCheckBox.IsChecked == true ||
				OnlineCoopCheckBox.IsChecked == true ||
				FPS120CheckBox.IsChecked == true ||
				FPSBoostCheckBox.IsChecked == true ||
				F2PCheckBox.IsChecked == true)
            {
				if (AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Xbox")
					CapabilityFilterButton.Foreground = new SolidColorBrush(Colors.Yellow);
				else
					CapabilityFilterButton.Foreground = new SolidColorBrush(Colors.Red);
			}
			else
			{
				if (AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Xbox")
					CapabilityFilterButton.Foreground = new SolidColorBrush(Colors.White);
				else
					CapabilityFilterButton.Foreground = new SolidColorBrush(Colors.Black);
			}

			if (FamilyKidsCheckBox.IsChecked == true ||
				FightingCheckBox.IsChecked == true ||
				EducationalCheckBox.IsChecked == true ||
				RacingFlyingCheckBox.IsChecked == true ||
				RolePlayingCheckBox.IsChecked == true ||
				MultiplayCheckBox.IsChecked == true ||
				ShooterCheckBox.IsChecked == true ||
				SportsCheckBox.IsChecked == true ||
				SimulationCheckBox.IsChecked == true ||
				ActionAdventureCheckBox.IsChecked == true ||
				MusicCheckBox.IsChecked == true ||
				StrategyCheckBox.IsChecked == true ||
				CardBoardCheckBox.IsChecked == true ||
				ClassicsCheckBox.IsChecked == true ||
				PuzzleTriviaCheckBox.IsChecked == true ||
				PlatformerCheckBox.IsChecked == true ||
				CasinoCheckBox.IsChecked == true ||
				OtherCheckBox.IsChecked == true)
			{
				if (AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Xbox")
					CategoryFilterButton.Foreground = new SolidColorBrush(Colors.Yellow);
				else
					CategoryFilterButton.Foreground = new SolidColorBrush(Colors.Red);
			}
			else
			{
				if (AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Xbox")
					CategoryFilterButton.Foreground = new SolidColorBrush(Colors.White);
				else
					CategoryFilterButton.Foreground = new SolidColorBrush(Colors.Black);
			}

			var games = SortList(gamesFilteredByDevices);

			GamesViewModel.Clear();
			foreach (var g in games)
			{
				GamesViewModel.Add(new GameViewModel(g, mGameNameDisplayLanguage, mOneTitleHeader, mSeriesXSHeader, mPlayAnywhereHeader, mPlayAnywhereSeriesHeader, mWindowsHeader, mShowRecommendTag, mShowDiscount, mShowGamepass, mShowName, mShowReleaseTime));
			}

			TitleBlock.Text = $"한국어 지원 타이틀 목록 ({games.Count:#,#0}개)";
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
			SearchBox_TextChanged(SearchBox, null);

			await Settings.Instance.SetValue("orderType", "name_asc");
		}

		private async void OrderByNameDescendItem_Click(object sender, RoutedEventArgs e)
		{
			SearchBox_TextChanged(SearchBox, null);

			await Settings.Instance.SetValue("orderType", "name_desc");
		}

		private async void OrderByReleaseAscendItem_Click(object sender, RoutedEventArgs e)
		{
			SearchBox_TextChanged(SearchBox, null);

			await Settings.Instance.SetValue("orderType", "release_asc");
		}

		private async void OrderByReleaseDescendItem_Click(object sender, RoutedEventArgs e)
		{
			SearchBox_TextChanged(SearchBox, null);

			await Settings.Instance.SetValue("orderType", "release_desc");
		}

		private List<Game> FilterByDevices(List<Game> games)
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
					{
						if (CategoryWindowsCheckBox.IsChecked == true || game.Message.ToLower().IndexOf("windowsmod") == -1)
							selectGamesList.Add(game);
					}
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

				return selectGamesList;
			}
			else
				return (from g in games
						select g).ToList();
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

		private void RecommendCheckBox_Click(object sender, RoutedEventArgs e)
		{
			SearchBox_TextChanged(SearchBox, null);

			var localSettings = ApplicationData.Current.LocalSettings;

			localSettings.Values["recommendPriority"] = (sender as CheckBox).IsChecked;
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

			UpdateDeviceFilterButton();
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
				ProgressDownload.Visibility = Visibility.Collapsed;
			}

			await CheckUpdateTime();
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

					var localSettings = ApplicationData.Current.LocalSettings;
					if (localSettings.Values["showRecommendTag"] != null)
						mShowRecommendTag = (bool)localSettings.Values["showRecommendTag"];
					else
						mShowRecommendTag = false;

					if (localSettings.Values["showDiscount"] != null)
						mShowDiscount = (bool)localSettings.Values["showDiscount"];
					else
						mShowDiscount = true;

					if (localSettings.Values["showGamepass"] != null)
						mShowGamepass = (bool)localSettings.Values["showGamepass"];
					else
						mShowGamepass = true;

					if (localSettings.Values["showName"] != null)
						mShowName = (bool)localSettings.Values["showName"];
					else
						mShowName = true;

					if (localSettings.Values["showReleaseTime"] != null)
						mShowReleaseTime = (bool)localSettings.Values["showReleaseTime"];
					else
						mShowReleaseTime = false;

					foreach (var gameViewModel in GamesViewModel) {
						gameViewModel.GameNameDisplayLanguage = mGameNameDisplayLanguage;
						gameViewModel.UpdateShowRecommendTag(mShowRecommendTag);
						gameViewModel.UpdateShowDiscount(mShowDiscount);
						gameViewModel.UpdateShowGamepass(mShowGamepass);
						gameViewModel.UpdateShowName(mShowName);
						gameViewModel.UpdateShowReleaseTime(mShowReleaseTime);
					}
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

		private async void GamesView_KeyUp(object sender, Windows.UI.Xaml.Input.KeyRoutedEventArgs e)
		{
			if (AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Xbox")
			{
				var game = (e.OriginalSource as GridViewItem).Content as GameViewModel;
				switch (e.Key)
				{
					case VirtualKey.GamepadMenu:
						await ShowErrorReportDialog(game);
						break;
					case VirtualKey.GamepadX:
						await RecommendGame(game);
						break;
					case VirtualKey.GamepadY:
						await ShowPackageInfo(game);
						break;
				}
			}
		}

		private async void MenuRecommend_Click(object sender, RoutedEventArgs e)
		{
			var game = (e.OriginalSource as MenuFlyoutItem).DataContext as GameViewModel;
			await RecommendGame(game);
		}

		private async void MenuPackages_Click(object sender, RoutedEventArgs e)
        {
			var game = (e.OriginalSource as MenuFlyoutItem).DataContext as GameViewModel;
			await ShowPackageInfo(game);			
		}

		private async Task ShowPackageInfo(GameViewModel game)
        {
			var supportPackageBuilder = new StringBuilder();
			if (game.Game.Packages != "")
				supportPackageBuilder.Append("* 한국어 지원 패키지: ").Append(game.Game.Packages);
			else
				supportPackageBuilder.Append("* 확인된 패키지가 없거나 정식 발매 패키지만 한국어를 지원합니다. 확인하신 패키지가 있으면, 오류 신고 기능을 이용해 신고해 주십시오.").Append(game.Game.Packages);

			if (game.Game.Message.ToLower().Contains("dlregiononly"))
				supportPackageBuilder.Append("\r\n").Append("* 한국어를 지원하지 않는 지역이 있습니다. 해외 패키지 구매시 한국어 지원 여부를 확인해 주십시오.");

			var dialog = new MessageDialog(supportPackageBuilder.ToString(), "한국어 지원 패키지 정보");
			if (mDialogQueue.TryAdd(dialog, 500))
			{
				await dialog.ShowAsync();
				mDialogQueue.Take();
			}
		}

		private async Task RecommendGame(GameViewModel game) {
			var message = "";

			if (mRecommendCount == 0)
				message = "추천은 한번에 5번까지만 하실 수 있습니다.";
			else
			{
				var requestParam = new Dictionary<string, string>
				{
					["product_id"] = game.ID,
					["device_id"] = mDeviceID
				};


				try
				{
					var httpClient = new HttpClient();

#if DEBUG
					//var response = await httpClient.PostAsync(new Uri("http://192.168.200.8:3000/recommend"), new HttpStringContent(JsonConvert.SerializeObject(requestParam), Windows.Storage.Streams.UnicodeEncoding.Utf8, "application/json"));
					var response = await httpClient.PostAsync(new Uri("http://127.0.0.1:3000/recommend"), new HttpStringContent(JsonConvert.SerializeObject(requestParam), Windows.Storage.Streams.UnicodeEncoding.Utf8, "application/json"));
#else
					var response = await httpClient.PostAsync(new Uri("https://xbox-korean-viewer-server2.herokuapp.com/recommend"), new HttpStringContent(JsonConvert.SerializeObject(requestParam), Windows.Storage.Streams.UnicodeEncoding.Utf8, "application/json"));
#endif

					var str = await response.Content.ReadAsStringAsync();

					var resultMap = new Dictionary<string, string>();

					try
					{
						resultMap = JsonConvert.DeserializeObject<Dictionary<string, string>>(str);
					}
					catch (JsonReaderException jsonErr)
					{
						resultMap["code"] = "PARSE_ERROR";
					}

					if (resultMap["code"] == "SUCCESS")
					{
						message = "해당 게임을 추천하였습니다.";
						mRecommendCount--;
					}
					else if (resultMap["code"] == "ALREADY_RECOMMEND")
						message = "이미 추천한 게임입니다.";
					else
						message = "해당 게임을 추천할 수 없습니다. 잠시 후 다시 시도해 주십시오.";
				}
				catch (Exception err)
				{
					message = "서버와 연결할 수 없습니다. 잠시 후 다시 시도해 주십시오.";
				}
			}

			var content = new ToastContentBuilder()
				.AddText("추천 결과", hintMaxLines: 1)
				.AddText(message)
				.GetToastContent();

			var notif = new ToastNotification(content.GetXml());

			// And show it!
			ToastNotificationManager.History.Clear();
			ToastNotificationManager.CreateToastNotifier().Show(notif);
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


		private void CategoryCheckBox_Click(object sender, RoutedEventArgs e)
		{
			if (sender == ResetCategoryFilter ||
				sender == CategorySeriesXSCheckBox ||
				sender == CategoryOneXEnhancedCheckBox ||
				sender == CategoryOneCheckBox ||
				sender == CategoryX360CheckBox ||
				sender == CategoryOGCheckBox || 
				sender == CategoryWindowsCheckBox ||
				sender == CategoryCloudCheckBox)
				UpdateCategoriesState();
			SearchBox_TextChanged(SearchBox, null);
		}

		private void TimingRadioButton_Click(object sender, RoutedEventArgs e)
		{
			SearchBox_TextChanged(SearchBox, null);
		}

		private void UpdateDeviceFilterButton()
        {
			if (CategorySeriesXSCheckBox.IsChecked == true ||
				CategoryOneXEnhancedCheckBox.IsChecked == true ||
				CategoryX360CheckBox.IsChecked == true ||
				CategoryOGCheckBox.IsChecked == true ||
				CategoryWindowsCheckBox.IsChecked == true ||
				CategoryCloudCheckBox.IsChecked == true)
			{
				if (AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Xbox")
					DeviceFilterButton.Foreground = new SolidColorBrush(Colors.Yellow);
				else
					DeviceFilterButton.Foreground = new SolidColorBrush(Colors.Red);
			}
			else
            {
				if (AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Xbox")
					DeviceFilterButton.Foreground = new SolidColorBrush(Colors.White);
				else
					DeviceFilterButton.Foreground = new SolidColorBrush(Colors.Black);
			}
		}

        private void ResetDeviceFilter_Click(object sender, RoutedEventArgs e)
        {
			if (CategorySeriesXSCheckBox.IsChecked == true ||
				CategoryOneXEnhancedCheckBox.IsChecked == true ||
				CategoryOneCheckBox.IsChecked == true ||
				CategoryX360CheckBox.IsChecked == true ||
				CategoryOGCheckBox.IsChecked == true ||
				CategoryWindowsCheckBox.IsChecked == true ||
				CategoryCloudCheckBox.IsChecked == true)
			{
				CategorySeriesXSCheckBox.IsChecked = false;
				CategoryOneXEnhancedCheckBox.IsChecked = false;
				CategoryOneCheckBox.IsChecked = false;
				CategoryX360CheckBox.IsChecked = false;
				CategoryOGCheckBox.IsChecked = false;
				CategoryWindowsCheckBox.IsChecked = false;
				CategoryCloudCheckBox.IsChecked = false;

				CategoryCheckBox_Click(sender, e);
			}
		}

		private void ResetCapabilityFilter_Click(object sender, RoutedEventArgs e)
		{
			if (GamePassCheckBox.IsChecked == true ||
				GamePassExcludeCheckBox.IsChecked == true ||
				DiscountCheckBox.IsChecked == true ||
				PlayAnywhereCheckBox.IsChecked == true ||
				DolbyAtmosCheckBox.IsChecked == true ||
				ConsoleKeyboardMouseCheckBox.IsChecked == true ||
				LocalCoopCheckBox.IsChecked == true ||
				OnlineCoopCheckBox.IsChecked == true ||
				FPS120CheckBox.IsChecked == true ||
				FPSBoostCheckBox.IsChecked == true ||
				F2PCheckBox.IsChecked == true)
			{
				GamePassCheckBox.IsChecked = false;
				GamePassExcludeCheckBox.IsChecked = false;
				DiscountCheckBox.IsChecked = false;
				PlayAnywhereCheckBox.IsChecked = false;
				DolbyAtmosCheckBox.IsChecked = false;
				ConsoleKeyboardMouseCheckBox.IsChecked = false;
				LocalCoopCheckBox.IsChecked = false;
				OnlineCoopCheckBox.IsChecked = false;
				FPS120CheckBox.IsChecked = false;
				FPSBoostCheckBox.IsChecked = false;
				F2PCheckBox.IsChecked = false;

				CategoryCheckBox_Click(sender, e);
			}
		}

		private void ResetCategoryFilter_Click(object sender, RoutedEventArgs e)
		{
			if (FamilyKidsCheckBox.IsChecked == true ||
				FightingCheckBox.IsChecked == true ||
				EducationalCheckBox.IsChecked == true ||
				RacingFlyingCheckBox.IsChecked == true ||
				RolePlayingCheckBox.IsChecked == true ||
				MultiplayCheckBox.IsChecked == true ||
				ShooterCheckBox.IsChecked == true ||
				SportsCheckBox.IsChecked == true ||
				SimulationCheckBox.IsChecked == true ||
				ActionAdventureCheckBox.IsChecked == true ||
				MusicCheckBox.IsChecked == true ||
				StrategyCheckBox.IsChecked == true ||
				CardBoardCheckBox.IsChecked == true ||
				ClassicsCheckBox.IsChecked == true ||
				PuzzleTriviaCheckBox.IsChecked == true ||
				PlatformerCheckBox.IsChecked == true ||
				CasinoCheckBox.IsChecked == true ||
				OtherCheckBox.IsChecked == true)
			{
				FamilyKidsCheckBox.IsChecked = false;
				FightingCheckBox.IsChecked = false;
				EducationalCheckBox.IsChecked = false;
				RacingFlyingCheckBox.IsChecked = false;
				RolePlayingCheckBox.IsChecked = false;
				MultiplayCheckBox.IsChecked = false;
				ShooterCheckBox.IsChecked = false;
				SportsCheckBox.IsChecked = false;
				SimulationCheckBox.IsChecked = false;
				ActionAdventureCheckBox.IsChecked = false;
				MusicCheckBox.IsChecked = false;
				StrategyCheckBox.IsChecked = false;
				CardBoardCheckBox.IsChecked = false;
				ClassicsCheckBox.IsChecked = false;
				PuzzleTriviaCheckBox.IsChecked = false;
				PlatformerCheckBox.IsChecked = false;
				CasinoCheckBox.IsChecked = false;
				OtherCheckBox.IsChecked = false;

				CategoryCheckBox_Click(sender, e);
			}
		}
	}
}
