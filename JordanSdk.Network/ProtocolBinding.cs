using System;
using System.Collections.Generic;
using System.Text;

namespace JordanSdk.Network
{
    public class ProtocolBinding
    {
        public string Name { get; set; }
        public ProtocolKind Kind { get; set; }
        public int Port { get; set; }
        public string DomainOrIP { get; set; }

    }
}
