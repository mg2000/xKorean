﻿using System;
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
        private string mIconSize;

        public SettingDialog()
        {
            this.InitializeComponent();

            if (AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Xbox")
                XboxNoti.Visibility = Visibility.Visible;
            else
                XboxNoti.Visibility = Visibility.Collapsed;

            var settings = Settings.Instance;
            
            mGameNameDisplayLanguage = settings.LoadValue("gameNameDisplayLanguage");
            if (mGameNameDisplayLanguage == "English")
                EnglishRadioButton.IsChecked = true;
            else
                KoreanRadioButton.IsChecked = true;

            mIconSize = settings.LoadValue("iconSize");
            if (mIconSize == "Normal")
                NormalRadioButton.IsChecked = true;
            else
                SmallRadioButton.IsChecked = true;

            if (settings.LoadValue("ShowNewTitle") != "False")
                ShowNewTitle.IsChecked = true;
        }

        private async void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            var settings = Settings.Instance;   
            if (NormalRadioButton.IsChecked == true)
                await settings.SetValue("iconSize", "Normal");
            else
                await settings.SetValue("iconSize", "Small");

            if (KoreanRadioButton.IsChecked == true)
                await settings.SetValue("gameNameDisplayLanguage", "Korean");
            else
                await settings.SetValue("gameNameDisplayLanguage", "English");

            await settings.SetValue("ShowNewTitle", ShowNewTitle.IsChecked.ToString());

            if (AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Xbox" && ((mIconSize == "Small" && NormalRadioButton.IsChecked == true) || (mIconSize != "Small" && SmallRadioButton.IsChecked == true))) {
                var result = await CoreApplication.RequestRestartAsync("");
                Debug.WriteLine("재시작 결과: " + result);
            }
        }
    }
}
