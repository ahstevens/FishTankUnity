using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace ImageStreamServer
{
    public class Client
    {
        public TcpClient client;
        public BinaryWriter writer;
        public Client(TcpClient aClient)
        {
            client = aClient;
            writer = new BinaryWriter(client.GetStream());
        }
        public bool SendImageData(byte[] aData)
        {
            if (!client.Connected)
                return false;
            writer.Write(aData.Length);
            writer.Write(aData);
            return true;
        }
    }

    [RequireComponent(typeof(Camera))]
    class RenderTextureStreamServer : MonoBehaviour
    {
        public int port = 1234;
        public float streamRate = 0.05f;
        public bool sendAsPNG = false;

        public int renderWidth = 1920;
        public int renderHeight = 1080;

        Camera streamingCam = null;

        TcpListener m_Server;
        bool m_ServerRunning;
        Thread m_ListenThread;

        bool m_Sending;
        Thread m_SendingThread;
        List<Client> m_Clients = new List<Client>();

        byte[] bytes;

        void ListenThread()
        {
            m_Server = new TcpListener(IPAddress.Any, port);
            m_Server.Start();
            m_ServerRunning = true;
            while (m_ServerRunning)
            {
                try
                {
                    var newClient = m_Server.AcceptTcpClient();
                    lock (m_Clients)
                    {
                        m_Clients.Add(new Client(newClient));
                    }
                }
                catch (Exception e)
                {
                    Console.Write(e.Message);
                }
            }
            lock(m_Clients)
            {
                foreach(var c in m_Clients)
                {
                    try
                    {
                        c.client.Close();
                    }
                    catch
                    {
                    }
                }
                m_Clients.Clear();
            }
        }

        class FileItem
        {
            public string name;
            public byte[] data;
        }

        void SendThread()
        {
            m_Sending = true;
            while (m_Sending)
            {
                Thread.Sleep(50);

                Client[] clients;
                lock(m_Clients)
                {
                    clients = m_Clients.ToArray();
                }
                foreach (var client in clients)
                {
                    bool success = false;
                    try
                    {
                        success = client.SendImageData(bytes);
                    }
                    catch
                    {
                        success = false;
                        client.client.Close();
                    }
                    finally
                    {
                        if (!success)
                        {
                            lock (m_Clients)
                            {
                                m_Clients.Remove(client);
                            }
                        }
                    }
                }
            }
        }

        private void Start()
        {
            m_ListenThread = new Thread(ListenThread);
            m_ListenThread.Start();

            m_SendingThread = new Thread(SendThread);
            m_SendingThread.Start();

            streamingCam = GetComponent<Camera>();

            if (streamingCam.targetTexture == null)
                streamingCam.targetTexture = new RenderTexture(renderWidth, renderHeight, 0);
        }

        private void Update()
        {
        }

        public void SendCameraImage()
        {
            Texture2D tex = new Texture2D(streamingCam.targetTexture.width, streamingCam.targetTexture.height, TextureFormat.RGB24, false);
            RenderTexture.active = streamingCam.targetTexture;
            tex.ReadPixels(new Rect(0, 0, streamingCam.targetTexture.width, streamingCam.targetTexture.height), 0, 0);
            tex.Apply();

            bytes = sendAsPNG ? tex.EncodeToPNG() : tex.EncodeToJPG();
            Destroy(tex);
        }

        private void OnEnable()
        {
            InvokeRepeating("SendCameraImage", 0, streamRate);
        }

        private void OnDisable()
        {
            CancelInvoke("SendCameraImage");
        }

        private void OnDestroy()
        {
            m_Sending = false;
            m_ServerRunning = false;
            m_Server.Stop();
            m_ListenThread.Join();
            m_SendingThread.Join();
        }
    }
}