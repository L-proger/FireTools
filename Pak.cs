using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace FireTools {
    


    namespace Native
    {
        public struct Header {
            public struct PageDesc {
                public uint offset;
                public uint size;
                public uint index;
            };
            public struct SomeRecordTableDesc {
                public uint unk3;
                public uint someRecordsOffset;
                public uint someRecordsCount;
            };

            [StructLayout(LayoutKind.Sequential, Pack = 1)]
            public struct BaseInfo {
                public ushort magic;
                public ushort someType;
                public uint totalHeaderSize;
                public uint loginTableFilePageId;
                public uint loginTableFileOffset;
                public uint pagesCount;
                public uint pageTableOffset;
                public uint someConstantValue; // always == 8
                public uint someRecordTableDescOffset; // always == 8
                public uint someAttachedDataSize; //bottom of pak file (after last page)

                public uint vramDescTableFilePageId;
                public uint vramDescTableFileOffset;
            }

            public BaseInfo baseInfo;

            public PageDesc[] pageTable;

            public SomeRecordTableDesc someRecordTableDesc;

            public byte[] someZeroData;

            public struct SomeRecord {
                public ushort w0;
                public ushort w1;
                public uint v1;
            };

            public SomeRecord[] someRecords;
        }
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct FileHeader {
            public uint fileNameOffset;
            public uint alwaysZeroValue;
            public uint fileClassNameOffset;
            public uint u3;
            public uint fileSize;
            public uint dummy0; //0x12345
            public uint dummy1; //0x12345
            public uint dummy2; //0x12345
        }
    }


    public class Page
    {
        public PakFileInput pakFile;
        public uint id;
        public class File
        {
            public PakFileInput Pak => page.pakFile;
            public Page page;
            public uint id;
            public Native.FileHeader header;
            public uint Size => header.fileSize;
            public uint LocalOffset => page.fileTable[id].fileOffset;
            public uint GlobalOffset => LocalOffset + page.Offset;

            public uint LocalDataOffset => (uint)(page.fileTable[id].fileOffset + Marshal.SizeOf<Native.FileHeader>());
            public uint GlobalDataOffset => LocalDataOffset + page.Offset;

            public string name;
            public string className;
        }
        public int FilesCount => baseInfo.filesTableHeader.filesCount;

        public uint Offset => pakFile.header.pageTable[id].offset;
        public uint Size => pakFile.header.pageTable[id].size;

        public long TotalFilesSize {
            get {
                return files.Select(v => (long)v.header.fileSize).Sum();
            }
        }


        public Native.Header.PageDesc desc;
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct BaseInfo {
            public struct FilesTableHeader {
                public ushort unk0;
                public ushort filesCount;
            }

            public uint magic0; //0xDEADBEEF
            public uint magic1; //0xDEADBEEF

            public uint unk0;
            public uint fullPageSize;

            public FilesTableHeader filesTableHeader;
        }

        public BaseInfo baseInfo;

       

        public struct FileTableRecord {
            public uint fileNameOffset;
            public uint alwaysZeroValue;
            public uint fileOffset;
            public uint dummy;
        }

        public FileTableRecord[] fileTable;
        public File[] files;
    }

    public class VRamDescFile {
        public struct Header {
            public uint u0;
            public uint u1;
            public uint maybeOffset;
            public uint u3;
            public uint maybeLength;
            public ushort h0;
            public ushort h1;
            public ushort h2;
            public ushort h3;
            public uint maybeFormat;
   
            public ushort h6;
            public ushort h7;
            public uint mipCount;
            public uint width;
            public uint height;
            public uint u12;
            public uint u13;
            public uint u14;
            public uint u15;
        }

        public Header header;
        public string fileName;
        public Page.File file;

        public static VRamDescFile Read(Stream stream, Page.File file) {
            var result = new VRamDescFile();
            result.file = file;
            stream.Seek(file.GlobalDataOffset, SeekOrigin.Begin);
            stream.ReadValue(out result.header);
            result.fileName = stream.ReadString();
            return result;
        }

        public byte[] ReadRawData(Stream stm)
        {
            stm.Seek((long)stm.Length - file.Pak.RawDataSize + header.maybeOffset, SeekOrigin.Begin);
            return stm.ReadBytes((int)header.maybeLength);
        }
    }


    public class PakFileInput {
        public string sourceName;
        public long realFullSize;
        public Native.Header header;
        public Page[] pages;

        public uint RawDataSize => header.baseInfo.someAttachedDataSize;
        public int TotalFilesCount {
            get {
                return pages.Select(v => v.files.Length).Sum();
            }
        }

        public PakFileInput(Stream stm, string sourceName) {
            this.sourceName = sourceName;
            realFullSize = stm.Length;
            stm.Seek(0, SeekOrigin.Begin);

            ParseHeader(stm);

            pages = new Page[header.baseInfo.pagesCount];
            for (uint i = 0; i < pages.Length; ++i) {
                pages[i] = ParsePage(stm, i);
            }
        }

        private Page ParsePage(Stream stm, uint id) {
            Page result = new Page();
            result.id = id;
            result.pakFile = this;

            var pageDesc = header.pageTable[id];
            result.desc = pageDesc;

            stm.Seek(pageDesc.offset, SeekOrigin.Begin);

            stm.ReadValue(out result.baseInfo);

            result.fileTable = new Page.FileTableRecord[result.FilesCount];
            for (uint i = 0; i < result.FilesCount; ++i) {
                stm.ReadValue(out result.fileTable[i]);
            }

            result.files = new Page.File[result.FilesCount];
            for (uint i = 0; i < result.FilesCount; ++i) {
                result.files[i] = new Page.File();
                result.files[i].id = i;
                result.files[i].page = result;
                stm.Seek(pageDesc.offset + result.fileTable[i].fileOffset, SeekOrigin.Begin);
                stm.ReadValue(out result.files[i].header);

                stm.Seek(pageDesc.offset + result.files[i].header.fileNameOffset, SeekOrigin.Begin);
                result.files[i].name = stm.ReadString();
                stm.Seek(pageDesc.offset + result.files[i].header.fileClassNameOffset, SeekOrigin.Begin);
                result.files[i].className = stm.ReadString();
            }
            return result;
        }


        private void ParseHeader(Stream stm) {
            stm.ReadValue(out header.baseInfo);

            header.pageTable = new Native.Header.PageDesc[header.baseInfo.pagesCount];
            for (uint i = 0; i < header.baseInfo.pagesCount; ++i) {
                stm.ReadValue(out header.pageTable[i]);
            }

            stm.Seek(header.baseInfo.someRecordTableDescOffset, SeekOrigin.Begin);
            stm.ReadValue(out header.someRecordTableDesc);

            var someZeroDataSize = (int)(header.someRecordTableDesc.someRecordsOffset - stm.Position);
            header.someZeroData = stm.ReadBytes(someZeroDataSize);

            if (header.someRecordTableDesc.someRecordsCount != 0) {
                header.someRecords = new Native.Header.SomeRecord[header.someRecordTableDesc.someRecordsCount];
                for (uint i = 0; i < header.someRecords.Length; ++i) {
                    stm.ReadValue(out header.someRecords[i]);
                }
            } else {
                header.someRecords = new Native.Header.SomeRecord[0];
            }
        }
    }
}
