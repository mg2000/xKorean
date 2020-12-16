using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.System.Profile;
using Windows.UI;
using Windows.UI.Xaml;

namespace xKorean
{
    public class GameViewModel : INotifyPropertyChanged
    {
        public Game Game { set; get; } = new Game();
        public string Title { set; get; }
        //public string Description { set; get; }

        public string ThumbnailUrl { set; get; }

        public Dictionary<string, long> DownloadSize { set; get; } = new Dictionary<string, long>();
        public DateTime ReleaseDate { set; get; }
        private string _thumbnailPath;

        public string PackageOnly { get; set; }
        public string ThumbnailPath
        {
            get
            {
                FileInfo thumbnailCacheInfo = new FileInfo(ApplicationData.Current.LocalFolder.Path + "\\ThumbnailCache\\" + ID + ".jpg");

                if (thumbnailCacheInfo.Exists)
                {
                    if (thumbnailCacheInfo.Length == 0)
                    {
                        LoadImage();
                        return null;
                    }
                    else
                    {
                        IsThumbnailCached = true;
                        return thumbnailCacheInfo.FullName;
                    }
                }
                else
                {
                    LoadImage();
                    return null;
                }
            }
        }

        public string Localize { set; get; } = "";
        //public int Metascore { set; get; } = -1;
        //public Uri MetacriticUrl { set; get; }

        //public List<string> Categories { set; get; } = new List<string>();
        public string ID { set; get; }

        public string StoreUri { get; set; } = "";
        public List<string> Screenshots { set; get; } = new List<string>();
        public GameViewModel(Game game)
        {
            Game = game;
            Title = game.KoreanName;
            //Description = game.Description.First().Value;
            ThumbnailUrl = game.Thumbnail;
            ID = game.ID;
            Localize = game.Localize.Replace("/", "\r\n");
            StoreUri = game.StoreLink;
            Discount = game.Discount;

            
            //Categories.AddRange(game.Categories);
            //ReleaseDate = DateTime.FromBinary(game.ReleaseDate);
            //Screenshots = game.ScreenShots;
            //if (game.MetaScore.Count != 0)
            //{
            //    Metascore = game.MetaScore.First().Value;

            //    if (game.MetacriticUrls.Count != 0)
            //    {
            //        MetacriticUrl = game.MetacriticUrls.First().Value;
            //    }

            //}

            //if(game.DownloadSize.Count != 0)
            //{
            //    DownloadSize = game.DownloadSize;
            //}
        }

        public string Message
        {
            get
            {
                return Game.Message;
            }
        }

        public string IsGamePassCloud
        {
            get
            {
                if (Game.GamePassCloud == "O")
                    return "클";
                else
                    return "";
            }
        }

        public string IsGamePassPC
        {
            get
            {
                if (Game.GamePassPC == "O")
                    return "피";
                else
                    return "";
            }
        }

        public string IsGamePassConsole
        {
            get
            {
                if (Game.GamePassConsole == "O")
                    return "엑";
                else
                    return "";
            }
        }

        public bool IsGamePass
        {
            get
            {
                if (Game.GamePassCloud == "O" || Game.GamePassPC == "O" || Game.GamePassConsole == "O")
                    return true;
                else
                    return false;
            }
        }

        public string GamePass
        {
            get
            {
                var gamePassStatus = "";
                if (Game.GamePassCloud == "O" || Game.GamePassPC == "O" || Game.GamePassConsole == "O")
                    gamePassStatus = "게임패스";

                if (Game.GamePassNew == "O")
                    gamePassStatus += " 신규";
                else if (Game.GamePassEnd == "O")
                    gamePassStatus += " 만기";

                return gamePassStatus;
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
        public Visibility IsLocalizeAvaliable
        {
            get
            {
                return Localize != "" ? Visibility.Visible : Visibility.Collapsed;
            }

        }
        public Color LocalizeColor
        {
            get
            {
                if (Localize.Contains("음성"))
                {
                    return Color.FromArgb(0xff, 0x74, 0xcb, 0x2c);
                }
                else if (Localize.Contains("자막"))
                {
                    return Color.FromArgb(0xff, 0xFB, 0xCC, 0x21);
                }
                else
                {
                    return Color.FromArgb(0xff, 0xF5, 0x16, 0x00);
                }
            }
        }

        public bool IsDiscounting
        {
            get
            {
                return Discount != ""; 
            }
        }

        public string Discount
        {
            get;
            set;
        } = "";

        public bool IsThumbnailCached { set; get; } = false;
        private async void LoadImage()
        {
            var httpClient = new Windows.Web.Http.HttpClient();

            try
            {
                var buffer = await httpClient.GetBufferAsync(new Uri(ThumbnailUrl));

                if (!IsThumbnailCached)
                {
                    try
                    {
                        var file = await App.CacheFolder.CreateFileAsync(ID + ".jpg", CreationCollisionOption.ReplaceExisting);

                        await FileIO.WriteBufferAsync(file, buffer);
                    }
                    catch (Exception exception)
                    {
                        switch ((uint)exception.HResult)
                        {
                            case 0x800700B7:
                                System.Diagnostics.Debug.WriteLine($"이미지를 저장할 수 없습니다: {exception.Message}");
                                break;
                        }
                    }
                }

                NotifyPropertyChanged("ThumbnailPath");

            }
            catch (Exception exception)
            {
                System.Diagnostics.Debug.WriteLine($"이미지를 다운로드할 수 없습니다: {exception.Message}");
            }
        }

        static public double MaxWidth
        {
            get
            {
                double maxWidth = 202.5;
                switch (AnalyticsInfo.VersionInfo.DeviceFamily)
                {
                    case "Windows.Xbox":
                        maxWidth = 150;
                        break;
                }
                return maxWidth;
            }
        }

        static public double TitleFontSize
        {
            get
            {
                double titleSize = 15;
                switch (AnalyticsInfo.VersionInfo.DeviceFamily)
                {
                    case "Windows.Xbox":
                        titleSize = 13;
                        break;
                }
                return titleSize;

            }
        }


        static public double ItemHeight
        {
            get
            {
                double maxWidth = 303.75;
                switch (AnalyticsInfo.VersionInfo.DeviceFamily)
                {
                    case "Windows.Xbox":
                        maxWidth = 225;
                        break;
                }
                return maxWidth;
            }
        }
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
