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
    public partial class Form2 : Form {

        private Form _parent = null;
        private string _name = "";
        private string _parts = "";
        private string _user = "";
        private List<string> _detailList = new List<string>();
        private string _itemDataFile = "_item.txt";
        private string _equipDataFile = "_equip.txt";

        public Form2() {
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

        private void button2_Click(object sender, EventArgs e) {
            this.Hide();
        }

        private void button1_Click(object sender, EventArgs e) {
            string saveFile = "";

            StringBuilder sb = new StringBuilder();
            if (_name.Length > 0) {
                saveFile = _user + _equipDataFile;
                sb.Append(_name + "\t");
                sb.Append(_parts + "\t");
                sb.Append(string.Join(" ", _detailList));

                if (checkBox1.Checked) {
                    if (File.Exists(saveFile)) {
                        File.Delete(saveFile);
                    }
                    checkBox1.Checked = false;
                }

                if (File.Exists(saveFile)) {
                    string[] data = File.ReadAllLines(saveFile);
                    if(data.Where(x => x == sb.ToString()).Count() > 0) {
                        if(MessageBox.Show("同じものがあります。追加しますか？", "確認", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == DialogResult.Cancel) {
                            this.Hide();
                            return;
                        }
                    }
                }
            }
            else {
                saveFile = _user + _itemDataFile;
                sb.Append(string.Join(Environment.NewLine, _detailList));

                if (checkBox1.Checked) {
                    if (File.Exists(saveFile)) {
                        File.Delete(saveFile);
                    }
                    checkBox1.Checked = false;
                }
            }

            File.AppendAllLines(saveFile, new string[] { sb.ToString() });
            this.Hide();
        }
    }
}
