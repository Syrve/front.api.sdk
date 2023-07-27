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
                Width = 310,
                Height = 870,
                Topmost = true,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                ResizeMode = ResizeMode.CanResize,
                ShowInTaskbar = true,
                VerticalContentAlignment = VerticalAlignment.Center,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                WindowStyle = WindowStyle.SingleBorderWindow,
            };

            AddButton("Add cash payment", AddCashPayment);
            AddButton("Add card payment", AddCardPayment);

            AddSeparator();

            AddButton("Add cash preliminary payment", AddCashPreliminaryPayment);
            AddButton("Add card preliminary payment", AddCardPreliminaryPayment);
            AddButton("Add credit preliminary payment", AddCreditPreliminaryPayment);
            AddButton("Delete preliminary payment", DeletePreliminaryPayment);
            AddButton("Add cash preliminary payment with discount", AddCashPreliminaryPaymentWithDiscount);
            AddButton("Delete preliminary payment with discount", DeletePreliminaryPaymentWithDiscount);

            AddSeparator();

            AddButton("Add cash external not processed payment", AddCashExternalNotProcessedPayment);
            AddButton("Add card external not processed payment", AddCardExternalNotProcessedPayment);
            AddButton("Add plugin external not processed payment", AddPluginExternalNotProcessedPayment);
            AddButton("Add cash external processed payment", AddCashExternalProcessedPayment);
            AddButton("Add card external processed payment", AddCardExternalProcessedPayment);

            AddSeparator();

            AddButton("Add cash external processed prepay", AddCashExternalProcessedPrepay);
            AddButton("Add card external processed prepay", AddCardExternalProcessedPrepay);
            AddButton("Add plugin external not processed prepay", AddPluginExternalNotProcessedPrepay);

            AddSeparator();

            AddButton("Add cash processed donation", AddCashProcessedDonation);
            AddButton("Add card processed donation", AddCardProcessedDonation);
            AddButton("Add plugin processed donation", AddPluginProcessedDonation);
            AddButton("Add plugin not processed donation", AddPluginNotProcessedDonation);
            AddButton("Delete donation", DeleteDonation);

            AddSeparator();

            AddButton("Add external fiscalized payment", AddExternalFiscalizedPayment);
            AddButton("Add external fiscalized prepay", AddExternalFiscalizedPrepay);
            AddButton("Delete external fiscalized payment", DeleteExternalFiscalizedPayment);

            AddSeparator();

            AddButton("Pay order on cash", PayOrderOnCashAndPayOutOnUser);
            AddButton("Pay order on card", PayOrderOnCardAndPayOutOnUser);
            AddButton("Pay order with existing payments", PayOrderWithExistingPayments);

            AddSeparator();

            AddButton("Open cafe session", OpenCafeSession);
            AddButton("Close cafe session", CloseCafeSession);

            AddSeparator();

            AddButton("Create order and process prepayments", CreateOrderAndProcessPrepayments);
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
                    var message = $"{ex.GetType()}{Environment.NewLine}Cannot {text} :-({Environment.NewLine}Message: {ex.Message}{Environment.NewLine}{ex.StackTrace}";
                    MessageBox.Show(message, GetType().Name, MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };
            buttons.Items.Add(button);
        }

        #region Add payment

        /// <summary>
        /// Adding a regular cash payment
        /// </summary>
        private void AddCashPayment()
        {
            var order = PluginContext.Operations.GetOrders().Last(o => o.Status == OrderStatus.New);
            var paymentType = PluginContext.Operations.GetPaymentTypes().First(x => x.Kind == PaymentTypeKind.Cash);
            PluginContext.Operations.AddPaymentItem(50, null, paymentType, order, PluginContext.Operations.GetCredentials());
        }

        /// <summary>
        /// Adding a regular card payment with VISA payment type
        /// </summary>
        private void AddCardPayment()
        {
            var order = PluginContext.Operations.GetOrders().Last(o => o.Status == OrderStatus.New);
            var paymentType = PluginContext.Operations.GetPaymentTypes().First(x => x.Kind == PaymentTypeKind.Card && x.Name.ToUpper() == "VISA");
            var additionalData = new CardPaymentItemAdditionalData { CardNumber = "123456" };
            PluginContext.Operations.AddPaymentItem(50, additionalData, paymentType, order, PluginContext.Operations.GetCredentials());
        }

        #endregion Add payment

        #region Add or delete preliminary payment

        /// <summary>
        /// Adding a preliminary cash payment
        /// </summary>
        private void AddCashPreliminaryPayment()
        {
            var deliveryOrder = PluginContext.Operations.GetDeliveryOrders().Last(o => o.Status == OrderStatus.New);
            var paymentType = PluginContext.Operations.GetPaymentTypes().First(x => x.Kind == PaymentTypeKind.Cash);
            PluginContext.Operations.AddPreliminaryPaymentItem(100, null, paymentType, deliveryOrder, PluginContext.Operations.GetCredentials());
        }

        /// <summary>
        /// Adding a preliminary payment with VISA payment type
        /// </summary>
        private void AddCardPreliminaryPayment()
        {
            var deliveryOrder = PluginContext.Operations.GetDeliveryOrders().Last(o => o.Status == OrderStatus.New);
            var paymentType = PluginContext.Operations.GetPaymentTypes().First(x => x.Kind == PaymentTypeKind.Card && x.Name.ToUpper() == "VISA");
            var additionalData = new CardPaymentItemAdditionalData { CardNumber = "123456" };
            PluginContext.Operations.AddPreliminaryPaymentItem(150, additionalData, paymentType, deliveryOrder, PluginContext.Operations.GetCredentials());
        }

        /// <summary>
        /// Adding a preliminary payment for counterparty "BBB" with payment type "cashless"
        /// </summary>
        private void AddCreditPreliminaryPayment()
        {
            var deliveryOrder = PluginContext.Operations.GetDeliveryOrders().Last(o => o.Status == OrderStatus.New || o.Status == OrderStatus.Bill);
            var paymentType = PluginContext.Operations.GetPaymentTypes().First(x => x.Kind == PaymentTypeKind.Credit && x.Name.ToUpper().StartsWith("CASHLESS"));
            var user = PluginContext.Operations.GetUsers().Last(u => u.Name.ToUpper().StartsWith("BBB"));
            var additionalData = new CreditPaymentItemAdditionalData { CounteragentUserId = user.Id };
            PluginContext.Operations.AddPreliminaryPaymentItem(200, additionalData, paymentType, deliveryOrder, PluginContext.Operations.GetCredentials());
        }

        /// <summary>
        /// Deleting a preliminary payment
        /// </summary>
        private void DeletePreliminaryPayment()
        {
            var deliveryOrder = PluginContext.Operations.GetDeliveryOrders().Last(o => o.Status == OrderStatus.New || o.Status == OrderStatus.Bill);
            var paymentItem = deliveryOrder.Payments.First(i => i.IsPreliminary);
            PluginContext.Operations.DeletePreliminaryPaymentItem(paymentItem, deliveryOrder, PluginContext.Operations.GetCredentials());
        }

        /// <summary>
        /// Adding a preliminary cash payment with discount from the payment type
        /// </summary>
        private void AddCashPreliminaryPaymentWithDiscount()
        {
            var deliveryOrder = PluginContext.Operations.GetDeliveryOrders().Last(o => o.Status == OrderStatus.New || o.Status == OrderStatus.Bill);
            var paymentType = PluginContext.Operations.GetPaymentTypes().First(x => x.Kind == PaymentTypeKind.Cash && x.DiscountType != null);
            var editSession = PluginContext.Operations.CreateEditSession();
            editSession.AddDiscount(paymentType.DiscountType, deliveryOrder);
            editSession.AddPreliminaryPaymentItem(100, null, paymentType, deliveryOrder);
            PluginContext.Operations.SubmitChanges(PluginContext.Operations.GetCredentials(), editSession);
        }

        /// <summary>
        /// Deleting a preliminary payment with discount from the payment type
        /// </summary>
        private void DeletePreliminaryPaymentWithDiscount()
        {
            var deliveryOrder = PluginContext.Operations.GetDeliveryOrders().Last(o => o.Status == OrderStatus.New || o.Status == OrderStatus.Bill);
            var paymentItem = deliveryOrder.Payments.First(i => i.IsPreliminary && i.Type.DiscountType != null);
            var discountItem = deliveryOrder.Discounts.Last(d => d.DiscountType.Equals(paymentItem.Type.DiscountType));
            var editSession = PluginContext.Operations.CreateEditSession();
            editSession.DeleteDiscount(discountItem, deliveryOrder);
            editSession.DeletePreliminaryPaymentItem(paymentItem, deliveryOrder);
            PluginContext.Operations.SubmitChanges(PluginContext.Operations.GetCredentials(), editSession);
        }

        #endregion Add or delete preliminary payment

        #region Add or delete external payment

        /// <summary>
        /// Adding an external cash payment that has not been processed
        /// </summary>
        private void AddCashExternalNotProcessedPayment()
        {
            const bool isProcessed = false;
            var order = PluginContext.Operations.GetOrders().Last(o => o.Status == OrderStatus.New);
            var paymentType = PluginContext.Operations.GetPaymentTypes().First(x => x.Kind == PaymentTypeKind.Cash);
            var credentials = PluginContext.Operations.GetCredentials();
            PluginContext.Operations.AddExternalPaymentItem(order.ResultSum / 2, isProcessed, null, null, paymentType, order, credentials);
        }

        /// <summary>
        /// Adding an external card payment with VISA payment type that has not been processed
        /// </summary>
        private void AddCardExternalNotProcessedPayment()
        {
            const bool isProcessed = false;
            var order = PluginContext.Operations.GetOrders().Last(o => o.Status == OrderStatus.New);
            var paymentType = PluginContext.Operations.GetPaymentTypes().First(x => x.Kind == PaymentTypeKind.Card && x.Name.ToUpper() == "VISA");
            var additionalData = new CardPaymentItemAdditionalData { CardNumber = "123456" };
            var credentials = PluginContext.Operations.GetCredentials();
            PluginContext.Operations.AddExternalPaymentItem(order.ResultSum / 2, isProcessed, additionalData, null, paymentType, order, credentials);
        }

        /// <summary>
        /// Adding an external payment that has not been processed
        /// </summary>
        private void AddPluginExternalNotProcessedPayment()
        {
            const bool isProcessed = false;
            var order = PluginContext.Operations.GetOrders().Last(o => o.Status == OrderStatus.New);
            var paymentType = PluginContext.Operations.GetPaymentTypes().First(x => x.Kind == PaymentTypeKind.External && x.Name == "SampleApiPayment");
            var additionalData = new ExternalPaymentItemAdditionalData { CustomData = Serializer.Serialize(new PaymentAdditionalData { SilentPay = true }) };
            var credentials = PluginContext.Operations.GetCredentials();
            PluginContext.Operations.AddExternalPaymentItem(order.ResultSum / 2, isProcessed, additionalData, null, paymentType, order, credentials);
        }

        /// <summary>
        /// Adding an external processed cash payment
        /// </summary>
        private void AddCashExternalProcessedPayment()
        {
            const bool isProcessed = true;
            var order = PluginContext.Operations.GetOrders().Last(o => o.Status == OrderStatus.New);
            var paymentType = PluginContext.Operations.GetPaymentTypes().First(x => x.Kind == PaymentTypeKind.Cash);
            var credentials = PluginContext.Operations.GetCredentials();
            PluginContext.Operations.AddExternalPaymentItem(order.ResultSum / 2, isProcessed, null, null, paymentType, order, credentials);
        }

        /// <summary>
        /// Adding an external processed cash payment with VISA payment type
        /// </summary>
        private void AddCardExternalProcessedPayment()
        {
            const bool isProcessed = true;
            var order = PluginContext.Operations.GetOrders().Last(o => o.Status == OrderStatus.New);
            var paymentType = PluginContext.Operations.GetPaymentTypes().First(x => x.Kind == PaymentTypeKind.Card && x.Name.ToUpper() == "VISA");
            var additionalData = new CardPaymentItemAdditionalData { CardNumber = "123456" };
            var credentials = PluginContext.Operations.GetCredentials();
            PluginContext.Operations.AddExternalPaymentItem(order.ResultSum / 2, isProcessed, additionalData, null, paymentType, order, credentials);
        }

        #endregion Add or delete external payment

        #region Add and process prepay

        /// <summary>
        /// Adding an external processed cash payment and converting it into a prepayment in Syrve
        /// </summary>
        private void AddCashExternalProcessedPrepay()
        {
            const bool isProcessed = true;
            var order = PluginContext.Operations.GetOrders().Last(o => o.Status == OrderStatus.New);
            var paymentType = PluginContext.Operations.GetPaymentTypes().First(x => x.Kind == PaymentTypeKind.Cash);
            var credentials = PluginContext.Operations.GetCredentials();
            var paymentItem = PluginContext.Operations.AddExternalPaymentItem(150, isProcessed, null, null, paymentType, order, credentials);
            order = PluginContext.Operations.GetOrderById(order.Id);

            PluginContext.Operations.ProcessPrepay(credentials, order, paymentItem);
        }

        /// <summary>
        /// Adding an external processed cash payment with VISA payment type and converting it into a prepayment in Syrve
        /// </summary>
        private void AddCardExternalProcessedPrepay()
        {
            const bool isProcessed = true;
            var order = PluginContext.Operations.GetOrders().Last(o => o.Status == OrderStatus.New);
            var paymentType = PluginContext.Operations.GetPaymentTypes().First(x => x.Kind == PaymentTypeKind.Card && x.Name.ToUpper() == "VISA");
            var additionalData = new CardPaymentItemAdditionalData { CardNumber = "123456" };
            var credentials = PluginContext.Operations.GetCredentials();
            var paymentItem = PluginContext.Operations.AddExternalPaymentItem(150, isProcessed, additionalData, null, paymentType, order, credentials);
            order = PluginContext.Operations.GetOrderById(order.Id);

            PluginContext.Operations.ProcessPrepay(credentials, order, paymentItem);
        }

        /// <summary>
        /// Adding an external payment that has not been processed and processing it.
        /// </summary>
        private void AddPluginExternalNotProcessedPrepay()
        {
            const bool isProcessed = false;
            var order = PluginContext.Operations.GetOrders().Last(o => o.Status == OrderStatus.New);
            var paymentType = PluginContext.Operations.GetPaymentTypes().First(x => x.Kind == PaymentTypeKind.External && x.Name == "SampleApiPayment");
            var additionalData = new ExternalPaymentItemAdditionalData { CustomData = Serializer.Serialize(new PaymentAdditionalData { SilentPay = true }) };
            var credentials = PluginContext.Operations.GetCredentials();
            var paymentItem = PluginContext.Operations.AddExternalPaymentItem(150, isProcessed, additionalData, null, paymentType, order, credentials);

            PluginContext.Operations.ProcessPrepay(credentials, order, paymentItem);
        }

        #endregion Add and process prepay

        #region Add and process or delete donation

        private void AddCashProcessedDonation()
        {
            const bool isProcessed = true;
            var order = PluginContext.Operations.GetOrders().Last(o => o.Status == OrderStatus.New);
            var donationType = PluginContext.Operations.GetDonationTypesCompatibleWith(order).First(dt => dt.PaymentTypes.Any(pt => pt.Kind == PaymentTypeKind.Cash));
            var paymentType = donationType.PaymentTypes.First(x => x.Kind == PaymentTypeKind.Cash);
            var credentials = PluginContext.Operations.GetCredentials();

            var paymentItem = PluginContext.Operations.AddDonation(credentials, order, donationType, paymentType, null, isProcessed, order.ResultSum / 10);
            order = PluginContext.Operations.GetOrderById(order.Id);
            Debug.Assert(order.Donations.Contains(paymentItem));
        }

        private void AddCardProcessedDonation()
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

        private void AddPluginProcessedDonation()
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

        private void AddPluginNotProcessedDonation()
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

        #region Add and process or delete external fiscalized payment

        /// <summary>
        /// Add external fiscalized card payment with VISA payment type
        /// </summary>
        private void AddExternalFiscalizedPayment()
        {
            var order = PluginContext.Operations.GetOrders().Last(o => o.Status == OrderStatus.New);
            var paymentType = PluginContext.Operations.GetPaymentTypes().First(x => x.Kind == PaymentTypeKind.Card && x.Name.ToUpper() == "VISA");
            var additionalData = new CardPaymentItemAdditionalData { CardNumber = "123456" };
            var credentials = PluginContext.Operations.GetCredentials();

            PluginContext.Operations.AddExternalFiscalizedPaymentItem(50, additionalData, paymentType, order, credentials);
        }

        /// <summary>
        /// Adding an external fiscalized card payment and converting it into a prepayment in Syrve.
        /// </summary>
        private void AddExternalFiscalizedPrepay()
        {
            var order = PluginContext.Operations.GetOrders().Last(o => o.Status == OrderStatus.New);
            var paymentType = PluginContext.Operations.GetPaymentTypes().First(x => x.Kind == PaymentTypeKind.Card && x.Name.ToUpper() == "VISA");
            var additionalData = new CardPaymentItemAdditionalData { CardNumber = "123456" };
            var credentials = PluginContext.Operations.GetCredentials();
            var paymentItem = PluginContext.Operations.AddExternalFiscalizedPaymentItem(50, additionalData, paymentType, order, credentials);

            order = PluginContext.Operations.GetOrderById(order.Id);
            PluginContext.Operations.ProcessPrepay(credentials, order, paymentItem);
        }

        /// <summary>
        /// Deleting an external fiscalized payment.
        /// </summary>
        private void DeleteExternalFiscalizedPayment()
        {
            var order = PluginContext.Operations.GetOrders().Last(o => o.Status == OrderStatus.New);
            var paymentItem = order.Payments.Last(i => i.IsFiscalizedExternally);

            PluginContext.Operations.DeleteExternalFiscalizedPaymentItem(paymentItem, order, PluginContext.Operations.GetCredentials());
        }

        #endregion Add and process or delete external fiscalized payment

        #region Pay order

        /// <summary>
        /// Remote payment in cash. Cash is treated as an employee's debt.
        /// </summary>
        private void PayOrderOnCashAndPayOutOnUser()
        {
            var order = PluginContext.Operations.GetOrders().Last(o => o.Status == OrderStatus.New || o.Status == OrderStatus.Bill);
            var paymentType = PluginContext.Operations.GetPaymentTypesToPayOutOnUser().First(x => x.Kind == PaymentTypeKind.Cash);
            // Payment is possible by user with opened personal session.
            var credentials = PluginContext.Operations.AuthenticateByPin("777");
            PluginContext.Operations.PayOrderAndPayOutOnUser(credentials, order, paymentType, order.ResultSum / 2);
        }

        /// <summary>
        /// Remote order payment by card.
        /// </summary>
        private void PayOrderOnCardAndPayOutOnUser()
        {
            var order = PluginContext.Operations.GetOrders().Last(o => o.Status == OrderStatus.New || o.Status == OrderStatus.Bill);
            var paymentType = PluginContext.Operations.GetPaymentTypesToPayOutOnUser().First(x => x.Kind == PaymentTypeKind.Card && x.Name.ToUpper() == "VISA");
            // Payment is possible by user with opened personal session.
            var credentials = PluginContext.Operations.AuthenticateByPin("777");
            PluginContext.Operations.PayOrderAndPayOutOnUser(credentials, order, paymentType, order.ResultSum / 2);
        }

        /// <summary>
        /// Remote order payment by existing payments in the order.
        /// </summary>
        private void PayOrderWithExistingPayments()
        {
            var order = PluginContext.Operations.GetOrders().Last(o => o.Status == OrderStatus.New || o.Status == OrderStatus.Bill);
            // Payment is possible only by user with opened personal session.
            var credentials = PluginContext.Operations.AuthenticateByPin("777");
            PluginContext.Operations.PayOrder(credentials, order);
        }

        #endregion Pay order

        private void AddAndProcessPreliminaryPayment()
        {
            var order = PluginContext.Operations.GetDeliveryOrders().Last(o => o.Status == OrderStatus.New || o.Status == OrderStatus.Bill);
            var paymentType = PluginContext.Operations.GetPaymentTypes().First(i => i.Kind == PaymentTypeKind.Cash);
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
                    var paymentItemStub = editSession.AddExternalPaymentItem(42m, true, null, null, paymentType, orderStub);
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
