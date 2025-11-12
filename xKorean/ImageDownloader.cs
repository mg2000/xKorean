using Microsoft.Data.Sqlite;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Storage;
using xKorean;

public class ImageDownloader
{
    private static readonly ImageDownloader _instance = new ImageDownloader();
    public static ImageDownloader Instance => _instance;

    private BlockingCollection<ProducedItem> Queue { get; set; }
    public CancellationTokenSource Cts { get; private set; }

    private List<Task> _producerTasks = new List<Task>();
    private Task _consumerTask;

    private int _producerIdCounter = 0;
    private int _producerShutdownCount = 0;

    private readonly object _lock = new object();

    private ImageDownloader() { }

    public void Start()
    {
        if (Queue != null) return;

        Queue = new BlockingCollection<ProducedItem>(20);
        Cts = new CancellationTokenSource();

        _consumerTask = Task.Run(Consumer);
    }

    public void Stop()
    {
        if (Cts == null || Cts.IsCancellationRequested)
            return;

        Cts.Cancel();

        try
        {
            Task.WaitAll(_producerTasks.ToArray());
            _consumerTask?.Wait();
        }
        catch (AggregateException) { }

        Queue?.Dispose();
        Cts.Dispose();

        Queue = null;
        Cts = null;
        _producerTasks.Clear();
        Console.WriteLine("All tasks stopped.");
    }

    public bool AreAllProducersStopped => _producerShutdownCount >= _producerIdCounter;

    /// ✅ 소비 완료 콜백 포함 생산자 추가
    public void AddProducer(string thumbnailUrl, string id, string thumbnailID, string seriesXS, string oneS, string pc, string playAnywhere, Action<bool> onConsumed = null)
    {
        if (Cts == null || Cts.IsCancellationRequested)
            return;

        var task = Task.Run(() => Producer(thumbnailUrl, id, thumbnailID, seriesXS, oneS, pc, playAnywhere, onConsumed));
        lock (_lock)
        {
            _producerTasks.Add(task);
        }
    }

    private void Producer(string thumbnailUrl, string id, string thumbnailID, string seriesXS, string oneS, string pc, string playAnywhere, Action<bool> onConsumedCallback)
    {
        try
        {
            if (!Cts.Token.IsCancellationRequested)
            {
                var item = new ProducedItem
                {
                    ThumbnailUrl = thumbnailUrl,
                    ID = id,
                    ThumbnailID = thumbnailID,
                    SeriesXS = seriesXS,
                    OneS = oneS,
                    PC = pc,
                    PlayAnywhere = playAnywhere,
                    OnConsumedCallback = onConsumedCallback
                };

                Queue.Add(item);
            }
        }
        finally
        {
        }
    }

