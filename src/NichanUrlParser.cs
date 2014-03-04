using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using System.IO;
using System.Text.RegularExpressions;
using System.Net.Http;
using System.Collections.ObjectModel;

namespace NichanUrlParser
{
    public class NichanUrlParser
    {
        private static string settingFile = "parserSetting.xml";
        private static XmlDocument xml = new XmlDocument();
        private string parseUrl = "";
        private int bbsId = 0;
        // したらば、わいわいなどの運営サイト名
        private string bbsType = "";
        // 板名
        private string bbsName = "";
        // 板URL
        private string baseUrl = "";
        // スレッドIDを除いた全スレッド共通部分のURL
        private string threadRootUrl = "";
        // 現在開いているスレッド名
        private string threadName = "";
        // 現在開いているスレッドURL
        private string threadUrl = "";
        // 現在開いているスレッドID
        private string threadId = "";
        // 現在開いているスレッドのdatファイルURL
        private string datUrl = "";
        // subject.txtの区切り文字
        private string subjectDelimStrings = "";
        // datファイルの区切り文字
        private string datDelimStrings = "";
        // subject.txtの区切り文字
        private string threadRootUrlFormat = "";
        private string encoding = "";
        private string bbsEncoding = "";
        public ObservableCollection<Subject> listSubjects = new ObservableCollection<Subject>();
        public NichanUrlParser()
        {
            if (File.Exists(settingFile))
            {//ファイルが有るかどうか
                try
                {
                    Console.WriteLine("読込OK");
                    xml.PreserveWhitespace = true;
                    xml.Load(settingFile);
                }
                catch (Exception ex)
                {
                    System.Console.WriteLine(ex.Message);//エラーメッセージ表示
                }
            }
            else
            {
                Console.WriteLine("設定ファイルが存在しません。");
            }
        }
        // 取得するURLの設定関数
        public void setUrl(string url){
            parseUrl = url;
        }

        //以下各種URLの取得関数
        public string getBaseUrl()
        {
            return baseUrl;
        }
        public string getThreadRootUrl()
        {
            return threadRootUrl;
        }
        public string getThreadUrl()
        {
            return threadUrl;
        }
        public string getThreadId()
        {
            return threadId;
        }
        public string getDatUrl()
        {
            return datUrl;
        }
        public string getThreadName()
        {
            return threadName;
        }

        // 現在のURLのメタデータ取得
        public string getBbsType()
        {
            return bbsType;
        }
        public string getBbsName()
        {
            return bbsName;
        }

        // セッティングURLの生出力(デバッグ用)
        public string getParseSettingText()
        {
            return xml.InnerXml;//テキストエリアにタグごとXMLを表示
        }

        // subject.txtのパーサパターン
        private static string threadTitleRegex = "(?<id>\\d+?)\\.\\w+...*{0}(?<title>.*?)\\((?<resCount>\\d+?)\\)$";


        // URLパースのログを文字列で返す(デバッグ用)
        public string getSetUrlsLog(){
            return bbsId + ":" + bbsType + "\n" + baseUrl + "\n" + threadUrl + "\n" + threadRootUrl + "\n";
        }
        // 板名の取得
        public async Task setBbsNameAsync()
        {
            bbsName = "";
            HttpClient httpClient = new HttpClient();
            if (baseUrl != "")
            {
                // スレトップをすべて読み込み板名を取得
                var stream = await httpClient.GetStreamAsync(baseUrl);
                var baseHtml = "";
                using (var reader = new StreamReader(stream, System.Text.Encoding.GetEncoding(bbsEncoding)))
                {
                    while (!reader.EndOfStream)
                    {
                        baseHtml += reader.ReadLine();
                    }
                }
                // タイトルタグを正規表現パース
                string titleRegex = "<title>(?<title>.*?)</title>";
                Regex retitleRegex = new Regex(titleRegex, RegexOptions.IgnoreCase | RegexOptions.Singleline);
                for (Match m = retitleRegex.Match(baseHtml); m.Success; m = m.NextMatch())
                {
                    bbsName = m.Groups["title"].Value.Trim();
                }
            }
        }

        // スレ一覧の取得
        public async Task setSubjects()
        {
            // subject.txtを読み込み、スレタイを取得
            HttpClient httpClient = new HttpClient();
            var stream = await httpClient.GetStreamAsync(baseUrl + "subject.txt");
            Regex titleRegex = new Regex(string.Format(threadTitleRegex,subjectDelimStrings), RegexOptions.IgnoreCase | RegexOptions.Singleline);
            using (var reader = new StreamReader(stream, System.Text.Encoding.GetEncoding(encoding)))
            {
                listSubjects.Clear();
                while (!reader.EndOfStream)
                {
                    // 1行ごとに正規表現でスレタイを取得
                    var subLine = reader.ReadLine();
                    Match m = titleRegex.Match(subLine);
                    if (m.Success)
                    {
                        Subject bufSubject = new Subject();
                        bufSubject.Title = m.Groups["title"].Value;
                        bufSubject.Url = threadRootUrl + m.Groups["id"].Value + "/";
                        bufSubject.ResCount = Int32.Parse(m.Groups["resCount"].Value);
                        DateTime orgTime = DateTime.Parse("1970/1/1 00:00:00");
                        double unixTime = (double)((DateTime.Now.ToFileTimeUtc() - orgTime.ToFileTimeUtc()) / 10000000) - System.Convert.ToDouble(m.Groups["id"].Value);
                        if (unixTime > 0.01f)
                        {
                            double power = System.Convert.ToDouble(bufSubject.ResCount) / unixTime * 60.0f * 60.0f * 24.0f;
                            bufSubject.Power = power;
                        }
                        listSubjects.Add(bufSubject);
                        if (m.Groups["id"].Value == threadId) threadName = m.Groups["title"].Value;
                    }
                }
            }
        }

