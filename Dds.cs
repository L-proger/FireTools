using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace AssetsProcessing {
    public static class Dds {

        public struct DdsMagic
        {
            public uint fourCC;
            public bool IsValid => fourCC == Make().fourCC;

            public static DdsMagic Make()
            {
                return new DdsMagic {fourCC = 0x20534444};
            }
        }


        [Flags]
        public enum DDS_PIXELFORMAT_FLAGS : uint {
            DDS_FOURCC = 0x00000004, // DDPF_FOURCC
            DDS_RGB = 0x00000040, // DDPF_RGB
            DDS_RGBA = 0x00000041, // DDPF_RGB | DDPF_ALPHAPIXELS
            DDS_LUMINANCE = 0x00020000, // DDPF_LUMINANCE
            DDS_LUMINANCEA = 0x00020001, // DDPF_LUMINANCE | DDPF_ALPHAPIXELS
            DDS_ALPHAPIXELS = 0x00000001, // DDPF_ALPHAPIXELS
            DDS_ALPHA = 0x00000002, // DDPF_ALPHA
            DDS_PAL8 = 0x00000020, // DDPF_PALETTEINDEXED8
            DDS_PAL8A = 0x00000021, // DDPF_PALETTEINDEXED8 | DDPF_ALPHAPIXELS
            DDS_BUMPDUDV = 0x00080000 // DDPF_BUMPDUDV
        }

        public struct DDS_PIXELFORMAT {
            public uint size;
            public DDS_PIXELFORMAT_FLAGS flags;
            public FrourCC fourCC;
            public uint RGBBitCount;
            public uint RBitMask;
            public uint GBitMask;
            public uint BBitMask;
            public uint ABitMask;

            public static DDS_PIXELFORMAT make(DDS_PIXELFORMAT_FLAGS flags,
                FrourCC fourCC, uint RGBBitCount, uint RBitMask,
                uint GBitMask, uint BBitMask, uint ABitMask) {

                DDS_PIXELFORMAT result = new DDS_PIXELFORMAT();
                result.size = (uint)Marshal.SizeOf<DDS_PIXELFORMAT>();
                result.flags = flags;
                result.fourCC = fourCC;
                result.RGBBitCount = RGBBitCount;
                result.RBitMask = RBitMask;
                result.GBitMask = GBitMask;
                result.BBitMask = BBitMask;
                result.ABitMask = ABitMask;
                return result;
            }

            public static DDS_PIXELFORMAT DXT1 => make(DDS_PIXELFORMAT_FLAGS.DDS_FOURCC, MAKEFOURCC('D', 'X', 'T', '1'), 0, 0, 0, 0, 0);
            public static DDS_PIXELFORMAT BC4_UNORM => make(DDS_PIXELFORMAT_FLAGS.DDS_FOURCC, MAKEFOURCC('B', 'C', '4', 'U'), 0, 0, 0, 0, 0);
        }

        [Flags]
        public enum DdsHeaderFlags : uint
        {
            DDSD_CAPS = 0x1,
            DDSD_HEIGHT = 0x2,
            DDSD_WIDTH = 0x4,
            DDSD_PITCH = 0x8,
            DDSD_PIXELFORMAT = 0x1000,
            DDSD_MIPMAPCOUNT = 0x20000,
            DDSD_LINEARSIZE = 0x80000,
            DDSD_DEPTH = 0x800000
        }



        public struct DDS_HEADER {
            public uint size;
            public DdsHeaderFlags flags;
            public uint height;
            public uint width;
            public uint pitchOrLinearSize;
            public uint depth; // only if DDS_HEADER_FLAGS_VOLUME is set in flags
            public uint mipMapCount;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 11)]
            public uint[] reserved1;

            public DDS_PIXELFORMAT ddspf;
            public uint caps;
            public uint caps2;
            public uint caps3;
            public uint caps4;
            public uint reserved2;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct FrourCC
        {
            [MarshalAs(UnmanagedType.U1)]
            public char c0;
            [MarshalAs(UnmanagedType.U1)]
            public char c1;
            [MarshalAs(UnmanagedType.U1)]
            public char c2;
            [MarshalAs(UnmanagedType.U1)]
            public char c3;
        }

        public static FrourCC MAKEFOURCC(char ch0, char ch1, char ch2, char ch3)
        {
            return new FrourCC {c0 = ch0, c1 = ch1, c2 = ch2, c3 = ch3};
        }
    }
}
