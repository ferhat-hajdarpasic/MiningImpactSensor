using System;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.ExtendedExecution;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using SensorTag;
using System.Threading.Tasks;
using Windows.UI.Core;

namespace Shokpod10
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : Application
    {
        MiningImpactSensor.SensorTag _sensor;
        ExtendedExecutionSession extendedExecutionSession;

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            this.Suspending += OnSuspending;
            System.Diagnostics.Tracing.EventListener verboseListener = new StorageFileEventListener("MyListenerVerbose");
            System.Diagnostics.Tracing.EventListener informationListener = new StorageFileEventListener("MyListenerInformation");

            verboseListener.EnableEvents(MetroEventSource.Log, System.Diagnostics.Tracing.EventLevel.Verbose);
            informationListener.EnableEvents(MetroEventSource.Log, System.Diagnostics.Tracing.EventLevel.Informational);
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected async override void OnLaunched(LaunchActivatedEventArgs e)
        {
            Frame rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    //TODO: Load state from previously suspended application
                }

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }

            if (e.PrelaunchActivated == false)
            {
                if (rootFrame.Content == null)
                {
                    // When the navigation stack isn't restored navigate to the first page,
                    // configuring the new page by passing required information as a navigation
                    // parameter
                    rootFrame.Navigate(typeof(MainPage), e.Arguments);
                }
                // Ensure the current window is active
                Window.Current.Activate();
            }

            BeginExtendedExecution();
        }

        void ClearExtendedExecution()
        {
            if (extendedExecutionSession != null)
            {
                extendedExecutionSession.Revoked -= ExtendedExecutionSession_RevokedAsync;
                extendedExecutionSession.Dispose();
                extendedExecutionSession = null;
            }
        }

        private async void BeginExtendedExecution()
        {
            ClearExtendedExecution();

            var newSession = new ExtendedExecutionSession
            {
                Reason = ExtendedExecutionReason.Unspecified,
                Description = "Keep Shokpod running in background"
            };

            newSession.Revoked += ExtendedExecutionSession_RevokedAsync;
            ExtendedExecutionResult result = await newSession.RequestExtensionAsync();

            switch (result)
            {
                case ExtendedExecutionResult.Allowed:
                    MetroEventSource.ToastAsync("Request for background execution granted!");
                    extendedExecutionSession = newSession;
                    break;

                default:
                case ExtendedExecutionResult.Denied:
                    MetroEventSource.ToastAsync("Request for background execution denied!");
                    newSession.Dispose();
                    break;
            }
        }

        private async void ExtendedExecutionSession_RevokedAsync(object sender, ExtendedExecutionRevokedEventArgs args)
        {
            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                switch (args.Reason)
                {
                    case ExtendedExecutionRevokedReason.Resumed:
                        MetroEventSource.ToastAsync("Extended execution revoked due to returning to foreground.");
                        BeginExtendedExecution();
                        break;

                    case ExtendedExecutionRevokedReason.SystemPolicy:
                        MetroEventSource.ToastAsync("Extended execution revoked due to system policy.");
                        ClearExtendedExecution();
                        break;
                    default:
                        ClearExtendedExecution();
                        break;
                }
            });
        }

        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            SuspendingDeferral deferral = e.SuspendingOperation.GetDeferral();
            MetroEventSource.ToastAsync("Shokpod suspended!");
            deferral.Complete();
        }

        public static void Debug(string v)
        {
            MetroEventSource.Log.Debug(v);
        }
        public static void Debug(string format, params object[] arguments)
        {
            MetroEventSource.Log.Debug(String.Format(format, arguments));
        }
        public MiningImpactSensor.SensorTag SensorTag
        {
            get { return _sensor; }
            set { _sensor = value; }
        }

        public static void SetSelectedSensorTag(MiningImpactSensor.SensorTag sensor)
        {
            ((App)Application.Current).SensorTag = sensor;
        }

        public static MiningImpactSensor.SensorTag getSelectedSensorTag()
        {
            return ((App)Application.Current).SensorTag;
        }
        public static async void SetSensorTagName(string text)
        {
            if (App.getSelectedSensorTag().AssignedToName != text)
            {
                App.getSelectedSensorTag().AssignedToName = text;
                PersistedDevices persistedDevices = await PersistedDevices.getPersistedDevices();
                persistedDevices.saveDevice(App.getSelectedSensorTag());
            }
        }
    }
}
