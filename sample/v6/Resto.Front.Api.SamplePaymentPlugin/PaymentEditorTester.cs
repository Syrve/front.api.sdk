using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Xml;
using System.Xml.Serialization;
using Resto.Front.Api.Data.Orders;
using Resto.Front.Api.Data.Payments;
using Resto.Front.Api.Extensions;

namespace Resto.Front.Api.SamplePaymentPlugin
{
    internal sealed class PaymentEditorTester : IDisposable
    {
        private Window window;
        private ListBox buttons;

        public PaymentEditorTester()
        {
            var windowThread = new Thread(EntryPoint);
            windowThread.SetApartmentState(ApartmentState.STA);
            windowThread.Start();
        }

        private void EntryPoint()
        {
            buttons = new ListBox();
            window = new Window
            {
                Content = buttons,
                Width = 300,
                Height = 500,
                Topmost = true,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                ResizeMode = ResizeMode.CanMinimize,
                ShowInTaskbar = true,
                VerticalContentAlignment = VerticalAlignment.Center,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                WindowStyle = WindowStyle.SingleBorderWindow,
            };

            AddButton("Add cash preliminary payment item", AddCashPreliminaryPaymentItem);
            AddButton("Add card preliminary payment item", AddCardPreliminaryPaymentItem);
            AddButton("Add credit preliminary payment item", AddCreditPreliminaryPaymentItem);
            AddButton("Add cash preliminary payment item with discount", AddCashPreliminaryPaymentItemWithDiscount);
            AddButton("Delete preliminary payment item", DeletePreliminaryPaymentItem);
            AddButton("Delete preliminary payment item with discount", DeletePreliminaryPaymentItemWithDiscount);
            AddButton("Create order and process prepayments", CreateOrderAndProcessPrepayments);

            AddSeparator();

            AddButton("Add cash external not processed payment item", AddCashExternalPaymentItem);
            AddButton("Add card external not processed payment item", AddCardExternalPaymentItem);

            AddSeparator();

            AddButton("Add cash external processed payment item", AddCashExternalProcessedPaymentItem);
            AddButton("Add card external processed payment item", AddCardExternalProcessedPaymentItem);

            AddSeparator();

            AddButton("Add cash external prepay", AddCashExternalPrepay);
            AddButton("Add card external prepay", AddCardExternalPrepay);
            AddButton("Add and process external payment", AddAndProcessExternalPrepay);

            AddSeparator();

            AddButton("Add processed cash donation", AddProcessedCashDonation);
            AddButton("Add processed card donation", AddProcessedCardDonation);
            AddButton("Add processed external donation", AddProcessedExternalDonation);
            AddButton("Add not processed external donation", AddNotProcessedExternalDonation);
            AddButton("Delete donation", DeleteDonation);

            AddSeparator();

            AddButton("Pay order on cash", PayOrderOnCash);
            AddButton("Pay order with existing payments", PayOrder);

            AddSeparator();

            AddButton("Open cafe session", OpenCafeSession);
            AddButton("Close cafe session", CloseCafeSession);

            AddSeparator();

            AddButton("Add and process preliminary payment", AddAndProcessPreliminaryPayment);

            window.ShowDialog();
        }

        private void AddSeparator()
        {
            buttons.Items.Add(new Separator { Margin = new Thickness(0, 4, 0, 4) });
        }

        private void AddButton(string text, Action action)
        {
            var button = new Button { Content = text, Margin = new Thickness(2) };
            button.Click += (s, e) =>
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    var message = string.Format("{4} \n Cannot {0} :-({1}Message: {2}{1}{3}", text, Environment.NewLine, ex.Message, ex.StackTrace, ex.GetType());
                    MessageBox.Show(message, "PaymentEditorTester", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };
            buttons.Items.Add(button);
        }

        /// <summary>
        /// Adding a preliminary cash payment
        /// </summary>
        private void AddCashPreliminaryPaymentItem()
        {
            var deliveryOrder = PluginContext.Operations.GetDeliveryOrders().Last(o => o.Status == OrderStatus.New);
            var paymentType = PluginContext.Operations.GetPaymentTypes().Last(x => x.Kind == PaymentTypeKind.Cash);
            PluginContext.Operations.AddPreliminaryPaymentItem(100, null, paymentType, deliveryOrder, PluginContext.Operations.GetCredentials());
        }

