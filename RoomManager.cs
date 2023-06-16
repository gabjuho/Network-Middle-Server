using System.Collections.Generic;
using System.Net.Sockets;
using PacketPackage;
using System;

namespace Server
{
    class RoomManager
    {
        Dictionary<int, Room> roomList = new Dictionary<int, Room>();
        int roomIDCount = 1000;

        public void AddRoom(List<Tuple<TcpClient, string>> playerList)
        {
            Room room = new Room(roomIDCount, playerList);
            roomList.Add(roomIDCount, room);
            roomIDCount++;
        }
        public void RemoveRoom(int roomID)
        {

        }

        public Room GetRoom(int roomID)
        {
            return roomList[roomID];
        }
    }
    class Room
    {
        List<Tuple<TcpClient,string>> playerList = new List<Tuple<TcpClient, string>>();
        Dictionary<string, float> playerRecord = new Dictionary<string, float>();
        int roomID;
        int count = 0;
        const int maxPlayerCount = 2;
        object lockObject = new object();

        public Room(int roomID, List<Tuple<TcpClient, string>> playerList)
        {
            this.playerList = playerList;
            this.roomID = roomID;

            foreach (var client in playerList)
            {
                Server.SendPacket(client.Item1, new RoomIDPacket(roomID));
            }
            Console.WriteLine($"Room {roomID} 생성");
        }
        
        public void SendDistancePacketInRoomPlayer(TcpClient client, float swipeLength)
        {
            foreach (var player in playerList)
            {
                if (client != player.Item1)
                {
                    Server.SendPacket(player.Item1, new UpdateMovePacket(swipeLength));
                }
            }
        }

        public void ExecuteGameOver(TcpClient client, float distance)
        {
            lock (lockObject)
            {
                count++;

                foreach(var player in playerList)
                {
                    if(player.Item1 == client)
                    {
                        playerRecord.Add(player.Item2, distance);
                        if(distance != -1)
                        {
                            Server.databaseManager.UpdatPlayerScore(player.Item2, distance);
                        }
                        break;
                    }
                }

                if(count == maxPlayerCount)
                {
                    SendMatchResult();
                }
            }
        }

        public void SendMatchResult()
        {
            string winner = "";
            float min = float.MaxValue;
            foreach(var player in playerRecord)
            {
                if(min > player.Value || player.Value != -1)
                {
                    winner = player.Key;
                    min = player.Value;
                }
            }

            if (min == float.MaxValue)
            {
                winner = "None";
                min = -1;
            }

            foreach (var client in playerList)
            {
                Server.SendPacket(client.Item1, new MatchResultPacket(winner, min));
            }
        }
    }
}