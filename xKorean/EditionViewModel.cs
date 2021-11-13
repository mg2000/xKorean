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
using Windows.UI.Xaml;

namespace xKorean
{
	public class EditionViewModel : INotifyPropertyChanged
	{
		private string mGamePassPC = "";
		private string mGamePassConsole = "";
		private string mGamePassCloud = "";
		private string mGamePassNew = "";
		private string mGamePassEnd = "";

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

		public string GamePassNew {
			get;
			set;
		}

		public string GamePassEnd
		{
			get;
			set;
		}

		public string GamePass
		{
			get
			{
				var gamePassStatus = "";
				if (mGamePassCloud != "" || mGamePassPC != "" || mGamePassConsole != "")
					gamePassStatus = "게임패스";

				if (mGamePassNew != "")
					gamePassStatus += " 신규";
				else if (mGamePassEnd != "")
					gamePassStatus += " 만기";

				return gamePassStatus;
			}
		}

		public bool IsDiscounting {
			get {
				return Discount != "" && ShowDiscount;
			}
		}

		public bool IsGamePass {
			get {
				return (mGamePassPC != "" || mGamePassConsole != "" || mGamePassCloud != "") && ShowGamePass;
			}
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

		public double Width
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
			private get;
			set;
		}

		public string OneS {
			private get;
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

		public byte[] SeriesXSHeader {
			private get;
			set;
		}

		public byte[] OneSHeader {
			private get;
			set;
		}

		public byte[] PlayAnywhereSeriesHeader
		{
			private get;
			set;
		}

		public byte[] PlayAnywhereHeader
		{
			private get;
			set;
		}

		public byte[] PCHeader {
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

		public string ThumbnailPath
		{
			get
			{
				var fileName = ID;
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
						return "none";
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
					return "none";
				}
			}
			set {
			
			}
		}

		private async void LoadImage()
		{
			try
			{
				if (!mThumbnailCached)
				{
					if (await Utils.DownloadImage(ThumbnailUrl, ID, SeriesXS, OneS, PC, PlayAnywhere, SeriesXSHeader, OneSHeader, PlayAnywhereSeriesHeader, PlayAnywhereHeader, PCHeader))
						NotifyPropertyChanged("ThumbnailPath");
				}	
			}
			catch (Exception exception)
			{
				Debug.WriteLine($"이미지를 다운로드할 수 없습니다: {ThumbnailUrl}({exception.Message})");
			}
		}


		private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		public event PropertyChangedEventHandler PropertyChanged;
	}
}
