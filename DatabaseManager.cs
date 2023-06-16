using System;
using MySql.Data.MySqlClient;
using System.Data;
using System.Collections.Generic;

namespace Server
{
    class DatabaseManager
    {
        private object checkCorrectLoginInfoLockObject = new object();
        private object checkCorrectSignupInfoLockObject = new object();
        private object addPlayerInfoLockObject = new object();
        const string strConn = "Server=localhost;Database=ckgame;Uid=root;Pwd=root;";


        //입력받은 로그인 정보 맞는지 확인
        public bool CheckCorrectLoginInfo(string id, string password)
        {
            DataSet dataSet = new DataSet();

            lock (checkCorrectLoginInfoLockObject)
            {
                try
                {
                    using (MySqlConnection conn = new MySqlConnection(strConn))
                    {
                        if (conn == null)
                        {
                            Console.WriteLine("DB가 연결되어있지 않습니다.");
                            return false;
                        }

                        string commandString = $"select * from player_table where id = '{id}' and pw = '{password}'";
                        MySqlDataAdapter adapter = new MySqlDataAdapter(commandString, conn);
                        adapter.Fill(dataSet);
                    }
                }
                catch (Exception e)
                {
                    return false;
                }

                if (dataSet.Tables[0].Rows.Count <= 0)
                {
                    Console.WriteLine(dataSet.Tables[0].Rows.Count);
                    return false;
                }

                return true;
            }
        }

        //아이디 중복 확인
        public bool CheckCorrectSignupInfo(string id, string password)
        {
            DataSet dataSet = new DataSet();

            lock (checkCorrectSignupInfoLockObject)
            {

                try
                {
                    using (MySqlConnection conn = new MySqlConnection(strConn))
                    {
                        if (conn == null)
                        {
                            Console.WriteLine("DB가 연결되어있지 않습니다.");
                            return false;
                        }

                        string commandString = $"select * from player_table where id = '{id}'";
                        MySqlDataAdapter adapter = new MySqlDataAdapter(commandString, conn);
                        adapter.Fill(dataSet);
                    }
                }
                catch (Exception e)
                {
                    return false;
                }

                if (dataSet.Tables[0].Rows.Count > 0)
                {
                    return false;
                }

                return true;
            }
        }

        //플레이어 계정 추가
        public void AddPlayerInfo(string id, string password)
        {
            lock (addPlayerInfoLockObject)
            {
                using (MySqlConnection conn = new MySqlConnection(strConn))
                {
                    try
                    {
                        if (conn == null)
                        {
                            Console.WriteLine("DB가 연결되어있지 않습니다.");
                            return;
                        }

                        string commandString = $"insert into player_table values('{id}', '{password}')";

                        conn.Open();
                        MySqlCommand mySqlCommand = new MySqlCommand(commandString, conn);
                        mySqlCommand.ExecuteNonQuery();
                    }
                    catch (Exception e)
                    {
                        return;
                    }
                }
            }
        }

        //플레이어 기록 갱신
        public void UpdatPlayerScore(string id, float distance)
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(strConn))
                {
                    if (conn == null)
                    {
                        Console.WriteLine("DB가 연결되어있지 않습니다.");
                        return;
                    }

                    Console.WriteLine("점수 업데이트");
                    string commandString = $"insert into score_table values('{id}',{distance})";

                    conn.Open();
                    MySqlCommand mySqlCommand = new MySqlCommand(commandString, conn);
                    mySqlCommand.ExecuteNonQuery();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return;
            }
        }

        public List<Tuple<string, float>> GetRankInfoFromDB()
        {
            DataSet dataSet = new DataSet();
            List<Tuple<string, float>> rankList = new List<Tuple<string, float>>();

            lock (checkCorrectSignupInfoLockObject)
            {
                try
                {
                    using (MySqlConnection conn = new MySqlConnection(strConn))
                    {
                        if (conn == null)
                        {
                            Console.WriteLine("DB가 연결되어있지 않습니다.");
                            return null;
                        }

                        string commandString = $"select * from score_table order by score";
                        MySqlDataAdapter adapter = new MySqlDataAdapter(commandString, conn);
                        adapter.Fill(dataSet);
                    }
                }
                catch (Exception e)
                {
                    return null;
                }

                if (dataSet.Tables[0].Rows.Count <= 0)
                {
                    return null;
                }

                foreach(DataRow r in dataSet.Tables[0].Rows)
                {
                    Tuple<string, float> playerInfo = new Tuple<string, float>(r["id"].ToString(), float.Parse(r["score"].ToString()));
                    rankList.Add(playerInfo);
                }

                return rankList;
            }
        }
    }
}