using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace SensorTag
{
    sealed class MetroEventSource : EventSource
    {
        public static MetroEventSource Log = new MetroEventSource();

        [Event(1, Level = EventLevel.Verbose)]
        public void Debug(string message)
        {
            this.WriteEvent(1, message);
        }

        [Event(2, Level = EventLevel.Informational)]
        public void Info(string message)
        {
            this.WriteEvent(2, message);
        }

        [Event(3, Level = EventLevel.Warning)]
        public void Warn(string message)
        {
            this.WriteEvent(3, message);
        }

        [Event(4, Level = EventLevel.Error)]
        public void Error(string message)
        {
            this.WriteEvent(4, message);
        }

        [Event(5, Level = EventLevel.Critical)]
        public void Critical(string message)
        {
            this.WriteEvent(5, message);
        }
        public static async void ToastAsync(string msg)
        {
            ShokpodSettings settings = await ShokpodSettings.getSettings();
            if (settings.DisplayAcceleration)
            {
                string xml = $@"
                <toast activationType='foreground' launch='args'>
                    <visual>
                        <binding template='ToastGeneric'>
                            <text>This is a toast notification</text>
                            <text>{msg}</text>
                        </binding>
                    </visual>
                </toast>";

                XmlDocument doc = new XmlDocument();
                doc.LoadXml(xml);

                ToastNotification notification = new ToastNotification(doc);
                ToastNotifier notifier = ToastNotificationManager.CreateToastNotifier();
                notifier.Show(notification);
            }
        }


    }
}
