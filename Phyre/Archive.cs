using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace FireTools.Phyre {
    public class Archive {

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct BaseHeader {
            public FourCC magic;
            public uint size;
            public uint objectsTableSize;
            public FourCC platformId;
            public uint instanceListCount;
            public uint arrayFixupSize;
            public uint arrayFixupCount;
            public uint pointerFixupSize;
            public uint pointerFixupCount;
            public uint pointerArrayFixupSize;
            public uint pointerArrayFixupCount;
            public uint pointersInArraysCount;
            public uint userFixupCount;
            public uint userFixupDataSize;
            public uint totalDataSize;
            public uint headerClassInstanceCount;
            public uint headerClassChildCount;
            public uint physicsEngineID;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct DX11Header {
            public BaseHeader baseHeader;
            public uint indexBufferSize;
            public uint vertexBufferSize;
            public uint maxTextureMipBufferSize;
        }


        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct PInstanceListHeader
        {
            public uint m_classID;                  // The ID of the class stored in this instance list.
            public uint m_count;                    // The number of instances of the class stored in this instance list.
            public uint m_size;                     // The size of the data for this instance list in bytes. This should be equal to m_objectsSize + m_arraysSize;
            public uint m_objectsSize;              // The size of the data for the objects in this instance list in bytes. Also includes the padding to make sure that the array data is correctly aligned.
            public uint m_arraysSize;               // The size of the non-object array data in this instance list in bytes
            public uint m_pointersInArraysCount;    // The number of pointers in pointer arrays.
            public uint m_arrayFixupCount;          // The count of array fixups required.
            public uint m_pointerFixupCount;        // The count of pointer fixups required.
            public uint m_pointerArrayFixupCount;   // The count of pointer array fixups required.
        };


        public static PInstanceListHeader ReadInstanceListHeader(Stream data)
        {
            return data.ReadStruct<PInstanceListHeader>();
        }

        public static object ReadHeader(Stream data)
        {
            var baseHeader = data.ReadStruct<BaseHeader>();
            var baseSize = Marshal.SizeOf<BaseHeader>();
            if (baseHeader.size < baseSize)
            {
                throw new Exception("Unexpected header size");
            }

            if (baseHeader.size == baseSize)
            {
                return baseHeader;
            }

            if (baseHeader.platformId.Value == 0x44583131)
            {
                //dx11 platform
                var dx11Size = Marshal.SizeOf<DX11Header>();
                var extraBytesCount = dx11Size - baseSize;
                var extraBytes = data.ReadBytes(extraBytesCount);
                var baseBytes = MemoryUtils.StructToBytes(baseHeader);
                List<byte> dx11Bytes = new List<byte>();
                dx11Bytes.AddRange(baseBytes);
                dx11Bytes.AddRange(extraBytes);
                return MemoryUtils.BytesToStruct<DX11Header>(dx11Bytes.ToArray());

            }
            throw new Exception("Unknown platform");
        }

        public static ObjectsTable ReadObjectsTable( Stream data)
        {
            /*var baseArcHeader = archiveHeader is BaseHeader
                ? (BaseHeader) archiveHeader
                : ((DX11Header) archiveHeader).baseHeader;*/
            ObjectsTable result = new ObjectsTable();
            result.header = data.ReadStruct<ObjectsTable.Header>();
            result.typeDescriptors = data.ReadStructArray<ObjectsTable.TypeDescriptor>(result.header.typeCount).ToArray();
            result.classDescriptors = data.ReadStructArray<ObjectsTable.ClassDescriptor>(result.header.classCount).ToArray();
            result.classMemberDescriptors = data.ReadStructArray<ObjectsTable.ClassMemberDescriptor>(result.header.classMemberCount).ToArray();
            result.stringTable = data.ReadBytes((int)result.header.stringTableSize);

            var s0 = Marshal.SizeOf<ObjectsTable.Header>();
            var s1 = Marshal.SizeOf<ObjectsTable.TypeDescriptor>() * result.header.typeCount;
            var s2 = Marshal.SizeOf<ObjectsTable.ClassDescriptor>() * result.header.classCount;
            var s3 = Marshal.SizeOf<ObjectsTable.ClassMemberDescriptor>() * result.header.classMemberCount;
            var s4 = result.header.stringTableSize;

            var paddingSize = result.header.size - (s0 + s1 + s2 + s3 + s4);
            if (paddingSize != 0)
            {
                data.ReadBytes((int)paddingSize);
            }
            return result;
        }


        /*SetBackColor(0x0000ff);
        Header header;
        SetBackColor(0x00ff00);
        PackedNamespaceHeader namespaceHeader;
        SetBackColor(0xff0000);

        PPackedType namespaceTypes[namespaceHeader.m_typeCount];
        PPackedClassDescriptor namespaceClassDescriptors[namespaceHeader.m_classCount];
        PPackedDataMember namespaceDataMembers[namespaceHeader.m_classDataMemberCount];

        CHAR namespaceStringTable[namespaceHeader.m_stringTableSize];*/


        //PChar* string table
        //PUInt8 * default buffers


    }
}
