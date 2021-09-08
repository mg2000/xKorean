using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Storage;

namespace xKorean
{
	class Utils
	{
		public static async Task<bool> DownloadImage(string thumbnailUrl, string id, string seriesXS, string oneS, string pc, byte[] seriesXSHeader, byte[] oneSHeader, byte[] pcHeader) {
			var httpClient = new Windows.Web.Http.HttpClient();

			try
			{
				var buffer = await httpClient.GetBufferAsync(new Uri(thumbnailUrl));

				Debug.WriteLine($"이미지 다운로드: {thumbnailUrl}");

				var fileName = id;
				if (seriesXS == "O")
					fileName += "_xs";
				else if (oneS == "O")
					fileName += "_os";
				else if (pc == "O")
					fileName += "_pc";

				var file = await App.CacheFolder.CreateFileAsync(fileName + ".jpg", CreationCollisionOption.ReplaceExisting);

				await FileIO.WriteBufferAsync(file, buffer);

				if (seriesXS == "O" || oneS == "O" || pc == "O")
				{
					var oldImageFile = new FileInfo($@"{ApplicationData.Current.LocalFolder.Path}\ThumbnailCache\{id}.jpg");
					if (oldImageFile.Exists)
					{
						var oriFile = await App.CacheFolder.GetFileAsync(oldImageFile.Name);
						await oriFile.DeleteAsync();
					}

					//if (seriesXS == "O")
					//	oldImageFile = new FileInfo($@"{ApplicationData.Current.LocalFolder.Path}\ThumbnailCache\{id}_os.jpg");
					//else if (oneS == "O")
					//	oldImageFile = new FileInfo($@"{ApplicationData.Current.LocalFolder.Path}\ThumbnailCache\{id}_xs.jpg");
					//else
					//	oldImageFile = new FileInfo($@"{ApplicationData.Current.LocalFolder.Path}\ThumbnailCache\{id}_pc.jpg");

					//if (oldImageFile.Exists)
					//{
					//	var oriFile = await App.CacheFolder.GetFileAsync(oldImageFile.Name);
					//	await oriFile.DeleteAsync();
					//}

					using (var imageStream = await file.OpenReadAsync())
					{
						var decoder = await BitmapDecoder.CreateAsync(imageStream);
						if (decoder.PixelWidth != 584)
						{
							var resizedImageFile = await App.CacheFolder.CreateFileAsync(fileName + ".resize", CreationCollisionOption.ReplaceExisting);
							using (var resizedStream = await resizedImageFile.OpenAsync(FileAccessMode.ReadWrite))
							{
								var encoder = await BitmapEncoder.CreateForTranscodingAsync(resizedStream, decoder);
								encoder.BitmapTransform.InterpolationMode = BitmapInterpolationMode.Linear;
								encoder.BitmapTransform.ScaledWidth = 584;
								encoder.BitmapTransform.ScaledHeight = 800;
								await encoder.FlushAsync();
							}
						}
					}

					var resizeImageInfo = new FileInfo($@"{ApplicationData.Current.LocalFolder.Path}\ThumbnailCache\{fileName}.resize");
					if (resizeImageInfo.Exists)
					{
						var oriFile = await App.CacheFolder.GetFileAsync($"{fileName}.jpg");
						await oriFile.DeleteAsync();
						var resizeImage = await App.CacheFolder.GetFileAsync($"{fileName}.resize");
						await resizeImage.RenameAsync($"{fileName}.jpg");
					}

					using (var readStream = await file.OpenAsync(FileAccessMode.ReadWrite))
					{ 
						var decoder = await BitmapDecoder.CreateAsync(readStream);
						var softwareBitmap = await decoder.GetSoftwareBitmapAsync();

						using (var bitmapBuffer = softwareBitmap.LockBuffer(BitmapBufferAccessMode.ReadWrite))
						{
							using (var reference = bitmapBuffer.CreateReference())
							{
								unsafe
								{
									((IMemoryBufferByteAccess)reference).GetBuffer(out byte* dataInBytes, out uint capacity);
									BitmapPlaneDescription bufferLayout = bitmapBuffer.GetPlaneDescription(0);

									byte[] titleHeader;
									if (seriesXS == "O")
										titleHeader = seriesXSHeader;
									else if (oneS == "O")
										titleHeader = oneSHeader;
									else
										titleHeader = pcHeader;

									for (var i = 0; i < titleHeader.Length; i++)
									{
										dataInBytes[bufferLayout.StartIndex + i] = titleHeader[i];
									}
								}
							}
						}

						readStream.Seek(0);
						var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, readStream);
						encoder.SetSoftwareBitmap(softwareBitmap);
						await encoder.FlushAsync();
					}
				}

				return true;
			}
			catch (Exception exception)
			{
				switch ((uint)exception.HResult)
				{
					case 0x800700B7:
						System.Diagnostics.Debug.WriteLine($"이미지를 저장할 수 없습니다: {exception.Message}");
						break;
				}

				return false;
			}
		}

		public static string ConvertLanguageCode(string language) {
			switch (language.ToLower())
			{
				case "en":
					return "en-us";
				case "hk":
					return "en-hk";
				case "jp":
					return "ja-jp";
				case "gb":
					return "en-gb";
				default:
					return "ko-kr";
			}
		}

		public static string GetReleaseStr(string releaseDateStr) {
			var releaseDate = DateTime.Parse(releaseDateStr);

			if (releaseDate > DateTime.Now)
				return $"{releaseDate.Month}월 {releaseDate.Day}일 {releaseDate.Hour}시 출시";
			else
				return "";
		}
	}
}