        /// <summary>
        /// Adding a preliminary payment with Diners payment type
        /// </summary>
        private void AddCardPreliminaryPaymentItem()
        {
            var deliveryOrder = PluginContext.Operations.GetDeliveryOrders().Last(o => o.Status == OrderStatus.New);
            var paymentType = PluginContext.Operations.GetPaymentTypes().Last(x => x.Kind == PaymentTypeKind.Card && x.Name.ToUpper() == "DINERS");
            var additionalData = new CardPaymentItemAdditionalData { CardNumber = "123456" };
            PluginContext.Operations.AddPreliminaryPaymentItem(150, additionalData, paymentType, deliveryOrder, PluginContext.Operations.GetCredentials());
        }

        /// <summary>
        /// Adding a preliminary payment for counterparty "BBB" with payment type "cashless"
        /// </summary>
        private void AddCreditPreliminaryPaymentItem()
        {
            var deliveryOrder = PluginContext.Operations.GetDeliveryOrders().Last(o => o.Status == OrderStatus.New || o.Status == OrderStatus.Bill);
            var paymentType = PluginContext.Operations.GetPaymentTypes().First(x => x.Kind == PaymentTypeKind.Credit && x.Name.ToUpper().StartsWith("CASHLESS"));
            var user = PluginContext.Operations.GetUsers().Last(u => u.Name.ToUpper().StartsWith("BBB"));
            var additionalData = new CreditPaymentItemAdditionalData { CounteragentUserId = user.Id };
            PluginContext.Operations.AddPreliminaryPaymentItem(200, additionalData, paymentType, deliveryOrder, PluginContext.Operations.GetCredentials());
        }

        /// <summary>
        /// Adding a preliminary cash payment with discount from the payment type
        /// </summary>
        private void AddCashPreliminaryPaymentItemWithDiscount()
        {
            var deliveryOrder = PluginContext.Operations.GetDeliveryOrders().Last(o => o.Status == OrderStatus.New || o.Status == OrderStatus.Bill);
            var paymentType = PluginContext.Operations.GetPaymentTypes().Last(x => x.Kind == PaymentTypeKind.Cash && x.DiscountType != null);
            var editSession = PluginContext.Operations.CreateEditSession();
            editSession.AddDiscount(paymentType.DiscountType, deliveryOrder);
            editSession.AddPreliminaryPaymentItem(100, null, paymentType, deliveryOrder);
            PluginContext.Operations.SubmitChanges(PluginContext.Operations.GetCredentials(), editSession);
        }

        /// <summary>
        /// Deleting a preliminary payment with discount from the payment type
        /// </summary>
        private void DeletePreliminaryPaymentItem()
        {
            var order = PluginContext.Operations.GetDeliveryOrders().Last(o => o.Status == OrderStatus.New || o.Status == OrderStatus.Bill);
            var paymentItem = order.Payments.Last(i => i.IsPreliminary);
            PluginContext.Operations.DeletePreliminaryPaymentItem(paymentItem, order, PluginContext.Operations.GetCredentials());
        }

        /// <summary>
        /// Deleting a preliminary payment
        /// </summary>
        private void DeletePreliminaryPaymentItemWithDiscount()
        {
            var deliveryOrder = PluginContext.Operations.GetDeliveryOrders().Last(o => o.Status == OrderStatus.New || o.Status == OrderStatus.Bill);
            var paymentItem = deliveryOrder.Payments.Last(i => i.IsPreliminary && i.Type.DiscountType != null);
            var discountItem = deliveryOrder.Discounts.Last(d => d.DiscountType.Equals(paymentItem.Type.DiscountType));
            var editSession = PluginContext.Operations.CreateEditSession();
            editSession.DeleteDiscount(discountItem, deliveryOrder);
            editSession.DeletePreliminaryPaymentItem(paymentItem, deliveryOrder);
            PluginContext.Operations.SubmitChanges(PluginContext.Operations.GetCredentials(), editSession);
        }

        /// <summary>
        /// Adding an external cash payment that has not been processed
        /// </summary>
        private void AddCashExternalPaymentItem()
        {
            var order = PluginContext.Operations.GetOrders().Last(o => o.Status == OrderStatus.New);
            var paymentType = PluginContext.Operations.GetPaymentTypes().Last(x => x.Kind == PaymentTypeKind.Cash);
            PluginContext.Operations.AddExternalPaymentItem(100, false, null, paymentType, order, PluginContext.Operations.GetCredentials());
        }

