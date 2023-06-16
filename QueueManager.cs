using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using PacketPackage;

namespace Server
{
    class QueueManager
    {
        const int matchPlayerCount = 2;
        private object addPlayerLockObject = new object();
        private object removePlayerLockObject = new object();
        private List<Tuple<TcpClient, string>> playerQueue = new List<Tuple<TcpClient, string>>();

        public void AddPlayer(TcpClient addingPlayer, string id)
        {
            lock(addPlayerLockObject)
            {
                if (addingPlayer == null)
                {
                    Console.WriteLine("삭제할 플레이어가 null입니다.");
                    return;
                }
                Tuple<TcpClient, string> playerInfo = new Tuple<TcpClient, string>(addingPlayer, id);
                playerQueue.Add(playerInfo);

                if (playerQueue.Count >= matchPlayerCount)
                {
                    List<Tuple<TcpClient, string>> playerList = new List<Tuple<TcpClient, string>>();

                    for (int i = 0; i < matchPlayerCount; i++)
                    {
                        Tuple<TcpClient, string> client = playerQueue[0];
                        playerList.Add(client);
                        Server.SendPacket(client.Item1, new MatchSuccessPacket());
                        playerQueue.RemoveAt(0);
                    }
                    Server.MakeRoom(playerList);
                }
                Console.WriteLine("클라이언트 매치 등록");
            }
        }

        public void RemovePlayer(TcpClient removingPlayer, string id)
        {
            lock (removePlayerLockObject)
            {
                if (removingPlayer == null)
                {
                    Console.WriteLine("삭제할 플레이어가 null입니다.");
                    return;
                }

                Tuple<TcpClient, string> player = new Tuple<TcpClient, string>(removingPlayer, id);
                playerQueue.Remove(player);

                Console.WriteLine("클라이언트 매치 캔슬");
            }
        }
    }
}
