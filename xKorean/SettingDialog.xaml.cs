using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.System.Profile;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Content Dialog item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace xKorean
{
    public sealed partial class SettingDialog : ContentDialog
    {
        private string mGameNameDisplayLanguage;

        public SettingDialog()
        {
            this.InitializeComponent();

            var settings = Settings.Instance;
            
            mGameNameDisplayLanguage = settings.LoadValue("gameNameDisplayLanguage");
            if (mGameNameDisplayLanguage == "English")
                EnglishRadioButton.IsChecked = true;
            else
                KoreanRadioButton.IsChecked = true;

            if (settings.LoadValue("ShowNewTitle") != "False")
                ShowNewTitle.IsChecked = true;

            var localSettings = ApplicationData.Current.LocalSettings;

            if (localSettings.Values["showDiscount"] != null)
                ShowDiscountCheckbox.IsChecked = (bool)localSettings.Values["showDiscount"];
            else
                ShowDiscountCheckbox.IsChecked = true;

            if (localSettings.Values["showGamepass"] != null)
                ShowGamepassCheckbox.IsChecked = (bool)localSettings.Values["showGamepass"];
            else
                ShowGamepassCheckbox.IsChecked = true;

            if (localSettings.Values["showName"] != null)
                ShowNameCheckbox.IsChecked = (bool)localSettings.Values["showName"];
            else
                ShowNameCheckbox.IsChecked = true;

            if (localSettings.Values["showReleaseTime"] != null)
                ShowReleaseTimeCheckbox.IsChecked = (bool)localSettings.Values["showReleaseTime"];
            else
                ShowReleaseTimeCheckbox.IsChecked = false;
        }

        private async void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            var settings = Settings.Instance;   

            if (KoreanRadioButton.IsChecked == true)
                await settings.SetValue("gameNameDisplayLanguage", "Korean");
            else
                await settings.SetValue("gameNameDisplayLanguage", "English");

            await settings.SetValue("ShowNewTitle", ShowNewTitle.IsChecked.ToString());

            var localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values["showDiscount"] = ShowDiscountCheckbox.IsChecked;
            localSettings.Values["showGamepass"] = ShowGamepassCheckbox.IsChecked;
            localSettings.Values["showName"] = ShowNameCheckbox.IsChecked;
            localSettings.Values["showReleaseTime"] = ShowReleaseTimeCheckbox.IsChecked;
        }
    }
}
