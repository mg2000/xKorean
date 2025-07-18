using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.System.Profile;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

namespace xKorean
{
	public class EditionViewModel : INotifyPropertyChanged
	{
		private string mGamePassPC = "";
		private string mGamePassConsole = "";
		private string mGamePassCloud = "";

		private bool mThumbnailCached = false;

		public EditionViewModel()
		{
			
		}

		public string ID {
			get;
			set;
		}

		public string Name {
			get;
			set;
		}

		public string Discount {
			get;
			set;
		}

		public string ReleaseDate
        {
			get;
			set;
        }

		public string NZReleaseDate
        {
			get;
			set;
        }

        public string KIReleaseDate
        {
            get;
            set;
        }

        public Brush DiscountDisplayColor
        {
			get
            {
				if (Discount.Contains("할인") && Price == LowestPrice)
					return new SolidColorBrush(Colors.Yellow);
				else
					return new SolidColorBrush(Colors.White);
			}
        }

		public string IsGamePassPC {
			get {
				return mGamePassPC;
			}
			set {
				if (value != "")
					mGamePassPC = "피";
				else
					mGamePassPC = "";
			}
		}

		public string IsGamePassConsole
		{
			get
			{
				return mGamePassConsole;
			}
			set
			{
				if (value != "")
					mGamePassConsole = "엑";
				else
					mGamePassConsole = "";
			}
		}

		public string IsGamePassCloud
		{
			get
			{
				return mGamePassCloud;
			}
			set
			{
				if (value != "")
					mGamePassCloud = "클";
				else
					mGamePassCloud = "";
			}
		}

		public string IsCloud {
			get {
				if (IsGamePassCloud != "" || BuyAndCloud != "")
					return "클";
				else
					return "";
			}
		}

		public string GamePassNew {
			get;
			set;
		}

		public string GamePassEnd
		{
			get;
			set;
		}

		public string GamePassComing
        {
			get;
			set;
        }

		public string BuyAndCloud
		{
			get;
			set;
		}

		public string GamePassOrBuyAndCloud
		{
			get
			{
				var gamePassStatus = "";
				if (mGamePassCloud != "" || mGamePassPC != "" || mGamePassConsole != "")
					gamePassStatus = "게임패스";

				if (GamePassNew != "")
					gamePassStatus += " 신규";
				else if (GamePassEnd != "")
					gamePassStatus += " 만기";
				else if (GamePassComing != "")
					gamePassStatus += " 예정";

				if (gamePassStatus == "")
				{
					if (BuyAndCloud == "O")
						gamePassStatus = "소유 게임";
				}

				return gamePassStatus;
			}
		}

		public bool IsDiscounting {
			get {
				return Discount != "" && ShowDiscount;
			}
		}

		public bool IsGamePassOrBuyAndCloud {
			get {
				return (mGamePassPC != "" || mGamePassConsole != "" || mGamePassCloud != "" || BuyAndCloud != "") && ShowGamePass;
			}
		}

		public double MaxWidth
		{
			get
			{
				switch (AnalyticsInfo.VersionInfo.DeviceFamily)
				{
					case "Windows.Xbox":
						return 129;
					default:
						return 160;
				}
			}
		}

		public double Width
		{
			get
			{
				switch (AnalyticsInfo.VersionInfo.DeviceFamily)
				{
					case "Windows.Xbox":
						return 129;
					default:
						return 160;
				}
			}
		}

		public double Height
		{
			get
			{
				switch (AnalyticsInfo.VersionInfo.DeviceFamily)
				{
					case "Windows.Xbox":
						return 178;
					default:
						return 219;
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

		public string SeriesXS {
			get;
			set;
		}

		public string OneS {
			get;
			set;
		}

		public string PC {
			private get;
			set;
		}

		public string PlayAnywhere {
			private get;
			set;
		}

		public string ThumbnailUrl {
			private get;
			set;
		}

		public string ThumbnailID
        {
			private get;
			set;
        }

		public byte[] SeriesXSHeader {
			private get;
			set;
		}

		public bool ShowDiscount {
			private get;
			set;
		}

		public bool ShowGamePass {
			private get;
			set;
		}

		public bool ShowName {
			get;
			set;
		}

		public float Price
        {
			get;
			set;
        }

		public float LowestPrice
        {
			get;
			set;
        }

		public string LanguageCode
        {
			get;
			set;
        }

		public string ThumbnailPath
		{
			get
			{
				var fileName = $"{ID}_{ThumbnailID}";
				if (PlayAnywhere == "O") {
					if (SeriesXS == "O")
						fileName += "_playanywhere_xs";
					else if (OneS == "O")
						fileName += "_playanywhere_os";
				}
				else if (SeriesXS == "O")
					fileName += "_xs";
				else if (OneS == "O")
					fileName += "_os";
				else if (PC == "O")
					fileName += "_pc";

				FileInfo thumbnailCacheInfo = new FileInfo($@"{ApplicationData.Current.LocalFolder.Path}\ThumbnailCache\{fileName}.jpg");

				if (thumbnailCacheInfo.Exists)
				{
					if (thumbnailCacheInfo.Length == 0)
					{
						LoadImage();
						return "ms-appx:///Assets/blank.png";
                    }
					else
					{
						mThumbnailCached = true;
						return thumbnailCacheInfo.FullName;
					}
				}
				else
				{
					LoadImage();
					return "ms-appx:///Assets/blank.png";
                }
			}
			set {
			
			}
		}

		private void LoadImage()
		{
            ImageDownloader.Instance.AddProducer(ThumbnailUrl, ID, ThumbnailID, SeriesXS, OneS, PC, PlayAnywhere, async (result) =>
            {
                if (result)
                {
                    await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        NotifyPropertyChanged("ThumbnailPath");
                    });
                }
            });
		}


		private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		public event PropertyChangedEventHandler PropertyChanged;
	}
}
