using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.DirectoryServices;


/*
 *     Первоначально, при помощи свойства авторизации Windows на IIS сервера получаем логин и пароль пользователя и сравниваем его с таблицей users в базе данных. 
 *   Если такой есть, получаем его статус, ФИО в домене AD и формируем контроллеры в связи с его доступом.
 *   Выясняем, каким способом вызвана страница, GET или POST с помощью обработчика и выполняем следующие действия.
 *   Далее в зависимости от метода и параметров выполняются действия по загрузке данных в Web форму.
 * 
 *   Вывод реализован не на Grid только потому, что хотелось закрепить навыки в CSS/HTML верстке. 
 *      
 *   Проект сделан по большей части ради обучения автора, тапком не кидаться.
 * 
 */

namespace TMGReportManager
{
    public partial class Default : System.Web.UI.Page
    {
        bool access = false;
        bool adminAccess = false;
        bool superAccess = false;

        protected void Page_Load(object sender, EventArgs e)
        {
            string connStr = "[АДРЕС СЕРВЕРА MYSQL];user=[ВАШ ПОЛЬЗОВАТЕЛЬ];database=tmgreport;password=[ПАРОЛЬ ПОЛЬЗОВАТЕЛЯ];";
            MySqlConnection conn = new MySqlConnection(connStr);
            try
            {
                conn.Open();
                Console.WriteLine("Успешно соединился с базой данных!");
            }
            catch
            {
                Console.WriteLine("Не удалось соединиться с базой данных");
                return;
            }

            //Процесс авторизации
            string sqlread = "SELECT * FROM tmgreport.users";
            MySqlCommand readsqlclient = new MySqlCommand(sqlread, conn);
            MySqlDataReader reader = readsqlclient.ExecuteReader();
            while (reader.Read())
            {
                if (User.Identity.Name.ToString() == reader[1].ToString())
                {
                    access = true;
                    if(reader[2].ToString() == "admin" || reader[2].ToString() == "superuser")
                    {
                        adminAccess = true;
                        if(reader[2].ToString() == "superuser")
                        {
                            superAccess = true;
                        }
                    }
                }
            }
            reader.Close();

            if (access)
            {
                try
                {
                    string[] splituser = User.Identity.Name.Split('\\');
                    ResultLabel.Text = "Добро пожаловать, " + getUser(splituser[1], "io") + " !";
                }
                catch
                {

                }
                Body.Visible = true;
                filterListEnable.Visible = adminAccess;
                filterIcon.Visible = adminAccess;
                filter.Visible = superAccess;
                filterLabel.Visible = superAccess;
            }
            else
            {
                ResultLabel.Text = User.Identity.Name + ", доступ запрещен!";
                Body.Visible = false;
            }

            //Последнее обновление
            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            
            MySqlCommand lastupdatecomm = new MySqlCommand("SELECT DISTINCT day FROM tmgreport.day_result", conn);
            MySqlDataReader lastupdatereader = lastupdatecomm.ExecuteReader();
            DateTime result = new DateTime();
            while (lastupdatereader.Read())
            {
                result = DateTime.Parse(lastupdatereader[0].ToString());
            }
            lastupdatereader.Close();
            LastUpdateTime.Text = "Последнее обновление базы: " + result.ToString("D");

            ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            //Заполнение формы фильтров
            ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            if (!IsPostBack)
            {
                LoadFilterList(conn);
            

            //Заполнение дропдоуна с пользователями
            userDropDown.Items.Clear();
            userDropDown.Items.Add("Все пользователи");
            MySqlCommand getusercomm = new MySqlCommand("SELECT distinct user FROM tmgreport.day_result ORDER BY user;", conn);
            MySqlDataReader getuserreader = getusercomm.ExecuteReader();
            while (getuserreader.Read())
            {
                userDropDown.Items.Add(getuserreader[0].ToString());
            }
            getuserreader.Close();
            }

            //Обработка входящих параметров при загрузке страницы
            try
                {
                    string param = Request.QueryString["param"];
                switch (param)
                {
                    case null:
                        HttpCookie cookie = new HttpCookie("TMGReportCookieParam");
                        cookie["LastState"] = "default";
                        Response.Cookies.Add(cookie);
                        ViewState["LastKey"] = "SELECT distinct day FROM tmgreport.day_result ORDER BY day DESC LIMIT 30;";
                        ViewState["LastState"] = "default";
                        CreateListData("default", "SELECT distinct day FROM tmgreport.day_result ORDER BY day DESC LIMIT 30;");
                        break;
                    case "day":
                        string day = Request.QueryString["day"];
                        HttpCookie cookieday = new HttpCookie("TMGReportCookieDay");
                        cookieday["LastDay"] = day;
                        Response.Cookies.Add(cookieday);
                        ViewState["LastKey"] = $"SELECT * FROM tmgreport.day_result WHERE day=\"{day}\" ORDER BY download DESC;";
                        ViewState["LastState"] = "day";
                        CreateListData("day", $"SELECT * FROM tmgreport.day_result WHERE day=\"{day}\" ORDER BY download DESC;");
                        break;
                    case "user":
                        HttpCookie cookieuser = new HttpCookie("TMGReportCookieParam");
                        cookieuser["LastState"] = "day";
                        Response.Cookies.Add(cookieuser);
                        string dayuser = Request.QueryString["day"];
                        string item = Request.QueryString["user"];
                        string[] chapter = item.Split('\\');
                        string search = chapter[0] + @"\\" + chapter[1];
                        ViewState["LastKey"] = $"SELECT * FROM tmgreport.logs WHERE Date=\"{dayuser}\" AND client_user_name=\"{search}\" ORDER BY download DESC;";
                        ViewState["LastState"] = "user";
                        CreateListData("user", $"SELECT * FROM tmgreport.logs WHERE Date=\"{dayuser}\" AND client_user_name=\"{search}\" ORDER BY download DESC;");
                        break;
                    case "back":
                        HttpCookie cookieReq = Request.Cookies["TMGReportCookieParam"];
                        if (cookieReq != null)
                        {
                            string oldparam = cookieReq["LastState"];
                            switch (oldparam)
                            {
                                case "default":
                                    ViewState["LastKey"] = "SELECT distinct day FROM tmgreport.day_result ORDER BY day DESC LIMIT 30;";
                                    ViewState["LastState"] = "default";
                                    CreateListData(oldparam, "SELECT distinct day FROM tmgreport.day_result ORDER BY day DESC LIMIT 30;");
                                    break;
                                case "day":
                                    HttpCookie daycookie = Request.Cookies["TMGReportCookieDay"];
                                    if(daycookie != null)
                                    {
                                        string dayback = daycookie["LastDay"].ToString();
                                        HttpCookie cookiebackday = new HttpCookie("TMGReportCookieParam");
                                        cookiebackday["LastState"] = "default";
                                        Response.Cookies.Add(cookiebackday);
                                        ViewState["LastKey"] = $"SELECT * FROM tmgreport.day_result WHERE day=\"{dayback}\" ORDER BY download DESC;";
                                        ViewState["LastState"] = "day";
                                        CreateListData("day", $"SELECT * FROM tmgreport.day_result WHERE day=\"{dayback}\" ORDER BY download DESC;");
                                    }
                                    break;
                                default:
                                    break;
                            }
                        }
                        break;
                    case "sortedday":
                        string by = Request.QueryString["by"];
                        string dir = Request.QueryString["dir"];
                        string daysort = Request.QueryString["day"];
                        HttpCookie cookiesorted = new HttpCookie("TMGReportCookieSorted");
                        cookiesorted["key"] = by + "#" + dir;
                        Response.Cookies.Add(cookiesorted);
                        ViewState["LastKey"] = $"SELECT * FROM tmgreport.day_result WHERE day=\"{daysort}\" ORDER BY {by} {dir};";
                        ViewState["LastState"] = "sortedday";
                        CreateListData("sortedday", $"SELECT * FROM tmgreport.day_result WHERE day=\"{daysort}\" ORDER BY {by} {dir};");
                        break;
                    case "sorteduser":
                        string byuser = Request.QueryString["by"];
                        string diruser = Request.QueryString["dir"];
                        string daysortuser = Request.QueryString["day"];
                        string client = Request.QueryString["client"];
                        HttpCookie cookieusersort = new HttpCookie("TMGReportCookieParam");
                        cookieusersort["LastState"] = "day";
                        Response.Cookies.Add(cookieusersort);
                        string[] chaptersorted = client.Split('\\');
                        string searchsorted = chaptersorted[0] + @"\\" + chaptersorted[1];
                        ViewState["LastKey"] = $"SELECT * FROM tmgreport.logs WHERE Date=\"{daysortuser}\" AND client_user_name=\"{searchsorted}\" ORDER BY {byuser} {diruser};";
                        ViewState["LastState"] = "user";
                        CreateListData("usersorted", $"SELECT * FROM tmgreport.logs WHERE Date=\"{daysortuser}\" AND client_user_name=\"{searchsorted}\" ORDER BY {byuser} {diruser};");
                        break;
                    case "filterdefaultfull":
                        string day0 = Request.QueryString["day0"];
                        string day1 = Request.QueryString["day1"];
                        string userfilter = Request.QueryString["user"];
                        string[] cashuser = userfilter.Split('\\');
                        string filteruser = cashuser[0] + @"\\" + cashuser[1];
                        HttpCookie cookiefilter = new HttpCookie("TMGReportCookieParamFilter");
                        cookiefilter["LastStateFilter"] = "filterdefaultfull";
                        cookiefilter["LastStateFilterBack"] = "filterdefaultfull";
                        HttpCookie param01 = new HttpCookie("TMGReportCookieFilterParam");
                        param01["day0filter"] = day0;
                        param01["day1filter"] = day1;
                        param01["userfilter"] = userfilter;
                        Response.Cookies.Add(cookiefilter);
                        Response.Cookies.Add(param01);
                        ViewState["LastKey"] = $"SELECT distinct day FROM tmgreport.day_result WHERE user = \"{filteruser}\" AND day >= \"{day0}\" AND day <= \"{day1}\" ORDER BY day DESC;";
                        ViewState["LastState"] = "filterdefault";
                        CreateFilterListData("filterdefault", $"SELECT distinct day FROM tmgreport.day_result WHERE user = \"{filteruser}\" AND day >= \"{day0}\" AND day <= \"{day1}\" ORDER BY day DESC;");
                        break;
                    case "filterdefaultalluser":
                        string day02 = Request.QueryString["day0"];
                        string day12 = Request.QueryString["day1"];
                        HttpCookie cookiefilter2 = new HttpCookie("TMGReportCookieParamFilter");
                        cookiefilter2["LastStateFilter"] = "filterdefaultalluser";
                        cookiefilter2["LastStateFilterBack"] = "filterdefaultalluser";
                        HttpCookie param02 = new HttpCookie("TMGReportCookieFilterParam");
                        param02["day0filter"] = day02;
                        param02["day1filter"] = day12;
                        Response.Cookies.Add(cookiefilter2);
                        Response.Cookies.Add(param02);
                        ViewState["LastKey"] = $"SELECT distinct day FROM tmgreport.day_result WHERE day >= \"{day02}\" AND day <= \"{day12}\" ORDER BY day DESC;";
                        ViewState["LastState"] = "filterdefault";
                        CreateFilterListData("filterdefault", $"SELECT distinct day FROM tmgreport.day_result WHERE day >= \"{day02}\" AND day <= \"{day12}\" ORDER BY day DESC;");
                        break;
                    case "filterdefaultalluserandfor":
                        string day13 = Request.QueryString["day1"];
                        HttpCookie cookiefilter3 = new HttpCookie("TMGReportCookieParamFilter");
                        cookiefilter3["LastStateFilter"] = "filterdefaultalluserandfor";
                        cookiefilter3["LastStateFilterBack"] = "filterdefaultalluserandfor";
                        HttpCookie param03 = new HttpCookie("TMGReportCookieFilterParam");
                        param03["day1filter"] = day13;
                        Response.Cookies.Add(cookiefilter3);
                        Response.Cookies.Add(param03);
                        ViewState["LastKey"] = $"SELECT distinct day FROM tmgreport.day_result WHERE day <= \"{day13}\" ORDER BY day DESC;";
                        ViewState["LastState"] = "filterdefault";
                        CreateFilterListData("filterdefault", $"SELECT distinct day FROM tmgreport.day_result WHERE day <= \"{day13}\" ORDER BY day DESC;");
                        break;
                    case "filterdefaultalluserandthis":
                        string day04 = Request.QueryString["day0"];
                        HttpCookie cookiefilter4 = new HttpCookie("TMGReportCookieParamFilter");
                        cookiefilter4["LastStateFilter"] = "filterdefaultalluserandthis";
                        cookiefilter4["LastStateFilterBack"] = "filterdefaultalluserandthis";
                        HttpCookie param04 = new HttpCookie("TMGReportCookieFilterParam");
                        param04["day0filter"] = day04;
                        Response.Cookies.Add(cookiefilter4);
                        Response.Cookies.Add(param04);
                        ViewState["LastKey"] = $"SELECT distinct day FROM tmgreport.day_result WHERE day >= \"{day04}\" ORDER BY day DESC;";
                        ViewState["LastState"] = "filterdefault";
                        CreateFilterListData("filterdefault", $"SELECT distinct day FROM tmgreport.day_result WHERE day >= \"{day04}\" ORDER BY day DESC;");
                        break;
                    case "filterdefaultonlyoneuser":
                        string userfilter5 = Request.QueryString["user"];
                        string[] cashuser5 = userfilter5.Split('\\');
                        string filteruser5 = cashuser5[0] + @"\\" + cashuser5[1];
                        HttpCookie cookiefilter5 = new HttpCookie("TMGReportCookieParamFilter");
                        cookiefilter5["LastStateFilter"] = "filterdefaultonlyoneuser";
                        cookiefilter5["LastStateFilterBack"] = "filterdefaultonlyoneuser";
                        HttpCookie param05 = new HttpCookie("TMGReportCookieFilterParam");
                        param05["userfilter"] = userfilter5;
                        Response.Cookies.Add(cookiefilter5);
                        Response.Cookies.Add(param05);
                        ViewState["LastKey"] = $"SELECT distinct day FROM tmgreport.day_result WHERE user = \"{filteruser5}\" ORDER BY day DESC;";
                        ViewState["LastState"] = "filterdefault";
                        CreateFilterListData("filterdefault", $"SELECT distinct day FROM tmgreport.day_result WHERE user = \"{filteruser5}\" ORDER BY day DESC;");
                        break;
                    case "filterdefaultoneuserfor":
                        string day16 = Request.QueryString["day1"];
                        string userfilter6 = Request.QueryString["user"];
                        string[] cashuser6 = userfilter6.Split('\\');
                        string filteruser6 = cashuser6[0] + @"\\" + cashuser6[1];
                        HttpCookie cookiefilter6 = new HttpCookie("TMGReportCookieParamFilter");
                        cookiefilter6["LastStateFilter"] = "filterdefaultoneuserfor";
                        cookiefilter6["LastStateFilterBack"] = "filterdefaultoneuserfor";
                        HttpCookie param06 = new HttpCookie("TMGReportCookieFilterParam");
                        param06["day1filter"] = day16;
                        param06["userfilter"] = userfilter6;
                        Response.Cookies.Add(cookiefilter6);
                        Response.Cookies.Add(param06);
                        ViewState["LastKey"] = $"SELECT distinct day FROM tmgreport.day_result WHERE user = \"{filteruser6}\" AND day <= \"{day16}\" ORDER BY day DESC;";
                        ViewState["LastState"] = "filterdefault";
                        CreateFilterListData("filterdefault", $"SELECT distinct day FROM tmgreport.day_result WHERE user = \"{filteruser6}\" AND day <= \"{day16}\" ORDER BY day DESC;");
                        break;
                    case "filterdefaultoneuserthis":
                        string day07 = Request.QueryString["day0"];
                        string userfilter7 = Request.QueryString["user"];
                        string[] cashuser7 = userfilter7.Split('\\');
                        string filteruser7 = cashuser7[0] + @"\\" + cashuser7[1];
                        HttpCookie cookiefilter7 = new HttpCookie("TMGReportCookieParamFilter");
                        cookiefilter7["LastStateFilter"] = "filterdefaultoneuserthis";
                        cookiefilter7["LastStateFilterBack"] = "filterdefaultoneuserthis";
                        HttpCookie param07 = new HttpCookie("TMGReportCookieFilterParam");
                        param07["day0filter"] = day07;
                        param07["userfilter"] = userfilter7;
                        Response.Cookies.Add(cookiefilter7);
                        Response.Cookies.Add(param07);
                        ViewState["LastKey"] = $"SELECT distinct day FROM tmgreport.day_result WHERE user = \"{filteruser7}\" AND day >= \"{day07}\"  ORDER BY day DESC;";
                        ViewState["LastState"] = "filterdefault";
                        CreateFilterListData("filterdefault", $"SELECT distinct day FROM tmgreport.day_result WHERE user = \"{filteruser7}\" AND day >= \"{day07}\"  ORDER BY day DESC;");
                        break;
                    case "filterday":
                        string dayfilter = Request.QueryString["day"];
                        HttpCookie cookiedaydayfilter = new HttpCookie("TMGReportCookieDayFilter");
                        cookiedaydayfilter["LastFilterDay"] = dayfilter;
                        Response.Cookies.Add(cookiedaydayfilter);
                        ViewState["LastKey"] = $"SELECT * FROM tmgreport.day_result WHERE day=\"{dayfilter}\" ORDER BY download DESC;";
                        ViewState["LastState"] = "filterday";
                        CreateFilterListData("filterday", $"SELECT * FROM tmgreport.day_result WHERE day=\"{dayfilter}\" ORDER BY download DESC;");
                        break;
                    case "userfilterday":
                        string dayuserfilter = Request.QueryString["day"];
                        string itemfilter = Request.QueryString["user"];
                        string[] chapterfilter = itemfilter.Split('\\');
                        string searchfilter = chapterfilter[0] + @"\\" + chapterfilter[1];
                        ViewState["LastKey"] = $"SELECT * FROM tmgreport.logs WHERE Date=\"{dayuserfilter}\" AND client_user_name=\"{searchfilter}\" ORDER BY download DESC;";
                        ViewState["LastState"] = "userfilterday";
                        CreateFilterListData("userfilterday", $"SELECT * FROM tmgreport.logs WHERE Date=\"{dayuserfilter}\" AND client_user_name=\"{searchfilter}\" ORDER BY download DESC;");
                        break;
                    case "backfilter":
                        HttpCookie cookieReqFilter = Request.Cookies["TMGReportCookieParamFilter"];
                        HttpCookie cookieReqr = Request.Cookies["TMGReportCookieFilterParam"]; 
                        if (cookieReqFilter != null)
                        {
                            string oldparam = cookieReqFilter["LastStateFilter"];
                            switch (oldparam)
                            {
                                case "filterdefaultfull":
                                    string backday0 = cookieReqr["day0filter"];
                                    string backday1 = cookieReqr["day1filter"];
                                    string backuserfilter = cookieReqr["userfilter"];
                                    Response.Redirect($"Default.aspx?param=filterdefaultfull&day0={backday0}&day1={backday1}&user={backuserfilter}");
                                    break;
                                case "filterdefaultalluser":
                                    string backday02 = cookieReqr["day0filter"];
                                    string backday12 = cookieReqr["day1filter"];
                                    Response.Redirect($"Default.aspx?param=filterdefaultalluser&day0={backday02}&day1={backday12}");
                                    break;
                                case "filterdefaultalluserandfor":
                                    string backday13 = cookieReqr["day1filter"];
                                    Response.Redirect($"Default.aspx?param=filterdefaultalluserandfor&day1={backday13}");
                                    break;
                                case "filterdefaultalluserandthis":
                                    string backday04 = cookieReqr["day0filter"];
                                    Response.Redirect($"Default.aspx?param=filterdefaultalluserandthis&day0={backday04}");
                                    break;
                                case "filterdefaultonlyoneuser":
                                    string backuserfilter5 = cookieReqr["userfilter"];
                                    Response.Redirect($"Default.aspx?param=filterdefaultonlyoneuser&user={backuserfilter5}");
                                    break;
                                case "filterdefaultoneuserfor":
                                    string backday16 = cookieReqr["day1filter"];
                                    string backuserfilter6 = cookieReqr["userfilter"];
                                    Response.Redirect($"Default.aspx?param=filterdefaultoneuserfor&day1={backday16}&user={backuserfilter6}");
                                    break;
                                case "filterdefaultoneuserthis":
                                    string backday07 = cookieReqr["day0filter"];
                                    string backuserfilter7 = cookieReqr["userfilter"];
                                    Response.Redirect($"Default.aspx?param=filterdefaultoneuserthis&day0={backday07}&user={backuserfilter7}");
                                    break;

                                case "filterday":
                                    HttpCookie daycookie = Request.Cookies["TMGReportCookieDayFilter"];
                                    if (daycookie != null)
                                    {
                                        string dayback = daycookie["LastFilterDay"].ToString();
                                        string filterback = cookieReqFilter["LastStateFilterBack"];
                                        HttpCookie cookiebackday = new HttpCookie("TMGReportCookieParamFilter");
                                        cookiebackday["LastStateFilter"] = filterback;
                                        cookiebackday["LastStateFilterBack"] = filterback;
                                        Response.Cookies.Add(cookiebackday);
                                        ViewState["LastKey"] = $"SELECT * FROM tmgreport.day_result WHERE day=\"{dayback}\" ORDER BY download DESC;";
                                        ViewState["LastState"] = "filterday";
                                        Response.Redirect($"~/Default.aspx?param=filterday&day={dayback}");
                                    }
                                    break;
                                default:
                                    break;
                            }
                        }
                        break;
                    case "sorteddayfilter":
                        string byfilter = Request.QueryString["by"];
                        string dirfilter = Request.QueryString["dir"];
                        string daysortfilter = Request.QueryString["day"];
                        //HttpCookie cookiesorted = new HttpCookie("TMGReportCookieSorted");
                        //cookiesorted["key"] = by + "#" + dir;
                        //Response.Cookies.Add(cookiesorted);
                        ViewState["LastKey"] = $"SELECT * FROM tmgreport.day_result WHERE day=\"{daysortfilter}\" ORDER BY {byfilter} {dirfilter};";
                        ViewState["LastState"] = "sortedday";
                        CreateFilterListData("sorteddayfilter", $"SELECT * FROM tmgreport.day_result WHERE day=\"{daysortfilter}\" ORDER BY {byfilter} {dirfilter};");
                        break;
                    case "sorteduserfilter":
                        string byuserfilter = Request.QueryString["by"];
                        string diruserfilter = Request.QueryString["dir"];
                        string daysortuserfilter = Request.QueryString["day"];
                        string clientfilter = Request.QueryString["client"];
                        string[] chaptersortedfilter = clientfilter.Split('\\');
                        string searchsortedfilter = chaptersortedfilter[0] + @"\\" + chaptersortedfilter[1];
                        ViewState["LastKey"] = $"SELECT * FROM tmgreport.logs WHERE Date=\"{daysortuserfilter}\" AND client_user_name=\"{searchsortedfilter}\" ORDER BY {byuserfilter} {diruserfilter};";
                        ViewState["LastState"] = "user";
                        CreateFilterListData("sorteduserfilter", $"SELECT * FROM tmgreport.logs WHERE Date=\"{daysortuserfilter}\" AND client_user_name=\"{searchsortedfilter}\" ORDER BY {byuserfilter} {diruserfilter};");
                        break;
                    case "userfilterbyday":
                        string daysortuserfilterbyday = Request.QueryString["day"];
                        string clientfilterbyday = Request.QueryString["user"];
                        HttpCookie cookieReqFilter01 = Request.Cookies["TMGReportCookieParamFilter"];
                        string back = cookieReqFilter01["LastStateFilterBack"];
                        HttpCookie cookiefilter01 = new HttpCookie("TMGReportCookieParamFilter");
                        cookiefilter01["LastStateFilter"] = "filterday";
                        cookiefilter01["LastStateFilterBack"] = back;
                        Response.Cookies.Add(cookiefilter01);
                        Response.Redirect($"Default.aspx?param=userfilterday&day={daysortuserfilterbyday}&user={clientfilterbyday}");
                        break;
                }
                }
                catch
                {
                    
                }
            //}

            
                


            hideForm.Visible = false;
            filterList.Visible = false;
        }

