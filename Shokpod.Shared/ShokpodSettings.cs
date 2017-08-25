using MiningImpactSensor;
using Newtonsoft.Json;
using Shokpod10;
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
        public int LiveTileUpdatePeriod { get; set; }
        public bool DisplayAcceleration { get; set; }
        public double ServerImpactThreshhold { get; set; }

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
                        _settings.LiveTileUpdatePeriod = (int)settingsObject.GetNamedNumber("LiveTileUpdatePeriod");
                        _settings.ServerImpactThreshhold = settingsObject.GetNamedNumber("ServerImpactThreshhold");
                        _settings.DisplayAcceleration = settingsObject.GetNamedBoolean("DisplayAcceleration");
                    } catch(Exception e)
                    {
                        App.Debug("Error reading " + FILE_NAME + "." + e.Message);
                        //_settings.ShokpodApiLocation = @"http://shokpod.australiaeast.cloudapp.azure.com:8080";
                        _settings.ShokpodApiLocation = @"http://10.10.47.13:8080";
                        
                        _settings.LiveTileUpdatePeriod = 5;
                        _settings.ServerImpactThreshhold = 10;
                        _settings.DisplayAcceleration = false;
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
            try
            {
                Windows.Storage.StorageFolder localFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
                StorageFile settingsFile = await localFolder.CreateFileAsync(FILE_NAME, CreationCollisionOption.ReplaceExisting);

                string json = JsonConvert.SerializeObject(_settings);
                App.Debug(json);
                await FileIO.WriteTextAsync(settingsFile, json);
            } catch(Exception e)
            {
                App.Debug("Error saving " + FILE_NAME + "." + e.Message);
            }
        }
    }
}