        /// <summary>
        /// Adding an external cash payment with Diners payment type that has not been processed
        /// </summary>
        private void AddCardExternalPaymentItem()
        {
            var order = PluginContext.Operations.GetOrders().Last(o => o.Status == OrderStatus.New);
            var paymentType = PluginContext.Operations.GetPaymentTypes().Last(x => x.Kind == PaymentTypeKind.Card && x.Name.ToUpper() == "DINERS");
            var additionalData = new CardPaymentItemAdditionalData { CardNumber = "123456" };
            PluginContext.Operations.AddExternalPaymentItem(150, false, additionalData, paymentType, order, PluginContext.Operations.GetCredentials());
        }

        /// <summary>
        /// Adding an external processed cash payment
        /// </summary>
        private void AddCashExternalProcessedPaymentItem()
        {
            var order = PluginContext.Operations.GetOrders().Last(o => o.Status == OrderStatus.New);
            var paymentType = PluginContext.Operations.GetPaymentTypes().Last(x => x.Kind == PaymentTypeKind.Cash);
            PluginContext.Operations.AddExternalPaymentItem(150, true, null, paymentType, order, PluginContext.Operations.GetCredentials());
        }

        /// <summary>
        /// Adding an external processed cash payment with Diners payment type
        /// </summary>
        private void AddCardExternalProcessedPaymentItem()
        {
            var order = PluginContext.Operations.GetOrders().Last(o => o.Status == OrderStatus.New);
            var paymentType = PluginContext.Operations.GetPaymentTypes().Last(x => x.Kind == PaymentTypeKind.Card && x.Name.ToUpper() == "DINERS");
            var additionalData = new CardPaymentItemAdditionalData { CardNumber = "123456" };
            PluginContext.Operations.AddExternalPaymentItem(150, true, additionalData, paymentType, order, PluginContext.Operations.GetCredentials());
        }

        #region Add and process prepay
        /// <summary>
        /// Adding an external processed cash payment and converting it into a prepayment in Syrve
        /// </summary>
        private void AddCashExternalPrepay()
        {
            var order = PluginContext.Operations.GetOrders().Last(o => o.Status == OrderStatus.New);
            var paymentType = PluginContext.Operations.GetPaymentTypes().Last(x => x.Kind == PaymentTypeKind.Cash);
            var credentials = PluginContext.Operations.GetCredentials();
            var paymentItem = PluginContext.Operations.AddExternalPaymentItem(150, true, null, paymentType, order, credentials);
            order = PluginContext.Operations.GetOrderById(order.Id);

            PluginContext.Operations.ProcessPrepay(credentials, order, paymentItem);
        }

        /// <summary>
        /// Adding an external processed cash payment with Diners payment type and converting it into a prepayment in Syrve
        /// </summary>
        private void AddCardExternalPrepay()
        {
            var order = PluginContext.Operations.GetOrders().Last(o => o.Status == OrderStatus.New);
            var paymentType = PluginContext.Operations.GetPaymentTypes().Last(x => x.Kind == PaymentTypeKind.Card && x.Name.ToUpper() == "DINERS");
            var additionalData = new CardPaymentItemAdditionalData { CardNumber = "123456" };
            var credentials = PluginContext.Operations.GetCredentials();
            var paymentItem = PluginContext.Operations.AddExternalPaymentItem(150, true, additionalData, paymentType, order, credentials);
            order = PluginContext.Operations.GetOrderById(order.Id);

            PluginContext.Operations.ProcessPrepay(credentials, order, paymentItem);
        }

        /// <summary>
        /// Adding an external payment that has not been processed and processing it.
        /// </summary>
        private void AddAndProcessExternalPrepay()
        {
            var order = PluginContext.Operations.GetOrders().Last(o => o.Status == OrderStatus.New);
            var paymentType = PluginContext.Operations.GetPaymentTypes().Last(x => x.Kind == PaymentTypeKind.External && x.Name == "SampleApiPayment");
            var additionalData = new ExternalPaymentItemAdditionalData { CustomData = Serializer.Serialize(new PaymentAdditionalData { SilentPay = true }) };
            var credentials = PluginContext.Operations.GetCredentials();
            var paymentItem = PluginContext.Operations.AddExternalPaymentItem(order.ResultSum, false, additionalData, paymentType, order, credentials);

            PluginContext.Operations.ProcessPrepay(credentials, order, paymentItem);
        }
        #endregion Add and process prepay

        #region Add and process or delete donation
        private void AddProcessedCashDonation()
        {
            const bool isProcessed = true;
            var order = PluginContext.Operations.GetOrders().Last(o => o.Status == OrderStatus.New);
            var donationType = PluginContext.Operations.GetDonationTypesCompatibleWith(order).Last(dt => dt.PaymentTypes.Any(pt => pt.Kind == PaymentTypeKind.Cash));
            var paymentType = donationType.PaymentTypes.First(x => x.Kind == PaymentTypeKind.Cash);
            var credentials = PluginContext.Operations.GetCredentials();

            var paymentItem = PluginContext.Operations.AddDonation(credentials, order, donationType, paymentType, null, isProcessed, order.ResultSum / 10);
            order = PluginContext.Operations.GetOrderById(order.Id);
            Debug.Assert(order.Donations.Contains(paymentItem));
        }

