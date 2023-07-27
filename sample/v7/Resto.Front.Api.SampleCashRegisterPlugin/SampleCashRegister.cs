using System;
using System.Collections.Generic;
using System.Linq;
using Resto.Front.Api.Attributes.JetBrains;
using Resto.Front.Api.Data.Device;
using Resto.Front.Api.Data.Device.Results;
using Resto.Front.Api.Data.Device.Settings;
using Resto.Front.Api.Data.Device.Tasks;
using Resto.Front.Api.Data.Security;
using Resto.Front.Api.Exceptions;
using Resto.Front.Api.UI;

namespace Resto.Front.Api.SampleCashRegisterPlugin
{
    internal sealed class SampleCashRegister : Devices.ICashRegister
    {
        private readonly Guid deviceId;
        [NotNull]
        private CashRegisterSettings cashRegisterSettings;
        private const string DeviceFriendlyName = "Fiscal registrar example";
        private State state = State.Stopped;

        public SampleCashRegister(Guid deviceId, CashRegisterSettings cashRegisterSettings)
        {
            this.deviceId = deviceId;
            this.cashRegisterSettings = cashRegisterSettings;
        }

        public Guid DeviceId => deviceId;

        public string DeviceName => DeviceFriendlyName;

        /// <summary>
        /// Check that the device is running.
        /// Exception <see cref="DeviceNotStartedException"/> is necessary 
        /// so that when the "Autostart" setting is enabled, an attempt 
        /// to start the device is automatically performed.
        /// </summary>
        private void CheckStarted()
        {
            if (state != State.Running)
                throw new DeviceNotStartedException("Device not started");
        }

        /// <summary>
        /// Get the current state of the FR
        /// </summary>
        /// <returns><see cref="DeviceInfo"/> 
        /// <see cref="DeviceInfo.State"/> Device status text />
        /// <see cref="DeviceInfo.Comment"/> Device status description<para />
        /// </returns>
        public DeviceInfo GetDeviceInfo()
        {
            return new DeviceInfo
            {
                State = state,
                Comment = "Running",
                Settings = cashRegisterSettings
            };
        }

        /// <summary>
        /// The command performs driver initialization, port opening, device connection and connection testing
        /// </summary>
        public void Start()
        {
            PluginContext.Log.InfoFormat("Device: '{0} ({1})' was started", DeviceName, deviceId);

            state = State.Running;
        }

        /// <summary>
        /// The command stops the device, releases resources, closes ports
        /// </summary>
        public void Stop()
        {
            PluginContext.Log.InfoFormat("Device: '{0} ({1})' was stopped", DeviceName, deviceId);

            state = State.Stopped;
        }

        /// <summary>
        /// The command to change device settings.
        /// </summary>
        public void Setup([NotNull] DeviceSettings newSettings)
        {
            if (newSettings == null)
                throw new ArgumentNullException(nameof(newSettings));

            cashRegisterSettings = (CashRegisterSettings)newSettings;

            PluginContext.Log.InfoFormat("Device: '{0} ({1})' was setup", DeviceName, deviceId);
        }

        public void RemoveDevice()
        {
            PluginContext.Log.InfoFormat("Device: '{0} ({1})' was removed", DeviceName, deviceId);
        }

