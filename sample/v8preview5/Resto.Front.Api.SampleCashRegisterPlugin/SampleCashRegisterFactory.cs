using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Resto.Front.Api.Attributes.JetBrains;
using Resto.Front.Api.Data.Device.Results;
using Resto.Front.Api.Data.Device.Settings;
using Resto.Front.Api.Devices;

namespace Resto.Front.Api.SampleCashRegisterPlugin
{
    internal sealed class SampleCashRegisterFactory : ICashRegisterFactory
    {
        [NotNull]
        private const string CashRegisterName = "An example of an external fiscal registrar plugin";

        public SampleCashRegisterFactory()
        {
            DefaultDeviceSettings = InitDefaultDeviceSettings();
        }

        [NotNull]
        public string CodeName => CashRegisterName;

        [NotNull]
        public DeviceSettings DefaultDeviceSettings { get; }

        //Initialize fiscal registrar settings
        private CashRegisterSettings InitDefaultDeviceSettings()
        {
            return new CashRegisterSettings
            {
                Code = CodeName,
                Settings = new List<DeviceSetting>(
                    typeof(SampleCashRegisterSettings).GetFields(BindingFlags.Static | BindingFlags.Public).Select(info => (DeviceSetting)info.GetValue(null))),
                Font0Width = new DeviceNumberSetting
                {
                    Name = "Font0Width",
                    Value = 42,
                    Label = "Characters per line",
                    MaxValue = 100,
                    MinValue = 10,
                    SettingKind = DeviceNumberSettingKind.Integer
                },
                OfdProtocolVersion = new DeviceCustomEnumSetting
                {
                    Name = "OfdProtocolVersion",
                    Label = "Fiscal documents protocol",
                    Values = new List<DeviceCustomEnumSettingValue>
                    {
                        new DeviceCustomEnumSettingValue
                        {
                            Name = string.Empty,
                            IsDefault = true,
                            Label = "Without"
                        },
                        new DeviceCustomEnumSettingValue
                        {
                            Name = "1.0",
                            IsDefault = false,
                            Label = "1.0"
                        },
                        new DeviceCustomEnumSettingValue
                        {
                            Name = "1.05",
                            IsDefault = false,
                            Label = "1.05"
                        },
                        new DeviceCustomEnumSettingValue
                        {
                            Name = "1.1",
                            IsDefault = false,
                            Label = "1.1"
                        },
                        new DeviceCustomEnumSettingValue
                        {
                            Name = "1.2",
                            IsDefault = false,
                            Label = "1.2"
                        }
                    }
                },
                FiscalRegisterPaymentTypes = new List<FiscalRegisterPaymentType>
                {
                    //Fill in the table of payment types
                    new FiscalRegisterPaymentType
                    {
                        Id = "1",
                        Name = "Payment type 1"
                    },
                    new FiscalRegisterPaymentType
                    {
                        Id = "2",
                        Name = "Payment type 2"
                    },
                    new FiscalRegisterPaymentType
                    {
                        Id = "3",
                        Name = "Payment type 3"
                    },
                },
                //Fill in the table of tax rates
                FiscalRegisterTaxItems = new List<FiscalRegisterTaxItem>
                {
                    new FiscalRegisterTaxItem("1", false, true, 0, "VAT 0%"),
                    new FiscalRegisterTaxItem("2", false, true, 18, "VAT 20%")
                }
            };
        }

        public ICashRegister Create(Guid deviceId, [NotNull] CashRegisterSettings settings)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            var sampleCashRegister = new SampleCashRegister(deviceId, settings);

            return sampleCashRegister;
        }
    }
}