        //Создание список линков
        void CreateListData(string param, string comm)
        {
            string connStr = "[АДРЕС СЕРВЕРА MYSQL];user=[ВАШ ПОЛЬЗОВАТЕЛЬ];database=tmgreport;password=[ПАРОЛЬ ПОЛЬЗОВАТЕЛЯ];";
            MySqlConnection conn = new MySqlConnection(connStr);
            MySqlConnection connread = new MySqlConnection(connStr);
            try
            {
                conn.Open();
                connread.Open();
                Console.WriteLine("Успешно соединился с базой данных!");
            }
            catch
            {
                Console.WriteLine("Не удалось соединиться с базой данных");
                return;
            }

            linkArea.Controls.Clear();

            switch (param)
            {
                //При загрузке страницы, или переходе по названию программы
                case "default":
                    MySqlCommand defparam = new MySqlCommand(comm, conn);
                    MySqlDataReader defreader = defparam.ExecuteReader();


                    while (defreader.Read())
                    {
                        string str = defreader[0].ToString();
                        DateTime dt = DateTime.Parse(str);

                        MySqlCommand defdayparam = new MySqlCommand("SELECT sum(upload), sum(download) FROM tmgreport.day_result WHERE day=\"" + dt.ToString("yyyy-MM-dd") + "\";", connread);
                        MySqlDataReader defdayreader = defdayparam.ExecuteReader();
                        decimal upl = 0;
                        decimal dow = 0;
                        while (defdayreader.Read())
                        {
                            string u = defdayreader[0].ToString();
                            string d = defdayreader[1].ToString();
                            upl = decimal.Parse(u);
                            dow = decimal.Parse(d); 
                        }
                        defdayreader.Close();
                        
                        string completeUpload = CalculateSize(upl);
                        string completeDownload = CalculateSize(dow);
                        
                        HyperLink deflink = new HyperLink()
                        {
                            CssClass = "fontSegoe linkLabel",
                            NavigateUrl = "~/Default.aspx?param=day&day=" + dt.ToString("yyyy-MM-dd"),
                            Text = "<span class=\"firstElement\">"+dt.ToString("D") + "</span><span class=\"secondElement\">Upload(" + completeUpload + ")</span><span class=\"thirdElement\">Download(" + completeDownload + ")</span>"
                        };

                        linkArea.Controls.Add(deflink);
                    }
                    defreader.Close();
                    conn.Close();
                    connread.Close();
                    break;
                //День без сортировки
                case "day":
                    MySqlCommand dayparam = new MySqlCommand(comm, conn);
                    MySqlDataReader dayreader = dayparam.ExecuteReader();
                    DateTime day = DateTime.Parse(Request.QueryString["day"]);


                    HyperLink backlink = new HyperLink()
                    {
                        CssClass = "fontSegoe backLabel",
                        NavigateUrl = $"~/Default.aspx?param=back",
                        Text = "Вернуться<br/><br/>"
                    };

                    linkArea.Controls.Add(backlink);

                    Label label = new Label()
                    {
                        CssClass= "fontSegoe grayFont titleLabel",
                        Text=$"Данные на {day.ToString("D")}<br/><br/>"
                    };

                    linkArea.Controls.Add(label);

                    Label sortedLabel = new Label()
                    {
                        CssClass = "fontSegoe grayFont sortedlabel",
                        Text = $"<a class=\"sortedLinkUser\" href=\"Default.aspx?param=sortedday&by=user&dir=DESC&day="+ day.ToString("yyyy-MM-dd") + "\">Имя пользователя</a><a class=\"sortedLink\" href=\"Default.aspx?param=sortedday&by=upload&dir=DESC&day=" + day.ToString("yyyy-MM-dd") + "\">Отдано</a><a class=\"sortedLink\" href=\"Default.aspx?param=sortedday&by=download&dir=ASC&day=" + day.ToString("yyyy-MM-dd") + "\">Загружено&#160;&#8595;</a><br/><br/>"
                    };

                    linkArea.Controls.Add(sortedLabel);

                    while (dayreader.Read())
                    {
                        string user = dayreader[2].ToString();

                        decimal upl = 0;
                        decimal dow = 0;

                        string u = dayreader[3].ToString();
                        string d = dayreader[4].ToString();
                        upl = decimal.Parse(u);
                        dow = decimal.Parse(d);

                        string completeUpload = CalculateSize(upl);
                        string completeDownload = CalculateSize(dow);

                        

                        HyperLink link = new HyperLink()
                        {
                            CssClass = "fontSegoe linkLabel",
                            NavigateUrl = "~/Default.aspx?param=user&day=" + day.ToString("yyyy-MM-dd") + "&user=" + user,
                            Text = "<span class=\"firstElement\">"+user + "</span><span class=\"secondElement\">" + completeUpload + "</span> <span class=\"thirdElement\">" + completeDownload + "</span>"
                        };

                        linkArea.Controls.Add(link);
                    }
                    dayreader.Close();
                    conn.Close();
                    connread.Close();
                    break;
                //Подробнее на пользователя
                case "user":
                    MySqlCommand userparam = new MySqlCommand(comm, conn);
                    MySqlDataReader userreader = userparam.ExecuteReader();
                    DateTime dayuser = DateTime.Parse(Request.QueryString["day"]);

                    string item = Request.QueryString["user"];

                    string[] chapter = item.Split('\\');
                    string search = chapter[0] + @"\\" + chapter[1];

                    string fullName = getUser(chapter[1], "fio");

                    HyperLink backlinkuser = new HyperLink()
                    {
                        CssClass = "fontSegoe backLabel",
                        NavigateUrl = $"~/Default.aspx?param=back&day={dayuser.ToString("yyyy-MM-dd")}",
                        Text = "Вернуться<br/><br/>"
                    };

                    linkArea.Controls.Add(backlinkuser);

                    Label labeluser = new Label()
                    {
                        CssClass = "fontSegoe grayFont titleLabel",
                        Text = $"Данные на пользователя: {fullName} ({Request.QueryString["user"]})<br/><br/>"
                    };


                    linkArea.Controls.Add(labeluser);

                    Label filteruserday = new Label()
                    {
                        CssClass = "fontSegoe grayFont titleLabel",
                        Text = $"за {dayuser.ToString("D")}<br/><br/>"
                    };

                    linkArea.Controls.Add(filteruserday);

                    Label sortedLabeluser = new Label()
                    {
                        CssClass = "fontSegoe grayFont sortedlabelUser",
                        Text = $"<a class=\"sortedLinkUser\" href=\"Default.aspx?param=sorteduser&by=target_host&dir=DESC&day=" + dayuser.ToString("yyyy-MM-dd") + $"&client={Request.QueryString["user"]}\">Хост</a><a class=\"sortedLink\" href=\"Default.aspx?param=sorteduser&by=upload&dir=DESC&day=" + dayuser.ToString("yyyy-MM-dd") + $"&client={Request.QueryString["user"]}\">Отдано</a><a class=\"sortedLink\" href=\"Default.aspx?param=sorteduser&by=download&dir=ASC&day=" + dayuser.ToString("yyyy-MM-dd") + $"&client={Request.QueryString["user"]}\">Загружено&#160;&#8595;</a><br/><br/>"
                    };

                    linkArea.Controls.Add(sortedLabeluser);

                    while (userreader.Read())
                    {
                        string host = userreader[2].ToString();

                        decimal upl = 0;
                        decimal dow = 0;

                        string u = userreader[4].ToString();
                        string d = userreader[5].ToString();
                        upl = decimal.Parse(u);
                        dow = decimal.Parse(d);

                        string completeUpload = CalculateSize(upl);
                        string completeDownload = CalculateSize(dow);

                        

                        HyperLink link = new HyperLink()
                        {
                            CssClass = "fontSegoe linkLabelUser",
                            NavigateUrl = "",
                            Text = "<span class=\"firstViewElement\">" + host + "</span><span class=\"secondElement\">" + completeUpload + "</span> <span class=\"thirdElement\">" + completeDownload + "</span>"
                        };

                        linkArea.Controls.Add(link);
                    }
                    userreader.Close();
                    conn.Close();
                    connread.Close();
                    break;
                //День после сортировки
                case "sortedday":
                    MySqlCommand sorteddayparam = new MySqlCommand(comm, conn);
                    MySqlDataReader sorteddayreader = sorteddayparam.ExecuteReader();
                    DateTime sortedday = DateTime.Parse(Request.QueryString["day"]);
                    
                    HyperLink sortedbacklink = new HyperLink()
                    {
                        CssClass = "fontSegoe backLabel",
                        NavigateUrl = $"~/Default.aspx?param=back",
                        Text = "Вернуться<br/><br/>"
                    };

                    linkArea.Controls.Add(sortedbacklink);

                    Label sortedlabel = new Label()
                    {
                        CssClass = "fontSegoe grayFont titleLabel",
                        Text = $"Данные на {sortedday.ToString("D")}<br/><br/>"
                    };

                    linkArea.Controls.Add(sortedlabel);

                    string by = Request.QueryString["by"];
                    string dir = Request.QueryString["dir"];
                    string[] req = CalculateDir(by, dir);
                    

                    Label sortedsortedLabel = new Label()
                    {
                        CssClass = "fontSegoe grayFont sortedlabel",
                        Text = $"<a class=\"sortedLink\" href=\"Default.aspx?param=sortedday&by=user&dir={req[0]}&day=" + sortedday.ToString("yyyy-MM-dd") + $"\">Имя пользователя{req[3]}</a><a class=\"sortedLink\" href=\"Default.aspx?param=sortedday&by=upload&dir={req[1]}&day=" + sortedday.ToString("yyyy-MM-dd") + $"\">Отдано{req[4]}</a><a class=\"sortedLink\" href=\"Default.aspx?param=sortedday&by=download&dir={req[2]}&day=" + sortedday.ToString("yyyy-MM-dd") + $"\">Загружено{req[5]}</a><br/><br/>"
                    };

                    linkArea.Controls.Add(sortedsortedLabel);

                    while (sorteddayreader.Read())
                    {
                        string user = sorteddayreader[2].ToString();

                        decimal upl = 0;
                        decimal dow = 0;

                        string u = sorteddayreader[3].ToString();
                        string d = sorteddayreader[4].ToString();
                        upl = decimal.Parse(u);
                        dow = decimal.Parse(d);

                        string completeUpload = CalculateSize(upl);
                        string completeDownload = CalculateSize(dow);



                        HyperLink link = new HyperLink()
                        {
                            CssClass = "fontSegoe linkLabel",
                            NavigateUrl = "~/Default.aspx?param=user&day=" + sortedday.ToString("yyyy-MM-dd") + "&user=" + user,
                            Text = "<span class=\"firstElement\">" + user + "</span><span class=\"secondElement\">" + completeUpload + "</span> <span class=\"thirdElement\">" + completeDownload + "</span>"
                        };

                        linkArea.Controls.Add(link);
                    }
                    sorteddayreader.Close();
                    conn.Close();
                    connread.Close();
                    break;
                case "usersorted":
                    MySqlCommand sorteduserparam = new MySqlCommand(comm, conn);
                    MySqlDataReader sorteduserreader = sorteduserparam.ExecuteReader();
                    DateTime sorteddayuser = DateTime.Parse(Request.QueryString["day"]);

                    string sorteditem = Request.QueryString["client"];

                    string[] sortedchapter = sorteditem.Split('\\');
                    string sortedsearch = sortedchapter[0] + @"\\" + sortedchapter[1];

                    string fullNamesorted = getUser(sortedchapter[1], "fio");

                    HyperLink sortedbacklinkuser = new HyperLink()
                    {
                        CssClass = "fontSegoe backLabel",
                        NavigateUrl = $"~/Default.aspx?param=back&day={sorteddayuser.ToString("yyyy-MM-dd")}",
                        Text = "Вернуться<br/><br/>"
                    };

                    linkArea.Controls.Add(sortedbacklinkuser);

                    Label sortedlabeluser = new Label()
                    {
                        CssClass = "fontSegoe grayFont titleLabel",
                        Text = $"Данные на пользователя: {fullNamesorted} ({Request.QueryString["client"]})<br/><br/>"
                    };

                    linkArea.Controls.Add(sortedlabeluser);

                    Label sortedfilteruserday = new Label()
                    {
                        CssClass = "fontSegoe grayFont titleLabel",
                        Text = $"за {sorteddayuser.ToString("D")}<br/><br/>"
                    };

                    linkArea.Controls.Add(sortedfilteruserday);

                    string sortedby = Request.QueryString["by"];
                    string sorteddir = Request.QueryString["dir"];
                    string[] sortedreq = CalculateDir(sortedby, sorteddir);

                    Label sortedsortedLabeluser = new Label()
                    {
                        CssClass = "fontSegoe grayFont sortedlabelUser",
                        Text = $"<a class=\"sortedLinkUser\" href=\"Default.aspx?param=sorteduser&by=target_host&dir={sortedreq[0]}&day=" + sorteddayuser.ToString("yyyy-MM-dd") + $"&client={Request.QueryString["client"]}\">Хост{sortedreq[3]}</a><a class=\"sortedLink\" href=\"Default.aspx?param=sorteduser&by=upload&dir={sortedreq[1]}&day=" + sorteddayuser.ToString("yyyy-MM-dd") + $"&client={Request.QueryString["client"]}\">Отдано{sortedreq[4]}</a><a class=\"sortedLink\" href=\"Default.aspx?param=sorteduser&by=download&dir={sortedreq[2]}&day=" + sorteddayuser.ToString("yyyy-MM-dd") + $"&client={Request.QueryString["client"]}\">Загружено{sortedreq[5]}</a><br/><br/>"
                    };

                    linkArea.Controls.Add(sortedsortedLabeluser);

                    while (sorteduserreader.Read())
                    {
                        string host = sorteduserreader[2].ToString();

                        decimal upl = 0;
                        decimal dow = 0;

                        string u = sorteduserreader[4].ToString();
                        string d = sorteduserreader[5].ToString();
                        upl = decimal.Parse(u);
                        dow = decimal.Parse(d);

                        string sortedcompleteUpload = CalculateSize(upl);
                        string sortedcompleteDownload = CalculateSize(dow);



                        HyperLink link = new HyperLink()
                        {
                            CssClass = "fontSegoe linkLabelUser",
                            NavigateUrl = "",
                            Text = "<span class=\"firstViewElement\">" + host + "</span><span class=\"secondElement\">" + sortedcompleteUpload + "</span> <span class=\"thirdElement\">" + sortedcompleteDownload + "</span>"
                        };

                        linkArea.Controls.Add(link);
                    }
                    sorteduserreader.Close();
                    conn.Close();
                    connread.Close();
                    break;
            }
        }

