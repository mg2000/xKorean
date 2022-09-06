using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Security.ExchangeActiveSyncProvisioning;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Web.Http;

// The Content Dialog item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace xKorean
{
    public sealed partial class ErrorReportDialog : ContentDialog
    {
        private string mGameName = "";

        public ErrorReportDialog(string gameName, string country)
        {
            this.InitializeComponent();

            this.Title = $"\'{gameName}' 오류 신고";

            mGameName = gameName;

            deviceInfo.Text = $"기기 지역: {Windows.System.UserProfile.GlobalizationPreferences.HomeGeographicRegion}, 스토어 지역: {country}";
        }

        private async void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            var httpClient = new HttpClient();

            try
            {
                var deviceType = new EasClientDeviceInformation().SystemProductName;
                if (deviceType.ToLower() == "xbox")
                    deviceType = "Xbox";
                else
                    deviceType = "PC";

                var requestParam = new Dictionary<string, string>
                {
                    ["name"] = mGameName,
                    ["cantBuy"] = cantBuy.IsChecked.ToString(),
                    ["noSupportRegion"] = noSupportRegion.IsChecked.ToString(),
                    ["message"] = etcMessage.Text.Trim(),
                    ["deviceRegion"] = Windows.System.UserProfile.GlobalizationPreferences.HomeGeographicRegion,
                    ["deviceType"] = deviceType
                };

#               if DEBUG
                var response = await httpClient.PostAsync(new Uri("http://127.0.0.1:8080/report_error"), new HttpStringContent(JsonConvert.SerializeObject(requestParam), Windows.Storage.Streams.UnicodeEncoding.Utf8, "application/json"));
#else
                var response = await httpClient.PostAsync(new Uri("https://xbox-korean-viewer-server2.herokuapp.com/report_error"), new HttpStringContent(JsonConvert.SerializeObject(requestParam), Windows.Storage.Streams.UnicodeEncoding.Utf8, "application/json"));
#endif

                await response.Content.ReadAsStringAsync();
            }
            catch (Exception exception)
            {
                System.Diagnostics.Debug.WriteLine($"오류 전송 에러: {exception.Message}");
            }
        }

        private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
        }
    }
}
