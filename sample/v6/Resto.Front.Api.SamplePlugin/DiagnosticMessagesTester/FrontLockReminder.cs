using System;
using System.Reactive.Linq;
using Resto.Front.Api.Data.Screens;


namespace Resto.Front.Api.SamplePlugin.DiagnosticMessagesTester
{
    internal sealed class FrontLockReminder : IDisposable
    {
        private readonly IDisposable subscription;

        // Reminder to lock the screen every time you go to the order screen
        public FrontLockReminder()
        {
            const string message = "Don't forget to lock the screen!";

            subscription = PluginContext.Notifications.ScreenChanged
                .Where(screen => screen is IOrderEditScreen)
                .Subscribe(_ => PluginContext.Operations.AddNotificationMessage(message, "SamplePlugin", TimeSpan.FromSeconds(15)));
        }

        public void Dispose()
        {
            subscription.Dispose();
        }
    }
}