        //То же создание линков, но с фильтрами
        void CreateFilterListData(string param, string comm)
        {
            string connStr = "[АДРЕС СЕРВЕРА MYSQL]; user =[ВАШ ПОЛЬЗОВАТЕЛЬ]; database = tmgreport; password =[ПАРОЛЬ ПОЛЬЗОВАТЕЛЯ]; ";
            MySqlConnection conn = new MySqlConnection(connStr);
            MySqlConnection connread = new MySqlConnection(connStr);
            try
            {
                conn.Open();
                connread.Open();
                Console.WriteLine("Успешно соединился с базой данных!");
            }
            catch
            {
                Console.WriteLine("Не удалось соединиться с базой данных");
                return;
            }

            string deffilparam = Request.QueryString["param"];
            string filtername = "";

            switch (deffilparam)
            {
                case "filterdefaultfull":
                    string userfilter = Request.QueryString["user"];
                    string[] cashuser = userfilter.Split('\\');
                    string filteruser = cashuser[0] + @"\\" + cashuser[1];
                    filtername = $"Использован фильтр: Искать с: {Request.QueryString["day0"]} по: {Request.QueryString["day1"]} пользователя: {Request.QueryString["user"]}  <a class=\"sortedLinkUser\" href=\"Default.aspx\">&#160;&#215;&#160;</a><br/><br/>";
                    break;
                case "filterdefaultalluser":
                    filtername = $"Использован фильтр: Искать с: {Request.QueryString["day0"]} по: {Request.QueryString["day1"]}  <a class=\"sortedLinkUser\" href=\"Default.aspx\">&#160;&#215;&#160;</a><br/><br/>";
                    break;
                case "filterdefaultalluserandfor":
                    filtername = $"Использован фильтр: Искать по: {Request.QueryString["day1"]}  <a class=\"sortedLinkUser\" href=\"Default.aspx\">&#160;&#215;&#160;</a><br/><br/>";
                    break;
                case "filterdefaultalluserandthis":
                    filtername = $"Использован фильтр: Искать с: {Request.QueryString["day0"]}  <a class=\"sortedLinkUser\" href=\"Default.aspx\">&#160;&#215;&#160;</a><br/><br/>";
                    break;
                case "filterdefaultonlyoneuser":
                    string userfilter0 = Request.QueryString["user"];
                    string[] cashuser0 = userfilter0.Split('\\');
                    string filteruser0 = cashuser0[0] + @"\\" + cashuser0[1];
                    filtername = $"Использован фильтр: Искать пользователя: {Request.QueryString["user"]}  <a class=\"sortedLinkUser\" href=\"Default.aspx\">&#160;&#215;&#160;</a><br/><br/>";
                    break;
                case "filterdefaultoneuserfor":
                    string userfilter1 = Request.QueryString["user"];
                    string[] cashuser1 = userfilter1.Split('\\');
                    string filteruser1 = cashuser1[0] + @"\\" + cashuser1[1];
                    filtername = $"Использован фильтр: Искать по: {Request.QueryString["day1"]} пользователя: {Request.QueryString["user"]}  <a class=\"sortedLinkUser\" href=\"Default.aspx\">&#160;&#215;&#160;</a><br/><br/>";
                    break;
                case "filterdefaultoneuserthis":
                    string userfilter2 = Request.QueryString["user"];
                    string[] cashuser2 = userfilter2.Split('\\');
                    string filteruse2 = cashuser2[0] + @"\\" + cashuser2[1];
                    filtername = $"Использован фильтр: Искать с: {Request.QueryString["day0"]} пользователя: {Request.QueryString["user"]}  <a class=\"sortedLinkUser\" href=\"Default.aspx\">&#160;&#215;&#160;</a><br/><br/>";
                    break;
                case "filterday":
                    filtername = $"Использован фильтр поиска по дате. <a class=\"sortedLinkUser\" href=\"Default.aspx\">&#160;&#215;&#160;</a><br/><br/>";
                    break;
                case "sorteddayfilter":
                    filtername = $"Использован фильтр поиска по дате. <a class=\"sortedLinkUser\" href=\"Default.aspx\">&#160;&#215;&#160;</a><br/><br/>";
                    break;
            }


            filterListEnable.Visible = false;
            filterIcon.Visible = false;

            linkArea.Controls.Clear();

            bool writeTitle = true;

            switch (param)
            {
                //При загрузке страницы, или переходе по названию программы
                case "filterdefault":
                    MySqlCommand defparam = new MySqlCommand(comm, conn);
                    MySqlDataReader defreader = defparam.ExecuteReader();
                    
                    while (defreader.Read())
                    {
                        string str = defreader[0].ToString();
                        DateTime dt = DateTime.Parse(str);

                        string dayfillink = "";
                        string deffilcomm = "";

                        
                        switch (deffilparam)
                        {
                            case "filterdefaultfull":
                                string userfilter = Request.QueryString["user"];
                                string[] cashuser = userfilter.Split('\\');
                                string filteruser = cashuser[0] + @"\\" + cashuser[1];
                                dayfillink = $"~/Default.aspx?param=userfilterday&day={dt.ToString("yyyy-MM-dd")}&user={Request.QueryString["user"]}";
                                deffilcomm = $"SELECT sum(upload), sum(download) FROM tmgreport.day_result WHERE day=\"{dt.ToString("yyyy-MM-dd")}\" AND user=\"{filteruser}\";";
                                break;
                            case "filterdefaultalluser":
                                dayfillink = $"~/Default.aspx?param=filterday&day={dt.ToString("yyyy-MM-dd")}";
                                deffilcomm = $"SELECT sum(upload), sum(download) FROM tmgreport.day_result WHERE day=\"{dt.ToString("yyyy-MM-dd")}\";";
                                break;
                            case "filterdefaultalluserandfor":
                                dayfillink = $"~/Default.aspx?param=filterday&day={dt.ToString("yyyy-MM-dd")}";
                                deffilcomm = $"SELECT sum(upload), sum(download) FROM tmgreport.day_result WHERE day=\"{dt.ToString("yyyy-MM-dd")}\";";
                                break;
                            case "filterdefaultalluserandthis":
                                dayfillink = $"~/Default.aspx?param=filterday&day={dt.ToString("yyyy-MM-dd")}";
                                deffilcomm = $"SELECT sum(upload), sum(download) FROM tmgreport.day_result WHERE day=\"{dt.ToString("yyyy-MM-dd")}\";";
                                break;
                            case "filterdefaultonlyoneuser":
                                string userfilter0 = Request.QueryString["user"];
                                string[] cashuser0 = userfilter0.Split('\\');
                                string filteruser0 = cashuser0[0] + @"\\" + cashuser0[1];
                                dayfillink = $"~/Default.aspx?param=userfilterday&day={dt.ToString("yyyy-MM-dd")}&user={Request.QueryString["user"]}";
                                deffilcomm = $"SELECT sum(upload), sum(download) FROM tmgreport.day_result WHERE day=\"{dt.ToString("yyyy-MM-dd")}\" AND user=\"{filteruser0}\";";
                                break;
                            case "filterdefaultoneuserfor":
                                string userfilter1 = Request.QueryString["user"];
                                string[] cashuser1 = userfilter1.Split('\\');
                                string filteruser1 = cashuser1[0] + @"\\" + cashuser1[1];
                                dayfillink = $"~/Default.aspx?param=userfilterday&day={dt.ToString("yyyy-MM-dd")}&user={Request.QueryString["user"]}";
                                deffilcomm = $"SELECT sum(upload), sum(download) FROM tmgreport.day_result WHERE day=\"{dt.ToString("yyyy-MM-dd")}\" AND user=\"{filteruser1}\";";
                                break;
                            case "filterdefaultoneuserthis":
                                string userfilter2 = Request.QueryString["user"];
                                string[] cashuser2 = userfilter2.Split('\\');
                                string filteruse2 = cashuser2[0] + @"\\" + cashuser2[1];
                                dayfillink = $"~/Default.aspx?param=userfilterday&day={dt.ToString("yyyy-MM-dd")}&user={Request.QueryString["user"]}";
                                deffilcomm = $"SELECT sum(upload), sum(download) FROM tmgreport.day_result WHERE day=\"{dt.ToString("yyyy-MM-dd")}\" AND user=\"{filteruse2}\";";
                                break;
                        }

                        if (writeTitle)
                        {
                            Label filter = new Label()
                            {
                                CssClass = "fontSegoe grayFont titleLabel",
                                Text = filtername
                            };

                            linkArea.Controls.Add(filter);
                            writeTitle = false;
                        }

                        MySqlCommand defdayparam = new MySqlCommand(deffilcomm, connread);
                        MySqlDataReader defdayreader = defdayparam.ExecuteReader();
                        decimal upl = 0;
                        decimal dow = 0;
                        while (defdayreader.Read())
                        {
                            string u = defdayreader[0].ToString();
                            string d = defdayreader[1].ToString();
                            upl = decimal.Parse(u);
                            dow = decimal.Parse(d);
                        }
                        defdayreader.Close();

                        string completeUpload = CalculateSize(upl);
                        string completeDownload = CalculateSize(dow);


                        HyperLink deflink = new HyperLink()
                        {
                            CssClass = "fontSegoe linkLabel",
                            NavigateUrl = dayfillink,
                            Text = "<span class=\"firstElement\">" + dt.ToString("D") + "</span><span class=\"secondElement\">Upload(" + completeUpload + ")</span><span class=\"thirdElement\">Download(" + completeDownload + ")</span>"
                        };

                        linkArea.Controls.Add(deflink);
                    }
                    defreader.Close();
                    conn.Close();
                    connread.Close();
                    break;
                //День без сортировки
                case "filterday":
                    MySqlCommand dayparam = new MySqlCommand(comm, conn);
                    MySqlDataReader dayreader = dayparam.ExecuteReader();
                    DateTime day = DateTime.Parse(Request.QueryString["day"]);

                    if (writeTitle)
                    {
                        Label filter = new Label()
                        {
                            CssClass = "fontSegoe grayFont titleLabel",
                            Text = filtername
                        };

                        linkArea.Controls.Add(filter);
                        writeTitle = false;
                    }

                    HyperLink backlink = new HyperLink()
                    {
                        CssClass = "fontSegoe backLabel",
                        NavigateUrl = $"~/Default.aspx?param=backfilter",
                        Text = "Вернуться<br/><br/>"
                    };

                    linkArea.Controls.Add(backlink);

                    Label label = new Label()
                    {
                        CssClass = "fontSegoe grayFont titleLabel",
                        Text = $"Данные на {day.ToString("D")}<br/><br/>"
                    };

                    linkArea.Controls.Add(label);

                    Label sortedLabel = new Label()
                    {
                        CssClass = "fontSegoe grayFont sortedlabel",
                        Text = $"<a class=\"sortedLinkUser\" href=\"Default.aspx?param=sorteddayfilter&by=user&dir=DESC&day=" + day.ToString("yyyy-MM-dd") + "\">Имя пользователя</a><a class=\"sortedLink\" href=\"Default.aspx?param=sorteddayfilter&by=upload&dir=DESC&day=" + day.ToString("yyyy-MM-dd") + "\">Отдано</a><a class=\"sortedLink\" href=\"Default.aspx?param=sorteddayfilter&by=download&dir=ASC&day=" + day.ToString("yyyy-MM-dd") + "\">Загружено&#160;&#8595;</a><br/><br/>"
                    };

                    linkArea.Controls.Add(sortedLabel);

                    while (dayreader.Read())
                    {
                        string user = dayreader[2].ToString();

                        decimal upl = 0;
                        decimal dow = 0;

                        string u = dayreader[3].ToString();
                        string d = dayreader[4].ToString();
                        upl = decimal.Parse(u);
                        dow = decimal.Parse(d);

                        string completeUpload = CalculateSize(upl);
                        string completeDownload = CalculateSize(dow);



                        HyperLink link = new HyperLink()
                        {
                            CssClass = "fontSegoe linkLabel",
                            NavigateUrl = "~/Default.aspx?param=userfilterbyday&day=" + day.ToString("yyyy-MM-dd") + "&user=" + user,
                            Text = "<span class=\"firstElement\">" + user + "</span><span class=\"secondElement\">" + completeUpload + "</span> <span class=\"thirdElement\">" + completeDownload + "</span>"
                        };

                        linkArea.Controls.Add(link);
                    }
                    dayreader.Close();
                    conn.Close();
                    connread.Close();
                    break;
                //Подробнее на пользователя
                case "userfilterday":
                    MySqlCommand userparam = new MySqlCommand(comm, conn);
                    MySqlDataReader userreader = userparam.ExecuteReader();
                    DateTime dayuser = DateTime.Parse(Request.QueryString["day"]);

                    string item = Request.QueryString["user"];

                    string[] chapter = item.Split('\\');
                    string search = chapter[0] + @"\\" + chapter[1];

                    string fullName = getUser(chapter[1], "fio");

                    HyperLink backlinkuser = new HyperLink()
                    {
                        CssClass = "fontSegoe backLabel",
                        NavigateUrl = $"~/Default.aspx?param=backfilter",
                        Text = "Вернуться<br/><br/>"
                    };

                    linkArea.Controls.Add(backlinkuser);

                    Label labeluser = new Label()
                    {
                        CssClass = "fontSegoe grayFont titleLabel",
                        Text = $"Данные на пользователя: {fullName} ({Request.QueryString["user"]})<br/><br/>"
                    };

                    linkArea.Controls.Add(labeluser);

                    Label filteruserday = new Label()
                    {
                        CssClass = "fontSegoe grayFont titleLabel",
                        Text = $"за {dayuser.ToString("D")}<br/><br/>"
                    };

                    linkArea.Controls.Add(filteruserday);

                    Label sortedLabeluser = new Label()
                    {
                        CssClass = "fontSegoe grayFont sortedlabelUser",
                        Text = $"<a class=\"sortedLinkUser\" href=\"Default.aspx?param=sorteduserfilter&by=target_host&dir=DESC&day=" + dayuser.ToString("yyyy-MM-dd") + $"&client={Request.QueryString["user"]}\">Хост</a><a class=\"sortedLink\" href=\"Default.aspx?param=sorteduserfilter&by=upload&dir=DESC&day=" + dayuser.ToString("yyyy-MM-dd") + $"&client={Request.QueryString["user"]}\">Отдано</a><a class=\"sortedLink\" href=\"Default.aspx?param=sorteduserfilter&by=download&dir=ASC&day=" + dayuser.ToString("yyyy-MM-dd") + $"&client={Request.QueryString["user"]}\">Загружено&#160;&#8595;</a><br/><br/>"
                    };

                    linkArea.Controls.Add(sortedLabeluser);

                    while (userreader.Read())
                    {
                        string host = userreader[2].ToString();

                        decimal upl = 0;
                        decimal dow = 0;

                        string u = userreader[4].ToString();
                        string d = userreader[5].ToString();
                        upl = decimal.Parse(u);
                        dow = decimal.Parse(d);

                        string completeUpload = CalculateSize(upl);
                        string completeDownload = CalculateSize(dow);



                        HyperLink link = new HyperLink()
                        {
                            CssClass = "fontSegoe linkLabelUser",
                            NavigateUrl = "",
                            Text = "<span class=\"firstViewElement\">" + host + "</span><span class=\"secondElement\">" + completeUpload + "</span> <span class=\"thirdElement\">" + completeDownload + "</span>"
                        };

                        linkArea.Controls.Add(link);
                    }
                    userreader.Close();
                    conn.Close();
                    connread.Close();
                    break;
                //День после сортировки
                case "sorteddayfilter":
                    MySqlCommand sorteddayparam = new MySqlCommand(comm, conn);
                    MySqlDataReader sorteddayreader = sorteddayparam.ExecuteReader();
                    DateTime sortedday = DateTime.Parse(Request.QueryString["day"]);

                    if (writeTitle)
                    {
                        Label filter = new Label()
                        {
                            CssClass = "fontSegoe grayFont titleLabel",
                            Text = filtername
                        };

                        linkArea.Controls.Add(filter);
                        writeTitle = false;
                    }

                    HyperLink sortedbacklink = new HyperLink()
                    {
                        CssClass = "fontSegoe backLabel",
                        NavigateUrl = $"~/Default.aspx?param=backfilter",
                        Text = "Вернуться<br/><br/>"
                    };

                    linkArea.Controls.Add(sortedbacklink);

                    Label sortedlabel = new Label()
                    {
                        CssClass = "fontSegoe grayFont titleLabel",
                        Text = $"Данные на {sortedday.ToString("D")}<br/><br/>"
                    };

                    linkArea.Controls.Add(sortedlabel);

                    string by = Request.QueryString["by"];
                    string dir = Request.QueryString["dir"];
                    string[] req = CalculateDir(by, dir);


                    Label sortedsortedLabel = new Label()
                    {
                        CssClass = "fontSegoe grayFont sortedlabel",
                        Text = $"<a class=\"sortedLink\" href=\"Default.aspx?param=sorteddayfilter&by=user&dir={req[0]}&day=" + sortedday.ToString("yyyy-MM-dd") + $"\">Имя пользователя{req[3]}</a><a class=\"sortedLink\" href=\"Default.aspx?param=sorteddayfilter&by=upload&dir={req[1]}&day=" + sortedday.ToString("yyyy-MM-dd") + $"\">Отдано{req[4]}</a><a class=\"sortedLink\" href=\"Default.aspx?param=sorteddayfilter&by=download&dir={req[2]}&day=" + sortedday.ToString("yyyy-MM-dd") + $"\">Загружено{req[5]}</a><br/><br/>"
                    };

                    linkArea.Controls.Add(sortedsortedLabel);

                    while (sorteddayreader.Read())
                    {
                        string user = sorteddayreader[2].ToString();

                        decimal upl = 0;
                        decimal dow = 0;

                        string u = sorteddayreader[3].ToString();
                        string d = sorteddayreader[4].ToString();
                        upl = decimal.Parse(u);
                        dow = decimal.Parse(d);

                        string completeUpload = CalculateSize(upl);
                        string completeDownload = CalculateSize(dow);



                        HyperLink link = new HyperLink()
                        {
                            CssClass = "fontSegoe linkLabel",
                            NavigateUrl = "~/Default.aspx?param=user&day=" + sortedday.ToString("yyyy-MM-dd") + "&user=" + user,
                            Text = "<span class=\"firstElement\">" + user + "</span><span class=\"secondElement\">" + completeUpload + "</span> <span class=\"thirdElement\">" + completeDownload + "</span>"
                        };

                        linkArea.Controls.Add(link);
                    }
                    sorteddayreader.Close();
                    conn.Close();
                    connread.Close();
                    break;
                case "sorteduserfilter":
                    MySqlCommand sorteduserparam = new MySqlCommand(comm, conn);
                    MySqlDataReader sorteduserreader = sorteduserparam.ExecuteReader();
                    DateTime sorteddayuser = DateTime.Parse(Request.QueryString["day"]);

                    string sorteditem = Request.QueryString["client"];

                    string[] sortedchapter = sorteditem.Split('\\');
                    string sortedsearch = sortedchapter[0] + @"\\" + sortedchapter[1];

                    string fullNamesorted = getUser(sortedchapter[1], "fio");

                    HyperLink sortedbacklinkuser = new HyperLink()
                    {
                        CssClass = "fontSegoe backLabel",
                        NavigateUrl = $"~/Default.aspx?param=backfilter",
                        Text = "Вернуться<br/><br/>"
                    };

                    linkArea.Controls.Add(sortedbacklinkuser);

                    Label sortedlabeluser = new Label()
                    {
                        CssClass = "fontSegoe grayFont titleLabel",
                        Text = $"Данные на пользователя: {fullNamesorted} ({Request.QueryString["client"]})<br/><br/>"
                    };

                    Label sortedfilteruserday = new Label()
                    {
                        CssClass = "fontSegoe grayFont titleLabel",
                        Text = $"за {sorteddayuser.ToString("D")}<br/><br/>"
                    };

                    linkArea.Controls.Add(sortedlabeluser);
                    linkArea.Controls.Add(sortedfilteruserday);

                    string sortedby = Request.QueryString["by"];
                    string sorteddir = Request.QueryString["dir"];
                    string[] sortedreq = CalculateDir(sortedby, sorteddir);

                    Label sortedsortedLabeluser = new Label()
                    {
                        CssClass = "fontSegoe grayFont sortedlabelUser",
                        Text = $"<a class=\"sortedLinkUser\" href=\"Default.aspx?param=sorteduserfilter&by=target_host&dir={sortedreq[0]}&day=" + sorteddayuser.ToString("yyyy-MM-dd") + $"&client={Request.QueryString["client"]}\">Хост{sortedreq[3]}</a><a class=\"sortedLink\" href=\"Default.aspx?param=sorteduserfilter&by=upload&dir={sortedreq[1]}&day=" + sorteddayuser.ToString("yyyy-MM-dd") + $"&client={Request.QueryString["client"]}\">Отдано{sortedreq[4]}</a><a class=\"sortedLink\" href=\"Default.aspx?param=sorteduserfilter&by=download&dir={sortedreq[2]}&day=" + sorteddayuser.ToString("yyyy-MM-dd") + $"&client={Request.QueryString["client"]}\">Загружено{sortedreq[5]}</a><br/><br/>"
                    };

                    linkArea.Controls.Add(sortedsortedLabeluser);

                    while (sorteduserreader.Read())
                    {
                        string host = sorteduserreader[2].ToString();

                        decimal upl = 0;
                        decimal dow = 0;

                        string u = sorteduserreader[4].ToString();
                        string d = sorteduserreader[5].ToString();
                        upl = decimal.Parse(u);
                        dow = decimal.Parse(d);

                        string sortedcompleteUpload = CalculateSize(upl);
                        string sortedcompleteDownload = CalculateSize(dow);



                        HyperLink link = new HyperLink()
                        {
                            CssClass = "fontSegoe linkLabelUser",
                            NavigateUrl = "",
                            Text = "<span class=\"firstViewElement\">" + host + "</span><span class=\"secondElement\">" + sortedcompleteUpload + "</span> <span class=\"thirdElement\">" + sortedcompleteDownload + "</span>"
                        };

                        linkArea.Controls.Add(link);
                    }
                    sorteduserreader.Close();
                    conn.Close();
                    connread.Close();
                    break;
                //case "userfilterdaybyday":
                //    MySqlCommand userparambyday = new MySqlCommand(comm, conn);
                //    MySqlDataReader userreaderbyday = userparambyday.ExecuteReader();
                //    DateTime dayuserbyday = DateTime.Parse(Request.QueryString["day"]);

                //    string itembyday = Request.QueryString["user"];

                //    string[] chapterbyday = itembyday.Split('\\');
                //    string searchbyday = chapterbyday[0] + @"\\" + chapterbyday[1];

                //    string fullNamebyday = getUser(chapterbyday[1], "fio");

                //    HyperLink backlinkuserbyday = new HyperLink()
                //    {
                //        CssClass = "fontSegoe backLabel",
                //        NavigateUrl = $"~/Default.aspx?param=backfilterbyday",
                //        Text = "Вернуться<br/><br/>"
                //    };

                //    linkArea.Controls.Add(backlinkuserbyday);

                //    Label labeluserbyday = new Label()
                //    {
                //        CssClass = "fontSegoe grayFont titleLabel",
                //        Text = $"Данные на пользователя: {fullNamebyday} ({Request.QueryString["user"]})<br/><br/>"
                //    };

                //    linkArea.Controls.Add(labeluserbyday);

                //    Label filteruserdaybyday = new Label()
                //    {
                //        CssClass = "fontSegoe grayFont titleLabel",
                //        Text = $"за {dayuserbyday.ToString("D")}<br/><br/>"
                //    };

                //    linkArea.Controls.Add(filteruserdaybyday);

                //    Label sortedLabeluserbyday = new Label()
                //    {
                //        CssClass = "fontSegoe grayFont sortedlabelUser",
                //        Text = $"<a class=\"sortedLinkUser\" href=\"Default.aspx?param=sorteduserfilterbyday&by=target_host&dir=DESC&day=" + dayuserbyday.ToString("yyyy-MM-dd") + $"&client={Request.QueryString["user"]}\">Хост</a><a class=\"sortedLink\" href=\"Default.aspx?param=sorteduserfilterbyday&by=upload&dir=DESC&day=" + dayuserbyday.ToString("yyyy-MM-dd") + $"&client={Request.QueryString["user"]}\">Отдано</a><a class=\"sortedLink\" href=\"Default.aspx?param=sorteduserfilterbyday&by=download&dir=ASC&day=" + dayuserbyday.ToString("yyyy-MM-dd") + $"&client={Request.QueryString["user"]}\">Загружено&#160;&#8595;</a><br/><br/>"
                //    };

                //    linkArea.Controls.Add(sortedLabeluserbyday);

                //    while (userreaderbyday.Read())
                //    {
                //        string host = userreaderbyday[2].ToString();

                //        decimal upl = 0;
                //        decimal dow = 0;

                //        string u = userreaderbyday[4].ToString();
                //        string d = userreaderbyday[5].ToString();
                //        upl = decimal.Parse(u);
                //        dow = decimal.Parse(d);

                //        string completeUpload = CalculateSize(upl);
                //        string completeDownload = CalculateSize(dow);



                //        HyperLink link = new HyperLink()
                //        {
                //            CssClass = "fontSegoe linkLabelUser",
                //            NavigateUrl = "",
                //            Text = "<span class=\"firstViewElement\">" + host + "</span><span class=\"secondElement\">" + completeUpload + "</span> <span class=\"thirdElement\">" + completeDownload + "</span>"
                //        };

                //        linkArea.Controls.Add(link);
                //    }
                //    userreaderbyday.Close();
                //    conn.Close();
                //    connread.Close();
                //    break;
                //case "sorteduserfilterbyday":
                //    MySqlCommand sorteduserparambyday = new MySqlCommand(comm, conn);
                //    MySqlDataReader sorteduserreaderbyday = sorteduserparambyday.ExecuteReader();
                //    DateTime sorteddayuserbyday = DateTime.Parse(Request.QueryString["day"]);

                //    string sorteditembyday = Request.QueryString["client"];

                //    string[] sortedchapterbyday = sorteditembyday.Split('\\');
                //    string sortedsearchbyday = sortedchapterbyday[0] + @"\\" + sortedchapterbyday[1];

                //    string fullNamesortedbyday = getUser(sortedchapterbyday[1], "fio");

                //    HyperLink sortedbacklinkuserbyday = new HyperLink()
                //    {
                //        CssClass = "fontSegoe backLabel",
                //        NavigateUrl = $"~/Default.aspx?param=backfilterbyday",
                //        Text = "Вернуться<br/><br/>"
                //    };

                //    linkArea.Controls.Add(sortedbacklinkuserbyday);

                //    Label sortedlabeluserbyday = new Label()
                //    {
                //        CssClass = "fontSegoe grayFont titleLabel",
                //        Text = $"Данные на пользователя: {fullNamesortedbyday} ({Request.QueryString["client"]})<br/><br/>"
                //    };

                //    Label sortedfilteruserdaybyday = new Label()
                //    {
                //        CssClass = "fontSegoe grayFont titleLabel",
                //        Text = $"за {sorteddayuserbyday.ToString("D")}<br/><br/>"
                //    };

                //    linkArea.Controls.Add(sortedlabeluserbyday);
                //    linkArea.Controls.Add(sortedfilteruserdaybyday);

                //    string sortedbybyday = Request.QueryString["by"];
                //    string sorteddirbyday = Request.QueryString["dir"];
                //    string[] sortedreqbyday = CalculateDir(sortedbybyday, sorteddirbyday);

                //    Label sortedsortedLabeluserbyday = new Label()
                //    {
                //        CssClass = "fontSegoe grayFont sortedlabelUser",
                //        Text = $"<a class=\"sortedLinkUser\" href=\"Default.aspx?param=sorteduserfilterbyday&by=target_host&dir={sortedreqbyday[0]}&day=" + sorteddayuserbyday.ToString("yyyy-MM-dd") + $"&client={Request.QueryString["client"]}\">Хост{sortedreqbyday[3]}</a><a class=\"sortedLink\" href=\"Default.aspx?param=sorteduserfilterbyday&by=upload&dir={sortedreqbyday[1]}&day=" + sorteddayuserbyday.ToString("yyyy-MM-dd") + $"&client={Request.QueryString["client"]}\">Отдано{sortedreqbyday[4]}</a><a class=\"sortedLink\" href=\"Default.aspx?param=sorteduserfilterbyday&by=download&dir={sortedreqbyday[2]}&day=" + sorteddayuserbyday.ToString("yyyy-MM-dd") + $"&client={Request.QueryString["client"]}\">Загружено{sortedreqbyday[5]}</a><br/><br/>"
                //    };

                //    linkArea.Controls.Add(sortedsortedLabeluserbyday);

                //    while (sorteduserreaderbyday.Read())
                //    {
                //        string host = sorteduserreaderbyday[2].ToString();

                //        decimal upl = 0;
                //        decimal dow = 0;

                //        string u = sorteduserreaderbyday[4].ToString();
                //        string d = sorteduserreaderbyday[5].ToString();
                //        upl = decimal.Parse(u);
                //        dow = decimal.Parse(d);

                //        string sortedcompleteUpload = CalculateSize(upl);
                //        string sortedcompleteDownload = CalculateSize(dow);



                //        HyperLink link = new HyperLink()
                //        {
                //            CssClass = "fontSegoe linkLabelUser",
                //            NavigateUrl = "",
                //            Text = "<span class=\"firstViewElement\">" + host + "</span><span class=\"secondElement\">" + sortedcompleteUpload + "</span> <span class=\"thirdElement\">" + sortedcompleteDownload + "</span>"
                //        };

                //        linkArea.Controls.Add(link);
                //    }
                //    sorteduserreaderbyday.Close();
                //    conn.Close();
                //    connread.Close();
                //    break;
            }
        }

