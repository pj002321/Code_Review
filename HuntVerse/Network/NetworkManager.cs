using NUnit.Framework;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using UnityEngine;

//using hunt.Net;

namespace Hunt.Net
{
    public class NetworkManager : MonoBehaviourSingleton<NetworkManager>
    {
        private NetModule m_loginConnection;
        //k: ip<<32 | port, v: netModule
        private ConcurrentDictionary<UInt64, NetModule> m_tcpConnections;//런타임에 커넥션 떨어지고 그럴 수 있음

        private Dictionary<NetModule.ServiceType, MsgDispatcherBase> m_dispatchers;//얘는 awake init된 이후로는 읽기만

        protected override void Awake()
        {
            base.Awake();
            m_tcpConnections = new ConcurrentDictionary<UInt64, NetModule>();

            m_dispatchers = new Dictionary<NetModule.ServiceType, MsgDispatcherBase>();
            //치트는 에디터에서만 가능
#if UNITY_EDITOR
            m_dispatchers.Add(NetModule.ServiceType.Cheat, new CheatMsgDispatcher());
#endif
            m_dispatchers.Add(NetModule.ServiceType.Login, new LoginMsgDispatcher());
            m_dispatchers.Add(NetModule.ServiceType.Common, new CommonMsgDispatcher());
            m_dispatchers.Add(NetModule.ServiceType.Game, new GameMsgDispatcher());
            foreach (var dispatcher in m_dispatchers.Values)
            {
                var suc = dispatcher.Init();
                Debug.Assert(suc);
            }
        }

        public async Task<bool> ConnLoginServer(Action<NetModule.ERROR, string>? disconnectHandler, Action connSuccessHandler, Action<SocketException> connFailHandler)
        {
            m_loginConnection = new NetModule(NetModule.ServiceType.Login, disconnectHandler, connSuccessHandler, connFailHandler);
            return await m_loginConnection.AsyncConn("127.0.0.1", 9000);
        }

        public bool ConnLoginServerSync(Action<NetModule.ERROR, string>? disconnectHandler, Action connSuccessHandler, Action<SocketException> connFailHandler)
        {
            m_loginConnection = new NetModule(NetModule.ServiceType.Login, disconnectHandler, connSuccessHandler, connFailHandler);
            return m_loginConnection.SyncConn("127.0.0.1", 9000);
        }

        public void StartLoginServer()
        {
            m_loginConnection.Start();
        }

        public void DisConnLoginServer()
        {
            m_loginConnection.Stop();
        }

        public void SendToLogin<ProtoT>(Hunt.Common.MsgId type, ProtoT data) where ProtoT : Google.Protobuf.IMessage
            => m_loginConnection.Send(type, data);

        public NetModule MakeNetModule(NetModule.ServiceType type, Action<NetModule.ERROR, string> disconnectHandler, Action connSuccessHandler, Action<SocketException> connFailHandler)
        {
            return new NetModule(type, disconnectHandler, connSuccessHandler, connFailHandler);
        }

        public bool IsExistConnection(UInt64 key)
        {
            return m_tcpConnections.ContainsKey(key);
        }

        public void StopNet(UInt64 key)
        {
            if (m_tcpConnections.TryRemove(key, out NetModule netModule))
            {
                netModule.Stop();
            }
        }

        public NetModule? GetNet(UInt64 key)
        {
            m_tcpConnections.TryGetValue(key, out var netModule);
            return netModule;
        }

        public bool InsertNetModule(UInt64 key, NetModule module)//after conn success, start
        {
            var suc = m_tcpConnections.TryAdd(key, module);
            if (suc)
            {
                m_tcpConnections[key].Start();
            }
            return suc;
        }

        public Action<byte[], int, int> GetDispatcher(NetModule.ServiceType serviceType, Hunt.Common.MsgId packetType)
        {
            m_dispatchers.TryGetValue(serviceType, out var dispatcher);
            return dispatcher.GetHandler(packetType);
        }

        //ip가 www.~.com 이런 경우는 나중에 생각을....
        static UInt64 MakeHash(string ip, UInt16 port)
        {
            string[] parts = ip.Split('.');
            UInt64 result = 0;
            result |= uint.Parse(parts[0]) << (24 + 32);
            result |= uint.Parse(parts[1]) << (16 + 32);
            result |= uint.Parse(parts[2]) << (8 + 32);
            result |= uint.Parse(parts[3]) << 32;
            result |= port;
            return result;
        }
    }
}
