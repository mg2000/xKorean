using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
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
        }

        private async void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            var settings = Settings.Instance;   

            if (KoreanRadioButton.IsChecked == true)
                await settings.SetValue("gameNameDisplayLanguage", "Korean");
            else
                await settings.SetValue("gameNameDisplayLanguage", "English");

            await settings.SetValue("ShowNewTitle", ShowNewTitle.IsChecked.ToString());
        }
    }
}