        //Получаем ФИО и отдел нашего пользователя
        static string getUser(string uLogin, string req)
        {
            string filter = string.Format("(&(ObjectClass={0})(sAMAccountName={1}))", "person", uLogin);
            string domain = "[АДРЕС ВАШЕГО ДОМЕНА]";
            string[] properties = new string[] { "fullname" };

            DirectoryEntry adRoot = new DirectoryEntry("LDAP://" + domain, "[ПОЛЬЗОВАТЕЛЬ ДОМЕНА]", "[ПАРОЛЬ ПОЛЬЗОВАТЕЛЯ]");
            DirectorySearcher searcher = new DirectorySearcher(adRoot);
            searcher.SearchScope = System.DirectoryServices.SearchScope.Subtree;
            searcher.ReferralChasing = ReferralChasingOption.All;
            searcher.PropertiesToLoad.AddRange(properties);
            searcher.Filter = filter;

            SearchResult result = searcher.FindOne();
            DirectoryEntry directoryEntry = result.GetDirectoryEntry();

            string displayName = "";
            string lastName = "";

            try
            {
                displayName = directoryEntry.Properties["displayName"][0].ToString();
            }
            catch
            {
                displayName = "Имя не указано";
            }
            try
            {
                lastName = directoryEntry.Properties["department"][0].ToString();
            }
            catch
            {
                lastName = "Подразделение не указано";
            }
            if(req =="fio")
            return displayName +"/" + lastName;
            else
            {
                string[] io = displayName.Split(' ');
                return io[1] + " " + io[2];
            }
            
        }


