﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.XPath;
using System.IO;
using System.Text.RegularExpressions;
using System.Net.Http;
using System.Web;
using System.Globalization;

namespace NichanBbsConnector
{
    // にちゃん互換掲示板へのアクセスを共通化する為のクラスライブラリ
    public class NichanBbsConnector
    {
        // メンバー変数定義
        // 基本的にprivateで定義し、外部からアクセスが必要なものについては別途ゲッターを定義
        // <summary>
        // 設定ファイル名定義
        // </summary>
        private static string settingFile = "parserSetting.xml";
        // <summary>
        // 設定ファイル解析用オブジェクト
        // </summary>
        private static System.Xml.XmlDocument xml = new System.Xml.XmlDocument();

        // <summary>
        // 解析元URL
        // </summary>
        private string parseUrl = "";
        // <summary>
        // XML設定ファイル上の識別ID(現状、処理には使っていない)
        // </summary>
        private int bbsId = 0;
        // <summary>
        // したらば、わいわいなどの運営サイト名
        // </summary>
        private string bbsType = "";
        // <summary>
        // 板名
        // </summary>
        private string bbsName = "";
        // <summary>
        // 板URL
        // </summary>
        private string baseUrl = "";
        // <summary>
        // スレッドIDを除いた全スレッド共通部分のURL
        // </summary>
        private string threadRootUrl = "";
        // <summary>
        // 現在開いているスレッド名
        // </summary>
        private string threadName = "";
        // <summary>
        // 現在開いているスレッドURL
        // </summary>
        private string threadUrl = "";
        // <summary>
        // 現在開いているスレッドID
        // </summary>
        private string threadId = "";
        // <summary>
        // 現在開いているスレッドのdatファイルURL
        // </summary>
        private string datUrl = "";
        // <summary>
        // subject.txtの区切り文字
        // </summary>
        private string subjectDelimStrings = "";
        // <summary>
        // datファイルの区切り文字
        // </summary>
        private string datDelimStrings = "";
        // <summary>
        // datファイルの差分取得方法
        // range：ヘッダ指定
        // resNo：レス番号指定
        // </summary>
        private string datDiffRequest = "";
        // <summary>
        // スレッドルートURLの変換用フォーマット
        // </summary>
        private string threadRootUrlFormat = "";
        // <summary>
        // datファイルの文字エンコード
        // </summary>
        private string encoding = "";
        // <summary>
        // datファイルの正規表現パーシング定義
        // </summary>
        private string datRegex = "";
        // <summary>
        // 板INDEXの文字エンコード(ほぼあっとちゃんねるず用)
        // </summary>
        private string bbsEncoding = "";

        // <summary>
        // subject.txtのパーサパターン
        // ここは基本的に固定で大丈夫だとは思うが、これだけ定数にするのも微妙？
        // </summary>
        private static string threadTitleRegex = "(?<id>\\d+?)\\.\\w+...*{0}(?<title>.*?)\\((?<resCount>\\d+?)\\)$";

        // <summary>
        // 取得したdatの各種差分取得用データ
        // </summary>
        private long datSize;
        private DateTimeOffset LastModified;
        private bool isSetLastModified = false;


        // <summary>
        // subject.txtの内容を格納するコレクション
        // </summary>
        private ObservableCollection<Subject> listSubjects = new ObservableCollection<Subject>();
        // <summary>
        // スレッドのレス内容を格納するコレクション
        // </summary>
        private ObservableCollection<ThreadLine> listTreadLines = new ObservableCollection<ThreadLine>();

        //以下各種メンバ変数のゲッター
        public string BaseUrl
        {
            get { return baseUrl; }
        }
        public string ThreadRootUrl
        {
            get { return threadRootUrl; }
        }
        public string ThreadUrl
        {
            get { return threadUrl; }
        }
        public string ThreadId
        {
            get { return threadId; }
        }
        public string DatUrl
        {
            get { return datUrl; }
        }
        public string ThreadName
        {
            get { return threadName; }
        }

        public string Encoding
        {
            get { return encoding; }
        }

        public string BbsEncoding
        {
            get { return bbsEncoding; }
        }