        /// <summary>
        /// The receipt printing operation includes opening a fiscal receipt,
        /// Adding settlement items, additional details, rounding off a change
        /// or discount, closing a receipt.
        /// For a refund check, the field <paramref name="chequeTask.IsRefund"/> is set to true
        /// </summary>
        /// <see cref="ChequeTask" /> object containing the following data: <para />
        /// <see cref="ChequeTask.TextAfterCheque"/> The text to be printed after the receipt body. Can be empty<para />
        /// <see cref="ChequeTask.TableNumber"/> Order table number<para />
        /// <see cref="ChequeTask.TextBeforeCheque"/> The text to be printed before the receipt body. Can be empty<para />
        /// <see cref="ChequeTask.Sales"/> List of calculation items<para />
        /// <see cref="ChequeTask.RoundSum"/> Rounding amount, discount contained in field <see cref="ChequeTask.DiscountSum"/>, <para />
        /// <see cref="ChequeTask.OfdEmail"/> The buyer's email to which the electronic receipt will be sent. Has a higher priority than<see cref="ChequeTask.OfdPhoneNumber"/> ,<para />
        /// <see cref="ChequeTask.OfdPhoneNumber"/> Customer's phone number to which the electronic receipt will be sent<para />
        /// <see cref="ChequeTask.CashPayment"/> Cash payment amount<para />
        /// <see cref="ChequeTask.CardPayments"/> The list of payments, except cash. The list of payment types must be synchronized with the payment identifiers on the FR through the settings in BackOffice<para />
        /// <returns>Returns object <see cref="CashRegisterResult"/>
        /// Throw an exception on error.
        /// </returns>
        public CashRegisterResult DoCheque(ChequeTask chequeTask, IViewManager viewManager)
        {
            CheckStarted();

            #region Example of reading settings
            var settings = new SampleCashRegisterSettings(cashRegisterSettings);
            //Get a number
            var fontWidth = settings.NumberSettingExample.Value;
            //Get a string
            var ofdVersion = settings.StringSettingExample.Value;
            //Get a sign
            var printOrderNumber = settings.BooleanSettingExample.Value;
            //Get an enum
            var fixPrepay = settings.ListSettingExample.Values.First(value => value.IsDefault).Name;
            #endregion Example of reading settings

            PluginContext.Log.InfoFormat("Device: '{0} ({1})'. Cheque printed.", DeviceName, deviceId);

            //An example of transferring taxes actually from a plugin
            List<CashRegisterVatData> vatTotalizers = new List<CashRegisterVatData>();
            foreach (ChequeSale sale in chequeTask.Sales)
            {
                var vatItem = vatTotalizers.FirstOrDefault(item => (item.TaxId == sale.TaxId) ||
                                                                   (item.IsTaxable == sale.IsTaxable &&
                                                                    item.TaxPercent == sale.Vat.GetValueOrDefault()));
                var rate = sale.Vat.GetValueOrDefault();

                if (vatItem == null)
                {
                    vatItem = new CashRegisterVatData(sale.TaxId, sale.IsTaxable, true,
                        sale.Vat.GetValueOrDefault(), $"vat {rate}%");
                    vatTotalizers.Add(vatItem);

                }

                //Here you can read the actual amount of the tax rate from the FR
                vatItem.TaxAmount += decimal.Round(rate * sale.Sum.GetValueOrDefault() / (100 + rate), 2,
                    MidpointRounding.AwayFromZero);
                vatItem.HaveTaxAmount = true;
            }

            var result = GetCashRegisterData();
            result.ChequeVatTotalizers = vatTotalizers;
            return result;
        }

        /// <summary>
        /// Get FR parameters, these parameters must be up-to-date at the time of receipt
        /// </summary>
        /// <returns>
        /// An object <see cref="CashRegisterResult" /> that contains the following data: <para />
        /// The amount of cash in the cash register <see cref="CashRegisterResult.CashSum" /><para />
        /// Total revenue per shift <see cref="CashRegisterResult.TotalIncomeSum" /><para />
        /// Shift number <see cref="CashRegisterResult.Session" /><para />
        /// FR serial number <see cref="CashRegisterResult.SerialNumber" /><para />
        /// Document number <see cref="CashRegisterResult.DocumentNumber" /><para />
        /// Number of sales <see cref="CashRegisterResult.SaleNumber" /><para />
        /// Current date and time on FR <see cref="CashRegisterResult.RtcDateTime" /><para />
        /// Order number, not relevant for most FRs, can be passed empty <see cref="CashRegisterResult.BillNumber" /><para />
        /// Can also contain register sums for all taxes in <see cref="CashRegisterResult.MapVatResult" /><para />
        /// And tax amounts for the current check in <see cref="CashRegisterResult.MapVatResultForOrder" /><para />
        /// </returns>
        public CashRegisterResult GetCashRegisterData()
        {
            return new CashRegisterResult(null, null, null, "", null, null, null, "");
        }