        //возвращает настройки колонки сортировки
        string[] CalculateDir(string by, string dir)
        {
            switch (by)
            {
                case "user":
                    if (dir == "ASC") return new string[6]{"DESC", "DESC", "DESC", "&#160;&#8593;", "","" };
                    else return new string[6] { "ASC", "DESC", "DESC", "&#160;&#8595;", "",""};
                case "target_host":
                    if (dir == "ASC") return new string[6] { "DESC", "DESC", "DESC", "&#160;&#8593;", "", "" };
                    else return new string[6] { "ASC", "DESC", "DESC", "&#160;&#8595;", "", "" };
                case "upload":
                    if (dir == "ASC") return new string[6] { "DESC", "DESC", "DESC", "", "&#160;&#8593;", "" };
                    else return new string[6] { "DESC", "ASC", "DESC", "", "&#160;&#8595;", "" };
                case "download":
                    if (dir == "ASC") return new string[6] { "DESC", "DESC", "DESC", "", "", "&#160;&#8593;" };
                    else return new string[6] { "DESC", "DESC", "ASC", "", "", "&#160;&#8595;" };
                default:
                    return new string[3];
            }
        }

        //Подсчитываем, сколько B KB MB накачал пользователь
        string CalculateSize(decimal size)
        {
            string complete = "";

            if (size > 1024)
            {
                if (size > 1024 * 1024)
                {
                    if (size > 1024 * 1024 * 1024)
                    {
                        decimal cashe = (decimal)size / (1024 * 1024 * 1024);
                        cashe = Math.Round(cashe, 2);
                        return complete = cashe + " GB";
                    }
                    else
                    {
                        decimal cashe = (decimal)size / (1024 * 1024);
                        cashe = Math.Round(cashe, 2);
                        return complete = cashe + " MB";
                    }
                }
                else
                {
                    decimal cashe = (decimal)size / 1024;
                    cashe = Math.Round(cashe, 2);
                    return complete = cashe + " KB";
                }
            }
            else
            {
                return complete = size.ToString() + " B";
            }
        }

