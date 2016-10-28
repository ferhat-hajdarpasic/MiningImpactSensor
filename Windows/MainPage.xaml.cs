using Windows.ApplicationModel.Background;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using System;
using Windows.UI.Popups;
using Windows.UI.Core;
using Windows.ApplicationModel.Core;
using System.Diagnostics;


// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace MiningImpactSensor
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
        

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.  The Parameter
        /// property is typically used to configure the page.</param>
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
                taskBuilder.SetTrigger(new TimeTrigger(15, false));
                var registration = taskBuilder.Register();
                Debug.WriteLine("Background task registration initiated for " + taskName + ".");
                //registration.Completed -= regCompleted;
                //registration.Completed -= regProgress;
            } else
            {
                var messageDialog = new MessageDialog("Background registration not attempted.");
                await messageDialog.ShowAsync();
            }

        }

        private async void regProgress(BackgroundTaskRegistration sender, BackgroundTaskCompletedEventArgs args)
        {
            Debug.WriteLine("Background task registration in progress for " + taskName + ".");
            CoreDispatcher coreDispatcher = CoreApplication.MainView.CoreWindow.Dispatcher;
            await coreDispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
             {
                 var messageDialog = new MessageDialog("Background registration is in progress.");
                 await messageDialog.ShowAsync();
             });
        }

        private async void regCompleted(BackgroundTaskRegistration sender, BackgroundTaskCompletedEventArgs args)
        {
            Debug.WriteLine("Background task registration completed for " + taskName + ".");
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
            this.RegisterBackgroundTask();
        }
    }    
}