    private async void Consumer()
    {
        try
        {
            while (true)
            {
                Debug.WriteLine("꺼내기 전 개수: " + Queue.Count);
                ProducedItem item = Queue.Take(Cts.Token);
                Debug.WriteLine("남은 개수: " + Queue.Count);

                var httpClient = new Windows.Web.Http.HttpClient();

                var oldFileName = new StringBuilder();
                lock (_lock)
                {
                    using (var db = new SqliteConnection($"FileName={CommonSingleton.Instance.DBPath}"))
                    {
                        db.Open();

                        var selectCommand = new SqliteCommand
                        {
                            Connection = db,
                            CommandText = "SELECT info FROM ThumbnailTable WHERE id = @id"
                        };
                        selectCommand.Parameters.AddWithValue("@id", item.ID);

                        var query = selectCommand.ExecuteReader();

                        while (query.Read())
                        {
                            var oldThumbnailInfo = JsonConvert.DeserializeObject<Dictionary<string, string>>(query.GetString(0));
                            oldFileName.Append(item.ID);
                            oldFileName.Append("_");
                            oldFileName.Append(oldThumbnailInfo["ThumbnailID"]);
                            if (oldThumbnailInfo["PlayAnywhere"] == "O")
                            {
                                if (oldThumbnailInfo["SeriesXS"] == "O")
                                    oldFileName.Append("_playanywhere_xs");
                                else if (item.OneS == "O")
                                    oldFileName.Append("_playanywhere_os");
                            }
                            else if (oldThumbnailInfo["SeriesXS"] == "O")
                                oldFileName.Append("_xs");
                            else if (oldThumbnailInfo["OneS"] == "O")
                                oldFileName.Append("_os");
                            else if (oldThumbnailInfo.ContainsKey("xbox_on_pc") && oldThumbnailInfo["xbox_on_pc"] == "O")
                                oldFileName.Append("_xbox_on_pc");
                        }

                        var deleteCommand = new SqliteCommand();
                        deleteCommand.Connection = db;
                        deleteCommand.CommandText = "DELETE FROM ThumbnailTable WHERE id = @id";
                        deleteCommand.Parameters.AddWithValue("@id", item.ID);

                        deleteCommand.ExecuteNonQuery();
                    }
                }

                if (oldFileName.Length > 0)
                {
                    var oldImageFile = new FileInfo($@"{ApplicationData.Current.LocalFolder.Path}\ThumbnailCache\{oldFileName}.jpg");
                    if (oldImageFile.Exists)
                    {
                        var oriFile = await App.CacheFolder.GetFileAsync(oldImageFile.Name);
                        await oriFile.DeleteAsync();
                    }
                }

                try
                {
                    var buffer = await httpClient.GetBufferAsync(new Uri(item.ThumbnailUrl));

                    var fileName = $"{item.ID}_{item.ThumbnailID}";
                    if (item.PlayAnywhere == "O")
                    {
                        if (item.SeriesXS == "O")
                            fileName += "_playanywhere_xs";
                        else if (item.OneS == "O")
                            fileName += "_playanywhere_os";
                    }
                    else if (item.SeriesXS == "O")
                        fileName += "_xs";
                    else if (item.OneS == "O")
                        fileName += "_os";
                    else if (item.PC == "O")
                        fileName += "_xbox_on_pc";

                    var file = await App.CacheFolder.CreateFileAsync(fileName + ".jpg", CreationCollisionOption.ReplaceExisting);

                    await FileIO.WriteBufferAsync(file, buffer);

                    if (item.SeriesXS == "O" || item.OneS == "O" || item.PC == "O")
                    {
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
                                        if (item.PlayAnywhere == "O")
                                        {
                                            if (item.SeriesXS == "O")
                                                titleHeader = CommonSingleton.Instance.PlayAnywhereSeriesTitleHeader;
                                            else
                                                titleHeader = CommonSingleton.Instance.PlayAnywhereTitleHeader;
                                        }
                                        else if (item.SeriesXS == "O")
                                            titleHeader = CommonSingleton.Instance.SeriesXSTitleHeader;
                                        else if (item.OneS == "O")
                                            titleHeader = CommonSingleton.Instance.OneTitleHeader;
                                        else
                                            titleHeader = CommonSingleton.Instance.WindowsTitleHeader;

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

                        lock (_lock)
                        {
                            using (var db = new SqliteConnection($"FileName={CommonSingleton.Instance.DBPath}"))
                            {
                                db.Open();

                                var thumbnailInfo = new Dictionary<string, string>();

                                thumbnailInfo["ThumbnailID"] = item.ThumbnailID;
                                thumbnailInfo["PlayAnywhere"] = item.PlayAnywhere;
                                thumbnailInfo["SeriesXS"] = item.SeriesXS;
                                thumbnailInfo["OneS"] = item.OneS;
                                thumbnailInfo["PC"] = item.PC;

                                var insertCommand = new SqliteCommand
                                {
                                    Connection = db,
                                    CommandText = "INSERT INTO ThumbnailTable VALUES (@id, @info)"
                                };
                                insertCommand.Parameters.AddWithValue("@id", item.ID);
                                insertCommand.Parameters.AddWithValue("@info", JsonConvert.SerializeObject(thumbnailInfo));

                                insertCommand.ExecuteNonQuery();
                            }
                        }

                        Debug.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} 이미지 다운로드: {item.ThumbnailID} {fileName}");
                    }

                    item.OnConsumedCallback?.Invoke(true);
                }
                catch (Exception exception)
                {
                    switch ((uint)exception.HResult)
                    {
                        case 0x800700B7:
                            Debug.WriteLine($"이미지를 저장할 수 없습니다: {exception.Message}");
                            break;
                    }

                    item.OnConsumedCallback?.Invoke(false);
                }
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Consumer cancelled.");
        }
    }

    private class ProducedItem
    {
        public string ThumbnailUrl { get; set; }
        public string ID { get; set; }
        public string ThumbnailID { get; set; }
        public string SeriesXS { get; set; }
        public string OneS { get; set; }
        public string PC { get; set; }
        public string PlayAnywhere { get; set; }
        public Action<bool> OnConsumedCallback { get; set; }
    }
}