        protected void GetResult_Click(object sender, EventArgs e)
        {
            //string name = Input.Text + " , уебище!";
            //ResultLabel.Text = name;
        }

        protected void hideForm_Click(object sender, EventArgs e)
        {
            hideForm.Visible = false;
            filterList.Visible = false;
            string last = ViewState["LastKey"].ToString();
            string param = ViewState["LastState"].ToString();
            CreateListData(param, last);
        }

        protected void filterList_Click(object sender, EventArgs e)
        {
            hideForm.Visible = true;
            filterList.Visible = true;
            
        }

        protected void saveFilterButton_Click(object sender, EventArgs e)
        {
            string connStr = "server=ubuntu-core-server;user=remote;database=tmgreport;password=FreedoM82;";
            MySqlConnection conn = new MySqlConnection(connStr);
            conn.Open();

            string filterUserText = filterUser.Text.Replace("\r\n","#");
            string[] insertUser = filterUserText.Split('#');
            MySqlCommand delUser = new MySqlCommand("TRUNCATE tmgreport.clientfilter", conn);
            delUser.ExecuteNonQuery();
            for (int i = 0; i < insertUser.Length; i++)
            {
                if (insertUser[i].Trim() != "")
                {
                    string cf = $"INSERT INTO tmgreport.clientfilter (nameClientFilter) VALUES (\"{insertUser[i].Trim()}\")";
                    MySqlCommand insertClientFilter = new MySqlCommand(cf, conn);
                    insertClientFilter.ExecuteNonQuery();
                }
            }


            string filterTargetText = filterTarget.Text.Replace("\r\n", "#");
            string[] insertTarget = filterTargetText.Split('#');
            MySqlCommand delTarget = new MySqlCommand("TRUNCATE tmgreport.targetfilter", conn);
            delTarget.ExecuteNonQuery();
            for (int i = 0; i < insertTarget.Length; i++)
            {
                if(insertTarget[i].Trim() != "")
                {
                    string tf = $"INSERT INTO tmgreport.targetfilter (nameFilter) VALUES (\"{insertTarget[i].Trim()}\")";
                    MySqlCommand insertTargetFilter = new MySqlCommand(tf, conn);
                    insertTargetFilter.ExecuteNonQuery();
                }
            }


            if (superAccess)
            {
                string filterText = filter.Text.Replace("\r\n", "#");
                string[] insertFilter = filterText.Split('#');
                string[] chapter = User.Identity.Name.ToString().Split('\\');
                string search = "";
                try
                {
                    search = chapter[0] + @"\\" + chapter[1];
                }
                catch
                {
                    //search = "ALTVAGON" + @"\\" + "usov.ia";
                }
                MySqlCommand delFilter = new MySqlCommand($"DELETE FROM tmgreport.filter WHERE userfilter=\"{search}\"", conn);
                delFilter.ExecuteNonQuery();

                for (int i = 0; i < insertFilter.Length; i++)
                {
                    if(insertFilter[i].Trim() != "")
                    {
                        string f = $"INSERT INTO tmgreport.filter(userfilter, targetfilter) VALUES(\"{search}\" ,\"{insertFilter[i].Trim()}\")";
                        MySqlCommand insertFilterComm = new MySqlCommand(f, conn);
                        insertFilterComm.ExecuteNonQuery();
                    }
                }
            }

            LoadFilterList(conn);
            conn.Close();
        }



