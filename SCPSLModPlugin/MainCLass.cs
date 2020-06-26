using EXILED;
using EXILED.Extensions;
using LiteNetLib;
using LiteNetLib.Utils;
using MEC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SCPSLModPlugin
{
    public class MainClass : Plugin
    {
        public EventBasedNetListener listener;
        public NetManager server;
        public List<Speaker> speakers = new List<Speaker>();
        public override string getName { get; } = "SCPSLModPlugin";

        public override void OnDisable()
        {
        }

        public override void OnEnable()
        {
            listener = new EventBasedNetListener();
            server = new NetManager(listener);
            listener.ConnectionRequestEvent += Listener_ConnectionRequestEvent;
            listener.PeerConnectedEvent += Listener_PeerConnectedEvent;
            listener.PeerDisconnectedEvent += Listener_PeerDisconnectedEvent;
            Events.RemoteAdminCommandEvent += OnRemoteAdminCommand;
            server.Start(9889);
            ServerConsole.AddLog("Server started");
            Timing.RunCoroutine(Run());
        }

        public override void OnReload()
        {
        }
        public class Speaker
        {
            public int id { get; set; }
            public float x { get; set; }
            public float y { get; set; }
            public float z { get; set; }
            public string url { get; set; }
        }




        public IEnumerator<float> Run()
        {
            while (true)
            {
                yield return Timing.WaitForOneFrame;
                server.PollEvents();
                foreach (var player in Player.GetHubs())
                {
                    var perr = server.ConnectedPeerList.FirstOrDefault(per => per.EndPoint.ToString().Split(':')[0] == player.GetIpAddress());
                    if (perr != null)
                    {
                        NetDataWriter writer = new NetDataWriter();
                        writer.Put(0);
                        writer.Put(player.GetPosition().x);
                        writer.Put(player.GetPosition().y);
                        writer.Put(player.GetPosition().z);
                        perr.Send(writer, DeliveryMethod.ReliableOrdered);
                    }
                }
            }
        }

        public void OnRemoteAdminCommand(ref RACommandEvent ev)
        {
            string[] query = ev.Command.Split(' ');
            switch (query[0].ToLower())
            {
                case "addspeaker":
                    bool contains = false;
                    foreach (var speak in speakers)
                    {
                        if (speak.id == int.Parse(query[1]))
                        {
                            contains = true;
                            speak.x = float.Parse(query[2]);
                            speak.y = float.Parse(query[3]);
                            speak.z = float.Parse(query[4]);
                            speak.url = query[5];
                            break;
                        }
                    }
                    if (!contains)
                    {
                        speakers.Add(new Speaker()
                        {
                            id = int.Parse(query[1]),
                            x = float.Parse(query[2]),
                            y = float.Parse(query[3]),
                            z = float.Parse(query[4]),
                            url = query[5]
                        });
                    }
                    foreach (var speak in speakers)
                    {
                        NetDataWriter writer = new NetDataWriter();
                        writer.Put(1);
                        writer.Put(speak.id);
                        writer.Put(speak.x);
                        writer.Put(speak.y);
                        writer.Put(speak.z);
                        writer.Put(speak.url);
                        server.SendToAll(writer, DeliveryMethod.ReliableOrdered);
                    }
                    break;
                case "removespeaker":
                    foreach (var speak in speakers)
                    {
                        if (speak.id == int.Parse(query[1]))
                        {
                            NetDataWriter writer = new NetDataWriter();
                            writer.Put(2);
                            writer.Put(speak.id);
                            server.SendToAll(writer, DeliveryMethod.ReliableOrdered);
                            break;
                        }
                    }
                    break;
            }
        }

        private void Listener_PeerDisconnectedEvent(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            ServerConsole.AddLog("New client disconnected " + peer.EndPoint.ToString() + ", ID: " + peer.Id + ", REASON: " + disconnectInfo.Reason.ToString());
        }

        private void Listener_PeerConnectedEvent(NetPeer peer)
        {
            ServerConsole.AddLog("New client connected " + peer.EndPoint.ToString() + " ID " + peer.Id);
            foreach (var speak in speakers)
            {
                NetDataWriter writer = new NetDataWriter();
                writer.Put(1);
                writer.Put(speak.id);
                writer.Put(speak.x);
                writer.Put(speak.y);
                writer.Put(speak.z);
                writer.Put(speak.url);
                peer.Send(writer, DeliveryMethod.ReliableOrdered);
            }
        }

        private void Listener_ConnectionRequestEvent(ConnectionRequest request)
        {
            ServerConsole.AddLog("Accepted connection from " + request.RemoteEndPoint.ToString());
            request.Accept();
        }
    }
}
