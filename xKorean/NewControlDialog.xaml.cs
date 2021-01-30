using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
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
    public sealed partial class NewControlDialog : ContentDialog
    {
        public NewControlDialog(List<string> newGames)
        {
            this.InitializeComponent();

            var titleListBuiler = new StringBuilder();

            for (var i = 0; i < 10 && i < newGames.Count; i++)
                titleListBuiler.Append(newGames[i]).Append("\n");

            if (newGames.Count > 10)
                titleListBuiler.Append($"외 {newGames.Count - 10}개의 타이틀\n");

            NewTitleText.Text = titleListBuiler.ToString();
        }

        private async void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            if (DontShow.IsChecked == true)
                await Settings.Instance.SetValue("ShowNewTitle", "False");
        }

        private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
        }
    }
}