        /// <summary>
        /// Call print Z-report (Closing shift)
        /// </summary>
        /// <param name="cafeSession">Cash register shift that closes</param>
        /// <param name="cashier">Cashier</param>
		/// <param name="printEklzReport"> Whether a shift billing status report should be printed, usually supported by older devices</param>
        /// <returns>Returns an object <see cref="CashRegisterResult"/>
        /// Throw an exception on error
        /// </returns>
        public CashRegisterResult DoZReport(ICafeSession cafeSession, IUser cashier, bool printEklzReport, IViewManager viewManager)
        {
            CheckStarted();

            PluginContext.Log.InfoFormat("Device: '{0} ({1})'. Z-report printed.", DeviceName, deviceId);
            return GetCashRegisterData();
        }

        /// <summary>
        /// Check device status
        /// </summary>
        /// <param name="statusFields">List of requested fields</param>
        /// <returns>Returns <see cref="CashRegisterStatus"/> instances with the requested fields filled in, or null if value is empty. </returns>
        public CashRegisterStatus GetCashRegisterStatus(IReadOnlyCollection<CashRegisterStatusField> statusFields)
        {
            return new CashRegisterStatus();
        }

        /// <summary>
        /// Call print X-report
        /// </summary>
        /// <param name="cashier">Cashier</param>
        /// <returns>Returns an object <seealso cref="CashRegisterResult"/>
        /// Throw an exception on error
        /// </returns>
        public CashRegisterResult DoXReport(IUser cashier, IViewManager viewManager)
        {
            PluginContext.Log.InfoFormat("Device: '{0} ({1})'. Z-report printed.", DeviceName, deviceId);
            return GetCashRegisterData();
        }

        /// <summary>
        /// Printing a text (non-fiscal) receipt, informational, advertising, guest bills, etc.
        /// The following tags may be present:<para />
        /// <![CDATA[<f0/>]]> - next will be font 0<para />
        /// <![CDATA[<f1/>]]> - next will be font 1<para />
        /// <![CDATA[<f2/>]]> - next will be font 2<para />
        /// <![CDATA[<papercut/>]]> - tape cut<para />
        /// <![CDATA[<bell/>]]> - bell<para />
        /// <![CDATA[<barcode data="..."/>]]> - barcode<para />
        /// <![CDATA[<logo data="..."/>]]> - logo<para />
        /// <![CDATA[<qrcode size="..." correction="...">...text...</qrcode>]]> - QR-code<para />
        /// </summary>
        /// <param name="text">Printable text, lines delimited by character LF (\\n)</param>
        public void PrintText(string text)
        {
            PluginContext.Log.InfoFormat("Device: '{0} ({1})' printed text {2}", DeviceName, deviceId, text);
        }

        /// <summary>
        /// Shift start operation
        /// </summary>
        /// <param name="cashier">Cashier</param>
        public void DoOpenSession(IUser cashier, IViewManager viewManager)
        {
            PluginContext.Log.InfoFormat("Device: '{0} ({1})'. Session opened.", DeviceName, deviceId);
        }

        /// <summary>
        /// Operation of depositing cash at the cash desk
        /// </summary>
        /// <param name="sum">Deposit amount</param>
        /// <param name="cashier">Cashier</param>
        /// <returns>Returns an object <see cref="CashRegisterResult"/>
        /// Throw an exception on error
        /// </returns>
        public CashRegisterResult DoPayIn(decimal sum, IUser cashier, IViewManager viewManager)
        {
            PluginContext.Log.InfoFormat("Device: '{0} ({1})'. PayIn.", DeviceName, deviceId);

            return GetCashRegisterData();
        }

