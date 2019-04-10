using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JordanSdk.Network.Core;
using JordanSdk.Network.Tcp;
using JordanSdk.Network.Udp;
using JordanSdk.Network.WebSocket;

namespace JordanSdk.Network.Server
{
    public class ManagedServer
    {
        #region Fields

        private List<IProtocol> protocols = new List<IProtocol>(3);
        private bool disposed = false;

        #endregion

        #region Public Properties

        public bool Listening => throw new NotImplementedException();

        #endregion

        #region Events

        public event SocketConnectedDelegate OnConnectionRequested;

        #endregion

        #region Constructor

        public ManagedServer(IEnumerable<ProtocolBinding> bindings)
        {
            if (bindings == null || bindings.Count() == 0)
                throw new ArgumentException("At least one binding must be specified.", "bindings");
            foreach(ProtocolBinding binding in bindings)
            {
                if((binding.Kind & ProtocolKind.Tcp) != 0)
                {
                    protocols.Add(new JordanSdk.Network.Tcp.TcpProtocol() { Port = binding.Port, Address = binding.DomainOrIP } as IProtocol);
                }
                if ((binding.Kind & ProtocolKind.Udp) != 0)
                {
                    protocols.Add(new JordanSdk.Network.Udp.UdpProtocol() { Port = binding.Port, Address = binding.DomainOrIP } as IProtocol);
                }
                if ((binding.Kind & ProtocolKind.WebSocket) != 0)
                {
                    protocols.Add(new JordanSdk.Network.WebSocket.WebSocketProtocol() { Port = binding.Port, Address = binding.DomainOrIP } as IProtocol);
                }
            }
        }

        #endregion

        #region Public Functions

        public void Listen(bool enableNatTraversal = false)
        {
            CheckDisposed();
            protocols.ForEach(protocol => protocol.Listen(enableNatTraversal));
        }

        public Task ListenAsync(bool enableNatTraversal = false)
        {
            CheckDisposed();
            return Task.WhenAll(protocols.Select(protocol => protocol.ListenAsync(enableNatTraversal)));
        }

        public void StopListening()
        {
            CheckDisposed();
            protocols.ForEach(protocol => protocol.StopListening());
        }

        public void Dispose()
        {
            CheckDisposed();
            protocols.ForEach(protocol => protocol.Dispose());
            protocols.Clear();
        }

        #endregion

        private void CheckDisposed()
        {
            if (disposed)
                throw new ObjectDisposedException("This Managed Server has already been disposed");
        }
    }
}
