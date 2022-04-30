using System;
using System.Collections.Generic;
using System.Text;

namespace FireTools.Phyre {
    public class FieldInstance
    {
        public ObjectsTable.ClassMemberDescriptor descriptor;
        public object value;
    }

    public class ClassInstance
    {
        public ClassInstance parentInstance;
        public ObjectsTable.Class thisClass;

    }
}
