using MiningImpactSensor;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Data.Json;
using Windows.Storage;

namespace SensorTag
{
    class ShokpodSettings
    {
        private static string FILE_NAME = "settings.json";
        public string ShokpodApiLocation { get; set; }
        private static ShokpodSettings _settings = null;

        public static async Task<ShokpodSettings> getSettings()
        {
            try
            {
                if (_settings == null)
                {
                    _settings = new ShokpodSettings();
                    Windows.Storage.StorageFolder localFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
                    StorageFile settingsFile = await localFolder.CreateFileAsync(FILE_NAME, CreationCollisionOption.OpenIfExists);
                    String settingJson = await FileIO.ReadTextAsync(settingsFile);

                    App.Debug(settingJson);

                    JsonObject settingsObject;
                    if (String.IsNullOrEmpty(settingJson))
                    {
                        settingsObject = new JsonObject();
                    }
                    else
                    {
                        settingsObject = JsonObject.Parse(settingJson);
                    }
                    try
                    {
                        _settings.ShokpodApiLocation = settingsObject.GetNamedString("ShokpodApiLocation");
                    } catch(Exception e)
                    {
                        _settings.ShokpodApiLocation = @"http://localhost:8081";
                        saveToFile(_settings);
                    }
                }
            }
            catch (Exception e)
            {
                App.Debug("Error reading " + FILE_NAME + "." + e.Message);
            }
            return _settings;
        }

        public static async void saveToFile(ShokpodSettings _settings)
        {
            Windows.Storage.StorageFolder localFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
            StorageFile settingsFile = await localFolder.CreateFileAsync(FILE_NAME, CreationCollisionOption.ReplaceExisting);

            string json = JsonConvert.SerializeObject(_settings);
            await FileIO.WriteTextAsync(settingsFile, json);
        }
    }
}