        private void LoadFilterList(MySqlConnection conn)
        {
            MySqlCommand filterUsercomm = new MySqlCommand("SELECT * FROM tmgreport.clientfilter", conn);
            MySqlDataReader filterUserreader = filterUsercomm.ExecuteReader();
            string filterUserText = "";
            while (filterUserreader.Read())
            {
                filterUserText += filterUserreader[1].ToString() + "\r";
            }
            filterUserreader.Close();
            filterUser.Text = filterUserText;

            MySqlCommand filterTargetcomm = new MySqlCommand("SELECT * FROM tmgreport.targetfilter", conn);
            MySqlDataReader filterTargetreader = filterTargetcomm.ExecuteReader();
            string filtertargetText = "";
            while (filterTargetreader.Read())
            {
                filtertargetText += filterTargetreader[1].ToString() + "\r";
            }
            filterTargetreader.Close();
            filterTarget.Text = filtertargetText;

            if (superAccess)
            {
                string[] chapter = User.Identity.Name.ToString().Split('\\');
                string search = "";
                try
                {
                    search = chapter[0] + @"\\" + chapter[1];
                }
                catch
                {
                    //search = "ALTVAGON" + @"\\" + "usov.ia";
                }
                MySqlCommand filtercomm = new MySqlCommand($"SELECT * FROM tmgreport.filter WHERE userfilter=\"{search}\"", conn);
                MySqlDataReader filterreader = filtercomm.ExecuteReader();
                string filterText = "";
                while (filterreader.Read())
                {
                    filterText += filterreader[2].ToString() + "\r";
                }
                filterreader.Close();
                filter.Text = filterText;
            }
        }

