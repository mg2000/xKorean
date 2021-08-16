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

namespace xKorean
{
	public class GameViewModel : INotifyPropertyChanged
	{
		private string mIconSize = "Small";
		private string mGameNameDisplayLanguage = "Korean";

		private byte[] mOneTitleHeader;
		private byte[] mSeriesXSTitleHeader;
		private byte[] mPCTitleHeader;

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
				var fileName = ID;
				if (Game.SeriesXS == "O")
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

		public string StoreUri { get; set; } = "";
		public List<string> Screenshots { set; get; } = new List<string>();
		public GameViewModel(Game game, string gameNameDisplayLanguage, string iconSize, byte[] oneTitleHeader, byte[] seriesXSTitleHeader, byte[] pcTitleHeader)
		{
			Game = game;

			mOneTitleHeader = oneTitleHeader;
			mSeriesXSTitleHeader = seriesXSTitleHeader;
			mPCTitleHeader = pcTitleHeader;

			ThumbnailUrl = game.Thumbnail;
			ID = game.ID;
			Localize = game.Localize.Replace("/", "\r\n");
			StoreUri = game.StoreLink;
			mIconSize = iconSize;
			mGameNameDisplayLanguage = gameNameDisplayLanguage;
			Bundle = game.Bundle;

			var discount = game.Discount;

			if (!game.IsAvailable && Bundle.Count == 1 && (Bundle[0].DiscountType.IndexOf("할인") >= 0 || Bundle[0].DiscountType.IndexOf("출시") >= 0))
				discount = Bundle[0].DiscountType;
			else
			{
				foreach (var bundle in Bundle)
				{
					if (bundle.DiscountType.IndexOf("할인") >= 0)
					{
						discount = "에디션 할인";
						break;
					}
					else if (discount == "판매 중지" && bundle.DiscountType != "판매 중지")
						discount = "";
				}
			}

			Discount = discount;
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
				if (Game.GamePassCloud == "" && Game.GamePassPC == "" && Game.GamePassConsole == "") {
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
				else {
					foreach (var bundle in Bundle)
					{
						if (bundle.GamePassNew != "")
						{
							gamePassStatus += " 신규";
							break;
						}
						else if (Game.GamePassEnd != "")
						{
							gamePassStatus += " 만기";
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
		public Visibility IsLocalizeAvaliable
		{
			get
			{
				return Localize != "" ? Visibility.Visible : Visibility.Collapsed;
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

		public bool IsSupporting
		{
			get
			{
				var productName = new EasClientDeviceInformation().SystemProductName.ToLower();
				if ((productName.Contains("xbox one") == true && Game.OneS == "X") || (productName.Contains("xbox series") == true && Game.SeriesXS == "X"))
					return true;
				else
					return false;
			}
		}

		public bool IsDiscounting
		{
			get
			{
				return Discount != ""; 
			}
		}

		public string Discount
		{
			get;
			set;
		} = "";

		public bool IsThumbnailCached { set; get; } = false;
		private async void LoadImage()
		{
			if (await Utils.DownloadImage(ThumbnailUrl, ID, Game.SeriesXS, Game.OneS, Game.PC, mSeriesXSTitleHeader, mOneTitleHeader, mPCTitleHeader))
				NotifyPropertyChanged("ThumbnailPath");
		}

		public double MaxWidth
		{
			get
			{
				if (mIconSize == "Normal")
				{
					switch (AnalyticsInfo.VersionInfo.DeviceFamily)
					{
						case "Windows.Xbox":
							return 150;
						default:
							return 202.5;
					}
				}
				else
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
		}

		public double TitleFontSize
		{
			get
			{
				if (mIconSize == "Normal")
				{
					switch (AnalyticsInfo.VersionInfo.DeviceFamily)
					{
						case "Windows.Xbox":
							return 10;
						default:
							return 15;
					}
				}
				else
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
		}

		public double MetadataFontSize
		{
			get
			{
				if (mIconSize == "Normal")
				{
					switch (AnalyticsInfo.VersionInfo.DeviceFamily)
					{
						case "Windows.Xbox":
							return 11;
						default:
							return 15;
					}
				}
				else
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
		}

		public string IconSize
		{
			set {
				mIconSize = value;
				NotifyPropertyChanged("MaxWidth");
				NotifyPropertyChanged("MetadataFontSize");
				NotifyPropertyChanged("TitleFontSize");
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

		public event PropertyChangedEventHandler PropertyChanged;
	}
}
