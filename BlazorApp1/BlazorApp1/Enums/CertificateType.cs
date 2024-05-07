using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;


namespace BlazorApp1.Enums
{
    [Serializable ()]
    [DataContract]
    public enum CertificateType : byte
    {
        Unknown = 0,

        File = 1,

        WindowsStore
    }
}
