using System.Collections.Generic;
using System.Linq;
using Resto.Front.Api.Data.Device.Settings;

namespace Resto.Front.Api.SampleCashRegisterPlugin
{
    /// <summary>
    /// Describe all available settings for this device.
    /// </summary>
    public class SampleCashRegisterSettings
    {
        private readonly CashRegisterSettings deviceSettings;

        public SampleCashRegisterSettings(CashRegisterSettings settings)
        {
            deviceSettings = settings;
        }

        private T GetSetting<T>(string name) where T : DeviceSetting
        {
            return (T)deviceSettings.Settings.FirstOrDefault(setting => setting.Name == name);
        }

        /// Example of adding settings
        /// Setting type inherited from <seealso cref="DeviceSetting"/>
        /// Numeric <seealso cref="DeviceNumberSetting"/>, text <seealso cref="DeviceStringSetting"/>,
        /// Sign <seealso cref="DeviceBooleanSetting"/>, enum <seealso cref="DeviceCustomEnumSetting"/>
        public DeviceNumberSetting NumberSettingExample => GetSetting<DeviceNumberSetting>("NumberSettingExample");

        /// For each setting, you must specify standard values and restrictions
        /// For numeric settings: <para />
        /// Name (mandatory) - <seealso cref="DeviceNumberSetting.Name"/>
        /// Standard value - <seealso cref="DeviceNumberSetting.Value"/>
        /// Description - <seealso cref="DeviceNumberSetting.Label"/>
        /// Maximum value - <seealso cref="DeviceNumberSetting.MaxValue"/>
        /// Minimum value - <seealso cref="DeviceNumberSetting.MinValue"/>
        /// Numeric type - <seealso cref="DeviceNumberSetting.SettingKind"/>
        public static readonly DeviceNumberSetting DefaultNumberSettingExample =
            new DeviceNumberSetting
            {
                Name = "NumberSettingExample",
                Value = 1,
                Label = "Numerical setting example",
                MaxValue = 999,
                MinValue = 1,
                SettingKind = DeviceNumberSettingKind.Integer
            };

        public DeviceStringSetting StringSettingExample => GetSetting<DeviceStringSetting>("StringSettingExample");

        /// For string settings: <para />
        /// Name (mandatory) - <seealso cref="DeviceStringSetting.Name"/>
        /// Standard value - <seealso cref="DeviceStringSetting.Value"/>
        /// Description - <seealso cref="DeviceStringSetting.Label"/>
        /// Maximum length - <seealso cref="DeviceStringSetting.MaxLength"/>
        public static readonly DeviceStringSetting DefaultStringSettingExample =
            new DeviceStringSetting
            {
                Name = "StringSettingExample",
                Label = "String setting example",
                Value = "1.0",
                MaxLength = 255,
            };

        public DeviceBooleanSetting BooleanSettingExample => GetSetting<DeviceBooleanSetting>("BooleanSettingExample");

        /// For sign settings: <para />
        /// Name (mandatory) - <seealso cref="DeviceBooleanSetting.Name"/>
        /// Standard value - <seealso cref="DeviceBooleanSetting.Value"/>
        /// Description - <seealso cref="DeviceBooleanSetting.Label"/>
        public static readonly DeviceBooleanSetting DefaultPrintItemsOnCheque =
            new DeviceBooleanSetting
            {
                Name = "BooleanSettingExample",
                Value = true,
                Label = "Sign setting example",
            };

        public DeviceCustomEnumSetting ListSettingExample => GetSetting<DeviceCustomEnumSetting>("ListSettingExample");

        /// For enums: <para />
        /// Name - <seealso cref="DeviceCustomEnumSetting.Name"/>
        /// Description - <seealso cref="DeviceCustomEnumSetting.Label"/>
        /// Display type - <seealso cref="DeviceCustomEnumSetting.IsList"/>
        /// Selection options - <seealso cref="DeviceCustomEnumSetting.Values"/>, <para />
        /// For each option: <para />
        /// Name - <seealso cref="DeviceCustomEnumSettingValue.Name"/>
        /// Default value (selected or not) - <seealso cref="DeviceCustomEnumSettingValue.IsDefault"/>
        /// Description - <seealso cref="DeviceCustomEnumSettingValue.Label"/>
        public static readonly DeviceCustomEnumSetting DefaultListSettingExample =
            new DeviceCustomEnumSetting()
            {
                Name = "ListSettingExample",
                Label = "List setting example",
                IsList = true,
                Values = new List<DeviceCustomEnumSettingValue>
                {
                    new DeviceCustomEnumSettingValue()
                    {
                        Name = "ListElement1",
                        IsDefault = true,
                        Label = "List item 1"
                    },
                    new DeviceCustomEnumSettingValue()
                    {
                        Name = "ListElement2",
                        IsDefault = false,
                        Label = "List item 2"
                    }
                }
            };
    }
}