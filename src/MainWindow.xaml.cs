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
using System.Collections.ObjectModel;

namespace NichanUrlParserUi
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        static public MainWindow Instance;
        private static NichanUrlParser.NichanUrlParser nps;
        private ObservableCollection<bbs> listBbs;

        private async Task getUrl(string url)
        {
            nps.setUrl(url);
            nps.setUrls();
            await nps.setNamesAndSubjectsAsync();
            addMsg(nps.ListSubjects.Count.ToString());
            setLabel();
        }

        private void setLabel()
        {
            this.Title = nps.ThreadName + " - " + nps.BbsName;
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
            listBoxUrl.IsEnabled = enabled;
            buttonSetUrl.IsEnabled = enabled;
        }

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
            listViewThreadView.ItemsSource = nps.ListTreadLines;
            listBoxUrl.SelectedIndex = 0;
        }

        private async void listBoxUrl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            webLoadControlEnabled(false);
            if (listBoxUrl.SelectedIndex >= 0)
            {
                await getUrl(listBoxUrl.SelectedItem.ToString());
                await nps.getThreadLines();
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
            await nps.getThreadLines();

            webLoadControlEnabled(true);
        }

        private async void dataGridSubjects_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (dataGridSubjects.Items.Count > 0)
            {
                webLoadControlEnabled(false);
                NichanUrlParser.Subject subject = (NichanUrlParser.Subject)dataGridSubjects.SelectedItem;

                await getUrl(subject.Url);
                await nps.getThreadLines();

                webLoadControlEnabled(true);
            }
        }

    }
    class bbs
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