        // 現在のスレタイの取得
        public async Task setThreadNameAsync()
        {
            threadName = "";
            HttpClient httpClient = new HttpClient();
            if (threadId != "")
            {
                Regex titleRegex = new Regex(string.Format(threadTitleRegex, subjectDelimStrings), RegexOptions.IgnoreCase | RegexOptions.Singleline);
                // subject.txtを読み込み、スレタイを取得
                var stream = await httpClient.GetStreamAsync(baseUrl + "subject.txt");
                using (var reader = new StreamReader(stream, System.Text.Encoding.GetEncoding(encoding)))
                {
                    while (!reader.EndOfStream)
                    {
                        // 1行ごとに正規表現でスレタイを取得
                        var subLine = reader.ReadLine();
                        Match m = titleRegex.Match(subLine);
                        if(m.Success)
                        {
                            if (m.Groups["id"].Value == threadId)
                            { 
                                threadName = m.Groups["title"].Value;
                                break;
                            }
                        }
                    }
                }
            }
        }
        
        public bool setUrls()
        {
            string baseUrlRegex = "";
            string threadUrlRegex = "";
            string datUrlFormat = "";
            baseUrl = "";
            threadRootUrl = "";
            threadUrl = "";
            datUrl = "";
            bbsName = "";
            bbsType = "";
            encoding = "";
            bbsEncoding = "";
            threadId = "";
            subjectDelimStrings = "";
            datDelimStrings = "";
            threadRootUrlFormat = "";
            bool ret = false;
            // 設定ファイルからドメイン判別正規表現を読み込み、渡されたURLのチェック
            XmlNodeList eDomain = xml.GetElementsByTagName("domainRegex");
            for (int i = 0; i < eDomain.Count; i++) 
            {
                if (System.Text.RegularExpressions.Regex.IsMatch(
                    parseUrl,
                    @eDomain[i].InnerText,
                    System.Text.RegularExpressions.RegexOptions.ECMAScript))
                {
                    // マッチした設定の定義を読み込み
                    bbsId               = Int32.Parse(eDomain[i].ParentNode.SelectSingleNode("id").InnerText);
                    bbsType             = eDomain[i].ParentNode.SelectSingleNode("type").InnerText;
                    baseUrlRegex        = eDomain[i].ParentNode.SelectSingleNode("baseUrlRegex").InnerText;
                    threadUrlRegex      = eDomain[i].ParentNode.SelectSingleNode("threadUrlRegex").InnerText;
                    datUrlFormat        = eDomain[i].ParentNode.SelectSingleNode("datUrlFormat").InnerText;
                    encoding            = eDomain[i].ParentNode.SelectSingleNode("encoding").InnerText;
                    bbsEncoding         = eDomain[i].ParentNode.SelectSingleNode("bbsEncoding").InnerText;
                    subjectDelimStrings = eDomain[i].ParentNode.SelectSingleNode("subjectDelimStrings").InnerText;
                    datDelimStrings     = eDomain[i].ParentNode.SelectSingleNode("datDelimStrings").InnerText;
                    threadRootUrlFormat = eDomain[i].ParentNode.SelectSingleNode("threadRootUrlFormat").InnerText;
                    // URL種別の判別とクラスオブジェクト変数へのURL保存

                    // スレッドURLパースのRegexオブジェクトを作成
                    System.Text.RegularExpressions.Regex rThread =
                        new System.Text.RegularExpressions.Regex(
                            @threadUrlRegex,
                            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    System.Text.RegularExpressions.Match mThread = rThread.Match(parseUrl);
                    if (mThread.Success)
                    {
                        baseUrl         = mThread.Groups[1].Value + mThread.Groups[2].Value + "/";
                        threadUrl       = mThread.Groups[0].Value;
                        threadId        = mThread.Groups[3].Value;
                        threadRootUrl   = string.Format(threadRootUrlFormat, mThread.Groups[1].Value, mThread.Groups[2].Value);
                        datUrl          = string.Format(datUrlFormat, mThread.Groups[1].Value, mThread.Groups[2].Value, mThread.Groups[3].Value);
                        ret = true;
                    }
                    else
                    {
                        // ベースURLパースのRegexオブジェクトを作成
                        System.Text.RegularExpressions.Regex rBase =
                            new System.Text.RegularExpressions.Regex(
                                @baseUrlRegex,
                                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                        System.Text.RegularExpressions.Match mBase = rBase.Match(parseUrl);
                        if(mBase.Success)
                        {
                            //一致した対象が見つかったときキャプチャした部分文字列を表示
                            baseUrl = mBase.Groups[1].Value + mBase.Groups[2].Value + "/";
                            threadRootUrl = string.Format(threadRootUrlFormat, mBase.Groups[1].Value, mBase.Groups[2].Value);
                            ret = true;
                        }
                    }
                }
            }
            return ret;
        }

    }
    // subject.txtを1行格納するクラス
    public class Subject
    {
        private string title;
        private string url;
        private int resCount;
        private double power;
        public string Title
        {
            get { return title; }
            set { title = value; }
        }
        public string Url
        {
            get { return url; }
            set { url = value; }
        }
        public int ResCount
        {
            get { return resCount; }
            set { resCount = value; }
        }
        public double Power
        {
            get { return power; }
            set { power = value; }
        }
    }
}
