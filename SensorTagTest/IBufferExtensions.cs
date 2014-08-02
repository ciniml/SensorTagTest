using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage.Streams;

namespace SensorTagTest
{
    static class IBufferExtensions
    {
        public static string DecodeUtf8String(this IBuffer buffer)
        {
            var data = buffer.ToArray();
            return Encoding.UTF8.GetString(data);
        }

        public static ulong DecodeUint40(this IBuffer buffer)
        {
            var data = buffer.ToArray();
            var decoded = data.Aggregate(0ul, (l, r) => (l << 8) | r);

            return decoded;
        }
    }
}
