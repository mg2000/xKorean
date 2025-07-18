﻿using Newtonsoft.Json;
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

		[JsonProperty("publisherName")]
		public string PublisherName { set; get; } = string.Empty;

		[JsonProperty("localize")]
		public string Localize { set; get; } = string.Empty;

		[JsonProperty("seriesXS")]
		public string SeriesXS { set; get; } = string.Empty;

		[JsonProperty("oneXEnhanced")]
		public string OneXEnhanced { set; get; } = string.Empty;

		[JsonProperty("oneS")]
		public string OneS { set; get; } = string.Empty;

		[JsonProperty("pc")]
		public string PC { set; get; } = string.Empty;

		[JsonProperty("x360")]
		public string X360 { set; get; } = string.Empty;

		[JsonProperty("og")]
		public string OG { set; get; } = string.Empty;

		[JsonProperty("message")]
		public string Message { set; get; } = string.Empty;

		[JsonProperty("thumbnail")]
		public string Thumbnail { set; get; } = string.Empty;

		[JsonProperty("thumbnailID")]
		public string ThumbnailID { set; get; } = string.Empty;

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

		[JsonProperty("gamePassComing")]
		public string GamePassComing { set; get; } = string.Empty;

        [JsonProperty("gamePassRegisterDate")]
        public string GamePassRegisterDate { set; get; } = string.Empty;

		[JsonProperty("buyAndCloud")]
		public string BuyAndCloud { set; get; } = string.Empty;

		[JsonProperty("discount")]
		public string Discount { set; get; } = string.Empty;

		[JsonProperty("releaseDate")]
		public string ReleaseDate { set; get; } = "";

		[JsonProperty("nzReleaseDate")]
		public string NZReleaseDate { set; get; } = "";

        [JsonProperty("kiReleaseDate")]
        public string KIReleaseDate { set; get; } = "";

        [JsonProperty("dolbyAtmos")]
		public string DolbyAtmos { set; get; } = "";

		[JsonProperty("consoleKeyboardMouse")]
		public string ConsoleKeyboardMouse { set; get; } = "";

		[JsonProperty("playAnywhere")]
		public string PlayAnywhere { set; get; } = "";

		[JsonProperty("localCoop")]
		public string LocalCoop { set; get; } = "";

		[JsonProperty("onlineCoop")]
		public string OnlineCoop { set; get; } = "";

		[JsonProperty("fps120")]
		public string FPS120 { set; get; } = "";

		[JsonProperty("fpsBoost")]
		public string FPSBoost { set; get; } = "";

		[JsonProperty("categories")]
		public string[] Categories { set; get; } = null;

		[JsonProperty("price")]
		public float Price
		{
			get;
			set;
		} = -1;

		[JsonProperty("bundle")]
		public List<Bundle> Bundle
		{
			get;
			set;
		} = new List<Bundle>();

		[JsonProperty("lowestPrice")]
		public float LowestPrice
		{
			get;
			set;
		} = -1;


		[JsonProperty("packages")]
		public string Packages
		{
			get;
			set;
		} = "";

		[JsonProperty("languageCode")]
		public string LanguageCode
		{
			get;
			set;
		} = "";

        [JsonProperty("isFirstParty")]
        public string IsFirstParty
        {
            get;
            set;
        } = "";

		[JsonProperty("age")]
		public string Age
		{
			get;
			set;
		} = "";

        public bool IsAvailable {
			get {
				return Discount != "판매 중지" || GamePassPC != "" || GamePassConsole != "" || GamePassCloud != "" || (Discount.IndexOf("출시") >= 0 && Price == -1 && Bundle.Count > 0) || Discount.IndexOf("주말 무료") >= 0;
			}
		}
	}

	public class Bundle {
		[JsonProperty("id")]
		public string ID {
			get;
			set;
		} = "";

		[JsonProperty("name")]
		public string Name
		{
			get;
			set;
		} = "";


		[JsonProperty("price")]
		public float Price
		{
			get;
			set;
		} = 0;

		[JsonProperty("discountType")]
		public string DiscountType {
			get;
			set;
		} = "";

		[JsonProperty("thumbnail")]
		public string Thumbnail
		{
			get;
			set;
		} = "";

		[JsonProperty("thumbnailID")]
		public string ThumbnailID
		{
			get;
			set;
		} = "";

		[JsonProperty("seriesXS")]
		public string SeriesXS
		{
			get;
			set;
		} = "";

		[JsonProperty("oneS")]
		public string OneS
		{
			get;
			set;
		} = "";

		[JsonProperty("pc")]
		public string PC
		{
			get;
			set;
		} = "";

        [JsonProperty("playAnywhere")]
        public string PlayAnywhere { set; get; } = "";

        [JsonProperty("gamePassPC")]
		public string GamePassPC
		{
			get;
			set;
		} = "";

		[JsonProperty("gamePassConsole")]
		public string GamePassConsole {
			get;
			set;
		} = "";

		[JsonProperty("gamePassCloud")]
		public string GamePassCloud
		{
			get;
			set;
		} = "";

		[JsonProperty("gamePassNew")]
		public string GamePassNew
		{
			get;
			set;
		} = "";

		[JsonProperty("gamePassEnd")]
		public string GamePassEnd
		{
			get;
			set;
		} = "";

		[JsonProperty("gamePassComing")]
		public string GamePassComing
		{
			get;
			set;
		} = "";

		[JsonProperty("buyAndCloud")]
		public string BuyAndCloud
		{
			get;
			set;
		} = "";

		[JsonProperty("releaseDate")]
		public string ReleaseDate
		{
			get;
			set;
		} = "";

		[JsonProperty("nzReleaseDate")]
		public string NZReleaseDate {
			get;
			set;
		} = "";

        [JsonProperty("kiReleaseDate")]
        public string KIReleaseDate
        {
            get;
            set;
        } = "";

        [JsonProperty("lowestPrice")]
		public float LowestPrice
		{
			get;
			set;
		} = -1;

		public bool IsAvailable
		{
			get
			{
				return DiscountType != "판매 중지" || GamePassPC != "" || GamePassConsole != "" || GamePassCloud != "" || BuyAndCloud != "" || Price > 0 || DiscountType.IndexOf("주말 무료") >= 0;
			}
		}
	}

	public class ExchangeRate
	{
        [JsonProperty("country")]
        public string Country
		{
			get;
			set;
		}

        [JsonProperty("rate")]
        public float Rate
        {
            get;
            set;
        }
    }

	public class FreeWeekendPeriod
	{
        [JsonProperty("startDate")]
        public string StartDate
        {
            get;
            set;
        }

        [JsonProperty("endDate")]
        public string EndDate
        {
            get;
            set;
        }
    }


    public class ServerData
	{
        [JsonProperty("games")]
        public List<Game> Games
		{
			get;
			set;
		}

        [JsonProperty("exchangeRate")]
        public List<ExchangeRate> ExchangeRates
		{
			get;
			set;
		}

		[JsonProperty("freeWeekendPeriod")]
		public FreeWeekendPeriod FreeWeekendPeriod {
			get;
			set;
		}
    }
}