        /// <summary>
        /// The operation of withdrawing cash from the cash register
        /// </summary>
        /// <param name="sum">Withdrawal amount</param>
        /// <param name="cashier">Cashier</param>
        /// <returns>Returns an object<see cref="CashRegisterResult"/>
        /// Throw an exception on error
        /// </returns>
        public CashRegisterResult DoPayOut(decimal sum, IUser cashier, IViewManager viewManager)
        {
            PluginContext.Log.InfoFormat("Device: '{0} ({1})'. PayOut.", DeviceName, deviceId);

            return GetCashRegisterData();
        }

        /// <summary>
        /// Open a cash drawer connected to a fiscal registrar
        /// </summary>
        /// <param name="cashier">Cashier</param>
        public void OpenDrawer(IUser cashier, IViewManager viewManager)
        {
            PluginContext.Log.InfoFormat("Device: '{0} ({1})'. Open drawer.", DeviceName, deviceId);
        }

        /// <summary>
        /// Get the state of the cash drawer connected to the fiscal registrar
        /// </summary>
        /// <returns>Is the cash drawer open</returns>
        public bool IsDrawerOpened()
        {
            return false;
        }

        /// <summary>
        /// Used to get supported additional operations specific to certain devices and regions
        /// </summary>
        /// <returns>
        /// Returns <see cref="QueryInfoResult"/> containing the list of additional commands supported by the device <see cref="SupportedCommand"/>
        /// <see cref="SupportedCommand.Name"/> Command name, for example: "TestPrinter"<para />
        /// <see cref="SupportedCommand.ResourceName"/> Command resource name, for example: "ExtFiscalCommandExtendedTest"<para />
        /// <see cref="SupportedCommand.Parameters"/> List of command parameters of type <see cref="RequiredParameter"/>, contains fields:<para />
        /// <see cref="RequiredParameter.Name"/> Parameter name, for example: "print"<para />
        /// <see cref="RequiredParameter.ResourceName"/> Parameter resource name, for example: "ExtFiscalParameterPrint"<para />
        /// <see cref="RequiredParameter.ResourceTip"/> Name of the parameter hint, for example: "ExtFiscalParameterPrintTip"<para />
        /// <see cref="RequiredParameter.Type"/> Parameter name, for example: "bool"<para />
        /// and support for printing the electronic journal <see cref="QueryInfoResult.capQueryElectronicJournalByLastSession"/>
        /// </returns>
        public QueryInfoResult GetQueryInfo()
        {
            PluginContext.Log.InfoFormat("Device: '{0} ({1})'. Get query info.", DeviceName, deviceId);

            return new QueryInfoResult(new List<SupportedCommand>(), false);
        }

        /// <summary>
        /// Call additional operation
        /// </summary>
        /// <param name="execute">
        /// Command information and parameters:<para />
        /// <see cref="CommandExecute.Name"/> Command name, for example: "TestPrinter"<para />
        /// <see cref="CommandExecute.Parameters"/> Parameter list containing the following fields:<para />
        /// <see cref="ParameterExecute.Name"/> Parameter name, for example: "print"<para />
        /// <see cref="ParameterExecute.Value"/> Parameter value, for example: "true"<para />
        /// </param>
        /// <returns>
        /// Execution result of type <see cref="Document"/>
        /// with a list of lines <see cref="Document.Lines"/> containing the result text<para />
        /// Throw an exception on error
        /// </returns>
        public DirectIoResult DirectIo(CommandExecute execute)
        {
            PluginContext.Log.InfoFormat("Device: '{0} ({1})'. DirectIo.", DeviceName, deviceId);

            return new DirectIoResult(new Document(), "123123123");
        }

        public CheckFfd12MarkingResult CheckFfd12Marking([NotNull] CheckFfd12MarkingTask task)
        {
            PluginContext.Log.WarnFormat("Device: '{0} ({1})'. CheckFfd12Marking not supported.", DeviceName, deviceId);
            throw new NotImplementedException();
        }