        private void AddProcessedCardDonation()
        {
            const bool isProcessed = true;
            var order = PluginContext.Operations.GetOrders().Last(o => o.Status == OrderStatus.Closed);
            var donationType = PluginContext.Operations.GetDonationTypesCompatibleWith(order).First(dt => dt.PaymentTypes.Any(pt => pt.Kind == PaymentTypeKind.Card));
            var paymentType = donationType.PaymentTypes.First(x => x.Kind == PaymentTypeKind.Card && x.Name.ToUpper() == "VISA");
            var additionalData = new CardPaymentItemAdditionalData { CardNumber = "123456" };
            var credentials = PluginContext.Operations.GetCredentials();

            var paymentItem = PluginContext.Operations.AddDonation(credentials, order, donationType, paymentType, additionalData, isProcessed, order.ResultSum / 4);
            order = PluginContext.Operations.GetOrderById(order.Id);
            Debug.Assert(order.Donations.Contains(paymentItem));
        }

        private void AddProcessedExternalDonation()
        {
            const bool isProcessed = true;
            var order = PluginContext.Operations.GetOrders().Last(o => o.Status == OrderStatus.New);
            var donationType = PluginContext.Operations.GetDonationTypesCompatibleWith(order).First(dt => dt.PaymentTypes.Any(pt => pt.Kind == PaymentTypeKind.External));
            var paymentType = donationType.PaymentTypes.First(x => x.Kind == PaymentTypeKind.External && x.Name == "SampleApiPayment");
            var credentials = PluginContext.Operations.GetCredentials();

            var paymentItem = PluginContext.Operations.AddDonation(credentials, order, donationType, paymentType, null, isProcessed, order.ResultSum / 3);
            order = PluginContext.Operations.GetOrderById(order.Id);
            Debug.Assert(order.Donations.Contains(paymentItem));
        }

        private void AddNotProcessedExternalDonation()
        {
            const bool isProcessed = false;
            var order = PluginContext.Operations.GetOrders().Last(o => o.Status == OrderStatus.New);
            var donationType = PluginContext.Operations.GetDonationTypesCompatibleWith(order).First(dt => dt.PaymentTypes.Any(pt => pt.Kind == PaymentTypeKind.External));
            var paymentType = donationType.PaymentTypes.First(x => x.Kind == PaymentTypeKind.External && x.Name == "SampleApiPayment");
            var additionalData = new ExternalPaymentItemAdditionalData { CustomData = Serializer.Serialize(new PaymentAdditionalData { SilentPay = true }) };
            var credentials = PluginContext.Operations.GetCredentials();

            var paymentItem = PluginContext.Operations.AddDonation(credentials, order, donationType, paymentType, additionalData, isProcessed, order.ResultSum / 2);
            order = PluginContext.Operations.GetOrderById(order.Id);
            Debug.Assert(order.Donations.Contains(paymentItem));
        }

        private void DeleteDonation()
        {
            var order = PluginContext.Operations.GetOrders().Last(o => o.Status == OrderStatus.New || o.Status == OrderStatus.Bill || o.Status == OrderStatus.Closed);
            var paymentItem = order.Donations.Last();
            PluginContext.Operations.DeleteDonation(PluginContext.Operations.GetCredentials(), order, paymentItem);
        }
        #endregion Add and process or delete donation

        private void PayOrderOnCash()
        {
            var order = PluginContext.Operations.GetOrders().Last(o => o.Status == OrderStatus.New || o.Status == OrderStatus.Bill);
            var paymentType = PluginContext.Operations.GetPaymentTypesToPayOutOnUser().Last(x => x.Kind == PaymentTypeKind.Cash);
            // Payment is possible by user with opened personal session.
            var credentials = PluginContext.Operations.AuthenticateByPin("777");
            PluginContext.Operations.PayOrderAndPayOutOnUser(credentials, order, paymentType, order.ResultSum);
        }

        private void PayOrder()
        {
            var order = PluginContext.Operations.GetOrders().Last(o => o.Status == OrderStatus.New || o.Status == OrderStatus.Bill);
            // Payment is possible only by user with opened personal session.
            var credentials = PluginContext.Operations.AuthenticateByPin("777");
            PluginContext.Operations.PayOrder(credentials, order);
        }

