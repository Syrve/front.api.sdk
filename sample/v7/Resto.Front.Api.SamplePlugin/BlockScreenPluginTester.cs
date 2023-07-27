using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Text;
using System.Threading.Tasks;
using Resto.Front.Api.Exceptions;
using Resto.Front.Api.UI;

namespace Resto.Front.Api.SamplePlugin
{
    using static PluginContext;

    /// <summary>
    /// <para>This is a wrapper for the operations of the <b>BlockScreenPlugin</b> plugin. Operations used:</para>
    /// <para><b>Initialize</b> - preliminary initialization of the blocking window. Speeds up the first display of the window. Calling this operation is optional. Can only be called for the current terminal.</para>
    /// <para><b>BlockTerminal</b> - show the blocking window with the given text. Can only be called for the current terminal.</para>
    /// <para><b>UnblockTerminal</b> - hide the blocking window. Can only be called for the current terminal.</para>
    /// <para><b>GetTerminalIsBlocked</b> - checking the blocking state. Can be called for any terminal.</para>
    /// </summary>
    /// <remarks>
    /// Requires <b>Resto.Front.Api.BlockScreenPlugin</b> plugin to work
    /// </remarks>
    // ReSharper disable once UnusedMember.Global
    internal sealed class BlockScreenPluginTester : IDisposable
    {
        private const int ServiceModuleId = 21041518;
        private const string ServiceName = "BlockScreenPlugin";
        private const string Initialize = "Initialize";
        private const string GetTerminalIsBlocked = "GetTerminalIsBlocked";
        private const string BlockTerminal = "BlockTerminal";
        private const string UnblockTerminal = "UnblockTerminal";

        private readonly CompositeDisposable subscriptions;

        public BlockScreenPluginTester()
        {
            subscriptions = new CompositeDisposable
            {
                Operations.AddButtonToPluginsMenu("BlockScreen: Initialize", x => DoIfPluginOperationAvailiable(x.vm, InitializeBlockWindow)),
                Operations.AddButtonToPluginsMenu("BlockScreen: Show for 10 seconds", x => DoIfPluginOperationAvailiable(x.vm, ShowAndHideBlockWindow)),
                Operations.AddButtonToPluginsMenu("BlockScreen: Check state", x => DoIfPluginOperationAvailiable(x.vm, () => CheckBlockWindowState(x.vm)))
            };
        }

        /// <summary>
        /// The method checks if the plugin is available. If the plugin is not available, an error window is shown. If the plugin is available, then "action" is called.
        /// </summary>
        private static void DoIfPluginOperationAvailiable(IViewManager viewManager, Action action)
        {
            if (!Operations.GetExternalOperations().Contains((ServiceModuleId, ServiceName, Initialize)))
            {
                viewManager.ShowErrorPopup($"External operation not found. The plugin \"{ServiceName}\" may not been installed.");
                return;
            }

            action();
        }

        /// <summary>
        /// You can call the Initialize operation to speed up the first display of the blocking window.
        /// </summary>
        private static void InitializeBlockWindow() => Operations.CallExternalOperation<object, bool>(ServiceModuleId, ServiceName, Initialize, Array.Empty<byte>());

        /// <summary>
        /// Show a blocking window with the text "Loading" for 10 seconds.
        /// </summary>
        private static void ShowAndHideBlockWindow()
        {
            Operations.CallExternalOperation<string, bool>(ServiceModuleId, ServiceName, BlockTerminal, "Loading");

            Task.Delay(TimeSpan.FromSeconds(10)).GetAwaiter().GetResult(); //Show a window for 10 seconds.

            Operations.CallExternalOperation<object, bool>(ServiceModuleId, ServiceName, UnblockTerminal, Array.Empty<byte>());
        }

        /// <summary>
        /// Check the lock status of all terminals.
        /// </summary>
        private static void CheckBlockWindowState(IViewManager viewManager)
        {
            var sb = new StringBuilder();
            foreach (var terminal in Operations.GetTerminals())
            {
                try
                {
                    var isBlocked = Operations.CallExternalOperation<byte[], bool>(ServiceModuleId, ServiceName, GetTerminalIsBlocked, Array.Empty<byte>(), terminal);
                    sb.AppendLine($"Terminal's \"{terminal.Name}\" blocking state: {isBlocked}");
                }
                catch (ExternalOperationCallingException e)
                {
                    sb.AppendLine($"Failed to get block state for terminal \"{terminal.Name}\". Error message: {e.Message}");
                }
            }

            viewManager.ShowOkPopup("Terminals' blocking state", sb.ToString());
        }

        public void Dispose() => subscriptions.Dispose();
    }
}
