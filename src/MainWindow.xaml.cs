using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using NichanUrlParserUi;
using NichanUrlParser;
using System.Collections;
using System.Windows.Forms;

namespace NichanUrlParserUi
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        static public MainWindow Instance;
        private static NichanUrlParser.NichanUrlParser nps = new NichanUrlParser.NichanUrlParser();
        public MainWindow()
        {
            Instance = this;
        }

        // リストビューのソート処理
        // http://gushwell.ldblog.jp/archives/52306883.html
        // 

        private async void Button_Click_1(object sender, RoutedEventArgs e)
        {
            buttonGetName.IsEnabled = false;
            await nps.setBbsNameAsync();
            await nps.setThreadNameAsync();
            await nps.setSubjects();

            textMsg.Text += nps.getBbsName() + "\n";
            textMsg.Text += nps.getThreadName() + "\n";
            textMsg.Text += nps.getThreadRootUrl() + "\n";
            listViewSubject.ItemsSource = nps.getListSubject();
            buttonGetName.IsEnabled = true;
        }

        private void listBoxUrl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (listBoxUrl.SelectedIndex >= 0)
            {
                nps.setUrl(listBoxUrl.SelectedItem.ToString());
                nps.setUrls();
                textMsg.Text += nps.getSetUrlsLog();
                labelBase.Content = nps.getBaseUrl();
                labelThread.Content = nps.getThreadUrl();
                labelDat.Content = nps.getDatUrl();
            }
        }

        private void formMain_Loaded(object sender, RoutedEventArgs e)
        {
            listBoxUrl.SelectedIndex = 0;
        }
    }

}
