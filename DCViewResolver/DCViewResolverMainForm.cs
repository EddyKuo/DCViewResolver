using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DCViewResolver
{
    public partial class DCViewResolverMainForm : Form
    {
        public DCViewResolverMainForm()
        {
            InitializeComponent();
            comboBox1.DataSource = StringValues.brands;
            comboBox3.DataSource = StringValues.classes;
            listView1.GetType().GetProperty("DoubleBuffered", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(listView1, true, null);
            GetLocation();

            CheckForIllegalCrossThreadCalls = false;
        }

        private void GetLocation()
        {
            // /html/body/main/div/div[3]/div/div[1]/aside/div[1]/div[2]/div/select/option[1]
            // /html/body/main/div/div[3]/div/div[1]/aside/div[1]/div[2]/div/select/option[2]
            // ...
            // /html/body/main/div/div[3]/div/div[1]/aside/div[1]/div[2]/div/select/option[23]

            HtmlWeb client = new HtmlWeb();
            HtmlAgilityPack.HtmlDocument doc = client.Load(dcviewURL.Replace("{number}", "1"));

            string queryXPath = "/html/body/main/div/div[3]/div/div[1]/aside/div[1]/div[2]/div/select/option[{number}]";

            for (int i = 1; i <= int.MaxValue; ++i)
            {
                
                HtmlNodeCollection nodes = doc.DocumentNode.SelectNodes(queryXPath.Replace("{number}", i.ToString()));
                if (nodes == null || nodes.Count == 0)
                    break;

                string location = nodes[0].InnerText.Replace("\t", "").Replace("\n", "");
                comboBox2.Items.Add(location);
            }
        }


        const string dcviewURL = "http://market.dcview.com/?page={number}";
        //const string postBaseURL = "http://market.dcview.com/post/{threadNumber}";


        public class DataStorage
        {
            public string brand;
            public bool complete;
            public string seekOrSell;
            public string location;
            public string information;
            public string link;
            public string threadNumber;
            public string price;
            public string user;
            public DateTime postDate;
            public DateTime lastUpdate;
        }

        CultureInfo provider = CultureInfo.InvariantCulture;
        string dateTimeFormat = "yyyy-MM-dd";
        private Dictionary<int, DataStorage> dataStore = new Dictionary<int, DataStorage>();
        private void button1_Click(object sender, EventArgs e)
        {
            button1.Enabled = false;
            new Thread(() =>
            {
                dataStore.Clear();
                HtmlWeb client = new HtmlWeb();
                for (int webCount = 1; webCount <= 20; ++webCount)
                {
                    HtmlAgilityPack.HtmlDocument doc = client.Load(dcviewURL.Replace("{number}", webCount.ToString()));
                    HtmlNodeCollection nodes = doc.DocumentNode.SelectNodes("/html/body/main/div/div[2]/div[1]/div/table/tbody/tr[*]");
                    for (int i = 1; i < 40; i += 2)
                    {
                        // select 2, 4, 6, ..., 40

                        DataStorage ds = new DataStorage();

                        ds.brand = nodes[i].SelectNodes("td[1]")[0].InnerText;

                        string[] topic = nodes[i].SelectNodes("td[2]/a")[0].InnerText.Replace("\t", "").Replace("&nbsp;", "").Split('\n');

                        if (topic.Length == 9)
                        {
                            ds.complete = false;
                            ds.seekOrSell = topic[2];
                            ds.location = topic[5];
                            ds.information = topic[6];
                        }
                        else if (topic.Length == 10)
                        {
                            ds.complete = true;
                            ds.seekOrSell = topic[3];
                            ds.location = topic[6];
                            ds.information = topic[7];
                        }

                        ds.link = nodes[i].SelectNodes("td[2]/a")[0].GetAttributeValue("href", "");
                        ds.threadNumber = ds.link.Substring(ds.link.LastIndexOf("/") + 1);

                        ds.price = nodes[i].SelectNodes("td[3]/small")[0].InnerText;
                        ds.user = nodes[i].SelectNodes("td[4]/small/a")[0].InnerText;
                        string postDate = nodes[i].SelectNodes("td[5]/small")[0].InnerText;
                        string lastUpdate = nodes[i].SelectNodes("td[6]/small")[0].InnerText;

                        if (postDate.Length != 0)
                        {
                            ds.postDate = DateTime.ParseExact(postDate, dateTimeFormat, provider);
                        }

                        if (lastUpdate.Length != 0)
                        {
                            ds.lastUpdate = DateTime.ParseExact(lastUpdate, dateTimeFormat, provider);
                        }

                        int threadNumber = int.Parse(ds.threadNumber);
                        if (!dataStore.ContainsKey(threadNumber))
                            dataStore.Add(threadNumber, ds);
                    }
                }
                SearchAllCondition();
                button1.Enabled = true;
            }).Start();

        }

        private void SetDataOnListView(Dictionary<int, DataStorage> ds)
        {
            listView1.BeginUpdate();
            listView1.Items.Clear();
            foreach (var data in ds)
            {
                ListViewItem lvi = new ListViewItem(data.Value.brand);
                lvi.SubItems.Add(data.Value.information);
                lvi.SubItems.Add(data.Value.price);
                lvi.SubItems.Add(data.Value.user);
                lvi.SubItems.Add(data.Value.postDate.ToShortDateString());
                lvi.SubItems.Add(data.Value.lastUpdate.ToShortDateString());
                lvi.SubItems.Add(data.Value.complete ? "●" : "");
                lvi.SubItems.Add(data.Value.link);
                listView1.Items.Add(lvi);

            }
            listView1.EndUpdate();
        }

        private void ComboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            // brand changed
            SearchAllCondition();

        }

        private void ComboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            // location changed
            SearchAllCondition();
        }

        private void ComboBox3_SelectedIndexChanged(object sender, EventArgs e)
        {
            Console.WriteLine("ComboBox3_SelectedIndexChanged");
        }

        private void TextBox1_TextChanged(object sender, EventArgs e)
        {
            // search key
            SearchAllCondition();
        }

        private void SearchAllCondition()
        {
            Dictionary<int, DataStorage> ds = new Dictionary<int, DataStorage>();
            foreach (var data in dataStore)
            {
                if ((data.Value.brand.Contains(comboBox1.Text, StringComparison.OrdinalIgnoreCase) || comboBox1.Text == "") &&
                    (data.Value.location.Contains(comboBox2.Text, StringComparison.OrdinalIgnoreCase) || comboBox2.Text == "" || comboBox2.Text == "不限地區") &&
                    (data.Value.information.Contains(textBox1.Text, StringComparison.OrdinalIgnoreCase) || textBox1.Text == ""))
                {
                    if((data.Value.seekOrSell == "售" && checkBox1.Checked))
                        ds.Add(data.Key, data.Value);
                    if((data.Value.seekOrSell == "徵" && checkBox2.Checked))
                        ds.Add(data.Key, data.Value);
                }
            }
            SetDataOnListView(ds);
        }

        private void CheckBox1_CheckedChanged(object sender, EventArgs e)
        {
            SearchAllCondition();
        }

        private void CheckBox2_CheckedChanged(object sender, EventArgs e)
        {
            SearchAllCondition();
        }

        private void ListView1_DoubleClick(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count == 1)
            {
                ListViewItem lvi = listView1.SelectedItems[0];
                string url = lvi.SubItems[7].Text;
                System.Diagnostics.Process.Start(url);
            }
        }
    }

    public static class StringExtensions
    {
        public static bool Contains(this string source, string toCheck, StringComparison comp)
        {
            return source?.IndexOf(toCheck, comp) >= 0;
        }
    }
}
