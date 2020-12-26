using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace xKorean
{
    class Settings
    {
        private static Settings mInstance = null;
        private static readonly object mLock = new object();

        private Dictionary<string, string> mSettings = null;

        Settings()
        {
        }

        public static Settings Instance {
            get {
                lock (mLock)
                {
                    if (mInstance == null)
                        mInstance = new Settings();

                    return mInstance;
                }
            }
        }

        public async Task Load()
        {
            var settingFileInfo = new FileInfo($"{ApplicationData.Current.LocalFolder.Path}\\setting.json");
            if (settingFileInfo.Exists)
            {
                var settingFile = await StorageFile.GetFileFromPathAsync($"{ApplicationData.Current.LocalFolder.Path}\\setting.json");
                var jsonString = await FileIO.ReadTextAsync(settingFile);

                var settingMap = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonString);
                if (settingMap == null)
                    mSettings = new Dictionary<string, string>();
                else
                    mSettings = settingMap;
            }
            else
                mSettings = new Dictionary<string, string>();
        }

        public string LoadValue(string name)
        {
            lock (mLock) {
                string value;
                if (mSettings.TryGetValue(name, out value))
                    return value;
                else
                    return "";
            }
            
        }

        public async Task SetValue(string name, string value)
        {
            lock (mLock)
            {
                mSettings[name] = value;
            }

            await Save();
        }

        private async Task Save()
        {
            var file = await ApplicationData.Current.LocalFolder.CreateFileAsync("setting.json", CreationCollisionOption.ReplaceExisting);
            await FileIO.WriteTextAsync(file, JsonConvert.SerializeObject(mSettings));
        }
    }
}
