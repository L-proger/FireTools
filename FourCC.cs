using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace FireTools {
    [StructLayout(LayoutKind.Sequential)]
    public struct FourCC {
        [MarshalAs(UnmanagedType.U1)]
        public char c0;
        [MarshalAs(UnmanagedType.U1)]
        public char c1;
        [MarshalAs(UnmanagedType.U1)]
        public char c2;
        [MarshalAs(UnmanagedType.U1)]
        public char c3;

        public uint Value
        {
            get
            {
                return (uint)(c0 | c1 << 8 | c2 << 16 | c3 << 24);
            }
        }

        public override string ToString()
        {
            return new string(new char[]{c0, c1, c2, c3});
        }

        public static FourCC Make(char ch0, char ch1, char ch2, char ch3) {
            return new FourCC { c0 = ch0, c1 = ch1, c2 = ch2, c3 = ch3 };
        }
    }
}
