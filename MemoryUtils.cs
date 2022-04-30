using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace FireTools {
    public static class MemoryUtils {
        public static byte[] StructToBytes<T>(T value) where T : struct
        {
            byte[] result = new byte[Marshal.SizeOf<T>()];
            var handle = GCHandle.Alloc(result, GCHandleType.Pinned);
            Marshal.StructureToPtr(value, handle.AddrOfPinnedObject(), false);
            handle.Free();
            return result;
        }

        public static T BytesToStruct<T>(byte[] data) where T : struct {
            var handle = GCHandle.Alloc(data, GCHandleType.Pinned);
            var result = Marshal.PtrToStructure<T>( handle.AddrOfPinnedObject());
            handle.Free();
            return result;
        }
    }
}
