using System;
using System.Collections.Generic;
using Resto.Front.Api.Attributes.JetBrains;
using Resto.Front.Api.Data.Device.Settings;
using Resto.Front.Api.Devices;

namespace Resto.Front.Api.SampleScalePlugin
{
    internal sealed class SampleScaleFactory : MarshalByRefObject, IScaleFactory
    {
        // Note http://msdn.microsoft.com/en-us/library/23bk23zc(v=vs.100).aspx
        public override object InitializeLifetimeService() { return null; }

        [NotNull]
        private const string ScaleName = "An example of a scales plug-in";

        public SampleScaleFactory()
        {
            DefaultDeviceSettings = InitDefaultDeviceSettings();
        }

        public string CodeName => ScaleName;

        [NotNull]
        public DeviceSettings DefaultDeviceSettings { get; }

        public IScale Create(Guid deviceId, DeviceSettings settings)
        {
            var scale = new SampleScale(deviceId, settings);

            return scale;
        }

        private DeviceSettings InitDefaultDeviceSettings()
        {
            return new DeviceSettings
            {
                Code = CodeName,
                Settings = new List<DeviceSetting>
                {
                    new DeviceNumberSetting
                    {
                        Name = "Int Setting",
                        Value = 1,
                        Label = "Setting an integer value",
                        MaxValue = 999,
                        MinValue = 1,
                        SettingKind = DeviceNumberSettingKind.Integer
                    },
                    new DeviceStringSetting
                    {
                        Name = "String setting",
                        Label = "Setting for string input",
                        Value = "A",
                        MaxLength = 255
                    }
                }
            };
        }

    }
}
