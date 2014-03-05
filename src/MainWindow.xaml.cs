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
        private static NichanUrlParser.NichanUrlParser nps;
        public MainWindow()
        {
            try
            {
                nps = new NichanUrlParser.NichanUrlParser();
                Instance = this;
            }
            catch (Exception e)
            {
                addMsg(e.Message);
            }
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            dataGridSubjects.ItemsSource = nps.ListSubjects;
            listViewThreadView.ItemsSource = nps.TreadLineSubject;
            listBoxUrl.SelectedIndex = 0;
        }

        private async void listBoxUrl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            dataGridSubjects.IsEnabled = false;
            listBoxUrl.IsEnabled = false;
            buttonSetUrl.IsEnabled = false;
            if (listBoxUrl.SelectedIndex >= 0)
            {
                await getUrl(listBoxUrl.SelectedItem.ToString());
                await nps.getThreadLines();
            }
            buttonSetUrl.IsEnabled = true;
            listBoxUrl.IsEnabled = true;
            dataGridSubjects.IsEnabled = true;
        }

        private void formMain_Loaded(object sender, RoutedEventArgs e)
        {
            //listBoxUrl.SelectedIndex = 0;
        }
        // スレ一覧の項目ダブルクリック
        private async void dataGridSubjects_MouseDoubleClik(object sender, MouseButtonEventArgs e)
        {
            dataGridSubjects.IsEnabled = false;
            listBoxUrl.IsEnabled = false;
            buttonSetUrl.IsEnabled = false;
            NichanUrlParser.Subject subject = (NichanUrlParser.Subject)dataGridSubjects.SelectedItem;
    
            await getUrl(subject.Url);
            await nps.getThreadLines();
            buttonSetUrl.IsEnabled = true;
            listBoxUrl.IsEnabled = true;
            dataGridSubjects.IsEnabled = true;
        }


        private async void buttonSetUrl_Click(object sender, RoutedEventArgs e)
        {
            dataGridSubjects.IsEnabled = false;
            listBoxUrl.IsEnabled = false;
            buttonSetUrl.IsEnabled = false;
            listBoxUrl.Items.Add(textBoxUrl.Text);
            
            await getUrl(textBoxUrl.Text);
            await nps.getThreadLines();

            buttonSetUrl.IsEnabled = true;
            listBoxUrl.IsEnabled = true;
            dataGridSubjects.IsEnabled = true;
        }

        private async Task getUrl(string url)
        {
            nps.setUrl(url);
            nps.setUrls();
            await nps.setNamesAndSubjectsAsync();
            setLabel();
        }

        private void setLabel()
        {
            labelBase.Content = nps.BaseUrl;
            labelThread.Content = nps.ThreadUrl;
            labelDat.Content = nps.DatUrl;
            labelBbsName.Content = nps.BbsName;
            labelThreadName.Content = nps.ThreadName;
        }
        
        private void addMsg(string msg)
        {
            textMsg.Text = msg + "\n" + textMsg.Text;

        }

    }

}
