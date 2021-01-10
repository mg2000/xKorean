using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace xKorean
{
    public class Game
    {
        [JsonProperty("id")]
        public string ID { set; get; } = string.Empty;

        [JsonProperty("name")]
        public string Name { set; get; } = string.Empty;

        [JsonProperty("koreanName")]
        public string KoreanName { set; get; } = string.Empty;

        [JsonProperty("localize")]
        public string Localize { set; get; } = string.Empty;

        [JsonProperty("seriesXS")]
        public string SeriesXS { set; get; } = string.Empty;

        [JsonProperty("oneXEnhanced")]
        public string OneXEnhanced { set; get; } = string.Empty;

        [JsonProperty("oneS")]
        public string OneS { set; get; } = string.Empty;

        [JsonProperty("x360")]
        public string X360 { set; get; } = string.Empty;

        [JsonProperty("og")]
        public string OG { set; get; } = string.Empty;

        [JsonProperty("message")]
        public string Message { set; get; } = string.Empty;

        [JsonProperty("storeLink")]
        public string StoreLink { set; get; } = string.Empty;

        [JsonProperty("thumbnail")]
        public string Thumbnail { set; get; } = string.Empty;

        [JsonProperty("gamePassCloud")]
        public string GamePassCloud { set; get; } = string.Empty;

        [JsonProperty("gamePassPC")]
        public string GamePassPC { set; get; } = string.Empty;

        [JsonProperty("gamePassConsole")]
        public string GamePassConsole { set; get; } = string.Empty;

        [JsonProperty("gamePassNew")]
        public string GamePassNew { set; get; } = string.Empty;

        [JsonProperty("gamePassEnd")]
        public string GamePassEnd { set; get; } = string.Empty;

        [JsonProperty("discount")]
        public string Discount { set; get; } = string.Empty;

        [JsonProperty("releaseDate")]
        public string ReleaseDate { set; get; } = "";

        [JsonProperty("dolbyAtmos")]
        public string DolbyAtmos { set; get; } = "";

        [JsonProperty("consoleKeyboardMouse")]
        public string ConsoleKeyboardMouse { set; get; } = "";

        [JsonProperty("hasPrimary")]
        public string HasPrimary { set; get; } = "";

        [JsonProperty("playAnywhere")]
        public string PlayAnywhere { set; get; } = "";
    }
}
