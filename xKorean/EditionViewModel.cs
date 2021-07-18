using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Windows.System.Profile;
using Windows.UI.Xaml;

namespace xKorean
{
	class EditionViewModel
	{
		private string mGamePassPC = "";
		private string mGamePassConsole = "";
		private string mGamePassCloud = "";

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
					mGamePassConsole = "콘";
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

		public bool IsDiscounting {
			get {
				return Discount != "";
			}
		}

		public bool IsGamePass {
			get {
				return mGamePassPC != "" || mGamePassConsole != "" || mGamePassCloud != "";
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


		private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		public event PropertyChangedEventHandler PropertyChanged;
	}
}
