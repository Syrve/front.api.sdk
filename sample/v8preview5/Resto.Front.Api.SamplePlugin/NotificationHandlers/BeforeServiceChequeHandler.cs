using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting;
using Resto.Front.Api.Attributes.JetBrains;
using Resto.Front.Api.Data.Orders;
using Resto.Front.Api.Extensions;
using Resto.Front.Api.UI;

namespace Resto.Front.Api.SamplePlugin.NotificationHandlers
{
    internal class BeforeServiceChequeHandler : IDisposable
    {
        private readonly IDisposable subscription;

        public BeforeServiceChequeHandler()
        {
            subscription = PluginContext.Notifications.BeforeServiceCheque.Subscribe(x => OnBeforeServiceCheque(x.order, x.printingItems, x.os, x.vm));
        }

        private void OnBeforeServiceCheque([NotNull] IOrder order,
            IReadOnlyCollection<IOrderCookingItem> printingItems, [NotNull] IOperationService os,
            [CanBeNull] IViewManager vm)
        {
            if (order.Status != OrderStatus.New)
                return;

            //Borderline scenarios before printing a receipt:
            //Operations for changing\removing dishes for printing directly affect the contents of printed receipts.

            //Change the number of dishes
            //foreach (var printingItem in printingItems)
            //{
            //    if (printingItem.Status == OrderItemStatus.Added)
            //    {
            //        os.ChangeOrderCookingItemAmount(printingItem.Amount + 1, printingItem, order, os.GetCredentials());
            //        order = os.GetOrderById(order.Id);
            //    }
            //}

            //Deleting dishes sent for printing
            //foreach (var printingItem in printingItems)
            //{
            //    if (printingItem.Status == OrderItemStatus.Added) //primary printing
            //        os.DeleteOrderItem(order, printingItem, os.GetCredentials());
            //    if (printingItem.Status > OrderItemStatus.Added) //Reprint
            //        os.DeletePrintedOrderItems("BeforeServiceCheque",
            //            WriteoffOptions.WithoutWriteoff(PluginContext.Operations.GetActiveRemovalTypes().First(rt => rt.WriteoffType == WriteoffType.None)),
            //            order, new[] { printingItem }, os.GetCredentials());
            //    order = os.GetOrderById(order.Id);
            //}

            PluginContext.Log.Info("On before order service cheque subscription.");
            vm?.ChangeProgressBarMessage("Waiting for confirmation...");
            var message = $"Allow service cheque of the order #{order.Number}?";
            if (!(vm?.ShowYesNoPopup("Sample", message) ?? true))
            {
                PluginContext.Log.Info($"Service cheque of order '{order.Id}' will be canceled.");
                throw new OperationCanceledException();
            }
        }

        public void Dispose()
        {
            try
            {
                subscription.Dispose();
            }
            catch (RemotingException)
            {
                // nothing to do with the lost connection
            }
        }
    }
}
