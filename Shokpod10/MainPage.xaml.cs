using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Background;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Shokpod10
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
        }
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            SensorTagPanel.Show();
        }

        protected override void OnNavigatedFrom(Windows.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            SensorTagPanel.Hide();
        }

        private async void RegisterBackgroundTask()
        {
            BackgroundAccessStatus backgroundAccessStatus = await BackgroundExecutionManager.RequestAccessAsync();
            if (backgroundAccessStatus == BackgroundAccessStatus.AllowedMayUseActiveRealTimeConnectivity ||
                backgroundAccessStatus == BackgroundAccessStatus.AllowedWithAlwaysOnRealTimeConnectivity)
            {
                foreach (var task in BackgroundTaskRegistration.AllTasks)
                {
                    if (task.Value.Name == taskName)
                    {
                        task.Value.Unregister(true);
                    }
                }

                BackgroundTaskBuilder taskBuilder = new BackgroundTaskBuilder();
                taskBuilder.Name = taskName;
                taskBuilder.TaskEntryPoint = taskEntryPoint;
                taskBuilder.SetTrigger(new TimeTrigger(5, false));
                var registration = taskBuilder.Register();
                App.Debug("Background task registration initiated for " + taskName + ".");
            }
            else
            {
                var messageDialog = new MessageDialog("Background registration not attempted.");
                await messageDialog.ShowAsync();
            }

        }

        private async void regProgress(BackgroundTaskRegistration sender, BackgroundTaskCompletedEventArgs args)
        {
            App.Debug("Background task registration in progress for " + taskName + ".");
            CoreDispatcher coreDispatcher = CoreApplication.MainView.CoreWindow.Dispatcher;
            await coreDispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                var messageDialog = new MessageDialog("Background registration is in progress.");
                await messageDialog.ShowAsync();
            });
        }

        private async void regCompleted(BackgroundTaskRegistration sender, BackgroundTaskCompletedEventArgs args)
        {
            App.Debug("Background task registration completed for " + taskName + ".");
            CoreDispatcher coreDispatcher = CoreApplication.MainView.CoreWindow.Dispatcher;
            await coreDispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                var messageDialog = new MessageDialog("Background registration is completed!");
                await messageDialog.ShowAsync();
            });
        }

        private const string taskName = "LiveTileBackgroundTask";
        private const string taskEntryPoint = "LiveTileBackgroundTask.LiveTileTask";

        private void Page_Loaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            //this.RegisterBackgroundTask();
        }
    }
}
