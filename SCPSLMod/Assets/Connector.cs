using LiteNetLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class Connector : MonoBehaviour
{
    public Text status;
    public InputField field;
    public EventBasedNetListener listener;
    public NetManager client;
    public List<Speaker> speakers = new List<Speaker>();
    public GameObject defaultObj;

    public class Speaker
    {
        public int id { get; set; }
        public float x { get; set; }
        public float y { get; set; }
        public float z { get; set; }
        public string url { get; set; }
        public GameObject obj { get; set; }
    }


    private void Start()
    {
        listener = new EventBasedNetListener();
        listener.PeerConnectedEvent += Listener_PeerConnectedEvent;
        listener.PeerDisconnectedEvent += Listener_PeerDisconnectedEvent;
        listener.NetworkReceiveEvent += Listener_NetworkReceiveEvent;
        client = new NetManager(listener);
    }

    public IEnumerator DownloadAudioCli(GameObject obj, string url)
    {
        using (var www = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.OGGVORBIS))
        {
            www.SendWebRequest();
            while (!www.isDone) { }
            obj.GetComponent<AudioSource>().clip = DownloadHandlerAudioClip.GetContent(www);
            obj.GetComponent<AudioSource>().Play();
        }
        yield break;
    }

    private void Listener_NetworkReceiveEvent(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
    {
        switch(reader.GetInt())
        {
            case 0:
                Vector3 vec = new Vector3(reader.GetFloat(), reader.GetFloat(), reader.GetFloat());
                transform.position = vec;
                break;
            case 1:
                int id = reader.GetInt();
                float x = reader.GetFloat();
                float y = reader.GetFloat();
                float z = reader.GetFloat();
                string url = reader.GetString();
                foreach (var speak in speakers)
                {
                    if (speak.id == id)
                    {
                        speak.x = x;
                        speak.y = y;
                        speak.z = z;
                        speak.url = url;
                        StartCoroutine(DownloadAudioCli(speak.obj, url));
                        speak.obj.transform.position = new Vector3(x,y,z);
                        return;
                    }
                }
                var obj = Instantiate(defaultObj);
                obj.transform.position = new Vector3(x, y, z);
                StartCoroutine(DownloadAudioCli(obj, url));
                speakers.Add(new Speaker()
                {
                    id = id,
                    x = x,
                    y = y,
                    z = z,
                    url = url,
                    obj = obj
                });
                break;
            case 2:
                int id2 = reader.GetInt();
                foreach (var speak in speakers)
                {
                    if (speak.id == id2)
                    {
                        speakers.Remove(speak);
                    }
                }
                break;
        }
        
    }

    private void Listener_PeerDisconnectedEvent(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        Debug.Log("Peer disconnected " + peer.Id);
        status.text = "OFFLINE";
    }

    private void Listener_PeerConnectedEvent(NetPeer peer)
    {
        Debug.Log("Peer connected " + peer.Id);
        status.text = "ONLINE";
    }

    private void Update()
    {
        if (client.IsRunning)
            client.PollEvents();
    }

    public void Connect()
    {
        try
        {
            client.Start();
            client.Connect(field.text.Split(':')[0], int.Parse(field.text.Split(':')[1]), "key");
        }catch(Exception ex)
        {
            Debug.Log(ex.ToString());
        }
        Debug.Log("done");
    }
}
