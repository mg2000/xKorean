using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using Windows.Graphics.Imaging;
using Windows.Security.ExchangeActiveSyncProvisioning;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System.Profile;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

namespace xKorean
{
	public class GameViewModel : INotifyPropertyChanged
	{
		private string mGameNameDisplayLanguage = "Korean";

		private bool mRegionAvailable = true;

		private bool mShowDiscount = true;
		private bool mShowGamepass = true;
		private bool mShowName = true;

		public Game Game { set; get; } = new Game();
		public string Title {
			get
			{
				if (mGameNameDisplayLanguage == "English")
					return Game.Name;
				else
					return Game.KoreanName;
			}
		}

		public string ThumbnailUrl { set; get; }

		public Dictionary<string, long> DownloadSize { set; get; } = new Dictionary<string, long>();
		public DateTime ReleaseDate { set; get; }

		public string PackageOnly { get; set; }
		public string ThumbnailPath
		{
			get
			{
				var fileName = $"{Game.ID}_{Game.ThumbnailID}";
				if (Game.PlayAnywhere == "O") {
					if (Game.SeriesXS == "O")
						fileName += "_playanywhere_xs";
					else if (Game.OneS == "O")
						fileName += "_playanywhere_os";
				}
				else if (Game.SeriesXS == "O")
					fileName += "_xs";
				else if (Game.OneS == "O")
					fileName += "_os";
				else if (Game.PC == "O")
					fileName += "_pc";

				FileInfo thumbnailCacheInfo = new FileInfo(ApplicationData.Current.LocalFolder.Path + "\\ThumbnailCache\\" + fileName + ".jpg");

				if (thumbnailCacheInfo.Exists)
				{
					if (thumbnailCacheInfo.Length == 0)
					{
						LoadImage();
						return "none";
					}
					else
					{
						IsThumbnailCached = true;
						return thumbnailCacheInfo.FullName;
					}
				}
				else
				{
					LoadImage();
					return "none";
				}
			}
		}

		public string Localize { set; get; } = "";
		public string ID { set; get; }

