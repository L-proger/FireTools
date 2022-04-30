using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Runtime.InteropServices;
using System.Text;
using FireTools.Phyre;

namespace FireTools {

    class Program {
        static void PrintPak(PakFileInput pak)
        {
            Console.WriteLine("Path: " + pak.sourceName);
            Console.WriteLine("Page count: " + pak.pages.Length);
            for (int i = 0; i < pak.pages.Length; ++i)
            {
                var page = pak.pages[i];
                Console.WriteLine("  Files count: " + page.FilesCount);
                Console.WriteLine("  Total files size: " + page.TotalFilesSize);
                for (int j = 0; j < page.files.Length; ++j)
                {
                    var file = page.files[j];
                    Console.WriteLine($"    File size: {file.header.fileSize}\tname: {file.name} \tclass name: { file.className}");
                }
            }
        }

        static byte[] UnswizzleTexture(byte[] data, uint width, uint height, uint mipCount)
        {
            List<byte> result = new List<byte>();
            
            uint offset = 0;
            for (uint i = 0; i < mipCount; ++i)
            {
                var rowPitch = width * 2;
                var slicePitch = (width * height) / 2;

                if (width < 4 || height < 4)
                {
                    rowPitch = 8;
                    slicePitch = 8;
                }

                var mipDataSegment = new ArraySegment<byte>(data, (int) offset, (int) slicePitch);
                var mipData = mipDataSegment.ToArray();

                var unswizzledMipData = TextureSwizzling.UnSwizzle(mipData, (int)width, (int)height, 8);
                result.AddRange(unswizzledMipData);


                width /= 2;
                height /= 2;

                offset += slicePitch;
            }

            return result.ToArray();
        }

        static void ExportDdsTexture(Stream stm, VRamDescFile texture)
        {
            if (texture.header.width == texture.header.height)
            {
                return;
            }
            Dds.DDS_HEADER header = new Dds.DDS_HEADER();
            header.flags = Dds.DdsHeaderFlags.DDSD_CAPS | Dds.DdsHeaderFlags.DDSD_HEIGHT |
                           Dds.DdsHeaderFlags.DDSD_WIDTH | Dds.DdsHeaderFlags.DDSD_PIXELFORMAT |
                           Dds.DdsHeaderFlags.DDSD_MIPMAPCOUNT | Dds.DdsHeaderFlags.DDSD_LINEARSIZE;


          


            var realFilePath = Path.GetDirectoryName(texture.fileName);
            var fileName = Path.GetFileName(realFilePath);

            string outFileName = Path.Join("D:\\TextureExport",
                fileName +
                $".{texture.header.width}_{texture.header.height}_unswizzled_page{texture.file.page.id}_file{texture.file.id}_fmt{texture.header.maybeFormat}" +
                ".dds");

            Console.WriteLine("Exporting texture " + outFileName);

            var rawData = texture.ReadRawData(stm);
            var unswizzledData =  UnswizzleTexture(rawData, texture.header.width, texture.header.height, texture.header.mipCount);

            if (texture.header.maybeFormat == 80) {
                header.ddspf = Dds.DDS_PIXELFORMAT.BC4_UNORM;
            } else if (texture.header.maybeFormat == 71 || (texture.header.maybeFormat == 72)) {
                header.ddspf = Dds.DDS_PIXELFORMAT.DXT1;
            } else if (texture.header.maybeFormat == 35) {
                header.ddspf = Dds.DDS_PIXELFORMAT.DXT1;
                unswizzledData = rawData;
            } else {
                header.ddspf = Dds.DDS_PIXELFORMAT.DXT1;
                Console.WriteLine($"unknown format {texture.header.maybeFormat} for texture {texture.fileName}");
                //return;
            }


            header.width = texture.header.width;
            header.height = texture.header.height;
            header.pitchOrLinearSize = (header.width * header.height) / 2;
            header.depth = 1;
            header.mipMapCount = texture.header.mipCount;
            header.reserved1 = new uint[11];
            header.caps = 0x1000; //DDSCAPS_TEXTURE
            header.size = (uint)Marshal.SizeOf<Dds.DDS_HEADER>();


         

            var ofs = File.OpenWrite(outFileName);

  
            ofs.WriteStruct(Dds.DdsMagic.Make());

            ofs.WriteStruct(header);
            ofs.Write(unswizzledData);
            ofs.Close();

            Console.WriteLine("Exporting texture done");

        }
        static void ExportTextures()
        {
            var path = @".pak";
            var stm = File.OpenRead(path);
            var p = new PakFileInput(stm, path);

            for (int i = 0; i < p.pages.Length; ++i)
            {
                var page = p.pages[i];
                for(int j = 0; j < page.files.Length; ++j)
                {
                    if (page.files[j].className == "VRAM_DESC")
                    {
                        var f = VRamDescFile.Read(stm, page.files[j]);

                        ExportDdsTexture(stm, f);

                        Console.WriteLine("Ololo");
                    }
                }
            }
        }



        class ClassPrinter
        {
            public List<Phyre.ObjectsTable.Class> printedClasses = new List<ObjectsTable.Class>();

            public void Print(Phyre.ObjectsTable.Class c)
            {
                string ident = "    ";

                if (printedClasses.Contains(c))
                {
                    return;
                }

                if (c.baseClass != null)
                {
                    Print(c.baseClass);
                }

                Console.Write($"class {c.name}");
                if (c.baseClass != null)
                {
                   
                    Console.Write($" : {c.baseClass.name}");
                }
                Console.WriteLine($"{{");
                Console.WriteLine($"public:");

                foreach (var classMember in c.members)
                {
                    Console.WriteLine($"{ident}{classMember.fieldType.name} {classMember.name}; //is class: {classMember.fieldType is ObjectsTable.Class}");
                }

                Console.WriteLine($"}};");
                Console.WriteLine($"");

                printedClasses.Add(c);
            }
        }

        static void Main(string[] args)
        {
            var stm = File.OpenRead(@"D:\tex.dds.phyre");
            var header = Phyre.Archive.ReadHeader(stm);
            var objectsTable = Phyre.Archive.ReadObjectsTable(stm);
            objectsTable.Unpack();

            ClassPrinter printer = new ClassPrinter();

            foreach (var c in objectsTable.Classes)
            {
                printer.Print(c);
            }

            var instanceListHeaders = new List<Archive.PInstanceListHeader>();
            var instances = ((Archive.DX11Header) header).baseHeader.instanceListCount;
            for (uint i = 0; i < instances; ++i)
            {
                var h = Phyre.Archive.ReadInstanceListHeader(stm);
                instanceListHeaders.Add(h);

                Console.WriteLine($"Objects count: {h.m_count} Type: {objectsTable.Classes[h.m_classID - 1]} {h.m_objectsSize}");

            }


            var mems = objectsTable.classDescriptors.Sum(v => v.classDataMemberCount);
            Console.WriteLine();

        }
    }
}
