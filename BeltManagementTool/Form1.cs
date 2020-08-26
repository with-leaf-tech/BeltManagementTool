using BeltManagementTool.Entity;
using BeltManagementTool.OCR;
using Google.Apis.Services;
using Google.Apis.Vision.v1;
using Google.Apis.Vision.v1.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BeltManagementTool {
    public partial class Form1 : Form {

        private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        System.Drawing.Bitmap captureImage = null;
        GoogleVisionApiOCR ocr = null;

        string settingFileName = "setting.txt";
        string replaceWordFileName = "replaceWord.txt";
        string userFileName = "user.txt";

        Dictionary<string, string> replaceWordDic = new Dictionary<string, string>();

        string name = "";
        string equip = "";
        List<string> detailList = new List<string>();

        Form2 entryWindow = new Form2();

        public Form1() {
            InitializeComponent();

            if(File.Exists(settingFileName)) {
                string[] settingValue = File.ReadAllText(settingFileName).Split(new char[] { ',' });
                numericUpDown1.Value = int.Parse(settingValue[0]);
                numericUpDown2.Value = int.Parse(settingValue[1]);
                numericUpDown3.Value = int.Parse(settingValue[2]);
                numericUpDown4.Value = int.Parse(settingValue[3]);
            }

            if(File.Exists(replaceWordFileName)) {
                string[] replaceWords = File.ReadAllLines(replaceWordFileName);
                for(int i = 0; i < replaceWords.Length; i++) {
                    string[] words = replaceWords[i].Split(new string[] { "!_!" }, StringSplitOptions.RemoveEmptyEntries);
                    replaceWordDic[words[0]] = words[1];
                }
            }
            loadUserData();

            ocr = new GoogleVisionApiOCR(@"C:\Users\tsutsumi\Downloads\try-apis-8b2095f28b0e.json");
        }

        private void outputReplaceWords() {
            StringBuilder sb = new StringBuilder();
            foreach(string key in replaceWordDic.Keys) {
                sb.Append(key);
                sb.Append("!_!");
                sb.Append(replaceWordDic[key]);
                sb.Append(Environment.NewLine);
            }

            File.WriteAllText(replaceWordFileName, sb.ToString());
        }

        private BeltEntity GetOCRTest() {
            string text = ocr.GetTextFromImage(captureImage).Replace("\n", "\r\n");
            logTextBox1.Text = text;
            foreach (string key in replaceWordDic.Keys) {
                text = text.Replace(key, replaceWordDic[key]);
            }
            logTextBox2.Text = text;

            analyze(text);

            logger.Info("読み取り結果：" + Environment.NewLine + logTextBox1.Text);

            return new BeltEntity(text);
        }

        private void button1_Click(object sender, EventArgs e) {
            screenCapture();
            BeltEntity entity = GetOCRTest();
        }


        private void Form1_Load(object sender, EventArgs e) {

        }

        private void button2_Click(object sender, EventArgs e) {
            if(numericUpDown1.Value != 0) {
                numericUpDown1.Value -= 1;
            }
            outputSetting();
        }

        private void button4_Click(object sender, EventArgs e) {
            numericUpDown1.Value += 1;
            outputSetting();
        }

        private void button3_Click(object sender, EventArgs e) {
            if (numericUpDown2.Value != 0) {
                numericUpDown2.Value -= 1;
            }
            outputSetting();
        }

        private void button5_Click(object sender, EventArgs e) {
            numericUpDown2.Value += 1;
            outputSetting();
        }

        private void screenCapture() {
            this.SendToBack();

            // スクリーンショット
            captureImage = new System.Drawing.Bitmap((int)numericUpDown3.Value, (int)numericUpDown4.Value);
            //Graphicsの作成
            Graphics g = Graphics.FromImage(captureImage);
            //画面全体をコピーする
            g.CopyFromScreen(new Point((int)numericUpDown1.Value, (int)numericUpDown2.Value), new Point(0, 0), captureImage.Size);
            //解放
            g.Dispose();

            //表示
            pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
            pictureBox1.Image = captureImage;

            this.TopMost = true;
            this.TopMost = false;
        }

        private void numericUpDown_ValueChanged(object sender, EventArgs e) {
            screenCapture();
            outputSetting();
        }

        private void outputSetting() {
            File.WriteAllText(settingFileName, numericUpDown1.Value + "," + numericUpDown2.Value + "," + numericUpDown3.Value + "," + numericUpDown4.Value);
        }

        private void button6_Click(object sender, EventArgs e) {
            timer1.Interval = (int)numericUpDown5.Value * 1000;
            timer1.Enabled = true;
            button1.Enabled = false;
            button6.Enabled = false;
            button7.Enabled = true;
        }

        private void timer1_Tick(object sender, EventArgs e) {
            timer1.Enabled = false;
            screenCapture();
            BeltEntity entity = GetOCRTest();
            timer1.Enabled = true;
        }

        private void button7_Click(object sender, EventArgs e) {
            timer1.Enabled = false;
            button1.Enabled = true;
            button6.Enabled = true;
            button7.Enabled = false;
        }

        private void analyze(string text) {
            name = "";
            equip = "";
            detailList = new List<string>();
            int remainBorder = 11;
            string tempName = "";


            foreach (string key in replaceWordDic.Keys) {
                text = text.Replace(key, replaceWordDic[key]);
            }

            if (
                text.Contains("1こ") ||
                text.Contains("2こ") ||
                text.Contains("3こ") ||
                text.Contains("4こ") ||
                text.Contains("5こ") ||
                text.Contains("6こ") ||
                text.Contains("7こ") ||
                text.Contains("8こ") ||
                text.Contains("9こ") ||
                text.Contains("0こ")
                ) {
                text = text
                    .Replace("1に", "1こ")
                    .Replace("2に", "2こ")
                    .Replace("3に", "3こ")
                    .Replace("4に", "4こ")
                    .Replace("5に", "5こ")
                    .Replace("6に", "6こ")
                    .Replace("7に", "7こ")
                    .Replace("8に", "8こ")
                    .Replace("9に", "9こ")
                    .Replace("0に", "0こ")
                    .Replace(" 2", "こ");
                int itemIndex = 0;
                string[] parts = text.Replace("”", "").Replace("\"", "").Replace("\r\n", "\n").Split(new char[] { '\n' });
                for (int i = 0; i < parts.Length; i++) {
                    if (parts[i] == "る") {
                        continue;
                    }
                    if (parts[i].Length == 0) {
                        continue;
                    }
                    if (
                        parts[i].Contains("1こ") ||
                        parts[i].Contains("2こ") ||
                        parts[i].Contains("3こ") ||
                        parts[i].Contains("4こ") ||
                        parts[i].Contains("5こ") ||
                        parts[i].Contains("6こ") ||
                        parts[i].Contains("7こ") ||
                        parts[i].Contains("8こ") ||
                        parts[i].Contains("9こ") ||
                        parts[i].Contains("0こ")
                        ) {
                        if (parts[i].Contains(" ")) {
                            string[] div = parts[i].Split(new char[] { ' ' });
                            detailList.Add(div[0] + "," + div[1].Replace("こ", ""));
                        }
                        else {
                            while (detailList[itemIndex].Contains(",")) {
                                itemIndex++;
                            }
                            detailList[itemIndex++] += "," + parts[i].Replace("こ", "");
                        }
                    }
                    else {
                        detailList.Add(parts[i]);
                    }
                }
            }
            else {
                string[] namehead = new string[] { "EO", "ED", "EE", "の", "N回", "E回", "回", "图", "NO", "NG", "NE", "v3", "①", "②", "D", "E", "S", "N", "O", "@", "3" };
                string[] equips = new string[] { "鎌", "アタマ", "からだ上", "からだ下", "ウデ", "足", "顔", "首", "指", "胸", "腰", "札", "その他", "紋章", "証" };
                string[] parts = text.Replace(" ", "").Replace("\r\n", "\n").Split(new char[] { '\n' });
                List<string> remainList = new List<string>();
                bool remainStart = false;
                for (int i = 0; i < parts.Length; i++) {
                    if (parts[i].Length == 0) {
                        continue;
                    }
                    if (tempName.Length == 0) {
                        tempName = parts[i];
                    }
                    if (remainStart == false && name.Length == 0 && parts[i].Contains("+")) {
                        name = parts[i].Replace(" ", "");
                        if (name.Contains("輝石") || name.Contains("戦神")) {
                            remainBorder = 13;
                        }
                    }

                    if (equip.Length == 0 && equips.Where(x => parts[i].Contains(x)).Count() > 0) {
                        equip = equips.Where(x => parts[i].Contains(x)).FirstOrDefault();
                    }

                    if (parts[i].Contains("追加効果")) {
                        remainStart = true;
                        continue;
                    }
                    if (parts[i].Contains("錬金石")) {
                        continue;
                    }

                    if (parts[i].Contains("錬金効") || parts[i].Contains("基礎効") || parts[i].Contains("合成効") || parts[i].Contains("伝承効")
                        || parts[i].Contains("輝石効") || parts[i].Contains("秘石効") || parts[i].Contains("戦神効") || parts[i].Contains("鬼石効")) {
                        string detail = parts[i]
                            .Replace("錬金効果:", "錬金:")
                            .Replace("錬金効果企", "錬金:")
                            .Replace("錬金効果金", "錬金:")
                            .Replace("錬金効果", "錬金:")

                            .Replace("輝石効果:企", "輝石:")
                            .Replace("輝石効果:金", "輝石:")
                            .Replace("輝石効果:", "輝石:")
                            .Replace("輝石効果企", "輝石:")
                            .Replace("輝石効果金", "輝石:")
                            .Replace("輝石効果", "輝石:")
                            .Replace("秘石効果:企", "秘石:")
                            .Replace("秘石効果:金", "秘石:")
                            .Replace("秘石効果:", "秘石:")
                            .Replace("秘石効果企", "秘石:")
                            .Replace("秘石効果金", "秘石:")
                            .Replace("秘石効果", "秘石:")

                            .Replace("戦神効果:企", "戦神:")
                            .Replace("戦神効果:金", "戦神:")
                            .Replace("戦神効果:", "戦神:")
                            .Replace("戦神効果企", "戦神:")
                            .Replace("戦神効果金", "戦神:")
                            .Replace("戦神効果", "戦神:")
                            .Replace("鬼石効果:企", "鬼石:")
                            .Replace("鬼石効果:金", "鬼石:")
                            .Replace("鬼石効果:", "鬼石:")
                            .Replace("鬼石効果企", "鬼石:")
                            .Replace("鬼石効果金", "鬼石:")
                            .Replace("鬼石効果", "鬼石:")

                            .Replace("合成効果:", "合成:")
                            .Replace("合成効果企", "合成:")
                            .Replace("合成効果金", "合成:")
                            .Replace("合成効果", "合成:")
                            .Replace("伝承効果:", "伝承:")
                            .Replace("伝承効果企", "伝承:")
                            .Replace("伝承効果金", "伝承:")
                            .Replace("伝承効果", "伝承:")
                            .Replace("基礎効果:", "基礎:")
                            .Replace("基礎効果", "基礎:");

                        detailList.Add(detail);
                    }
                    else if (!parts[i].Contains("できのよさ")) {
                        for (int j = 0; j < detailList.Count; j++) {
                            //if(detailList[j] == "錬金:" || detailList[j] == "基礎:") {
                            //    detailList[j] += parts[i].Replace(" ", "");
                            //    break;
                            //}
                            if (detailList[j].Length < remainBorder && remainList.Count > 0) {
                                detailList[j] += remainList[remainList.Count - 1];
                                remainList.RemoveAt(remainList.Count - 1);
                                break;
                            }
                        }
                        if (remainStart) {
                            remainList.Add(parts[i].Replace(" ", ""));
                        }
                    }

                    if (parts[i].Contains("戦士") || parts[i].Contains("僧侶")) {
                        break;
                    }
                }

                if (name.Length == 0) {
                    name = tempName.Replace(" ", "");
                }
                for (int i = 0; i < namehead.Length; i++) {
                    if (name.Substring(0, namehead[i].Length) == namehead[i]) {
                        name = name.Substring(namehead[i].Length);
                        break;
                    }
                }
                if (name.IndexOf("+") > 0 && name.IndexOf("+") + 2 != name.Length) {
                    name = name.Substring(0, name.IndexOf("+") + 2);
                }
                for (int i = 0; i < detailList.Count; i++) {
                    detailList[i] = detailList[i].Replace(" ", "").Replace("_", "");
                    if (detailList[i].IndexOf("+") < 0) {
                        detailList[i] = detailList[i]
                            .Replace("ダメージ10%", "ダメージ+10%")
                            .Replace("ダメージ11%", "ダメージ+11%")
                            .Replace("ダメージ12%", "ダメージ+12%")
                            .Replace("ダメージ13%", "ダメージ+13%")
                            .Replace("ダメージ1%", "ダメージ+1%")
                            .Replace("ダメージ2%", "ダメージ+2%")
                            .Replace("ダメージ3%", "ダメージ+3%")
                            .Replace("ダメージ4%", "ダメージ+4%")
                            .Replace("ダメージ5%", "ダメージ+5%")
                            .Replace("ダメージ6%", "ダメージ+6%")
                            .Replace("ダメージ7%", "ダメージ+7%")
                            .Replace("ダメージ8%", "ダメージ+8%")
                            .Replace("ダメージ9%", "ダメージ+9%")
                            .Replace("ダメージ108", "ダメージ+10%")
                            .Replace("ダメージ118", "ダメージ+11%")
                            .Replace("ダメージ128", "ダメージ+12%")
                            .Replace("ダメージ138", "ダメージ+13%")
                            .Replace("ダメージ18", "ダメージ+1%")
                            .Replace("ダメージ28", "ダメージ+2%")
                            .Replace("ダメージ38", "ダメージ+3%")
                            .Replace("ダメージ48", "ダメージ+4%")
                            .Replace("ダメージ58", "ダメージ+5%")
                            .Replace("ダメージ68", "ダメージ+6%")
                            .Replace("ダメージ78", "ダメージ+7%")
                            .Replace("ダメージ88", "ダメージ+8%")
                            .Replace("ダメージ98", "ダメージ+9%");
                    }
                }
            }
        }


        private void button8_Click(object sender, EventArgs e) {
            string text = @"
せかいじゅの葉
せかいじゅの葉
せかいじゅのしずく
まほうのせいすい
まほうのせいすい
まほうのせいすい
まほうのせいすい
まほうのせいすい
まほうのせいすい
まほうのせいすい
99こ
33こ
4こ
99こ
99こ
99こ
99こ
99こ
99こ
75こ
";

            analyze(text);

            int a = 0;
            //captureImage = (Bitmap)System.Drawing.Image.FromFile(@"C:\Test\sample1.bmp");
            //BeltEntity entity = GetOCRTest();
        }

        private void button10_Click(object sender, EventArgs e) {
            string str = Microsoft.VisualBasic.Interaction.InputBox("入力してください", "ユーザー登録", default, 300, 400);
            if(str.Length > 0) {
                listBox1.Items.Add(str);
            }
            saveUserData();
        }

        private void button11_Click(object sender, EventArgs e) {
            if(listBox1.SelectedIndex != -1) {
                if(MessageBox.Show("削除していいですか？", "確認", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == DialogResult.OK) {
                    listBox1.Items.RemoveAt(listBox1.SelectedIndex);
                }
            }
            saveUserData();
        }

        private void loadUserData() {
            if(File.Exists(userFileName)) {
                int index = -1;
                string[] users = File.ReadAllLines(userFileName);
                for(int i = 0; i < users.Length; i++) {
                    string[] userParts = users[i].Split(new char[] { ',' });
                    if(userParts[1] == "+") {
                        index = i;
                    }
                    listBox1.Items.Add(userParts[0]);
                }
                if(index != -1) {
                    listBox1.SelectedIndex = index;
                }
            }
        }

        private void saveUserData() {
            int index = listBox1.SelectedIndex;
            StringBuilder sb = new StringBuilder();
            for(int i = 0; i < listBox1.Items.Count; i++) {
                sb.Append(listBox1.Items[i] + ",");
                if(i == index) {
                    sb.Append("+");
                }
                sb.Append(Environment.NewLine);
            }
            File.WriteAllText(userFileName, sb.ToString());
        }

        private void button9_Click(object sender, EventArgs e) {
            logTextBox2.Text = logTextBox1.Text.Replace(textBox1.Text, textBox2.Text);
            replaceWordDic[textBox1.Text] = textBox2.Text;
            outputReplaceWords();
        }

        private void button12_Click(object sender, EventArgs e) {
            entryWindow.setItems(name, equip, listBox1.SelectedItem.ToString(), detailList);
            entryWindow.ShowDialog();
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e) {
            saveUserData();
        }
    }
}