        /// <summary>
        /// Used to generate an invoice
        /// </summary>
        /// <param name="billTask"> Similar to the <see cref="ChequeTask"/> parameter in the receipt printing operation <see cref="DoCheque"/>,
        /// but without payments</param>
        /// <returns>Returns an object <see cref="CashRegisterResult"/>
        /// Throw an exception on error
        /// </returns>
        public CashRegisterResult DoBillCheque(BillTask billTask, IViewManager viewManager)
        {
            PluginContext.Log.InfoFormat("Device: '{0} ({1})'. BillCheque.", DeviceName, deviceId);

            return GetCashRegisterData();
        }

        public void DoCorrection(CorrectionTask task, IViewManager viewManager)
        {
            PluginContext.Log.InfoFormat("Device: '{0} ({1})'. Correction.", DeviceName, deviceId);
        }

        /// <summary>
        /// Puts the customer's display in standby mode, if present
        /// </summary>
        /// <param name="timeToIdle">Timeout to turn off the display</param>
        public void CustomerDisplayIdle(TimeSpan timeToIdle)
        {
            PluginContext.Log.InfoFormat("Device: '{0} ({1})'. CustomerDisplayIdle.", DeviceName, deviceId);
        }

        /// <summary>
        /// Display text on customer display, if present
        /// </summary>
        /// <param name="task"> parameter contains 4 fields with text <see cref="CustomerDisplayTextTask.BottomLeft"/>,
        /// <see cref="CustomerDisplayTextTask.BottomRight"/>,
        /// <see cref="CustomerDisplayTextTask.TopLeft"/>, <see cref="CustomerDisplayTextTask.TopRight"/></param>
        public void CustomerDisplayText(CustomerDisplayTextTask task)
        {
            PluginContext.Log.InfoFormat("Device: '{0} ({1})'. CustomerDisplayText.", DeviceName, deviceId);
        }

        /// <summary>
        /// Returns the options and capabilities of the printing device <see cref="CashRegisterDriverParameters"/>
        /// </summary>
        /// <returns>Contains the following data:<para/>
        /// <see cref="CashRegisterDriverParameters.IsCancellationSupported"/> Does FR support the Cancellation operation?<para />
        /// <see cref="CashRegisterDriverParameters.CanUseFontSizes"/>Does the FR support the use of fonts for printing text<para />
        /// <see cref="CashRegisterDriverParameters.CanPrintQRCode"/>Does FR support QR code printing?<para />
        /// <see cref="CashRegisterDriverParameters.CanPrintLogo"/>Does FR support logo printing?<para />
        /// <see cref="CashRegisterDriverParameters.CanPrintBarcode"/>Does FR support barcode printing?<para />
        /// <see cref="CashRegisterDriverParameters.ZeroCashOnClose"/>Does FR reset the amount of cash at the cash desk when closing a shift<para />
        /// <see cref="CashRegisterDriverParameters.CanPrintText"/>Does FR support text printing?<para />
        /// Line width for each font:<see cref="CashRegisterDriverParameters.Font0Width"/>, 
        /// <see cref="CashRegisterDriverParameters.Font1Width"/>, <see cref="CashRegisterDriverParameters.Font2Width"/> <para />
        /// <see cref="CashRegisterDriverParameters.IsBillTaskSupported"/>Does the FR support the printing of the guest bill through the "Invoice" command?<para />
        /// </returns>
        public CashRegisterDriverParameters GetCashRegisterDriverParameters()
        {
            return new CashRegisterDriverParameters
            {
                CanPrintText = true,
                CanPrintBarcode = true,
                CanPrintLogo = true,
                CanPrintQRCode = true,
                CanUseFontSizes = true,
                Font0Width = 44,
                Font1Width = 42,
                Font2Width = 22,
                IsCancellationSupported = true,
                ZeroCashOnClose = true,
                IsBillTaskSupported = false,
            };
        }
    }
}