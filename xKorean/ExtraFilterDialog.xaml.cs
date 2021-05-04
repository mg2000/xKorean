using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
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
    public sealed partial class ExtraFilterDialog : ContentDialog
    {
        public ExtraFilterDialog()
        {
            this.InitializeComponent();

            var settings = Settings.Instance;
            if (settings.LoadValue("usePlayAnywhere") == "True")
                UsePlayAnywhere.IsChecked = true;

            if (settings.LoadValue("useDolbyAtmos") == "True")
                UseDolbyAtmos.IsChecked = true;

            if (settings.LoadValue("useKeyboardMouse") == "True")
                UseKeyboardMouse.IsChecked = true;

            if (settings.LoadValue("useLocalCoop") == "True")
                UseLocalCoopCheckBox.IsChecked = true;

            if (settings.LoadValue("useOnlineCoop") == "True")
                UseOnlineCoopCheckBox.IsChecked = true;

            if (settings.LoadValue("useFPS120") == "True")
                UseFPS120CheckBox.IsChecked = true;

            if (settings.LoadValue("useFPSBoost") == "True")
                UseFPS120CheckBox.IsChecked = true;
        }

        private async void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            var settings = Settings.Instance;
            await settings.SetValue("usePlayAnywhere", UsePlayAnywhere.IsChecked.ToString());
            await settings.SetValue("useDolbyAtmos", UseDolbyAtmos.IsChecked.ToString());
            await settings.SetValue("useKeyboardMouse", UseKeyboardMouse.IsChecked.ToString());
            await settings.SetValue("useLocalCoop", UseLocalCoopCheckBox.IsChecked.ToString());
            await settings.SetValue("useOnlineCoop", UseOnlineCoopCheckBox.IsChecked.ToString());
            await settings.SetValue("useFPS120", UseFPS120CheckBox.IsChecked.ToString());
            await settings.SetValue("useFPSBoost", UseFPSBoostCheckBox.IsChecked.ToString());
        }

        private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
        }
    }
}

