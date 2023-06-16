using System;
using System.Collections.Generic;
// 다음을 추가 합니다.
using System.Net;
using System.Net.Sockets;
using System.Threading;
using ProtoBuf;
using PacketPackage;
using System.IO;

namespace Server
{
    class Server
    {
        public static RSAManager rsaManager = new RSAManager();
        public static DatabaseManager databaseManager = new DatabaseManager();
        static QueueManager queueManager = new QueueManager();
        static RoomManager roomManager = new RoomManager();
        const int serverPort = 1001;
        static Dictionary<int, Action<TcpClient, Packet>> packetDictionary = new Dictionary<int, Action<TcpClient, Packet>>
        {
            {0, ExecuteLoginPacket },
            {1, ExecuteSignupPacket},
            {4, ExecuteMatchMakingPacket},
            {5, ExecuteMatchCancelingPacket },
            {8, ExecuteMovePacket },
            {10, ExecuteGameOverPacket },
            {12, ExecuteRankRequestPacket }
        };

        static void Main(string[] args)
        {
            // 서버 소켓을 별도 스레드로 동작시킵니다.
            Thread serverThread = new Thread(serverFunc);
            serverThread.IsBackground = true;
            serverThread.Start();

            Thread.Sleep(500);
            Console.WriteLine(" *** 자동차 게임을 위한 게임 서버 입니다.  ***");
            Console.WriteLine(" 서버를 종료하려면 아무 키나 누르세요");

            Console.ReadLine();
            serverThread.Abort();
            Console.WriteLine(" 서버가 종료되었습니다.");
        }

        private static void serverFunc(object obj)
        {
            TcpListener listener = new TcpListener(IPAddress.Any, serverPort);

            listener.Start();

            byte[] recBytes = new byte[1024];

            while (true)
            {
                TcpClient client = listener.AcceptTcpClient();

                SendPacket(client, new PublicKeyPacket(rsaManager.PublicKeyText));

                Thread thread = new Thread(ReceivePacket);
                thread.Start(client);
            }
        }

        private static void ReceivePacket(object obj)
        {
            Console.WriteLine("클라이언트 한명 접속");
            TcpClient client = (TcpClient)obj;
            NetworkStream networkStream = client.GetStream();

            while(true)
            {
                byte[] headBuffer = new byte[4];
                int ret = networkStream.Read(headBuffer, 0, headBuffer.Length);

                if(ret == 0)
                {
                    client.Close();
                    Console.WriteLine("클라이언트와의 접속이 끊겼습니다.");
                    return;
                }

                int head = BytesToInt(headBuffer);

                byte[] bodyLengthBuffer = new byte[4];

                networkStream.Read(bodyLengthBuffer, 0, bodyLengthBuffer.Length);

                int bodyLength = BytesToInt(bodyLengthBuffer);

                byte[] bodyBuffer = new byte[bodyLength];
                networkStream.Read(bodyBuffer, 0, bodyBuffer.Length);
                Packet packet;

                using (MemoryStream memoryStream = new MemoryStream(bodyBuffer))
                {
                    packet = Serializer.Deserialize<Packet>(memoryStream);
                }

                packetDictionary[head](client, packet);
            }
        }

        public static void SendPacket(TcpClient client, Packet packet)
        {
            SendHeadPacket(client, packet);
            SendBodyPacket(client, packet);
        }

        private static void SendHeadPacket(TcpClient client, Packet packet)
        {
            byte[] headBuffer = IntToBytes(packet.Head);

            NetworkStream networkStream = client.GetStream();
            networkStream.Write(headBuffer, 0, headBuffer.Length);
        }
        private static void SendBodyPacket(TcpClient client, Packet packet)
        {
            int bodyLength;

            using (MemoryStream memoryStream = new MemoryStream())
            {
                Serializer.Serialize(memoryStream, packet);
                var buffer = memoryStream.ToArray();
                bodyLength = buffer.Length;
            }

            byte[] bodyLengthBuffer = IntToBytes(bodyLength);
            NetworkStream networkStream = client.GetStream();

            networkStream.Write(bodyLengthBuffer, 0, bodyLengthBuffer.Length);

            using (MemoryStream memoryStream = new MemoryStream())
            {
                Serializer.Serialize(memoryStream, packet);
                var buffer = memoryStream.ToArray();
                networkStream.Write(buffer, 0, buffer.Length);
            }
        }

