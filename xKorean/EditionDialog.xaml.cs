using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System;
using Windows.System.Profile;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Content Dialog item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace xKorean
{
	public sealed partial class EditionDialog : ContentDialog
	{
		private string mLanguage;
		private List<Bundle> mBundleList;
		private List<Button> mLinkButtonList = new List<Button>();

		private ObservableCollection<EditionViewModel> mEditionViewModel { get; set; } = new ObservableCollection<EditionViewModel>();

		public EditionDialog()
		{
			this.InitializeComponent();

			switch (AnalyticsInfo.VersionInfo.DeviceFamily)
			{
				case "Windows.Xbox":
					GamesView.DesiredWidth = 130;
					GamesView.ItemHeight = 195;
					break;
				default:
					GamesView.DesiredWidth = 160;
					GamesView.MinWidth = 160;
					GamesView.ItemHeight = 240;
					break;
			}
		}

		public void SetEdition(string displayLanguage, string language, Game game, byte[] seriesXSHeader, byte[] oneSHeader)
		{
			mLinkButtonList.Clear();

			mLanguage = language;
			mBundleList = game.Bundle;

			mEditionViewModel.Clear();

			if (game.IsAvailable)
			{
				mEditionViewModel.Add(new EditionViewModel
				{
					ID = game.ID,
					Name = displayLanguage == "Korean" ? game.KoreanName : game.Name,
					Discount = game.Discount,
					SeriesXS = game.SeriesXS,
					IsGamePassPC = game.GamePassPC,
					IsGamePassConsole = game.GamePassConsole,
					IsGamePassCloud = game.GamePassCloud,
					GamePassNew = game.GamePassNew,
					GamePassEnd = game.GamePassEnd,
					ThumbnailUrl = game.Thumbnail,
					SeriesXSHeader = seriesXSHeader,
					OneSHeader = oneSHeader
				});
			}

			foreach (var bundle in mBundleList)
			{
				mEditionViewModel.Add(new EditionViewModel
				{
					ID = bundle.ID,
					Name = bundle.Name,
					Discount = bundle.DiscountType,
					SeriesXS = bundle.SeriesXS,
					IsGamePassPC = bundle.GamePassPC,
					IsGamePassConsole = bundle.GamePassConsole,
					IsGamePassCloud = bundle.GamePassCloud,
					GamePassNew = bundle.GamePassNew,
					GamePassEnd = bundle.GamePassEnd,
					ThumbnailUrl = game.Thumbnail,
					SeriesXSHeader = seriesXSHeader,
					OneSHeader = oneSHeader
				});
			}
		}

		private async void GamesView_ItemClick(object sender, ItemClickEventArgs e)
		{
			if (e.ClickedItem != null)
			{
				var bundle = e.ClickedItem as EditionViewModel;

				if (mLanguage.ToLower().IndexOf(Windows.System.UserProfile.GlobalizationPreferences.HomeGeographicRegion.ToLower()) >= 0)
					await Launcher.LaunchUriAsync(new Uri($"ms-windows-store://pdp/?productId={bundle.ID}"));
				else
				{
					await Launcher.LaunchUriAsync(new Uri($"https://www.microsoft.com/{mLanguage}/p/xkorean/{bundle.ID}"));
				}
			}
		}

		private void PosterImage_ImageOpened(object sender, RoutedEventArgs e)
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
	}
}
