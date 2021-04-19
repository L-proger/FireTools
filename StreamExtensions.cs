using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace AssetsProcessing {
    public static class StreamExtensions {

        public static byte[] ReadBytes(this Stream stm, int size) {
            byte[] bytes = new byte[size];
            if (stm.Read(bytes, 0, size) != size) {
                throw new Exception("Failed to read stream");
            }
            return bytes;
        }


        public static void WriteStruct<T>(this Stream stm, T value) where T : struct
        {
            var bytes = MemoryUtils.StructToBytes(value);
            stm.Write(bytes, 0, bytes.Length);
        }

        public static T ReadStruct<T>(this Stream stm) where T : struct {
            byte[] bytes = new byte[Marshal.SizeOf<T>()];
            if (stm.Read(bytes, 0, Marshal.SizeOf<T>()) != bytes.Length) {
                throw new Exception("Failed to read stream");
            }
            var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            var result = Marshal.PtrToStructure<T>(handle.AddrOfPinnedObject());
            handle.Free();
            return result;
        }

        public static uint ReadUInt(this Stream stm) {
            return ReadStruct<uint>(stm);
        }
        public static int ReadInt(this Stream stm) {
            return ReadStruct<int>(stm);
        }
        public static ushort ReadUShort(this Stream stm) {
            return ReadStruct<ushort>(stm);
        }
        public static short ReadShort(this Stream stm) {
            return ReadStruct<short>(stm);
        }


        public static string ReadString(this Stream stm) {
            var result = new List<byte>();
            while (true) {
                int c = stm.ReadByte();
                if (c < 0) {
                    throw new Exception("End of file");
                }
                if (c == 0) {
                    break;
                }
                result.Add((byte)(c & 0xff));
            }
            return Encoding.ASCII.GetString(result.ToArray());
        }



        public static void ReadValue(this Stream stm, out string result) {
            result = ReadString(stm);
        }

        public static void ReadValue<T>(this Stream stm, out T result) where T : struct {
            result = ReadStruct<T>(stm);
        }
    }
}