        private static void ExecuteLoginPacket(TcpClient client, Packet packet)
        {
            LoginPacket loginPacket = packet as LoginPacket;

            Console.WriteLine("id: " + loginPacket.ID + " password: " + loginPacket.Password);

            string descryptedID = rsaManager.RSADecrypt(loginPacket.ID, rsaManager.PrivateKeyText);
            string descryptedPassword = rsaManager.RSADecrypt(loginPacket.Password, rsaManager.PrivateKeyText);

            Console.WriteLine("descrypted id: " + descryptedID + " descrypted password: " + descryptedPassword);

            bool isCorrectLoginInfo = databaseManager.CheckCorrectLoginInfo(descryptedID, descryptedPassword);

            Console.WriteLine(isCorrectLoginInfo);

            SendPacket(client, new CheckLoginInfoPacket(isCorrectLoginInfo));
        }

        private static void ExecuteSignupPacket(TcpClient client, Packet packet)
        {
            SignupPacket loginPacket = packet as SignupPacket;

            Console.WriteLine("id: " + loginPacket.ID + " password: " + loginPacket.Password);

            string descryptedID = rsaManager.RSADecrypt(loginPacket.ID, rsaManager.PrivateKeyText);
            string descryptedPassword = rsaManager.RSADecrypt(loginPacket.Password, rsaManager.PrivateKeyText);

            Console.WriteLine("descrypted id: " + descryptedID + " descrypted password: " + descryptedPassword);

            bool isCorrectSignupInfo = databaseManager.CheckCorrectSignupInfo(descryptedID, descryptedPassword);

            if(isCorrectSignupInfo == true)
            {
                databaseManager.AddPlayerInfo(descryptedID, descryptedPassword);
            }

            SendPacket(client, new CheckSignupInfoPacket(isCorrectSignupInfo));
        }
        private static void ExecuteMatchMakingPacket(TcpClient client, Packet packet)
        {
            MatchMakingPacket matchMakingPacket = packet as MatchMakingPacket;
            queueManager.AddPlayer(client, matchMakingPacket.ID);
        }
        private static void ExecuteMatchCancelingPacket(TcpClient client, Packet packet)
        {
            MatchCancelingPacket matchCancelingPacket = packet as MatchCancelingPacket;
            queueManager.RemovePlayer(client, matchCancelingPacket.ID);
        }
        private static void ExecuteMovePacket(TcpClient client, Packet packet)
        {
            MovePacket movePacket = packet as MovePacket;
            roomManager.GetRoom(movePacket.RoomID).SendDistancePacketInRoomPlayer(client, movePacket.SwipeLength);
        }
        private static void ExecuteGameOverPacket(TcpClient client, Packet packet)
        {
            GameOverPacket gameOverPacket = packet as GameOverPacket;
            roomManager.GetRoom(gameOverPacket.RoomID).ExecuteGameOver(client, gameOverPacket.Distance);
        }
        private static void ExecuteRankRequestPacket(TcpClient client, Packet packet)
        {
            SendPacket(client, new RankPacket(databaseManager.GetRankInfoFromDB()));
        }

        public static void MakeRoom(List<Tuple<TcpClient,string>> list)
        {
            roomManager.AddRoom(list);
        }
        static byte[] IntToBytes(int n)
        {
            byte[] arr = new byte[4];
            arr[0] = (byte)(n & 0xff);
            arr[1] = (byte)((n >> 8) & 0xff);
            arr[2] = (byte)((n >> 16) & 0xff);
            arr[3] = (byte)((n >> 24) & 0xff);
            return arr;
        }
        static int BytesToInt(byte[] buf)
        {
            int num = (buf[0] & 0xff) | ((buf[1] & 0xff) << 8) | ((buf[2] & 0xff) << 16) | ((buf[3] & 0xff) << 24);
            return num;
        }
    }
}