        private void AddAndProcessPreliminaryPayment()
        {
            var order = PluginContext.Operations.GetDeliveryOrders().Last(o => o.Status == OrderStatus.New || o.Status == OrderStatus.Bill);
            var paymentType = PluginContext.Operations.GetPaymentTypes().Last(i => i.Kind == PaymentTypeKind.Cash);
            var credentials = PluginContext.Operations.GetCredentials();
            //add a prepayment of 100 to the existing delivery order, the guest needs change from this amount
            var paymentItem = PluginContext.Operations.AddPreliminaryPaymentItem(100m, null, paymentType, order, credentials);

            //The courier delivered the order, it's time to pay
            PluginContext.Operations.ExecuteContinuousOperation(
                operations =>
                {
                    //Change the amount of the prepayment, here you can calculate the change (Change = Prepayment Amount - Order Amount)
                    operations.ChangePaymentItemSum(order.ResultSum, null, null, paymentItem, PluginContext.Operations.GetDeliveryOrderById(order.Id), credentials);
                    //Turn the preliminary payment into a prepayment
                    operations.ProcessPrepay(credentials, PluginContext.Operations.GetDeliveryOrderById(order.Id), paymentItem);
                });
        }

        private void CreateOrderAndProcessPrepayments()
        {
            PluginContext.Operations.ExecuteContinuousOperation( // start a long running operation using the always available global PluginContext.Operations
                operations => // inside this operation, we use the instance of operations received as input that exists only for the duration of this call
                {
                    var credentials = operations.GetCredentials();

                    // global PluginContext.Operations can also be used here if some caches and helpers methods are already written with it
                    // to perform read operations, it is not necessary to transfer all code to local operations, in this part both instances of the service work identically:
                    var paymentType = PluginContext.Operations.GetPaymentTypes().First(x => x.CanBeExternalProcessed);

                    // The difference is that, for example, an order created via PluginContext.Operations will immediately become editable by everyone
                    // - the user will be able to make changes to this order;
                    // - other plugins, if they monitor the appearance of orders, will be able to change something in it;
                    // - background built-in handlers will be able to autoprint to the kitchen, autocomplete cooking and other operations
                    // In other words, if we were going to create an order and then do something else with it, there is no guarantee that we can do it in a row,
                    // the order can be “hijacked” in time (we will get an EntityAlreadyInUseException) and
                    // make changes inconsistent with our plans (we'll get an EntityModifiedException).

                    // If possible, perform actions within the same editing session - this will be atomic, "all or nothing".
                    // However, some actions cannot be performed within the edit session (usually irreversible actions),
                    // then a chain of such actions can be performed in a row within one continuous operation (continuous operation).
                    // At the same time, all objects that we have changed through operations will be implicitly locked for all the rest until the end of this operation
                    var editSession = operations.CreateEditSession();
                    var orderStub = editSession.CreateOrder(null);
                    editSession.AddOrderGuest("Herbert", orderStub);
                    var paymentItemStub = editSession.AddExternalPaymentItem(42m, true, null, paymentType, orderStub);
                    var submittedEntities = operations.SubmitChanges(credentials, editSession);

                    // The order has already been created, everyone can see it, but only we can edit it                
                    var order = submittedEntities.Get(orderStub);
                    var paymentItem = submittedEntities.Get(paymentItemStub);

                    // It's important not to stay in this place too long to allow others to make changes too
                    // It is desirable to obtain and prepare all the necessary data in advance (especially if this requires network requests to external services)
                    operations.ProcessPrepay(credentials, order, paymentItem);
                });
        }

        private void OpenCafeSession()
        {
            var credentials = PluginContext.Operations.GetCredentials();
            PluginContext.Operations.OpenCafeSession(credentials);
        }

        private void CloseCafeSession()
        {
            var credentials = PluginContext.Operations.GetCredentials();
            PluginContext.Operations.CloseCafeSession(credentials);
        }

        public void Dispose()
        {
            window.Dispatcher.InvokeShutdown();
            window.Dispatcher.Thread.Join();
        }
    }

    [Serializable]
    public sealed class PaymentAdditionalData
    {
        public bool SilentPay { get; set; }
    }

    internal static class Serializer
    {
        internal static string Serialize<T>(T data) where T : class
        {
            using (var sw = new StringWriter())
            using (var writer = XmlWriter.Create(sw))
            {
                new XmlSerializer(typeof(T)).Serialize(writer, data);
                return sw.ToString();
            }
        }
    }
}
