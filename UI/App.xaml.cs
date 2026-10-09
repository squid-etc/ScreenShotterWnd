using System.Configuration;
using System.Data;
using System.Windows;
using Application = System.Windows.Application;

namespace UI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private const string MutexName = "ScreenshotterWnd_TheOnlyOneInstance_Mutex";
        private const string EventName = "ScreenshotterWnd_SendSignalAboutCloseSecondaryInstance_Event";

        private Mutex? _mutexSingleInstance;
        private EventWaitHandle? _eventShowFirstInstance;

        private bool _isMutexOwner;

        protected override void OnStartup(StartupEventArgs e)
        {
            bool bCurrentInstanceIsFirst;
            _mutexSingleInstance = new Mutex(true, MutexName, out bCurrentInstanceIsFirst);
            _isMutexOwner = bCurrentInstanceIsFirst;

            if (!bCurrentInstanceIsFirst)
            {
                // another the first instance is working already
                // - send him a signal and closing this instance
                EventWaitHandle signalToExisting = EventWaitHandle.OpenExisting(EventName);
                signalToExisting.Set();
                Shutdown();
                return;
            }

            // this is the first and the only legal instance - listening starts fromn other instances
            _eventShowFirstInstance = new EventWaitHandle(false, EventResetMode.AutoReset, EventName);
            RegisterWaitHandle(_eventShowFirstInstance);

            base.OnStartup(e);
        }

        private void RegisterWaitHandle(EventWaitHandle showEvent)
        {
            WaitOrTimerCallback callback = OnShowSignalReceived;

            ThreadPool.RegisterWaitForSingleObject(
                showEvent,
                callback,
                null,
                Timeout.Infinite,
                false);
        }

        private void OnShowSignalReceived(object? state, bool timedOut)
        {
            Current.Dispatcher.Invoke(ShowMainWindow);
        }

        private void ShowMainWindow()
        {
            MainWindow mainWindow = (MainWindow)Current.MainWindow;
            mainWindow.Show();
            mainWindow.WindowState = WindowState.Normal;
            mainWindow.Activate();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            if (_isMutexOwner)
            {
                _mutexSingleInstance?.ReleaseMutex();
            }
            _mutexSingleInstance?.Dispose();
            _eventShowFirstInstance?.Dispose();
            base.OnExit(e);
        }
    }
}
