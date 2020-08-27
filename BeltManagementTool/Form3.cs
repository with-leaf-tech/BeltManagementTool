using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BeltManagementTool {
    public partial class Form3 : Form {
        private string _name = "";
        private string _parts = "";
        private string _user = "";
        private List<string> _detailList = new List<string>();
        private string _itemDataFile = "_item.txt";
        private string _equipDataFile = "_equip.txt";

        public Form3() {
            InitializeComponent();
        }

        public void setItems(string name, string parts, string user, List<string> details) {
            _name = name;
            _parts = parts;
            _user = user;
            _detailList = details;

            textBox1.Text = "";
            textBox1.Text += "ユーザー:" + _user + Environment.NewLine;
            textBox1.Text += Environment.NewLine;

            if (_name.Length > 0) {
                textBox1.Text += "名前:" + _name + Environment.NewLine;
                textBox1.Text += Environment.NewLine;
                textBox1.Text += "部位:" + _parts + Environment.NewLine;
                textBox1.Text += Environment.NewLine;

                textBox1.Text += "詳細:" + Environment.NewLine;
            }
            else {
                textBox1.Text += "アイテム:" + _parts + Environment.NewLine;
            }

            for (int i = 0; i < _detailList.Count; i++) {
                textBox1.Text += _detailList[i] + Environment.NewLine;
            }
            textBox1.SelectionStart = 0;
        }

        public void setItems(string name, string parts, string user, string count) {
            _name = name;
            _parts = parts;
            _user = user;

            textBox1.Text = "";
            textBox1.Text += "ユーザー:" + _user + Environment.NewLine;
            textBox1.Text += Environment.NewLine;

            textBox1.Text += "アイテム:" + _parts + Environment.NewLine;

            string[] data = File.ReadAllLines(user + _itemDataFile);
            data = data.Where(x => x.Contains(parts + ",")).ToArray();

            for (int i = 0; i < data.Length; i++) {
                _detailList.Add(data[i]);
                textBox1.Text += data[i] + Environment.NewLine;
            }
            textBox1.SelectionStart = 0;
        }

        private void button3_Click(object sender, EventArgs e) {
            this.Hide();
        }

        private void button1_Click(object sender, EventArgs e) {
            string saveFile = "";

            StringBuilder sb = new StringBuilder();
            StringBuilder updateSb = new StringBuilder();
            if (_name.Length > 0) {
                saveFile = _user + _equipDataFile;
                sb.Append(_name + "\t");
                sb.Append(_parts + "\t");
                sb.Append(string.Join(" ", _detailList));

                string[] updateText = textBox1.Text.Replace("\r\n", "\n").Split(new char[] { '\n' });
                for(int i = 0; i < updateText.Length; i++) {
                    //if(updateText[i].Contains("ユーザー:")) {
                    //    updateSb.Append(updateText[i].Replace("ユーザー:", "") + "\t");
                    //}
                    if (updateText[i].Contains("名前:")) {
                        updateSb.Append(updateText[i].Replace("名前:", "") + "\t");
                    }
                    if (updateText[i].Contains("部位:")) {
                        updateSb.Append(updateText[i].Replace("部位:", "") + "\t");
                    }
                    if (updateText[i].Contains("詳細:")) {
                        for (int j = i + 1; j < updateText.Length; j++) {
                            if(updateText[j].Length > 0) {
                                if(j != i + 1) {
                                    updateSb.Append(" ");
                                }
                                updateSb.Append(updateText[j]);
                            }
                        }
                    }
                }


                if (File.Exists(saveFile)) {
                    string[] data = File.ReadAllLines(saveFile);
                    for(int i = 0; i < data.Length; i++) {
                        if(data[i] == sb.ToString()) {
                            data[i] = updateSb.ToString();
                        }
                    }
                    File.WriteAllLines(saveFile, data);
                }
            }
            else {
                saveFile = _user + _itemDataFile;
                List<string> appendList = new List<string>();
                string[] updateText = textBox1.Text.Replace("\r\n", "\n").Split(new char[] { '\n' });
                for (int i = 0; i < updateText.Length; i++) {
                    //if (updateText[i].Contains("ユーザー:")) {
                    //    updateSb.Append(updateText[i].Replace("ユーザー:", "") + " ");
                    //}
                    if (updateText[i].Contains("アイテム:")) {
                        for (int j = i + 1; j < updateText.Length; j++) {
                            if (updateText[j].Length > 0) {
                                appendList.Add(updateText[j]);
                            }
                        }
                    }
                }
                if (File.Exists(saveFile)) {
                    List<string> data = File.ReadAllLines(saveFile).ToList();
                    for (int i = 0; i < data.Count; i++) {
                        if (data[i] == _detailList[0]) {
                            data.RemoveAt(i);
                            _detailList.RemoveAt(0);
                            i--;
                            if(_detailList.Count == 0) {
                                break;
                            }
                        }
                    }
                    data.AddRange(appendList);
                    File.WriteAllLines(saveFile, data);
                }
            }
            this.Hide();
        }

        private void button2_Click(object sender, EventArgs e) {
            string saveFile = "";

            StringBuilder sb = new StringBuilder();
            StringBuilder updateSb = new StringBuilder();
            if (_name.Length > 0) {
                saveFile = _user + _equipDataFile;
                sb.Append(_name + "\t");
                sb.Append(_parts + "\t");
                sb.Append(string.Join(" ", _detailList));

                if (File.Exists(saveFile)) {
                    List<string> data = File.ReadAllLines(saveFile).ToList();
                    for (int i = 0; i < data.Count; i++) {
                        if (data[i] == sb.ToString()) {
                            data.RemoveAt(0);
                            i--;
                        }
                    }
                    File.WriteAllLines(saveFile, data);
                }
            }
            else {
                saveFile = _user + _itemDataFile;
                if (File.Exists(saveFile)) {
                    List<string> data = File.ReadAllLines(saveFile).ToList();
                    data = data.Where(x => !x.Contains(_parts + ",")).ToList();

                    File.WriteAllLines(saveFile, data);
                }
            }
            this.Hide();
        }
    }
}