        //Кнопка фильтра
        protected void filterButton_Click(object sender, EventArgs e)
        {
            string date0 = "";
            string date1 = "";
            if (Date0.Value != "")
            {
                date0 = DateTime.Parse(Date0.Value).ToString("yyyy-MM-dd");
            }
            if (Date1.Value != "")
            {
                date1 = DateTime.Parse(Date1.Value).ToString("yyyy-MM-dd");
            }
            string user = userDropDown.SelectedItem.ToString();
            
            if(date0 !="" && date1 !="" && user != "Все пользователи")
            {
                Response.Redirect($"Default.aspx?param=filterdefaultfull&day0={date0}&day1={date1}&user={user}");
            }
            if(date0 != "" && date1 != "" && user == "Все пользователи")
            {
                Response.Redirect($"Default.aspx?param=filterdefaultalluser&day0={date0}&day1={date1}");
            }
            if (date0 == "" && date1 != "" && user == "Все пользователи")
            {
                Response.Redirect($"Default.aspx?param=filterdefaultalluserandfor&day1={date1}");
            }
            if (date0 != "" && date1 == "" && user == "Все пользователи")
            {
                Response.Redirect($"Default.aspx?param=filterdefaultalluserandthis&day0={date0}");
            }
            if (date0 == "" && date1 == "" && user != "Все пользователи")
            {
                Response.Redirect($"Default.aspx?param=filterdefaultonlyoneuser&user={user}");
            }
            if (date0 == "" && date1 != "" && user != "Все пользователи")
            {
                Response.Redirect($"Default.aspx?param=filterdefaultoneuserfor&day1={date1}&user={user}");
            }
            if (date0 != "" && date1 == "" && user != "Все пользователи")
            {
                Response.Redirect($"Default.aspx?param=filterdefaultoneuserthis&day0={date0}&user={user}");
            }
        }
    }
}