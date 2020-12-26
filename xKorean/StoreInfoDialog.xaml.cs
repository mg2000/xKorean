using System;
using System.Text;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Content Dialog item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace xKorean
{
    public sealed partial class StoreInfoDialog : ContentDialog
    {
        public string ChooseItem
        {
            get;
            private set;
        }

        public StoreInfoDialog()
        {
            this.InitializeComponent();
        }

        public StoreInfoDialog(string message, bool useOneStore, bool useRemaster, bool use360Market, string dlRegionName)
        {
            this.InitializeComponent();

            StoreInfoTextBlock.Text = message;

            if (useOneStore)
                GoToOneButton.Visibility = Visibility.Visible;

            if (dlRegionName != "")
            {
                GoToDLRegionButton.Content = $"{dlRegionName} 스토어로 이동";
                GoToDLRegionButton.Visibility = Visibility.Visible;
            }

            if (useRemaster)
                GoToRemasterButton.Visibility = Visibility.Visible;

            if (use360Market)
                GoTo360Market.Visibility = Visibility.Visible;
        }

        private void GoToStoreButton_Click(object sender, RoutedEventArgs e)
        {
            ChooseItem = "store";
            Hide();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            ChooseItem = "";
            Hide(); 
        }

        private void GoToOneButton_Click(object sender, RoutedEventArgs e)
        {
            ChooseItem = "oneStore";
            Hide();
        }

        private void GoTo360Market_Click(object sender, RoutedEventArgs e)
        {
            ChooseItem = "360market";
            Hide();
        }

        private void GoToRemasterButton_Click(object sender, RoutedEventArgs e)
        {
            ChooseItem = "remaster";
            Hide();
        }

        private void DLRegionButton_Click(object sender, RoutedEventArgs e)
        {
            ChooseItem = "DLstore";
            Hide();
        }
    }
}
