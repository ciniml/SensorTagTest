using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;

namespace SensorTagTest
{
    class Program
    {
        /// <summary>
        /// DeviceInformationServiceのUUID
        /// </summary>
        private static readonly Guid DeviceInformationServiceUuid = GattDeviceService.ConvertShortIdToUuid(0x180a);
        /// <summary>
        /// キー入力のサービスのUUID
        /// </summary>
        private static readonly Guid KeyInputServiceUuid = GattDeviceService.ConvertShortIdToUuid(0xffe0);
        /// <summary>
        /// キー入力のCharacteristicsのUUID
        /// </summary>
        private static readonly Guid KeyInputCharacteristicUuid = GattDeviceService.ConvertShortIdToUuid(0xffe1);

        static void Main(string[] args)
        {
            var cancellationTokenSource = new CancellationTokenSource();

            var task = UpdateServiceAsync(cancellationTokenSource.Token);

            Console.ReadKey();
            cancellationTokenSource.Cancel();
            task.Wait();
        }

        static async Task UpdateServiceAsync(CancellationToken cancellationToken)
        {
            // キー入力サービスを検索する．
            var filter = GattDeviceService.GetDeviceSelectorFromUuid(KeyInputServiceUuid);
            var serviceDeviceIds = await DeviceInformation.FindAllAsync(filter, new[] { ContainerIdProperty }).AsTask(cancellationToken);
            foreach (var keyInputDevice in serviceDeviceIds)
            {
                // Access to Generic Attribute Profile service
                var gapService = await GetOtherServiceAsync(keyInputDevice, GattServiceUuids.GenericAccess, cancellationToken);
                var deviceName = gapService.GetCharacteristics(GattDeviceService.ConvertShortIdToUuid(0x2a00)).First();
                var deviceNameValue = await deviceName.ReadValueAsync(BluetoothCacheMode.Uncached).AsTask(cancellationToken);
                var decodedDeviceName = deviceNameValue.Value.DecodeUtf8String();
                Console.WriteLine("Device({0}):", decodedDeviceName);

                // Access to Device Information Service
                var deviceInformationService = await GetOtherServiceAsync(keyInputDevice, DeviceInformationServiceUuid, cancellationToken);
                var systemId = deviceInformationService.GetCharacteristics(GattDeviceService.ConvertShortIdToUuid(0x2a23)).First(); // System ID
                var systemIdValue = await systemId.ReadValueAsync(BluetoothCacheMode.Uncached).AsTask(cancellationToken);
                Console.WriteLine("\tSystemID: {0:X16}", systemIdValue.Value.DecodeUint40());

                // Access to Key Input Service
                var keyInputService = await GattDeviceService.FromIdAsync(keyInputDevice.Id);
                var keyInputData = keyInputService.GetCharacteristics(KeyInputCharacteristicUuid).First();
                await keyInputData.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify);
                keyInputData.ValueChanged += (o, e) => Console.WriteLine("KeyInputChanged({0}): {1:X}", decodedDeviceName, e.CharacteristicValue.ToArray().First());
            }
        }

        private const string ContainerIdProperty = "System.Devices.ContainerId";

        /// <summary>
        /// 指定したサービスデバイスが属するBluetoothデバイスに属する他のサービスを取得する．
        /// </summary>
        /// <param name="serviceInformation">サービスデバイスを表すDeviceInformation．ContainerIdプロパティを含んでいる必要がある．</param>
        /// <param name="serviceUuid">取得するサービスのUUID</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        static async Task<GattDeviceService> GetOtherServiceAsync(DeviceInformation serviceInformation, Guid serviceUuid, CancellationToken cancellationToken)
        {
            var containerId = serviceInformation.Properties[ContainerIdProperty].ToString();
            var selector = GattDeviceService.GetDeviceSelectorFromUuid(serviceUuid);
            var selectorWithContainer = String.Format("{0} AND System.Devices.ContainerId:=\"{{{1}}}\"", selector, containerId);
            var serviceInformations = await DeviceInformation.FindAllAsync(selectorWithContainer, new[] { ContainerIdProperty }).AsTask(cancellationToken);
            return await GattDeviceService.FromIdAsync(serviceInformations.Single().Id);
        }
    }
}