        public string DatRegex
        {
            set { datRegex = value; }
            get { return datRegex; }
        }
        // 現在のURLのメタデータ取得
        public string getBbsType
        {
            get { return bbsType; }
        }
        public string BbsName
        {
            get { return bbsName; }
        }

        // <summary>
        // サブジェクトリストを返すゲッター
        // </summary>
        public ObservableCollection<Subject> ListSubjects
        {
            get { return listSubjects; }
        }
        // <summary>
        // スレッドラインリストを返すゲッター
        // </summary>
        public ObservableCollection<ThreadLine> ListTreadLines
        {
            get { return listTreadLines; }
        }

        // 取得するURLの設定関数
        public void setUrl(string url)
        {
            parseUrl = url;
        }


        // <summary>
        // コンストラクタ
        // 設定ファイルの存在チェックのみを行いインスタンスを返す
        // </summary>
        public NichanBbsConnector()
        {
            if (System.IO.File.Exists(@settingFile))
            {
                try
                {
                    xml.PreserveWhitespace = true;
                    xml.Load(settingFile);
                }
                catch (Exception e)
                {
                    throw new NichanBbsConnectorException("NichanBbsConnectorの初期化中にエラーが発生しました" + e.Message + e.StackTrace);
                }
            }
            else
            {
                throw new NichanBbsConnectorException("設定ファイルが見つかりません");
            }
        }

        // セッティングXMLの生出力(デバッグ用)
        public string getParseSettingText()
        {
            return xml.InnerXml;//テキストエリアにタグごとXMLを表示
        }

        // URLパースのログを文字列で返す(デバッグ用)
        public string getSetUrlsLog()
        {
            return bbsId + ":" + bbsType + "\n" + baseUrl + "\n" + threadUrl + "\n" + threadRootUrl + "\n";
        }

        // 現在のdatサイズのバイト数を返す(デバッグ用)
        public long DatSize
        {
            get { return datSize; }
        }

        // 一括コール用ファンクション
        public async Task setNamesAndSubjectsAsync()
        {
            await setBbsNameAsync();
            await setThreadNameAsync();
            await setSubjectsAsync();
        }

        // <summary>
        // 板名の取得
        // </summary>
        public async Task setBbsNameAsync()
        {
            if (baseUrl != "")
            {
                using (System.Net.Http.HttpClient httpClient = new System.Net.Http.HttpClient())
                {
                    bbsName = "";
                    // スレトップをすべて読み込み板名を取得
                    Stream stream = await httpClient.GetStreamAsync(baseUrl);
                    string baseHtml = "";
                    using (StreamReader reader = new StreamReader(stream, System.Text.Encoding.GetEncoding(bbsEncoding)))
                    {
                        while (!reader.EndOfStream)
                        {
                            baseHtml += reader.ReadLine();
                        }
                    }
                    // タイトルタグを正規表現パース
                    string titleRegex = "<title>(?<title>.*?)</title>";
                    Regex retitleRegex = new Regex(titleRegex, RegexOptions.IgnoreCase | RegexOptions.Singleline);
                    Match m = retitleRegex.Match(baseHtml);
                    if(m.Success)
                    {
                        bbsName = m.Groups["title"].Value.Trim();
                    }
                }
            }
        }

