﻿using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Controls;

namespace Client
{
    class MessageClient
    {
        public bool ShouldStop { get; private set; }

        private Socket socket;
        //private string ServerIP = "127.0.0.1";
        private string ServerIP = "2.56.212.56";
        private int port = 1998;
        private const int MAX_MESSAGE_BYTES = 10_000_000;
        private string prefix = "User";
        public ListBox chatbox;
        public bool canSend = true;

        public bool Connected()
        {
            try
            {
                bool part1 = socket.Poll(1000, SelectMode.SelectRead);
                bool part2 = (socket.Available == 0);
                if (part1 && part2)
                    return false;
                else
                    return true;
            }
            catch
            {
                return false;
            }
        }

        public void Connect()
        {
            try
            {
                socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(ServerIP), port);
                socket.Connect(endPoint);
                if (!Connected())
                {
                    Disconnect();
                }
            }
            catch
            {
                Console.WriteLine("Client couldn't connect");
                Connect();
            }

        }

        public void Send(IMessage message)
        {
            if (Connected())
            {
                SendImplementation(message.GetBytes());
            }
            else
            {
                Disconnect();
            }
        }

        public void Recieve()
        {
            if (Connected())
            {
                ReceiveImplementation();
            }
            else
            {
                Disconnect();
            }
        }

        private void SendImplementation(byte[] buffer)
        {
            if (canSend)
            {
                socket.Send(buffer, 0, buffer.Length, 0);
                canSend = false;
                Thread canSendThread = new Thread(CantSend);
                canSendThread.Start();
               
            }

        }

        private void CantSend()
        {
            Thread.Sleep(250);
            canSend = true;
        }

        private void ReceiveImplementation()
        {
            byte[] recvBuffer = new byte[MAX_MESSAGE_BYTES];
            int received = socket.Receive(recvBuffer, recvBuffer.Length, SocketFlags.None);
            if(received == 0)
            {
                return;
            }
            byte[] contentBuffer = new byte[received - 1];
            MessageType messageType = (MessageType)recvBuffer[0];
            Array.Copy(recvBuffer, 1, contentBuffer, 0, contentBuffer.Length);
            Console.WriteLine(messageType);
            try
            {
                IMessage message = messageType switch
                {
                    MessageType.String => StringMessage.ParseData(contentBuffer),
                    MessageType.Image => ImageMessage.ParseData(contentBuffer),
                    MessageType.Command => CommandMessage.ParseData(contentBuffer),
                    // default
                    _ => throw new Exception("Unrecognized message!")
                };



            if (message is StringMessage strMessage)
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    chatbox.Items.Insert(0, prefix + ": " + strMessage.Message);
                });
            else if (message is ImageMessage ImgMessage)
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    Image newImage = new Image();
                    newImage.Source = ImgMessage.Image;
                    chatbox.Items.Insert(0, newImage);
                });
            else if (message is CommandMessage commandMessage)
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    checkCommand(commandMessage.parameters);
                });
            }
            
            catch
            {

            }
            // TODO: ViewModel gamer
        }
        private void checkCommand(string[] tokens)
        {

            switch (tokens[0])
            {
                case (string)Command.setUserName:
                    prefix = tokens[1];
                    chatbox.Items.Insert(0, "New username: " + prefix);
                    break;

                case (string)Command.clear:
                    chatbox.Items.Clear();
                    break;

                case (string)Command.setIp:
                    ServerIP = tokens[1];
                    chatbox.Items.Insert(0, "New ip address: " + ServerIP);
                    break;

                case (string)Command.help:
                  
                    break;

                default:
                    chatbox.Items.Insert(0, "unknown command: " + tokens[0]);
                    break;
            }
        }


        public void Disconnect()
        {
            if (socket != null)
            {
                ShouldStop = true;
                Console.WriteLine("Disonnecting");
                socket.Close();
            }
        }
    }
}