		public List<string> Screenshots { set; get; } = new List<string>();
		public GameViewModel(Game game, string gameNameDisplayLanguage, bool showDiscount, bool showGamepass , bool showName, bool showReleaseTime)
		{
			Game = game;

			ThumbnailUrl = game.Thumbnail;
			ID = game.ID;
			Localize = game.Localize.Replace("/", "\r\n");
			mGameNameDisplayLanguage = gameNameDisplayLanguage;
			Bundle = game.Bundle;

			UpdateReleaseTime(game, showReleaseTime);

			if (((game.OG == "O" || game.X360 == "O" || game.OneS == "O" || game.SeriesXS == "O") && AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Xbox") ||
                (game.PC == "O" && AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Desktop"))
            {
				var region = Windows.System.UserProfile.GlobalizationPreferences.HomeGeographicRegion.ToUpper();
				if (Game.Message.ToLower().IndexOf("dlregiononly") >= 0)
				{
					var messageArr = Game.Message.Split("\n");
					foreach (var message in messageArr)
					{
						if (message.ToLower().IndexOf("dlregiononly") >= 0)
						{
							var parsePart = message.Split("=");
							if (parsePart.Length > 1 && parsePart[1].ToUpper() != region)
								mRegionAvailable = false;

							break;
						}
					}
				}
			}

			mShowGamepass = showGamepass;
			mShowDiscount = showDiscount;
			mShowName = showName;
		}

		public string Message
		{
			get
			{
				return Game.Message;
			}
		}

		public string IsGamePassCloud
		{
			get
			{
				if (Game.GamePassCloud == "" && Bundle.Count > 0) {
					foreach (var bundle in Bundle) {
						if (bundle.GamePassCloud != "")
							return "클";
					}

					return "";
				}
				else if (Game.GamePassCloud == "O")
					return "클";
				else
					return "";
			}
		}

		public string IsGamePassPC
		{
			get
			{
				if (Game.GamePassPC == "" && Bundle.Count > 0)
				{
					foreach (var bundle in Bundle)
					{
						if (bundle.GamePassPC != "")
							return "피";
					}

					return "";
				}
				else if (Game.GamePassPC == "O")
					return "피";
				else
					return "";
			}
		}

		public string IsGamePassConsole
		{
			get
			{
				if (Game.GamePassConsole == "" && Bundle.Count > 0)
				{
					foreach (var bundle in Bundle)
					{
						if (bundle.GamePassConsole != "")
							return "엑";
					}

					return "";
				}
				else if (Game.GamePassConsole == "O")
					return "엑";
				else
					return "";
			}
		}

		public string UseDolbyAtmos
		{
			get
			{
				return Game.DolbyAtmos;
			}
		}

		public string UseConsoleKeyboardMouse
		{
			get
			{
				return Game.ConsoleKeyboardMouse;
			}
		}

		public bool IsGamePass
		{
			get
			{
				if (mShowGamepass)
				{
					if (Game.GamePassCloud == "" && Game.GamePassPC == "" && Game.GamePassConsole == "")
					{
						foreach (var bundle in Bundle)
						{
							if (bundle.GamePassCloud != "" || bundle.GamePassPC != "" || bundle.GamePassConsole != "")
								return true;
						}

						return false;
					}
					else if (Game.GamePassCloud == "O" || Game.GamePassPC == "O" || Game.GamePassConsole == "O")
						return true;
					else
						return false;
				}
				else
					return false;
			}
		}

		public string GamePass
		{
			get
			{
				var gamePassStatus = "";
				if (Game.GamePassCloud == "O" || Game.GamePassPC == "O" || Game.GamePassConsole == "O")
					gamePassStatus = "게임패스";
				else if (Bundle.Count > 0) {
					foreach (var bundle in Bundle) {
						if (bundle.GamePassCloud != "" || bundle.GamePassPC != "" || bundle.GamePassConsole != "") {
							gamePassStatus = "게임패스";
							break;
						}
					}
				}

				if (Game.GamePassNew == "O")
					gamePassStatus += " 신규";
				else if (Game.GamePassEnd == "O")
					gamePassStatus += " 만기";
				else if (Game.GamePassComing == "O")
					gamePassStatus += " 예정";
				else
				{
					foreach (var bundle in Bundle)
					{
						if (bundle.GamePassNew != "")
						{
							gamePassStatus += " 신규";
							break;
						}
						else if (bundle.GamePassEnd != "")
						{
							gamePassStatus += " 만기";
							break;
						}
						else if (bundle.GamePassComing != "")
                        {
							gamePassStatus += " 예정";
							break;
						}
					}
				}

				return gamePassStatus;
			}
		}

		public List<Bundle> Bundle {
			get;
			set;
		}

		private Visibility _isImageLoaded = Visibility.Visible;
		public Visibility IsImageLoaded
		{
			set
			{
				_isImageLoaded = value;
				NotifyPropertyChanged();
			}
			get
			{
				return _isImageLoaded;
			}
		}

		public Color LocalizeColor
		{
			get
			{
				if (Localize.Contains("음성"))
				{
					return Color.FromArgb(0xff, 0x44, 0x85, 0x0E);
				}
				else if (Localize.Contains("자막"))
				{
					return Color.FromArgb(0xff, 0xA6, 0x88, 0x19);
				}
				else
				{
					return Color.FromArgb(0xff, 0x95, 0x0D, 0x00);
				}
			}
		}

		public bool Unavailable
		{
			get
			{
				var productName = new EasClientDeviceInformation().SystemProductName.ToLower();
				if ((productName.Contains("xbox one") == true && Game.OneS == "X") || (productName.Contains("xbox series") == true && Game.SeriesXS == "X") || 
					!mRegionAvailable ||
					(Windows.System.UserProfile.GlobalizationPreferences.HomeGeographicRegion.ToUpper() == "KR" && Windows.System.UserProfile.GlobalizationPreferences.HomeGeographicRegion.ToUpper() != Utils.GetRegionCodeFromLanguageCode(Game.LanguageCode).ToUpper() && (Game.Discount != "판매 중지" || Game.Bundle.Count > 0)) ||
                    Game.Message.Contains("packageOnly"))
					return true;
				else
					return false;
			}
		}

		public string UnavailableReason
		{
			get
			{
				var productName = new EasClientDeviceInformation().SystemProductName.ToLower();
				if ((productName.Contains("xbox one") == true && Game.OneS == "X") || (productName.Contains("xbox series") == true && Game.SeriesXS == "X"))
					return "미지원 기기";
				else if (Game.Message.Contains("packageOnly"))
                    return "DL 한국어 미지원";
                else if (Windows.System.UserProfile.GlobalizationPreferences.HomeGeographicRegion.ToUpper() == "KR" && Windows.System.UserProfile.GlobalizationPreferences.HomeGeographicRegion.ToUpper() != Utils.GetRegionCodeFromLanguageCode(Game.LanguageCode).ToUpper() && (Game.Discount != "판매 중지" || Game.Bundle.Count > 0))
                    return "한국 스토어에 없음";
                else if (!mRegionAvailable)
					return "한국어 미지원 지역";
				else
					return "알 수 없음";
			}
		}

		public bool IsDiscounting
		{
			get
			{
				return Discount != "" && mShowDiscount; 
			}
		}

		public string Discount
		{
			get;
			set;
		} = "";

		public Brush DiscountDisplayColor
        {
			get;
			set;
        }

		public bool ShowName {
			get {
				return mShowName;
			}
		}

		public void UpdateShowDiscount(bool showDiscount) {
			mShowDiscount = showDiscount;
			NotifyPropertyChanged("IsDiscounting");
		}

		public void UpdateShowGamepass(bool showGamepass)
		{
			mShowGamepass = showGamepass;
			NotifyPropertyChanged("IsGamePass");
		}

		public void UpdateShowName(bool showName) {
			mShowName = showName;
			NotifyPropertyChanged("ShowName");
		}

		public void UpdateShowReleaseTime(bool showReleaseTime)
		{
			UpdateReleaseTime(Game, showReleaseTime);
			NotifyPropertyChanged("Discount");
			NotifyPropertyChanged("DiscountDisplayColor");
		}
		
		public bool IsThumbnailCached { set; get; } = false;
		private async void LoadImage()
		{
			if (await Utils.DownloadImage(ThumbnailUrl, ID, Game.ThumbnailID, Game.SeriesXS, Game.OneS, Game.PC, Game.PlayAnywhere))
				NotifyPropertyChanged("ThumbnailPath");
		}

		public double MaxWidth
		{
			get
			{
				switch (AnalyticsInfo.VersionInfo.DeviceFamily)
				{
					case "Windows.Xbox":
						return 130;
					default:
						return 160;
				}
			}
		}

		public double TitleFontSize
		{
			get
			{
				switch (AnalyticsInfo.VersionInfo.DeviceFamily)
				{
					case "Windows.Xbox":
						return 9;
					default:
						return 12;
				}
			}
		}

		public double MetadataFontSize
		{
			get
			{
				switch (AnalyticsInfo.VersionInfo.DeviceFamily)
				{
					case "Windows.Xbox":
						return 9;
					default:
						return 12;
				}
			}
		}

		public string GameNameDisplayLanguage
		{
			set
			{
				mGameNameDisplayLanguage = value;
				NotifyPropertyChanged("Title");
			}
		}

		private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		private void UpdateReleaseTime(Game game, bool showReleaseTime)
        {
			var discount = game.Discount;

			DiscountDisplayColor = new SolidColorBrush(Colors.White);

			if (game.IsAvailable && game.Discount.Contains("할인") && game.LowestPrice == game.Price)
				DiscountDisplayColor = new SolidColorBrush(Colors.Yellow);
			else if (!game.IsAvailable && Bundle.Count == 1)
			{
				discount = Bundle[0].DiscountType;

				if (discount.Contains("할인") && Bundle[0].LowestPrice == Bundle[0].Price)
					DiscountDisplayColor = new SolidColorBrush(Colors.Yellow);
			}
			else if (Bundle.Count >= 1)
			{
				foreach (var bundle in Bundle)
				{
					if (bundle.DiscountType.IndexOf("할인") >= 0)
					{
						discount = "에디션 할인";
						if (bundle.LowestPrice == bundle.Price)
							DiscountDisplayColor = new SolidColorBrush(Colors.Yellow);
					}
					else if (discount == "판매 중지" && bundle.DiscountType != "판매 중지")
						discount = "";
				}

				if (!game.IsAvailable && discount == "")
					discount = Bundle[0].DiscountType;
			}

			if (discount == "곧 출시" || (showReleaseTime && discount != "출시 예정" && discount.Contains(" 출시")))
				discount = Utils.GetReleaseStr(game.ReleaseDate);

			Discount = discount;
		}

		public event PropertyChangedEventHandler PropertyChanged;
	}
}