        // <summary>
        // スレ一覧の取得
        // </summary>
        public async Task setSubjectsAsync()
        {
            if (baseUrl != "")
            {
                // subject.txtを読み込み、スレタイを取得
                using (System.Net.Http.HttpClient httpClient = new System.Net.Http.HttpClient())
                {
                    Stream stream = await httpClient.GetStreamAsync(baseUrl + "subject.txt");
                    Regex titleRegex = new Regex(string.Format(threadTitleRegex, subjectDelimStrings), RegexOptions.IgnoreCase | RegexOptions.Singleline);
                    using (StreamReader reader = new StreamReader(stream, System.Text.Encoding.GetEncoding(encoding)))
                    {
                        if (!reader.EndOfStream)
                        {
                            listSubjects.Clear();
                            var subjectOrder = 1;
                            while (!reader.EndOfStream)
                            {
                                // 1行ごとに正規表現でスレタイを取得
                                var subLine = reader.ReadLine();
                                Match m = titleRegex.Match(subLine);
                                if (m.Success)
                                {
                                    Subject bufSubject = new Subject();
                                    bufSubject.Order = subjectOrder++;
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
                }
            }
        }

        // <summary>
        // 現在のスレタイの取得
        // </summary>
        public async Task setThreadNameAsync()
        {
            if (threadId != "")
            {
                using (System.Net.Http.HttpClient httpClient = new System.Net.Http.HttpClient())
                {
                    threadName = "";
                    Regex titleRegex = new Regex(string.Format(threadTitleRegex, subjectDelimStrings), RegexOptions.IgnoreCase | RegexOptions.Singleline);
                    // subject.txtを読み込み、スレタイを取得
                    Stream stream = await httpClient.GetStreamAsync(baseUrl + "subject.txt");
                    using (StreamReader reader = new StreamReader(stream, System.Text.Encoding.GetEncoding(encoding)))
                    {
                        while (!reader.EndOfStream)
                        {
                            // 1行ごとに正規表現でスレタイを取得
                            var subLine = reader.ReadLine();
                            Match m = titleRegex.Match(subLine);
                            if (m.Success)
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
        }
        // <summary>
        // 現在のオブジェクトにスレッドURLが設定されているときに、datを取得してコレクションに追加する関数
        // 参考URL：http://www.studyinghttp.net/range
        // 上記URLを参考にdatの差分取得をする
        // Gzipの時はどうなる？
        // 差分取得について：http://sonson.jp/?p=541
        // 差分についてはしたらばとその他で取得方法違うようなので保留
        // そもそもストリームリーダーでエンコードしてる場合にサーバ側の保有バイト数が取れるかが謎
        // </summary>
        public async Task getThreadLines()
        {
            string lastModifiedFormat = "r";
            if (datUrl != "")
            {
                //clog("\n\n\n\n\n\n");
                HttpClientHandler handler = new HttpClientHandler();
                handler.AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate;
                using (System.Net.Http.HttpClient httpClient = new System.Net.Http.HttpClient(handler))
                {
                    string ifModifiedSince = LastModified.ToString(lastModifiedFormat, CultureInfo.CreateSpecificCulture("en-US"));
                    HttpRequestMessage reqMsg = new HttpRequestMessage();
                    bool clearThreadLines = false;
                    reqMsg.Headers.Add("Cache-Control", "no-cache");
                    reqMsg.Headers.Add("Accept", "text/plain");
                    //clog("現在のdatSize：" + datSize);
                    if (datSize > 0)
                    {
                        reqMsg.Headers.Add("Accept-Encoding", "identity");
                        if (isSetLastModified) reqMsg.Headers.Add("If-Modified-Since", ifModifiedSince);
                        reqMsg.Headers.Add("Range", "bytes= " + (datSize - 1).ToString() + "-");
                    }
                    else
                    {
                        reqMsg.Headers.Add("Accept-Encoding", "gzip");
                    }
                    string reqUrl = datUrl;
                    if (datDiffRequest == "resNo")
                    {
                        reqUrl = datUrl + (listTreadLines.Count + 1).ToString() + "-";
                    }
                    httpClient.BaseAddress = new Uri(reqUrl);
                    //clog("現在のreqUrl：" + reqUrl);
                    Regex rdatRegex = new Regex(datRegex, RegexOptions.IgnoreCase | RegexOptions.Singleline);
                    Task<Stream> stream = null;
                    ObservableCollection<ThreadLine> bufListthreadLines = new ObservableCollection<ThreadLine>();
                    Task<HttpResponseMessage> response = null;
                    try
                    {
                        await Task.Run(() =>
                        {
                            bool getDiff = false;
                            // リクエストヘッダを要求
                            response = httpClient.SendAsync(reqMsg);
                            //clog("要求リクエストヘッダ：" + reqMsg.Headers);
                            //clog("レスポンスコード：" + (Int32)response.Result.StatusCode);
                            //clog("レスポンスのリクエストヘッダ：" + response.Result.RequestMessage.Headers);
                            if (
                                response.Result.StatusCode == System.Net.HttpStatusCode.OK
                                || response.Result.StatusCode == System.Net.HttpStatusCode.PartialContent
                                || response.Result.StatusCode == System.Net.HttpStatusCode.RequestedRangeNotSatisfiable
                              )
                            {
                                httpClient.DefaultRequestHeaders.Add("Accept", "text/plain");
                                httpClient.DefaultRequestHeaders.Add("Cache-Control", "no-cache");
                                // 実体のリクエスト発生経路
                                if (response.Result.StatusCode == System.Net.HttpStatusCode.OK && listTreadLines.Count == 0)
                                {
                                    // 全件
                                    httpClient.DefaultRequestHeaders.Add("Accept-Encoding", "gzip");
                                }
                                else if (response.Result.StatusCode == System.Net.HttpStatusCode.PartialContent || response.Result.StatusCode == System.Net.HttpStatusCode.OK)
                                {
                                    getDiff = true;
                                    // 差分
                                    httpClient.DefaultRequestHeaders.Add("Accept-Encoding", "identity");
                                    //if (isSetLastModified) httpClient.DefaultRequestHeaders.Add("If-Modified-Since", ifModifiedSince);
                                    if (datDiffRequest == "range")
                                    {
                                        httpClient.DefaultRequestHeaders.Add("Range", "bytes= " + (datSize - 1).ToString() + "-");
                                    }
                                }
                                else if (response.Result.StatusCode == System.Net.HttpStatusCode.RequestedRangeNotSatisfiable)
                                {
                                    // 全件取得しなおし
                                    // 拾得済みdatサイズと格納済みdatをクリア
                                    //clog("データ破損、全件再取得");
                                    datSize = 0;
                                    clearThreadLines = true;
                                    // 全件取得ヘッダ作成
                                    httpClient.DefaultRequestHeaders.Add("Accept-Encoding", "gzip");
                                }
                                // datに更新があればLast-Modifiedを更新
                                if (response.Result.Content.Headers.Contains("Last-Modified"))
                                {
                                    LastModified = (DateTimeOffset)response.Result.Content.Headers.LastModified;
                                    isSetLastModified = true;
                                }
                                else
                                {
                                    LastModified = new DateTimeOffset();
                                    isSetLastModified = false;
                                }
                                // 実体アクセス処理
                                stream = httpClient.GetStreamAsync(reqUrl);
                                // 受信データサイズ計算用Encodingインスタンス
                                Encoding enc = System.Text.Encoding.GetEncoding(encoding);
                                clog(encoding);
                                //enc = System.Text.Encoding.GetEncoding("UTF-8");
                                clog(httpClient.DefaultRequestHeaders.ToString());
                                using (StreamReader reader = new StreamReader(stream.Result, enc))
                                {
                                    //clog(DateTime.Now.ToString() + " : 取得開始");
                                    DateTime dt;
                                    int idx = 0;
                                    while (!reader.EndOfStream)
                                    {
                                        try
                                        {
                                            idx++;
                                            // 差分取得時の先頭1バイト判別
                                            if (idx == 1 && getDiff)
                                            {
                                                char[] charbuffer = new char[1];
                                                int result = 0;
                                                result = reader.Read(charbuffer, 0, 1);
                                                //clog("差分確認");
                                                int code = (int)charbuffer[0];
                                                if (code == 10 || code == 13)
                                                {
                                                    //clog("差分正常");
                                                    continue;
                                                }
                                                else
                                                {
                                                    clog("あぼーん検出");
                                                    datSize = 0;
                                                    clearThreadLines = true;
                                                    break;
                                                }
                                            }
                                            ThreadLine threadLine = new ThreadLine();
                                            string sDatetime = "";
                                            // 1行ごとに正規表現でthreadLineの各要素を取得
                                            string datLine = reader.ReadLine();
                                            datSize += enc.GetByteCount(datLine) + enc.GetByteCount("\n");
                                            //clog("取得データ:" + datLine);
                                            //clog("取得データサイズ : " + datSize.ToString());
                                            //clog(datLine);
                                            Match m = rdatRegex.Match(datLine);
                                            if (m.Success)
                                            {
                                                threadLine.Name = m.Groups["name"].Value;
                                                threadLine.Url = m.Groups["url"].Value;
                                                // わいわいなどは西暦が下2ケタなのでむりくり4ケタに修正
                                                if (Int32.Parse(m.Groups["date"].Value.Substring(0, 1)) >= 8)
                                                {
                                                    sDatetime = "19" + m.Groups["date"].Value + " " + m.Groups["time"].Value;
                                                }
                                                else
                                                {
                                                    sDatetime = "20" + m.Groups["date"].Value + " " + m.Groups["time"].Value;
                                                }
                                                dt = DateTime.Parse(sDatetime);
                                                threadLine.Date = DateTime.Parse(sDatetime);
                                                // 改行をhtmlタグから制御文字に変換、htmlエンコードされた特殊文字(&amp;等)をデコード、
                                                // HTMLタグを除去(>>1等のレス番指定やあっとちゃんねるずのURLにつくアンカータグ除去のため)
                                                // クラスに保存するデータは生データにする方針のため。再度アンカーなどを付けるのは各クライアントに任せる
                                                threadLine.Body = stritpTag(HttpUtility.HtmlDecode(m.Groups["body"].Value.Replace("<br>", "\n")));
                                            }
                                            else
                                            {
                                                // 削除されたレスと判断して空データをセット
                                                threadLine.Name = null;
                                                threadLine.Url = null;
                                                threadLine.Date = new DateTime();
                                                threadLine.Body = null;
                                            }
                                            bufListthreadLines.Add(threadLine);
                                        }
                                        catch (Exception e)
                                        {
                                            clog("Webアクセスに失敗しました。" + e.Message);
                                            throw new NichanBbsConnectorException("Webアクセスに失敗しました。" + e.Message + e.StackTrace);
                                        }
                                        finally
                                        {
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if (response.Result.StatusCode == System.Net.HttpStatusCode.NotModified)
                                {
                                    // 変更なし
                                    // 何もせず非同期スレッドを終了
                                }
                                else
                                {
                                    // その他HTTPレスポンスエラーの処理
                                    throw new NichanBbsConnectorException((Int32)response.Result.StatusCode + " [" + response.Result.StatusCode.ToString() + "] Webアクセスに失敗しました。");
                                }
                            }
                        });
                    }
                    catch (Exception e)
                    {
                        clog("非同期中に何らかのエラーが発生しました。" + e.Message + e.StackTrace);
                        throw new NichanBbsConnectorException("非同期中に何らかのエラーが発生しました。" + e.Message + e.StackTrace);
                    }
                    //Etag  = response.Result.Content.Headers.GetValues("Etag").ToString();
                    //clog("datSize:" + datSize);
                    //System.Console.WriteLine(DateTime.Now.ToString() + " : 取得完了" + bufListthreadLines.Count.ToString() + "件");
                    if (clearThreadLines) listTreadLines.Clear();
                    foreach (ThreadLine res in bufListthreadLines)
                    {
                        listTreadLines.Add(res);
                    }
                }
            }
        }

        // <summary>
        // オブジェクトに設定されたURLから各コンテンツへアクセスするURLやパーシング用パラメータを生成
        // </summary>
        public bool setUrls()
        {
            // 内部処理用変数の初期化
            string baseUrlRegex = "";
            string threadUrlRegex = "";
            string datUrlFormat = "";
            // メンバ変数の初期化
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
            datDiffRequest = "";
            threadRootUrlFormat = "";
            datSize = 0;
            threadName = "";
            bbsName = "";
            listTreadLines.Clear();
            bool ret = false;
            // 設定ファイルからドメイン判別正規表現を読み込み、渡されたURLのチェック
            using (System.Xml.XmlNodeList eDomain = xml.GetElementsByTagName("domainRegex"))
            {
                try
                {
                    if (eDomain.Count == 0) throw new NichanBbsConnectorException("設定ファイルが正しくありません。");
                    for (int i = 0; i < eDomain.Count; i++)
                    {
                        if (System.Text.RegularExpressions.Regex.IsMatch(
                            parseUrl,
                            @eDomain[i].InnerText,
                            System.Text.RegularExpressions.RegexOptions.ECMAScript))
                        {
                            try
                            {
                                // マッチした設定の定義を読み込み
                                System.Xml.XmlNode eBbs = eDomain[i].ParentNode;
                                bbsId = Int32.Parse(eBbs.SelectSingleNode("id").InnerText);
                                bbsType = eBbs.SelectSingleNode("type").InnerText;
                                baseUrlRegex = eBbs.SelectSingleNode("baseUrlRegex").InnerText;
                                threadUrlRegex = eBbs.SelectSingleNode("threadUrlRegex").InnerText;
                                datUrlFormat = eBbs.SelectSingleNode("datUrlFormat").InnerText;
                                encoding = eBbs.SelectSingleNode("encoding").InnerText;
                                bbsEncoding = eBbs.SelectSingleNode("bbsEncoding").InnerText;
                                subjectDelimStrings = eBbs.SelectSingleNode("subjectDelimStrings").InnerText;
                                datDelimStrings = eBbs.SelectSingleNode("datDelimStrings").InnerText;
                                threadRootUrlFormat = eBbs.SelectSingleNode("threadRootUrlFormat").InnerText;
                                datRegex = eBbs.SelectSingleNode("datRegex").InnerText;
                                datDiffRequest = eBbs.SelectSingleNode("datDiffRequest").InnerText;
                            }
                            catch (Exception e)
                            {
                                throw new NichanBbsConnectorException("設定ファイルが正しくありません" + e.Message + e.StackTrace);
                            }

                            // URL種別の判別とクラスオブジェクト変数へのURL保存

                            // スレッドURLパースのRegexオブジェクトを作成
                            System.Text.RegularExpressions.Regex rThread =
                                new System.Text.RegularExpressions.Regex(
                                    @threadUrlRegex,
                                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                            System.Text.RegularExpressions.Match mThread = rThread.Match(parseUrl);
                            if (mThread.Success)
                            {
                                baseUrl = mThread.Groups[1].Value + mThread.Groups[2].Value + "/";
                                threadUrl = mThread.Groups[0].Value;
                                threadId = mThread.Groups[3].Value;
                                threadRootUrl = string.Format(threadRootUrlFormat, mThread.Groups[1].Value, mThread.Groups[2].Value);
                                datUrl = string.Format(datUrlFormat, mThread.Groups[1].Value, mThread.Groups[2].Value, mThread.Groups[3].Value);
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
                                if (mBase.Success)
                                {
                                    //一致した対象が見つかったときキャプチャした部分文字列を表示
                                    baseUrl = mBase.Groups[1].Value + mBase.Groups[2].Value + "/";
                                    threadRootUrl = string.Format(threadRootUrlFormat, mBase.Groups[1].Value, mBase.Groups[2].Value);
                                    ret = true;
                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    throw new NichanBbsConnectorException("例外が発生しました" + e.Message + e.StackTrace);
                }

            }
            return ret;
        }

        public static string stritpTag(string str)
        {
            return Regex.Replace(str, "<.*?>", String.Empty);
        }

        public void clog(string msg)
        {
#if DEBUG
            System.Console.WriteLine(msg);
#endif
        }
    }
    class NichanBbsConnectorException : System.ApplicationException
    {
        // メッセージの始めにクラス名を付けてみただけ。
        public NichanBbsConnectorException(string msg)
            : base("NichanUrlParserException : " + msg)
        {
        }

    }
    // <summary>
    // subject.txtを1行に相当するクラス
    // </summary>
    public class Subject
    {
        private string title;
        private string url;
        private int resCount;
        private double power;
        private int order;
        public int Order
        {
            get { return order; }
            set { order = value; }
        }
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
    // <summary>
    // datファイルの1行に相当するクラス
    // </summary>
    public class ThreadLine
    {
        private int number;
        private string name;
        private string url;
        private DateTime date;
        private string body;
        private string id;
        public int Number
        {
            get { return number; }
            set { number = value; }
        }
        public string Name
        {
            get { return name; }
            set { name = value; }
        }
        public string Url
        {
            get { return url; }
            set { url = value; }
        }
        public DateTime Date
        {
            set { date = value; }
        }
        public string DateFormat
        {
            get { return date.ToString(); }
        }
        public string Body
        {
            get { return body; }
            set { body = value; }
        }
        public string Id
        {
            get { return id; }
            set { id = value; }
        }
    }
}
