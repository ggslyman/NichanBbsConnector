using System;
using System.Collections.Generic;
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
using System.Collections;
using System.Windows.Forms;
using System.Collections.ObjectModel;
using NichanBbsConnector;

namespace NichanUrlParserUi
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        static public MainWindow Instance;
        private static NichanBbsConnector.NichanBbsConnector nbc = new NichanBbsConnector.NichanBbsConnector();
        private ObservableCollection<Bbs> listBbs = new ObservableCollection<Bbs>();

        private async Task getUrl(string url)
        {
            nbc.setUrl(url);
            nbc.setUrls();
            await nbc.setNamesAndSubjectsAsync();
            Bbs bufBbs = new Bbs();
            bufBbs.Title = nbc.BbsName;
            bufBbs.Url = nbc.BaseUrl;
            bufBbs.Order = 1;
            listBbs.Add(bufBbs);
            datGridBbsList.ItemsSource = listBbs;
            dataGridSubjects.ItemsSource = nbc.ListSubjects;
            setLabel();
        }

        private async Task getThread()
        {
            await nbc.getThreadLines();
            datGridThread.ItemsSource = nbc.ListTreadLines;
        }

        private void setLabel()
        {
            this.Title = nbc.ThreadName + " - " + nbc.BbsName;
            //labelBase.Content = nps.BaseUrl;
            //labelThread.Content = nps.ThreadUrl;
            //labelDat.Content = nps.DatUrl;
            //labelBbsName.Content = ;
            //labelThreadName.Content = nps.ThreadName;
        }

        private void addMsg(string msg)
        {
            textMsg.Text = msg + "\n" + textMsg.Text;

        }
        private void webLoadControlEnabled(bool enabled){
            dataGridSubjects.IsEnabled = enabled;
            datGridBbsList.IsEnabled = enabled;
            listBoxUrl.IsEnabled = enabled;
            buttonSetUrl.IsEnabled = enabled;
            buttonReroad.IsEnabled = enabled;
        }

        public MainWindow()
        {
            try
            {
                Instance = this;
            }
            catch (Exception e)
            {
                addMsg(e.Message);
            }
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            listBoxUrl.SelectedIndex = 0;
        }

        private async void listBoxUrl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            webLoadControlEnabled(false);
            if (listBoxUrl.SelectedIndex >= 0)
            {
                await getUrl(listBoxUrl.SelectedItem.ToString());
                await getThread();
            }
            webLoadControlEnabled(true);
        }

        private void formMain_Loaded(object sender, RoutedEventArgs e)
        {
            //listBoxUrl.SelectedIndex = 0;
        }

        private async void buttonSetUrl_Click(object sender, RoutedEventArgs e)
        {
            webLoadControlEnabled(false);
            listBoxUrl.Items.Add(textBoxUrl.Text);
            
            await getUrl(textBoxUrl.Text);
            await getThread();

            webLoadControlEnabled(true);
        }

        private async void dataGridSubjects_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (dataGridSubjects.Items.Count > 0)
            {
                webLoadControlEnabled(false);
                NichanBbsConnector.Subject subject = (NichanBbsConnector.Subject)dataGridSubjects.SelectedItem;

                await getUrl(subject.Url);
                await getThread();

                webLoadControlEnabled(true);
            }
        }

        private async void buttonReroad_Click(object sender, RoutedEventArgs e)
        {
            webLoadControlEnabled(false);
            await getThread();
            webLoadControlEnabled(true);
        }

    }
    class Bbs
    {
        private string title;
        private string url;
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
    }

}
