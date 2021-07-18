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
					GamesView.ItemHeight = 240;
					break;
			}
		}

		public void SetEdition(string displayLanguage, string language, Game game)
		{
			mLinkButtonList.Clear();

			mLanguage = language;
			mBundleList = game.Bundle;

			var titleBuilder = new StringBuilder();
			if (displayLanguage == "Korean")
				titleBuilder.Append(game.KoreanName);
			else
				titleBuilder.Append(game.Name);

			if (game.GamePassConsole == "O" || game.GamePassConsole == "O" || game.GamePassCloud == "O")
				titleBuilder.Append(" [게임패스]");

			if (game.Discount != "")
				titleBuilder.Append($" [{game.Discount}]");

			var defaultButton = new Button
			{
				Content = titleBuilder.ToString(),
				HorizontalAlignment = HorizontalAlignment.Stretch,
				Margin = new Thickness(10)
			};
			defaultButton.Click += async (sender, e) =>
			{
				if (mLanguage.ToLower().IndexOf(Windows.System.UserProfile.GlobalizationPreferences.HomeGeographicRegion.ToLower()) >= 0)
				{
					string baseUri = "ms-windows-store://pdp/?ProductId=" + game.ID;
					Uri storeUri = new Uri(baseUri);
					await Launcher.LaunchUriAsync(storeUri);
				}
				else
					await Launcher.LaunchUriAsync(new Uri($"https://www.microsoft.com/{mLanguage}/p/xKorean/{game.ID}"));
			};
			EditionPanel.Children.Add(defaultButton);

			foreach (var bundle in mBundleList)
			{
				titleBuilder.Clear();
				titleBuilder.Append(bundle.Name);

				if (bundle.GamePassConsole == "O" || bundle.GamePassConsole == "O" || bundle.GamePassCloud == "O")
					titleBuilder.Append(" [게임패스]");

				if (bundle.DiscountType != "")
					titleBuilder.Append($" [{bundle.DiscountType}]");

				var button = new Button
				{
					HorizontalAlignment = HorizontalAlignment.Stretch,
					Margin = new Thickness(10)
				};

				var textBlock = new TextBlock
				{
					TextAlignment = TextAlignment.Center,
					TextWrapping = TextWrapping.WrapWholeWords
				};


				var run = new Run();
				run.Text = titleBuilder.ToString();
				textBlock.Inlines.Add(run);
				button.Content = textBlock;

				button.Click += async (sender, e) =>
				{
					if (mLanguage.ToLower().IndexOf(Windows.System.UserProfile.GlobalizationPreferences.HomeGeographicRegion.ToLower()) >= 0)
					{
						string baseUri = "ms-windows-store://pdp/?ProductId=" + bundle.ID;
						Uri storeUri = new Uri(baseUri);
						await Launcher.LaunchUriAsync(storeUri);
					}
					else
						await Launcher.LaunchUriAsync(new Uri($"https://www.microsoft.com/{mLanguage}/p/xKorean/{bundle.ID}"));
				};

				EditionPanel.Children.Add(button);
				mLinkButtonList.Add(button);
			}

			mEditionViewModel.Clear();
			mEditionViewModel.Add(new EditionViewModel
			{
				ID = game.ID,
				Name = displayLanguage == "Korean" ? game.KoreanName : game.Name,
				Discount = game.Discount,
				IsGamePassPC = game.GamePassPC,
				IsGamePassConsole = game.GamePassConsole,
				IsGamePassCloud = game.GamePassCloud
			});

			foreach (var bundle in mBundleList)
			{
				mEditionViewModel.Add(new EditionViewModel
				{
					ID = bundle.ID,
					Name = bundle.Name,
					Discount = bundle.DiscountType,
					IsGamePassPC = bundle.GamePassPC,
					IsGamePassConsole = bundle.GamePassConsole,
					IsGamePassCloud = bundle.GamePassCloud
				});
			}
		}

		private void GamesView_ItemClick(object sender, ItemClickEventArgs e)
		{
			if (e.ClickedItem != null)
			{
				//GoToStore((e.ClickedItem as GameViewModel).Game);
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
