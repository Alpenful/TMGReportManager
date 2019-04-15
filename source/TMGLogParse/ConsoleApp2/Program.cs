using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp2
{
    class Program
    {
        static void Main(string[] args)
        {
            DateTime focusTime = DateTime.Now.AddDays(-1);
            string hashDate = focusTime.ToString("yyyy.MM.dd");
            string[] hashDateTwo = hashDate.Split('.');
            hashDate = hashDateTwo[0] + hashDateTwo[1] + hashDateTwo[2];
            string fileName = args[0] + "\\ISALOG_" + hashDate + "_WEB_000.w3c";

            string connStr = "[ИМЯ СЕРВЕРА MYSQL];user=[ИМЯ ПОЛЬЗОВАТЕЛЯ];database=tmgreport;password=[ПАРОЛЬ ПОЛЬЗОВАТЕЛЯ];";
            MySqlConnection conn = new MySqlConnection(connStr);
            int count =0;

            List<string> targetFilter = new List<string>();
            List<string> clientFilter = new List<string>();
            List<string[]> filter = new List<string[]>();
            int counterror = 0;

            try
            {   
                conn.Open();
                Console.WriteLine("Успешно соединился с базой данных!");
            }
            catch
            {
                Console.WriteLine("Не удалось соединиться с базой данных");
                Console.ReadLine();
            }

            try
            {
                string sqltargetFilter = "SELECT * FROM tmgreport.targetfilter";
                MySqlCommand readtargetFilter = new MySqlCommand(sqltargetFilter, conn);
                MySqlDataReader readertargetFilter = readtargetFilter.ExecuteReader();
                while (readertargetFilter.Read())
                {
                    targetFilter.Add(readertargetFilter[1].ToString());
                }
                readertargetFilter.Close();

                string sqlclientFilter = "SELECT * FROM tmgreport.clientfilter";
                MySqlCommand readclientFilter = new MySqlCommand(sqlclientFilter, conn);
                MySqlDataReader readerclientFilterr = readclientFilter.ExecuteReader();
                while (readerclientFilterr.Read())
                {
                    clientFilter.Add(readerclientFilterr[1].ToString());
                }
                readerclientFilterr.Close();

                string sqlfilter = "SELECT * FROM tmgreport.filter";
                MySqlCommand readfilter = new MySqlCommand(sqlfilter, conn);
                MySqlDataReader readerfilter = readfilter.ExecuteReader();
                while (readerfilter.Read())
                {
                    string[] filt = new string[]
                    {
                    readerfilter[1].ToString(),
                    readerfilter[2].ToString()
                    };
                    filter.Add(filt);
                }
                readerfilter.Close();

                List<string> readLog = new List<string>();
                MySqlCommand clearDayLog = new MySqlCommand("TRUNCATE tmgreport.day_logs", conn);
                clearDayLog.ExecuteNonQuery();

                Console.WriteLine(Environment.CurrentDirectory);

                Console.WriteLine("Начинаю парсить");
                using (StreamReader sr = new StreamReader(fileName, System.Text.Encoding.UTF8))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        readLog.Add(line);
                    }
                    sr.Close(); //и закрываем ридер
                    Console.WriteLine($"В коллекции {readLog.Count} строк");
                }

                try
                {
                    for (int i = 0; i < readLog.Count; i++)
                    {
                    try
                    {
                        if (i > 3)
                            {
                                string[] vs = readLog[i].Split('\t');
                                bool ready = WriteReady(vs, clientFilter, targetFilter, filter);

                                if (ready)
                                {
                                    string sqlExpression = "INSERT INTO day_logs (host_ip, client_user_name, target_host,target_path, Date, upload, download) VALUES (@host_ip, @client_user_name, @target_host,@target_path, @Date, @upload, @download)";

                                    MySqlCommand command = new MySqlCommand(sqlExpression, conn);
                                    // создаем параметр для имени
                                    MySqlParameter hostIP = new MySqlParameter("@host_ip", vs[0]);
                                    MySqlParameter clientUserName = new MySqlParameter("@client_user_name", vs[1]);
                                    DateTime dt = DateTime.Parse(vs[3] + " " + vs[4]);
                                    MySqlParameter date = new MySqlParameter("@Date", dt.ToString("yyyy-MM-dd"));
                                    string[] target = vs[15].Split('/');
                                    MySqlParameter targetHost;
                                    if (vs[9] == "443")
                                    {
                                        targetHost = new MySqlParameter("@target_host", target[0]);
                                    }
                                    else
                                    {
                                        try
                                        {
                                            targetHost = new MySqlParameter("@target_host", target[2]);
                                        }
                                        catch
                                        {
                                            try
                                            {
                                                targetHost = new MySqlParameter("@target_host", target[1]);
                                            }
                                            catch
                                            {
                                                targetHost = new MySqlParameter("@target_host", target[0]);
                                            }
                                        }
                                    }
                                    MySqlParameter targetPath = new MySqlParameter("@target_path", vs[15]);
                                    MySqlParameter upload = new MySqlParameter("@upload", vs[11]);
                                    MySqlParameter dowload = new MySqlParameter("@download", vs[12]);
                                    // добавляем параметры к команде
                                    command.Parameters.Add(hostIP);
                                    command.Parameters.Add(clientUserName);
                                    command.Parameters.Add(targetHost);
                                    command.Parameters.Add(targetPath);
                                    command.Parameters.Add(date);
                                    command.Parameters.Add(upload);
                                    command.Parameters.Add(dowload);

                                    command.ExecuteNonQuery();
                                    count++;
                                }
                            }
                        }
                    catch (Exception ex)
                        {
                            Console.WriteLine(ex);
                        }
                    }       
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Ошибка добавления записи в day_logs");
                    Console.WriteLine(ex);
                    counterror++;
                }

                readLog = new List<string>();

                Console.WriteLine($"Добавлено в дневную базу {count} записей.");
                List<string> readclient = new List<string>();
                string sqlread = "SELECT DISTINCT client_user_name FROM tmgreport.day_logs";
                MySqlCommand readsqlclient = new MySqlCommand(sqlread, conn);
                MySqlDataReader reader = readsqlclient.ExecuteReader();
                while (reader.Read())
                {
                    readclient.Add(reader[0].ToString());
                }

                reader.Close();

                foreach (var item in readclient)
                {
                    try
                    {
                        string[] chapter = item.Split('\\');
                        string search = chapter[0] + @"\\" + chapter[1];
                        Console.WriteLine($"Хосты пользователя {search}");
                        string sqlreadFor = $"SELECT DISTINCT target_host FROM tmgreport.day_logs WHERE client_user_name=\"{search}\"";
                        List<string> target = new List<string>();
                        Console.WriteLine(sqlreadFor);
                        MySqlCommand targetcommand = new MySqlCommand(sqlreadFor, conn);
                        MySqlDataReader targetreader = targetcommand.ExecuteReader();
                        while (targetreader.Read())
                        {
                            target.Add(targetreader[0].ToString());
                        }

                        targetreader.Close();

                        foreach (var host in target)
                        {
                            string uploadstr = $"SELECT sum(upload), sum(download) FROM tmgreport.day_logs where target_host = \"{host}\" and client_user_name=\"{search}\"";
                            MySqlCommand uploadcomm = new MySqlCommand(uploadstr, conn);

                            MySqlDataReader uploadreader = uploadcomm.ExecuteReader();
                            decimal resultupload = 0;
                            decimal resultdownload = 0;

                            while (uploadreader.Read())
                            {
                                resultupload = decimal.Parse(uploadreader[0].ToString());
                                resultdownload = decimal.Parse(uploadreader[1].ToString());
                            }
                            uploadreader.Close();

                            string logcreatestr = "INSERT INTO logs (client_user_name, target_host, Date, upload, download) VALUES (@client_user_name, @target_host, @Date, @upload, @download)";
                            MySqlCommand command = new MySqlCommand(logcreatestr, conn);
                            MySqlParameter clientUserName = new MySqlParameter("@client_user_name", item);
                            MySqlParameter date = new MySqlParameter("@Date", focusTime);
                            MySqlParameter targetHost = new MySqlParameter("@target_host", host);
                            MySqlParameter uploadcommand = new MySqlParameter("@upload", resultupload);
                            MySqlParameter dowloadcommand = new MySqlParameter("@download", resultdownload);
                            // добавляем параметры к команде
                            command.Parameters.Add(clientUserName);
                            command.Parameters.Add(targetHost);
                            command.Parameters.Add(date);
                            command.Parameters.Add(uploadcommand);
                            command.Parameters.Add(dowloadcommand);

                            command.ExecuteNonQuery();
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Ошибка добавления записи в logs");
                        Console.WriteLine(ex);
                        counterror++;
                    }

                    try
                    {
                        string[] chapter = item.Split('\\');
                        string search = chapter[0] + @"\\" + chapter[1];
                        string dayresultstr = $"SELECT sum(upload), sum(download) FROM tmgreport.logs where Date = \"{focusTime.ToString("yyyy.MM.dd")}\" and client_user_name=\"{search}\"";
                        MySqlCommand daycomm = new MySqlCommand(dayresultstr, conn);

                        MySqlDataReader dayreader = daycomm.ExecuteReader();
                        string dayupload = "";
                        string daydownload = "";

                        while (dayreader.Read())
                        {
                            dayupload = dayreader[0].ToString();
                            daydownload = dayreader[1].ToString();
                        }
                        dayreader.Close();

                        string daycreatestr = "INSERT INTO day_result (day, user, upload, download) VALUES (@day, @user, @upload, @download)";
                        MySqlCommand daycommand = new MySqlCommand(daycreatestr, conn);
                        MySqlParameter day = new MySqlParameter("@day", focusTime);
                        MySqlParameter user = new MySqlParameter("@user", item);
                        MySqlParameter dayuploadcom = new MySqlParameter("@upload", dayupload);
                        MySqlParameter daydownloadcom = new MySqlParameter("@download", daydownload);
                        // добавляем параметры к команде
                        daycommand.Parameters.Add(day);
                        daycommand.Parameters.Add(user);
                        daycommand.Parameters.Add(dayuploadcom);
                        daycommand.Parameters.Add(daydownloadcom);

                        daycommand.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Ошибка добавления записи в day_rezult");
                        Console.WriteLine(ex);
                        counterror++;
                    }

                }
                Console.WriteLine($"{readclient.Count} пользователей интернета");
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex);
                Console.ReadKey();
            }

            if(counterror > 0)
            {
                Console.WriteLine($"Ошибок при добавлении в базу за {focusTime.ToString("D")} = {counterror}");
                //Console.ReadKey();
            }

        }

        static bool WriteReady(string[] vs, List<string> clientFilter, List<string> targetFilter, List<string[]> filter)
        {
            string action = "Allowed";
            try
            {
                if (vs[24] == action)
                {
                    if (vs[1] != "anonymous")
                    {
                        for (int i = 0; i < clientFilter.Count; i++)
                        {
                            if (vs[2] == clientFilter[i]) { return false; }
                        }

                        for (int i = 0; i < targetFilter.Count; i++)
                        {
                            if (vs[15] == targetFilter[i]) { return false; }
                        }

                        //for (int i = 0; i < filter.Count; i++)
                        //{
                        //    if (vs[1] == filter[i][0])
                        //    {
                        //        string[] target = vs[15].Split('/');
                        //        string targetHost = "";
                        //        if (vs[9] == "443")
                        //        {
                        //            targetHost = target[0];
                        //        }
                        //        else
                        //        {
                        //            try
                        //            {
                        //                targetHost = target[2];
                        //            }
                        //            catch
                        //            {
                        //                try
                        //                {
                        //                    targetHost = target[1];
                        //                }
                        //                catch
                        //                {
                        //                    targetHost = target[0];
                        //                }
                        //            }
                        //        }

                        //        if (targetHost == filter[i][1]) { return false; }
                        //    }
                        //}

                        return true;
                    }
                    else return false;
                }
            
            else return false;
            }
            catch
            {
                return false;
            }
        }

    }
}
