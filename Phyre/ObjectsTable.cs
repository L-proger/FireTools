using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace FireTools.Phyre {
    public class ObjectsTable {
        public struct Header {
            public uint magic;                
            public uint size;                 
            public uint typeCount;            
            public uint classCount;           
            public uint classMemberCount;    
            public uint stringTableSize;      
            public uint someBufferCount;   
            public uint someBufferSize;    
        }

        public struct TypeDescriptor {
            public uint nameOffset;
        }

        public struct ClassDescriptor {
            public int baseClassID;                   // 0 implies not set, positive implies local, negative implies global.
            public uint sizeInBytesAndAlignment;        // The size of this class in bytes with the alignment shift in the top 4 bits.
            public uint nameOffset;                 // The offset to the name of this class in the string table.
            public uint classDataMemberCount;           // The number of data members in this class.
            public uint offsetFromParent;               // The offset of the start of this class from the start of the parent class.
            public uint offsetToBase;                   // The offset of the start of this class from the start of the PBase class.
            public uint offsetToBaseInAllocatedBlock;   // The offset of the start of the PBase class in a block allocated for this class.
            public uint flags;                        // The flags for this class.
            public uint defaultBufferOffset;            // 0 implies not saved. Subtract one to get the correct default buffer offset if non-zero.
        }

        public struct ClassMemberDescriptor {
            public uint nameOffset;     // The offset to the name of this member in the string table.
            public uint typeID;         // The type of this member
            public uint valueOffset;        // The offset to the value of this member in the class.
            public uint sizeInBytes;        // The size of this member in bytes.
            public uint flags;            // The flags for this member.
            public uint fixedArraySize; // If non zero, this represents the size of a fixed size array.
        }

        public Header header;
        public TypeDescriptor[] typeDescriptors;
        public ClassDescriptor[] classDescriptors;
        public ClassMemberDescriptor[] classMemberDescriptors;
        public byte[] stringTable;

        public class ClassMember
        {
            public Type fieldType;
            public string name;
            public uint offset;
            public uint size;
            public uint flags;           
            public uint fixedArraySize;

            public override string ToString()
            {
                return name;
            }
        }


        public class Type
        {
            public string name;
            public override string ToString()
            {
                return name;
            }
        }

        public class Class : Type
        {
            public Class baseClass;
            public List<ClassMember> members = new List<ClassMember>();

            public override string ToString()
            {
                var baseStr = baseClass == null ? "" : $" Base: {baseClass.name}";
                return $"Name: {name} Members: {members.Count} Local members size: {members.Sum(v=>v.size)} {baseStr}";
            }
        }

        public class ExternalClass : Class {
            public ExternalClass()
            {
                name = "ExternalClass";
            }
        }

        public Type[] Types;
        public Class[] Classes;


        public void ParseTypes()
        {
            Types = new Type[typeDescriptors.Length];

            for (uint i = 0; i < Types.Length; ++i) {
                Types[i] = new Type();
                Types[i].name = ReadString((int)typeDescriptors[i].nameOffset);
            }
        }

        public void ParseClasses() {
            Classes = new Class[classDescriptors.Length];
            for (uint m = 0; m < Classes.Length; ++m)
            {
                Classes[m] = new Class();
            }


            int memberId = 0;

            for (uint i = 0; i < Classes.Length; ++i)
            {
                var c = classDescriptors[i];
                var cls = Classes[i];

                cls.name = ReadString((int) c.nameOffset);
                

                if (c.baseClassID < 0)
                {
                    cls.baseClass = new ExternalClass();
                }else if (c.baseClassID > 0)
                {
                    cls.baseClass = Classes[c.baseClassID - 1 ];
                }

                for (uint m = 0; m < c.classDataMemberCount; ++m)
                {
                    var d = classMemberDescriptors[memberId++];
                    ClassMember mem = new ClassMember();
                    mem.size = d.sizeInBytes;
                    mem.fixedArraySize = d.fixedArraySize;
                    mem.flags = d.flags;
                    mem.offset = d.valueOffset;
                    mem.name = ReadString((int)d.nameOffset);

                    Console.WriteLine("Member type ID: " + d.typeID);

                    if (d.typeID >= Types.Length) {
                        mem.fieldType = Classes[(int)d.typeID - Types.Length - 1];
                    } else {
                        mem.fieldType = Types[(int)d.typeID];
                    }




                   
                    cls.members.Add(mem);
                }

             
            }
        }

        public void Unpack()
        {
            ParseTypes();
            ParseClasses();
        }

        public string ReadString(int offset)
        {
            var handle = GCHandle.Alloc(stringTable, GCHandleType.Pinned);
            var result = Marshal.PtrToStringAnsi(handle.AddrOfPinnedObject() + offset);
            handle.Free();
            return result;
        }


    }
}
