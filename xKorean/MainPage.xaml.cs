using Microsoft.Data.Sqlite;
using Microsoft.Toolkit.Uwp.Notifications;
using Microsoft.Toolkit.Uwp.UI.Controls;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Windows.Globalization;
using Windows.Graphics.Imaging;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Core;
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
using Windows.UI.Xaml.Controls.Primitives;
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

		private Dictionary<string, float> mExchangeRateMap = new Dictionary<string, float>();

		private readonly BlockingCollection<object> mDialogQueue = new BlockingCollection<object>(1);
		private readonly BlockingCollection<object> mTipQueue = new BlockingCollection<object>(1);

		private const string windowsTitlePath = "ms-appx:///Assets/windows_title.png";
		private const string oneTitlePath = "ms-appx:///Assets/xbox_one_title.png";
		private const string seriesTitlePath = "ms-appx:///Assets/xbox_series_xs_title.png";
		private const string playAnywherePath = "ms-appx:///Assets/xbox_playanywhere_title.png";
		private const string playAnywhereSeriesPath = "ms-appx:///Assets/xbox_playanywhere_xs_title.png";

		private List<Game> mExistGames = new List<Game>();
		private List<string> mNewGames = new List<string>();
		private List<string> mEventIDList = new List<string>();

		private string mEditionLanguage;

		private int mSelectedIdx = 0;

		private string mDeviceID = "";

		private bool mShowDiscount = true;
		private bool mShowGamepass = true;
		private bool mShowName = true;
		private bool mShowReleaseTime = false;

		private Game mSelectedGame = null;
		private EditionViewModel mSelectedEdition = null;

        private readonly char[] mKorChr = { 'ㄱ', 'ㄲ', 'ㄴ', 'ㄷ', 'ㄸ', 'ㄹ', 'ㅁ', 'ㅂ', 'ㅃ', 'ㅅ', 'ㅆ', 'ㅇ', 'ㅈ', 'ㅉ', 'ㅊ', 'ㅋ', 'ㅌ', 'ㅍ', 'ㅎ' };
        private readonly string[] mKorStr = { "가", "까", "나", "다", "따", "라", "마", "바", "빠", "사", "싸", "아", "자", "짜", "차","카","타", "파", "하" };
        private readonly int[] mKorChrInt = { 44032, 44620, 45208, 45796, 46384, 46972, 47560, 48148, 48736, 49324, 49912, 50500, 51088, 51676, 52264, 52852, 53440, 54028, 54616, 55204 };

		public MainPage()
		{
			this.InitializeComponent();

            mExchangeRateMap["en-us"] = 1300;
            mExchangeRateMap["ja-jp"] = 980;
            mExchangeRateMap["en-hk"] = 165;
			mExchangeRateMap["en-gb"] = 1570;

            if (AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Xbox")
				DonationButton.Visibility = Visibility.Collapsed;

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
			mMessageTemplateMap["usermode"] = "이 게임은 유저 모드를 설치하셔야 한국어가 지원됩니다.";					// 오타 버전. 삭제 예정
            mMessageTemplateMap["usermod"] = "이 게임은 유저 모드를 설치하셔야 한국어가 지원됩니다.";
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
			await ApplicationData.Current.LocalFolder.CreateFileAsync("xKorean.sqlite", CreationCollisionOption.OpenIfExists);
			CommonSingleton.Instance.DBPath = Path.Combine(ApplicationData.Current.LocalFolder.Path, "xKorean.sqlite");
			using (var db = new SqliteConnection($"FileName={CommonSingleton.Instance.DBPath}"))
            {
				db.Open();

				new SqliteCommand("CREATE TABLE IF NOT EXISTS ThumbnailTable (id TEXT PRIMARY KEY NOT NULL UNIQUE, " +
					"info TEXT NOT NULL DEFAULT '')", db).ExecuteReader();
            }

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
								CommonSingleton.Instance.OneTitleHeader = titleBuffer;
							else if (fileName == seriesTitlePath)
								CommonSingleton.Instance.SeriesXSTitleHeader = titleBuffer;
							else if (fileName == playAnywherePath)
								CommonSingleton.Instance.PlayAnywhereTitleHeader = titleBuffer;
							else if (fileName == playAnywhereSeriesPath)
								CommonSingleton.Instance.PlayAnywhereSeriesTitleHeader = titleBuffer;
							else
								CommonSingleton.Instance.WindowsTitleHeader = titleBuffer;

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

			var priorityType = settings.LoadValue("priorityType");
			switch (priorityType) {
				case "":
				case "none":
					PriorityNoneItem.IsChecked = true;
					break;
				case "gamepass":
					PriorityByGamepassItem.IsChecked = true;
					break;
				case "discount":
					PriorityByDiscountItem.IsChecked = true;
					break;
				case "price":
                    PriorityByPriceItem.IsChecked = true;
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
				await CheckEventData();
		}

		private async Task CheckEventData()
        {
			var httpClient = new HttpClient();

			try
			{
#if DEBUG
				var response = await httpClient.PostAsync(new Uri("http://127.0.0.1:8080/get_event_list"), new HttpStringContent("{}", Windows.Storage.Streams.UnicodeEncoding.Utf8, "application/json"));
#else
				var response = await httpClient.PostAsync(new Uri("https://xKorean.info/get_event_list"), new HttpStringContent("{}", Windows.Storage.Streams.UnicodeEncoding.Utf8, "application/json"));
#endif

				var str = response.Content.ReadAsStringAsync().GetResults();

				var eventIDList = JsonConvert.DeserializeObject<List<string>>(str);
				mEventIDList.Clear();
				foreach (var eventID in eventIDList)
                {
					mEventIDList.Add(eventID);
                }

				await CheckUpdateTime();
			}
			catch (Exception exception)
			{
				await CheckUpdateTime();
			}
		}

		private async Task CheckUpdateTime()
		{
			var now = DateTime.Now;

			var httpClient = new HttpClient();
			
			try
			{
#if DEBUG
				var response = await httpClient.PostAsync(new Uri("http://127.0.0.1:8080/last_modified_time"), new HttpStringContent("{}", Windows.Storage.Streams.UnicodeEncoding.Utf8, "application/json"));
#else
				var response = await httpClient.PostAsync(new Uri("https://xKorean.info/last_modified_time"), new HttpStringContent("{}", Windows.Storage.Streams.UnicodeEncoding.Utf8, "application/json"));
#endif

                var str = response.Content.ReadAsStringAsync().GetResults();

				var settingMap = JsonConvert.DeserializeObject<Dictionary<string, string>>(str);

				var downloadedJsonFile = new FileInfo(ApplicationData.Current.LocalFolder.Path + "\\games_ex.json");
				
				var settings = Settings.Instance;
				if (settingMap["lastModifiedTime"] == "")
				{
					if (LoadingPanel.Visibility == Visibility.Visible)
						LoadingPanel.Visibility = Visibility.Collapsed;

					if (CheckDownloadData())
					{
						var content = new ToastContentBuilder()
							.AddText("업데이트 오류", hintMaxLines: 1)
							.AddText("서버 정보를 업데이트를 중이므로 기존 정보를 표시합니다. 잠시 후에 다시 시도해 주십시오.")
							.GetToastContent();

						var notif = new ToastNotification(content.GetXml());

						// And show it!
						ToastNotificationManager.History.Clear();
						ToastNotificationManager.CreateToastNotifier().Show(notif);

						ReadGamesFromJson();
					}
					else
					{
						var dialog = new MessageDialog("현재 서버 정보를 최신 정보로 업데이트 중입니다. 잠시 후에 다시 시도해 주십시오.", "데이터 수신 오류");
						if (mDialogQueue.TryAdd(dialog, 500))
						{
							await dialog.ShowAsync();
							mDialogQueue.Take();
						}
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

				if (CheckDownloadData())
				{
					var content = new ToastContentBuilder()
						.AddText("업데이트 오류", hintMaxLines: 1)
						.AddText("서버에서 정보를 확인할 수 없어서, 기존 정보를 표시합니다. 잠시 후에 다시 시도해 주십시오.")
						.GetToastContent();

					var notif = new ToastNotification(content.GetXml());

					// And show it!
					ToastNotificationManager.History.Clear();
					ToastNotificationManager.CreateToastNotifier().Show(notif);

					ReadGamesFromJson();
				}
				else
				{
					var dialog = new MessageDialog($"서버에서 한국어 지원 정보를 확인할 수 없습니다. 잠시 후 다시 시도해 주십시오.{additionalMessage}", "데이터 수신 오류");
					if (mDialogQueue.TryAdd(dialog, 500))
					{
						await dialog.ShowAsync();
						mDialogQueue.Take();
					}
				}
			}
		}

		private bool CheckDownloadData()
		{
			var downloadedJsonFile = new FileInfo(ApplicationData.Current.LocalFolder.Path + "\\games_ex.json");

			if (downloadedJsonFile.Exists && downloadedJsonFile.Length > 0)
				return true;
			else
				return false;
		}

		private async void UpateJsonData()
		{
			var httpClient = new HttpClient();

			try
			{
				var client = new HttpClient();


#if DEBUG
				var request = new HttpRequestMessage(HttpMethod.Post, new Uri("http://127.0.0.1:8080/title_list_ex_zip"));
#else
				var request = new HttpRequestMessage(HttpMethod.Post, new Uri("https://xKorean.info/title_list_ex_zip"));
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
					var downloadedJsonFile = new FileInfo(ApplicationData.Current.LocalFolder.Path + "\\games_ex.json");

					if (downloadedJsonFile.Exists)
					{
						var existFile = await ApplicationData.Current.LocalFolder.CreateFileAsync("games_ex.json", CreationCollisionOption.OpenIfExists);

						if (downloadedJsonFile.Length > 0)
						{
							var existStr = await FileIO.ReadTextAsync(existFile);
							var existData = JsonConvert.DeserializeObject<ServerData>(existStr);
							mExistGames = existData.Games.OrderBy(g => g.ID).ToList();
						}

						await existFile.DeleteAsync();
					}
				}

				File.WriteAllText(ApplicationData.Current.LocalFolder.Path + "\\games_ex.json", str);

				ReadGamesFromJson();
			}
			catch (Exception exception)
			{
				if (LoadingPanel.Visibility == Visibility.Visible)
					LoadingPanel.Visibility = Visibility.Collapsed;

				if (CheckDownloadData())
				{
					var content = new ToastContentBuilder()
						.AddText("업데이트 오류", hintMaxLines: 1)
						.AddText("서버에서 정보를 다운로드 할 수 없어서, 기존 정보를 표시합니다. 잠시 후에 다시 시도해 주십시오.")
						.GetToastContent();

					var notif = new ToastNotification(content.GetXml());

					// And show it!
					ToastNotificationManager.History.Clear();
					ToastNotificationManager.CreateToastNotifier().Show(notif);

					ReadGamesFromJson();
				}
				else
				{
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
		}
		private async void ReadGamesFromJson()
		{
			var downloadedJsonFile = new FileInfo(ApplicationData.Current.LocalFolder.Path + "\\games_ex.json");

			StorageFile jsonFile = null;
			if (downloadedJsonFile.Exists && downloadedJsonFile.Length > 0)
			{
				jsonFile = await StorageFile.GetFileFromPathAsync(ApplicationData.Current.LocalFolder.Path + "\\games_ex.json");
			}
			else
			{
				UpateJsonData();
				return;
			}


			var jsonString = await FileIO.ReadTextAsync(jsonFile);
			var serverData = JsonConvert.DeserializeObject<ServerData>(jsonString);
            var games = serverData.Games.OrderBy(g => g.ID).ToList();

			foreach (var exchangeRate in serverData.ExchangeRates)
			{
				mExchangeRateMap[exchangeRate.Country] = exchangeRate.Rate;
			}

			if (AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Xbox") {
				for (var i = 0; i < games.Count; i++) {
					if (games[i].OG != "O" && games[i].X360 != "O" && games[i].OneS != "O" && games[i].SeriesXS != "O" && games[i].PC == "O" || games[i].Message.ToLower().IndexOf("windowsmod") >= 0)
					{
						games.RemoveAt(i);
						i--;
					}
				}
			}

            mNewGames.Clear();
            if (mExistGames.Count > 0)
			{
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
			
			if (e.ClickedItem != null)
			{
				mSelectedIdx = gridView.Items.IndexOf(e.ClickedItem);

				var game = (e.ClickedItem as GameViewModel).Game;

				var buffer = CryptographicBuffer.ConvertStringToBinary(game.ID, BinaryStringEncoding.Utf8);
				var hashAlgorithm = HashAlgorithmProvider.OpenAlgorithm(HashAlgorithmNames.Sha1);
				var hashByte = hashAlgorithm.HashData(buffer).ToArray();
				var sb = new StringBuilder();
				foreach (var b in hashByte)
                {
					sb.Append(b.ToString("x2"));
                }

				var win = false;
				foreach (var id in mEventIDList)
                {
					if (sb.ToString() == id)
                    {
						win = true;
						mEventIDList.Remove(id);
						await RequestEventCode(id, game);
						break;
                    }
                }

				if (!win)
					await CheckExtraInfo(game);
			}
		}

		private async Task RequestEventCode(string id, Game game)
		{
			var httpClient = new HttpClient();

			try
			{
				var requestParam = new Dictionary<string, string>
				{
					["device_id"] = mDeviceID,
                    ["id"] = id
				};

#if DEBUG
				var response = await httpClient.PostAsync(new Uri("http://127.0.0.1:8080/request_event_code"), new HttpStringContent(JsonConvert.SerializeObject(requestParam), Windows.Storage.Streams.UnicodeEncoding.Utf8, "application/json"));
#else
				var response = await httpClient.PostAsync(new Uri("https://xKorean.info/request_event_code"), new HttpStringContent(JsonConvert.SerializeObject(requestParam), Windows.Storage.Streams.UnicodeEncoding.Utf8, "application/json"));
#endif

                var str = response.Content.ReadAsStringAsync().GetResults();

				var eventIDList = JsonConvert.DeserializeObject<Dictionary<string, string>>(str);

				if (eventIDList["code"] == "")
					await CheckExtraInfo(game);
				else
                {
					var dialog = new MessageDialog($"이벤트에 당첨되었습니다. 아래 코드를 활성화 하시면 게임을 받으실 수 있습니다.\n이 창을 닫으면 코드를 다시 확인할 수 없으니 창을 닫기 전에 코드를 활성화하거나 다른 곳에 적어두십시오.\n\n{eventIDList["code"]}", "이벤트 당첨");
					if (mDialogQueue.TryAdd(dialog, 500))
					{
						await dialog.ShowAsync();
						mDialogQueue.Take();
					}
				}
			}
			catch (Exception exception)
			{
				await CheckExtraInfo(game);
			}
		}

		private void ShowEditionPanel(Game game)
		{
			EditionPanelView.Visibility = Visibility.Visible;

			mEditionLanguage = game.LanguageCode;

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
					GamePassComing = game.GamePassComing,
					ThumbnailUrl = game.Thumbnail,
					ThumbnailID = game.ThumbnailID,
					ShowDiscount = mShowDiscount,
					ShowGamePass = mShowGamepass,
					ShowName = mShowName,
					Price = game.Price,
					LowestPrice = game.LowestPrice,
					LanguageCode = game.LanguageCode,
					ReleaseDate = game.ReleaseDate,
					NZReleaseDate = game.NZReleaseDate,
					KIReleaseDate = game.KIReleaseDate
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
					PlayAnywhere = bundle.PlayAnywhere,
					IsGamePassPC = bundle.GamePassPC,
					IsGamePassConsole = bundle.GamePassConsole,
					IsGamePassCloud = bundle.GamePassCloud,
					GamePassNew = bundle.GamePassNew,
					GamePassEnd = bundle.GamePassEnd,
					GamePassComing = bundle.GamePassComing,
					ThumbnailUrl = bundle.Thumbnail,
					ThumbnailID = bundle.ThumbnailID,
					ShowDiscount = mShowDiscount,
					ShowGamePass = mShowGamepass,
					ShowName = mShowName,
					Price = bundle.Price,
					LowestPrice = bundle.LowestPrice,
					LanguageCode = game.LanguageCode,
					ReleaseDate = bundle.ReleaseDate,
					NZReleaseDate = bundle.NZReleaseDate,
					KIReleaseDate = bundle.KIReleaseDate
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
				if (PriorityByGamepassItem.IsChecked == true)
				{
					var gamePassComingList = new List<Game>();
					var gamePassNewList = new List<Game>();
					var gamePassList = new List<Game>();
					var gamePassEndList = new List<Game>();
					var nonGamePassList = new List<Game>();

					foreach (var game in unSortedGames)
					{
						if (game.GamePassCloud == "" && game.GamePassPC == "" && game.GamePassConsole == "")
						{
							if (game.Bundle.Count > 0)
							{
								foreach (var bundle in game.Bundle)
								{
									if (bundle.GamePassCloud == "O" || bundle.GamePassPC == "O" || bundle.GamePassConsole == "O")
									{
										if (bundle.GamePassNew == "O")
											gamePassNewList.Add(game);
										else if (bundle.GamePassEnd == "O")
											gamePassEndList.Add(game);
										else if (bundle.GamePassComing == "O")
											gamePassComingList.Add(game);
										else
											gamePassList.Add(game);
										break;
									}
								}
							}
						}
						else
						{
							if (game.GamePassNew == "O")
								gamePassNewList.Add(game);
							else if (game.GamePassEnd == "O")
								gamePassEndList.Add(game);
							else if (game.GamePassComing == "O")
								gamePassComingList.Add(game);
							else
								gamePassList.Add(game);
						}
					}

					IOrderedEnumerable<Game> sortedGamePassComingList;
					IOrderedEnumerable<Game> sortedGamePassNewList;
					IOrderedEnumerable<Game> sortedGamePassList;
					IOrderedEnumerable<Game> sortedGamePassEndList;

					if (OrderByNameAscendItem.IsChecked == true)
					{
						if (mGameNameDisplayLanguage == "English")
						{
							sortedGamePassComingList = gamePassComingList.OrderBy(g => g.Name);
							sortedGamePassNewList = gamePassNewList.OrderBy(g => g.Name);
							sortedGamePassList = gamePassList.OrderBy(g => g.Name);
							sortedGamePassEndList = gamePassEndList.OrderBy(g => g.Name);
						}
						else
						{
							sortedGamePassComingList = gamePassComingList.OrderBy(g => g.KoreanName);
							sortedGamePassNewList = gamePassNewList.OrderBy(g => g.KoreanName);
							sortedGamePassList = gamePassList.OrderBy(g => g.KoreanName);
							sortedGamePassEndList = gamePassEndList.OrderBy(g => g.KoreanName);
						}
					}
					else if (OrderByNameDescendItem.IsChecked == true)
					{
						if (mGameNameDisplayLanguage == "English")
						{
							sortedGamePassComingList = gamePassComingList.OrderByDescending(g => g.Name);
							sortedGamePassNewList = gamePassNewList.OrderByDescending(g => g.Name);
							sortedGamePassList = gamePassList.OrderByDescending(g => g.Name);
							sortedGamePassEndList = gamePassEndList.OrderByDescending(g => g.Name);
						}
						else
						{
							sortedGamePassComingList = gamePassComingList.OrderByDescending(g => g.KoreanName);
							sortedGamePassNewList = gamePassNewList.OrderByDescending(g => g.KoreanName);
							sortedGamePassList = gamePassList.OrderByDescending(g => g.KoreanName);
							sortedGamePassEndList = gamePassEndList.OrderByDescending(g => g.KoreanName);
						}
					}
					else if (OrderByReleaseAscendItem.IsChecked == true)
					{
						if (mGameNameDisplayLanguage == "English")
						{
							sortedGamePassComingList = gamePassComingList.OrderBy(g => g.ReleaseDate).ThenBy(g => g.Name);
							sortedGamePassNewList = gamePassNewList.OrderBy(g => g.ReleaseDate).ThenBy(g => g.Name);
							sortedGamePassList = gamePassList.OrderBy(g => g.ReleaseDate).ThenBy(g => g.Name);
							sortedGamePassEndList = gamePassEndList.OrderBy(g => g.ReleaseDate).ThenBy(g => g.Name);
						}
						else
						{
							sortedGamePassComingList = gamePassComingList.OrderBy(g => g.ReleaseDate).ThenBy(g => g.KoreanName);
							sortedGamePassNewList = gamePassNewList.OrderBy(g => g.ReleaseDate).ThenBy(g => g.KoreanName);
							sortedGamePassList = gamePassList.OrderBy(g => g.ReleaseDate).ThenBy(g => g.KoreanName);
							sortedGamePassEndList = gamePassEndList.OrderBy(g => g.ReleaseDate).ThenBy(g => g.KoreanName);
						}
					}
					else
					{
						if (mGameNameDisplayLanguage == "English")
						{
							sortedGamePassComingList = gamePassComingList.OrderByDescending(g => g.ReleaseDate).ThenBy(g => g.Name);
							sortedGamePassNewList = gamePassNewList.OrderByDescending(g => g.ReleaseDate).ThenBy(g => g.Name);
							sortedGamePassList = gamePassList.OrderByDescending(g => g.ReleaseDate).ThenBy(g => g.Name);
							sortedGamePassEndList = gamePassEndList.OrderByDescending(g => g.ReleaseDate).ThenBy(g => g.Name);
						}
						else
						{
							sortedGamePassComingList = gamePassComingList.OrderByDescending(g => g.ReleaseDate).ThenBy(g => g.KoreanName);
							sortedGamePassNewList = gamePassNewList.OrderByDescending(g => g.ReleaseDate).ThenBy(g => g.KoreanName);
							sortedGamePassList = gamePassList.OrderByDescending(g => g.ReleaseDate).ThenBy(g => g.KoreanName);
							sortedGamePassEndList = gamePassEndList.OrderByDescending(g => g.ReleaseDate).ThenBy(g => g.KoreanName);
						}
					}

					foreach (var game in sortedGamePassComingList)
					{
						sortedList.Add(game);
						unSortedGames.Remove(game);
					}

					foreach (var game in sortedGamePassNewList)
					{
						sortedList.Add(game);
						unSortedGames.Remove(game);
					}
					
					foreach (var game in sortedGamePassList)
					{
						sortedList.Add(game);
						unSortedGames.Remove(game);
					}
					
					foreach (var game in sortedGamePassEndList)
					{
						sortedList.Add(game);
						unSortedGames.Remove(game);
					}					
				}
				else if (PriorityByDiscountItem.IsChecked == true)
                {
					var discountList = unSortedGames.OrderByDescending(x => x, new GameDiscountComparator());

					if (OrderByNameAscendItem.IsChecked == true)
					{
						if (mGameNameDisplayLanguage == "English")
							discountList = discountList.ThenBy(g => g.Name);
						else
							discountList = discountList.ThenBy(g => g.KoreanName);
					}
					else if (OrderByNameDescendItem.IsChecked == true)
					{
						if (mGameNameDisplayLanguage == "English")
							discountList = discountList.ThenByDescending(g => g.Name);
						else
							discountList = discountList.ThenByDescending(g => g.KoreanName);
					}
					else if (OrderByReleaseAscendItem.IsChecked == true)
					{
						if (mGameNameDisplayLanguage == "English")
							discountList = discountList.ThenBy(g => g.ReleaseDate).ThenBy(g => g.Name);
						else
							discountList = discountList.ThenBy(g => g.ReleaseDate).ThenBy(g => g.KoreanName);
					}
					else
					{
						if (mGameNameDisplayLanguage == "English")
							discountList = discountList.ThenByDescending(g => g.ReleaseDate).ThenBy(g => g.Name);
						else
							discountList = discountList.ThenByDescending(g => g.ReleaseDate).ThenBy(g => g.KoreanName);
					}

					sortedList.AddRange(discountList);
					unSortedGames.Clear();
				}
				else if (PriorityByPriceItem.IsChecked == true)
				{
					var prictList = unSortedGames.OrderBy(x => x, new GamePriceComparator(mExchangeRateMap));

                    if (OrderByNameAscendItem.IsChecked == true)
                    {
                        if (mGameNameDisplayLanguage == "English")
                            prictList = prictList.ThenBy(g => g.Name);
                        else
                            prictList = prictList.ThenBy(g => g.KoreanName);
                    }
                    else if (OrderByNameDescendItem.IsChecked == true)
                    {
                        if (mGameNameDisplayLanguage == "English")
                            prictList = prictList.ThenByDescending(g => g.Name);
                        else
                            prictList = prictList.ThenByDescending(g => g.KoreanName);
                    }
                    else if (OrderByReleaseAscendItem.IsChecked == true)
                    {
                        if (mGameNameDisplayLanguage == "English")
                            prictList = prictList.ThenBy(g => g.ReleaseDate).ThenBy(g => g.Name);
                        else
                            prictList = prictList.ThenBy(g => g.ReleaseDate).ThenBy(g => g.KoreanName);
                    }
                    else
                    {
                        if (mGameNameDisplayLanguage == "English")
                            prictList = prictList.ThenByDescending(g => g.ReleaseDate).ThenBy(g => g.Name);
                        else
                            prictList = prictList.ThenByDescending(g => g.ReleaseDate).ThenBy(g => g.KoreanName);
                    }

                    sortedList.AddRange(prictList);
                    unSortedGames.Clear();
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
			string text = "";
			var searchPattern = searchBlock.Text.Trim().Replace(" ", "").Replace("\\", "\\\\").Replace("[", "\\[").Replace("^", "\\^").Replace("(", "\\(").Replace("*", "\\*").Replace("?", "\\?").Replace("+", "\\+").Replace("|", "\\|").ToLower();
			if (searchBlock != null)
			{
				for (var i = 0; i < searchPattern.Length; i++)
                {
					if ('ㄱ' <= searchPattern[i] && searchPattern[i] <= 'ㅎ')
                    {
						for (var j = 0; j < mKorChr.Length; j++)
                        {
							if (searchPattern[i] == mKorChr[j])
								text += $"[{mKorStr[j]}-{(char)(mKorChrInt[j + 1] - 1)}]";
                        }
                    }
					else if (searchPattern[i] >= '가')
                    {
						var magic = (searchPattern[i] - '가') % 588;

						if (magic == 0)
							text += $"[{searchPattern[i]}-{(char)(searchPattern[i] + 27)}]";
						else
                        {
							magic = 27 - (magic % 28);
							text += $"[{searchPattern[i]}-{(char)(searchPattern[i] + magic)}]";
						}
                    }
					else
						text += searchPattern[i];
				}
			}

			var gamesFilteredByDevices = FilterByDevices(mGameList);
			if (gamesFilteredByDevices == null)
			{
				gamesFilteredByDevices = mGameList;
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
                F2PCheckBox.IsChecked == true ||
                AvailableOnlyCheckBox.IsChecked == true)
            {
                if (Application.Current.RequestedTheme == ApplicationTheme.Dark)
                    CapabilityFilterButton.Foreground = new SolidColorBrush(Colors.Yellow);
                else
                    CapabilityFilterButton.Foreground = new SolidColorBrush(Colors.Red);
            }
            else
            {
                if (Application.Current.RequestedTheme == ApplicationTheme.Dark)
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
                if (Application.Current.RequestedTheme == ApplicationTheme.Dark)
                    CategoryFilterButton.Foreground = new SolidColorBrush(Colors.Yellow);
                else
                    CategoryFilterButton.Foreground = new SolidColorBrush(Colors.Red);
            }
            else
            {
                if (Application.Current.RequestedTheme == ApplicationTheme.Dark)
                    CategoryFilterButton.Foreground = new SolidColorBrush(Colors.White);
                else
                    CategoryFilterButton.Foreground = new SolidColorBrush(Colors.Black);
            }

			if (KoreanVoiceRadioButton.IsChecked == true || KoreanSubtitleRadioButton.IsChecked == true)
            {
                if (Application.Current.RequestedTheme == ApplicationTheme.Dark)
                    KoreanSupportButton.Foreground = new SolidColorBrush(Colors.Yellow);
                else
                    KoreanSupportButton.Foreground = new SolidColorBrush(Colors.Red);
            }
            else
            {
                if (Application.Current.RequestedTheme == ApplicationTheme.Dark)
                    KoreanSupportButton.Foreground = new SolidColorBrush(Colors.White);
                else
                    KoreanSupportButton.Foreground = new SolidColorBrush(Colors.Black);
            }

            if (AgeType15RadioButton.IsChecked == true || AgeType12RadioButton.IsChecked == true || AgeTypeChildRadioButton.IsChecked == true)
            {
                if (Application.Current.RequestedTheme == ApplicationTheme.Dark)
                    AgeRangetButton.Foreground = new SolidColorBrush(Colors.Yellow);
                else
                    AgeRangetButton.Foreground = new SolidColorBrush(Colors.Red);
            }
            else
            {
                if (Application.Current.RequestedTheme == ApplicationTheme.Dark)
                    AgeRangetButton.Foreground = new SolidColorBrush(Colors.White);
                else
                    AgeRangetButton.Foreground = new SolidColorBrush(Colors.Black);
            }


            var today = DateTime.Now;

			for (var i = 0; i < gamesFilteredByDevices.Count; i++)
            {
				if (text.Trim() != "" &&
					!Regex.IsMatch(gamesFilteredByDevices[i].KoreanName.ToLower().Replace(" ", ""), text) &&
					!Regex.IsMatch(gamesFilteredByDevices[i].Name.ToLower().Replace(" ", ""), text))
				{
					gamesFilteredByDevices.RemoveAt(i);
					i--;
					continue;
				}

				// 한국어 지원 범위 필터링
				if ((KoreanVoiceRadioButton != null && (bool)KoreanVoiceRadioButton.IsChecked && !gamesFilteredByDevices[i].Localize.Contains("음성")) ||
					(KoreanSubtitleRadioButton != null && (bool)KoreanSubtitleRadioButton.IsChecked && !gamesFilteredByDevices[i].Localize.Contains("자막")))
				{
					gamesFilteredByDevices.RemoveAt(i);
					i--;
					continue;
				}

                // 연령대 범위 필터링
                if (AgeType15RadioButton.IsChecked == true && gamesFilteredByDevices[i].Age == "18" ||
                    (AgeType12RadioButton.IsChecked == true && (gamesFilteredByDevices[i].Age == "18" || gamesFilteredByDevices[i].Age == "15")) ||
                    (AgeTypeChildRadioButton.IsChecked == true && (gamesFilteredByDevices[i].Age == "18" || gamesFilteredByDevices[i].Age == "15" || gamesFilteredByDevices[i].Age == "12")))
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
					F2PCheckBox != null && (bool)F2PCheckBox.IsChecked && !gamesFilteredByDevices[i].Discount.Contains("무료") ||
                    AvailableOnlyCheckBox != null && (bool)AvailableOnlyCheckBox.IsChecked && ((DateTime.Parse(gamesFilteredByDevices[i].ReleaseDate) > today && (gamesFilteredByDevices[i].GamePassComing == "O" || (gamesFilteredByDevices[i].GamePassPC != "O" && gamesFilteredByDevices[i].GamePassConsole != "O" && gamesFilteredByDevices[i].GamePassCloud != "O"))) || (gamesFilteredByDevices[i].Discount == "판매 중지" && gamesFilteredByDevices[i].GamePassPC != "O" && gamesFilteredByDevices[i].GamePassConsole != "O" && gamesFilteredByDevices[i].GamePassCloud != "O" && gamesFilteredByDevices[i].Bundle.Count == 0)))
                    //AvailableOnlyCheckBox != null && (bool)AvailableOnlyCheckBox.IsChecked && (gamesFilteredByDevices[i].Discount != "판매 중지" || gamesFilteredByDevices[i].Bundle.Count > 0))
                {
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
			}

			var games = SortList(gamesFilteredByDevices);

			GamesViewModel.Clear();
			foreach (var g in games)
			{
				GamesViewModel.Add(new GameViewModel(g, mGameNameDisplayLanguage, mShowDiscount, mShowGamepass, mShowName, mShowReleaseTime));
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

            var storeRegion = Utils.GetRegionCodeFromLanguageCode(game.LanguageCode);
            if (AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Xbox")
			{
				if (storeRegion.ToLower() != Windows.System.UserProfile.GlobalizationPreferences.HomeGeographicRegion.ToLower())
				{
					var template = mMessageTemplateMap["noRegion"];
					tipBuilder.Append(template.Replace("[name]", ConvertCodeToStr(storeRegion)));

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
						tipBuilder.Append(message);
				}
				else if (code != "")
					tipBuilder.Append(parsePart[0]);

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
				await GoToStore(game.LanguageCode, game.ID);
			else
			{
				if (game.IsAvailable || game.Bundle.Count > 1)
					ShowEditionPanel(game);
				else
				{
					await GoToStore(game.LanguageCode, game.Bundle[0].ID);
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
				}
			}
		}

		private async Task ShowErrorReportDialog() {
			var dialog = new ErrorReportDialog(mGameNameDisplayLanguage == "English" ? mSelectedGame.Name : mSelectedGame.KoreanName, Utils.GetRegionCodeFromLanguageCode(mSelectedGame.LanguageCode).ToUpper());
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

			await CheckEventData();
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

		private void GamesView_KeyDown(object sender, Windows.UI.Xaml.Input.KeyRoutedEventArgs e)
		{
			if (AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Xbox")
			{
				var game = (e.OriginalSource as GridViewItem).Content as GameViewModel;
				mSelectedGame = game.Game;

				switch (e.Key)
				{
					case VirtualKey.GamepadMenu:
						CheckContextMenu(game.Game, (sender as FrameworkElement).ContextFlyout as MenuFlyout);
						break;
				}
			}
		}

		private void EditionView_KeyDown(object sender, Windows.UI.Xaml.Input.KeyRoutedEventArgs e)
		{
			if (AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Xbox")
			{
				var edition = (e.OriginalSource as GridViewItem).Content as EditionViewModel;
				mSelectedEdition = edition;

				switch (e.Key)
				{
					case VirtualKey.GamepadMenu:
						CheckPreorderBundle(edition, (sender as FrameworkElement).ContextFlyout as MenuFlyout);
						break;
				}
			}
		}

		private async void MenuPackages_Click(object sender, RoutedEventArgs e)
        {
			await ShowPackageInfo();
		}

		private async void MenuCheckPrice_Click(object sender, RoutedEventArgs e)
		{
			await CheckPriceInfo();
		}

		private async Task CheckPriceInfo()
		{
			if (mSelectedGame == null)
				return;

			if (mSelectedGame.Bundle.Count == 0)
				await ShowPriceInfo(mSelectedGame.Price, mSelectedGame.LowestPrice, mSelectedGame.LanguageCode);
			else
			{
				if (mSelectedGame.IsAvailable || mSelectedGame.Bundle.Count > 1)
				{
					var dialog = new MessageDialog("* 해당 게임은 여러 에디션이 있습니다. 에디션 항목에서 가격을 확인해 주십시오.", "가격 정보");
					if (mDialogQueue.TryAdd(dialog, 500))
					{
						await dialog.ShowAsync();
						mDialogQueue.Take();
					}
				}
				else
					await ShowPriceInfo(mSelectedGame.Bundle[0].Price, mSelectedGame.Bundle[0].LowestPrice, mSelectedGame.LanguageCode);
			}
		}

		private async void MenuBundleCheckPrice_Click(object sender, RoutedEventArgs e)
		{
			await ShowPriceInfo(mSelectedEdition.Price, mSelectedEdition.LowestPrice, mSelectedEdition.LanguageCode);
		}

		private async Task ShowPackageInfo()
        {
			if (mSelectedGame == null)
				return;

			var supportPackageBuilder = new StringBuilder();
			if (mSelectedGame.Packages != "")
				supportPackageBuilder.Append("* 한국어 지원 패키지: ").Append(mSelectedGame.Packages);
			else
				supportPackageBuilder.Append("* 확인된 패키지가 없거나 정식 발매 패키지만 한국어를 지원합니다. 확인하신 패키지가 있으면, 오류 신고 기능을 이용해 신고해 주십시오.");

			if (mSelectedGame.Message.ToLower().Contains("dlregiononly"))
				supportPackageBuilder.Append("\r\n").Append("* 한국어를 지원하지 않는 지역이 있습니다. 해외 패키지 구매시 한국어 지원 여부를 확인해 주십시오.");

			var dialog = new MessageDialog(supportPackageBuilder.ToString(), "한국어 지원 패키지 정보");
			if (mDialogQueue.TryAdd(dialog, 500))
			{
				await dialog.ShowAsync();
				mDialogQueue.Take();
			}
		}

		private async Task ShowPriceInfo(float price, float lowestPrice, string languageCode)
        {
			var priceInfoBuilder = new StringBuilder();
			if (price >= 0)
			{
				priceInfoBuilder.Append("* 현재 판매가: ").Append(price.ToString("C", CultureInfo.CreateSpecificCulture(languageCode)));
				if (languageCode != "ko-kr" && mExchangeRateMap.ContainsKey(languageCode))
					priceInfoBuilder.Append(" (약 ").Append((price * mExchangeRateMap[languageCode]).ToString("C", CultureInfo.CreateSpecificCulture("ko-kr"))).Append(")");

				if (lowestPrice > 0)
                {
					priceInfoBuilder.Append("\r\n* 역대 최저가: ").Append(lowestPrice.ToString("C", CultureInfo.CreateSpecificCulture(languageCode)));
                    if (languageCode != "ko-kr" && mExchangeRateMap.ContainsKey(languageCode))
                        priceInfoBuilder.Append(" (약 ").Append((lowestPrice * mExchangeRateMap[languageCode]).ToString("C", CultureInfo.CreateSpecificCulture("ko-kr"))).Append(")");
                }
			}
			else
				priceInfoBuilder.Append("* 판매를 시작하지 않았거나 판매가 중지된 타이틀입니다.");

			priceInfoBuilder.Append("\r\n\r\n* xKorean에서 제공하는 가격 정보는 스토어 가격 정보와 시간차가 있을 수 있습니다. 구매 전에 실제 스토어 가격을 확인해 주십시오.");

			var dialog = new MessageDialog(priceInfoBuilder.ToString(), "가격 정보");
			if (mDialogQueue.TryAdd(dialog, 500))
			{
				await dialog.ShowAsync();
				mDialogQueue.Take();
			}
		}

		private async void MenuImmigration_Click(object sender, RoutedEventArgs e)
		{
			if (mSelectedGame == null)
				return;

			if (mSelectedGame.Bundle.Count == 0)
				await ShowImmigrantResult(mSelectedGame.NZReleaseDate, mSelectedGame.KIReleaseDate, mSelectedGame.ReleaseDate);
			else
			{
				if (mSelectedGame.IsAvailable || mSelectedGame.Bundle.Count > 1)
                {
					var dialog = new MessageDialog("* 본 게임은 여러 에디션이 있습니다. 에디션을 선택해서 확인해 주십시오.", "지역 변경시 선행 플레이 가능 여부");
					if (mDialogQueue.TryAdd(dialog, 500))
					{
						await dialog.ShowAsync();
						mDialogQueue.Take();
					}
				}
				else
				{
					await ShowImmigrantResult(mSelectedGame.Bundle[0].NZReleaseDate, mSelectedGame.Bundle[0].KIReleaseDate, mSelectedGame.Bundle[0].ReleaseDate);
				}
			}
		}

		private async void MenuBundleImmigration_Click(object sender, RoutedEventArgs e)
		{
			if (mSelectedEdition == null)
				return;

			await ShowImmigrantResult(mSelectedEdition.NZReleaseDate, mSelectedEdition.KIReleaseDate, mSelectedEdition.ReleaseDate);
		}

        private async void MenuPlayCloud_Click(object sender, RoutedEventArgs e)
		{
            var productID = "";
            if (mSelectedGame.GamePassCloud == "O")
                productID = mSelectedGame.ID;
            else
            {
                foreach (var bundle in mSelectedGame.Bundle)
                {
                    if (bundle.GamePassCloud == "O")
                    {
                        productID = bundle.ID;
                        break;
                    }
                }
            }

            await Launcher.LaunchUriAsync(new Uri($"https://www.xbox.com/play/games/xKorean/{productID}"));
        }

        private async void MenuErrorReport_Click(object sender, RoutedEventArgs e)
		{
			await ShowErrorReportDialog();
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
			(GamesView.ContainerFromIndex(mSelectedIdx) as GridViewItem).Focus(FocusState.Programmatic);

			var gotoStoreButton = sender as Button;
			await CheckEditionPanel(gotoStoreButton.Tag as Game);
		}

		private async void Goto360Market_Click(object sender, RoutedEventArgs e)
		{
			InfoPanelView.Visibility = Visibility.Collapsed;
			(GamesView.ContainerFromIndex(mSelectedIdx) as GridViewItem).Focus(FocusState.Programmatic);

			var goto360market = sender as Button;
			await Launcher.LaunchUriAsync(new Uri(goto360market.Tag as string));
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
				if (Application.Current.RequestedTheme == ApplicationTheme.Dark)
					DeviceFilterButton.Foreground = new SolidColorBrush(Colors.Yellow);
				else
					DeviceFilterButton.Foreground = new SolidColorBrush(Colors.Red);
			}
			else
            {
				if (Application.Current.RequestedTheme == ApplicationTheme.Dark)
					DeviceFilterButton.Foreground = new SolidColorBrush(Colors.White);
				else
					DeviceFilterButton.Foreground = new SolidColorBrush(Colors.Black);
			}
		}

		private async Task ShowImmigrantResult(string nzReleaseDate, string kiReleaseDate, string releaseDate)
		{
			string message = "";
			if (AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Desktop")
			{
                if (kiReleaseDate != "" && DateTime.Parse(kiReleaseDate) < DateTime.Parse(releaseDate))
				{
                    var kiReleaseTime = DateTime.Parse(kiReleaseDate);
                    message = $"* 키리바시(윈도우) 지역 변경시 플레이 가능 시간: {kiReleaseTime:yyyy.MM.dd tt hh:mm}";
                }
            }

			if (nzReleaseDate != "" && DateTime.Parse(nzReleaseDate) < DateTime.Parse(releaseDate))
			{
				if (message != "")
					message += "\r\n\r\n";

                var nzReleaseTime = DateTime.Parse(nzReleaseDate);
				if (kiReleaseDate == "" || AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Xbox")
                    message += $"* 뉴질랜드 지역 변경시 플레이 가능 시간: {nzReleaseTime:yyyy.MM.dd tt hh:mm}";
				else
					message += $"* 뉴질랜드(엑스박스) 지역 변경시 플레이 가능 시간: {nzReleaseTime:yyyy.MM.dd tt hh:mm}";
			}

			if (message == "")
			{
				if (AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Desktop")
					message = $"* 키리바시/뉴질랜드로 지역 변경을 하셔도 일찍 플레이하실 수 없습니다.";
				else
					message = $"* 뉴질랜드로 지역 변경을 하셔도 일찍 플레이하실 수 없습니다.";
			}
			
			var dialog = new MessageDialog(message, "지역 변경시 선행 플레이 가능 여부");
			if (mDialogQueue.TryAdd(dialog, 500))
			{
				await dialog.ShowAsync();
				mDialogQueue.Take();
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
				DiscountCheckBox.IsChecked == true ||
				PlayAnywhereCheckBox.IsChecked == true ||
				DolbyAtmosCheckBox.IsChecked == true ||
				ConsoleKeyboardMouseCheckBox.IsChecked == true ||
				LocalCoopCheckBox.IsChecked == true ||
				OnlineCoopCheckBox.IsChecked == true ||
				FPS120CheckBox.IsChecked == true ||
				FPSBoostCheckBox.IsChecked == true ||
				F2PCheckBox.IsChecked == true ||
                AvailableOnlyCheckBox.IsChecked == true)
			{
				GamePassCheckBox.IsChecked = false;
				DiscountCheckBox.IsChecked = false;
				PlayAnywhereCheckBox.IsChecked = false;
				DolbyAtmosCheckBox.IsChecked = false;
				ConsoleKeyboardMouseCheckBox.IsChecked = false;
				LocalCoopCheckBox.IsChecked = false;
				OnlineCoopCheckBox.IsChecked = false;
				FPS120CheckBox.IsChecked = false;
				FPSBoostCheckBox.IsChecked = false;
				F2PCheckBox.IsChecked = false;
				AvailableOnlyCheckBox.IsChecked = false;

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

		private async void PriorityNoneItem_Click(object sender, RoutedEventArgs e)
		{
			SearchBox_TextChanged(SearchBox, null);

			await Settings.Instance.SetValue("priorityType", "none");
		}

		private async void PriorityByGamepassItem_Click(object sender, RoutedEventArgs e)
		{
			SearchBox_TextChanged(SearchBox, null);

			await Settings.Instance.SetValue("priorityType", "gamepass");
		}

		private async void PriorityByDiscountItem_Click(object sender, RoutedEventArgs e)
		{
			SearchBox_TextChanged(SearchBox, null);

			await Settings.Instance.SetValue("priorityType", "discount");
		}

        private async void PriorityByPriceItem_Click(object sender, RoutedEventArgs e)
        {
            SearchBox_TextChanged(SearchBox, null);

            await Settings.Instance.SetValue("priorityType", "price");
        }

        private void CheckContextMenu(Game game, MenuFlyout menuFlyout)
		{
			if (game.Discount.Contains("출시"))
				menuFlyout.Items[2].Visibility = Visibility.Visible;
			else if (game.Bundle.Count > 0)
			{
				var notReleased = false;
				foreach (var bundle in game.Bundle)
				{
					if (bundle.DiscountType.Contains("출시"))
					{
						menuFlyout.Items[2].Visibility = Visibility.Visible;
						notReleased = true;
						break;
					}
				}

				if (!notReleased)
					menuFlyout.Items[2].Visibility = Visibility.Collapsed;
			}
			else
				menuFlyout.Items[2].Visibility = Visibility.Collapsed;

			if (AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Desktop")
			{
                menuFlyout.Items[3].Visibility = Visibility.Collapsed;

                if (game.GamePassCloud == "O")
					menuFlyout.Items[3].Visibility = Visibility.Visible;
				else if (game.Bundle.Count > 0)
				{
					foreach (var bundle in game.Bundle)
					{
						if (bundle.GamePassCloud == "O")
						{
							menuFlyout.Items[3].Visibility = Visibility.Visible;
							break;
						}
					}
				}
			}
			else
                menuFlyout.Items[3].Visibility = Visibility.Collapsed;

			if (game.GamePassRegisterDate != "")
                menuFlyout.Items[4].Visibility = Visibility.Visible;
			else
				menuFlyout.Items[4].Visibility = Visibility.Collapsed;

        }

		private void CheckPreorderBundle(EditionViewModel edition, MenuFlyout menuFlyout) {
			if (edition.Discount.Contains("출시"))
				menuFlyout.Items[1].Visibility = Visibility.Visible;
			else
				menuFlyout.Items[1].Visibility = Visibility.Collapsed;
		}

		private void Grid_PointerPressed(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
			if (e.Pointer.PointerDeviceType == Windows.Devices.Input.PointerDeviceType.Mouse && e.GetCurrentPoint(null).Properties.IsRightButtonPressed)
			{
				var game = ((FrameworkElement)e.OriginalSource).DataContext as GameViewModel;
				CheckContextMenu(game.Game, GamesView.ContextFlyout as MenuFlyout);
				mSelectedGame = game.Game;
			}
		}

        private void Grid_PointerPressed_1(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
			if (e.Pointer.PointerDeviceType == Windows.Devices.Input.PointerDeviceType.Mouse && e.GetCurrentPoint(null).Properties.IsRightButtonPressed)
			{
				var edition = ((FrameworkElement)e.OriginalSource).DataContext as EditionViewModel;
				CheckPreorderBundle(edition, EditionView.ContextFlyout as MenuFlyout);
				mSelectedEdition = edition;
			}
		}

        private async void DonationButton_Click(object sender, RoutedEventArgs e)
        {
			await Launcher.LaunchUriAsync(new Uri($"https://fanding.kr/user/xKorean"));
		}

		private class GameDiscountComparator : IComparer<Game>
		{
			public int Compare(Game x, Game y)
			{
				int GetMaxDiscount(Game game)
				{
					int ExtractDiscount(string str)
					{
						var v = 0;

						var idx = str.IndexOf("%");
						if (idx > 0)
						{
							var startIdx = idx;
							while (startIdx > 0)
							{
								if (str[startIdx] == ' ')
								{
									startIdx++;
									break;
								}

								startIdx--;
							}

							v = Convert.ToInt32(str.Substring(startIdx, idx - startIdx));
						}

						return v;
					}

					var discount = ExtractDiscount(game.Discount);

					if (game.Bundle.Count > 0)
					{
						foreach (var bundle in game.Bundle)
						{
							var bundleDicount = ExtractDiscount(bundle.DiscountType);
							if (bundleDicount > discount)
								discount = bundleDicount;
						}
					}

					return discount;
				}

				var xDiscount = GetMaxDiscount(x);
				var yDiscount = GetMaxDiscount(y);

				if (xDiscount < yDiscount)
					return -1;
				else if (xDiscount == yDiscount)
					return 0;
				else
					return 1;
			}
		}

        private class GamePriceComparator : IComparer<Game>
        {
			private Dictionary<string, float> mExchangeMap;

			public GamePriceComparator(Dictionary<string, float> exchangeMap)
			{
				mExchangeMap = exchangeMap;
			}

            public int Compare(Game x, Game y)
            {
                float getLowestPrice(Game game)
                {
                    float extractPrice(float price, string languageCode)
                    {
                        if (price == -1 || languageCode == "ko-kr")
                            return price;
                        else
                            return price * mExchangeMap[languageCode];
                    }

                    var lowestPrice = extractPrice(game.Price, game.LanguageCode);

                    if (game.Bundle.Count > 0)
                    {
                        for (var i = 0; i < game.Bundle.Count; i++)
                        {
                            var bundlePrice = extractPrice(game.Bundle[i].Price, game.LanguageCode);

                            if (bundlePrice > 0 && (bundlePrice < lowestPrice || lowestPrice <= 0))
                                lowestPrice = bundlePrice;
                        }
                    }

                    return lowestPrice;
                }

				if (getLowestPrice(x) > -1 && getLowestPrice(y) == -1)
					return -1;
				else if (getLowestPrice(x) == -1 && getLowestPrice(y) > -1)
					return 1;
				else if (getLowestPrice(x) < getLowestPrice(y))
					return -1;
				else if (getLowestPrice(x) > getLowestPrice(y))
					return 1;
				else
					return 0;
            }
        }

        private async void MenuGamePassPeriod_Click(object sender, RoutedEventArgs e)
        {
			var registerDate = DateTime.Parse(mSelectedGame.GamePassRegisterDate);
            var timeDiff = DateTime.Now - registerDate;

			var message = $"게임패스 등록일: {registerDate.Year}. {registerDate.Month}. {registerDate.Day}. ({timeDiff.TotalDays:#,#0}일)";

			var gamePassEnd = false;
			if (mSelectedGame.GamePassEnd == "O")
				gamePassEnd = true;
			else if (mSelectedGame.Bundle.Count > 0)
			{
				for (var i = 0; i < mSelectedGame.Bundle.Count; i++)
				{
					if (mSelectedGame.Bundle[i].GamePassEnd == "O")
					{
						gamePassEnd = true;
						break;
					}
				}
			}

			if (gamePassEnd)
				message += "\n\n* 이 게임은 2주 내에 게임패스에 내려갈 예정입니다.";
			else if (mSelectedGame.IsFirstParty == "O")
				message += "\n\n* 이 게임은 퍼스트 파티 타이틀로 게임패스에서 내려가지 않습니다. 다만 일부 게임은 게임 내 컨텐츠 라이센스 만료등의 이유로 판매가 종료되면서, 게임패스에서 내려갈 수도 있습니다. (예: 스포츠 / 레이싱 게임)";
			else if (mSelectedGame.IsFirstParty == "EA")
                message += "\n\n* 이 게임은 EA가 배급하는 게임으로 게임패스에서 내려가지 않습니다. 다만 일부 게임은 게임 내 컨텐츠 라이센스 만료등의 판매가 종료되면서, 게임패스에서 내려갈 수도 있습니다. (예: 스포츠 / 레이싱 게임) 또한 마이크로소프트와 EA간의 향후 계약에 따라서 내려갈 가능성도 있습니다.";

            var dialog = new MessageDialog(message, "게임패스 등록 정보");
            if (mDialogQueue.TryAdd(dialog, 500))
            {
                await dialog.ShowAsync();
                mDialogQueue.Take();
            }
        }
    }
}
