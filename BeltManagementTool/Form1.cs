using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
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
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BeltManagementTool {
    public partial class Form1 : Form {

        private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        //Bitmap captureImage = null;
        GoogleVisionApiOCR googleOcr = null;
        WindowsOCR windowsOcr = null;
        TesseractOCR tesseractOcr = null;
        AzureComputerVisionApiOCR azureOcr = null;
        OcrBase ocr = null;

        string settingFileName = "setting.txt";
        string replaceWordFileName = "replaceWord.txt";
        string userFileName = "user.txt";
        string imageFileName = "image.png";

        private string _itemDataFile = "_item.txt";
        private string _equipDataFile = "_equip.txt";
        private string _itemDicFile = "itemDictionary.txt";
        
        string[] equips = new string[] { "鎌", "両手杖", "アタマ", "からだ上", "からだ下", "ウデ", "足", "顔", "首", "指", "胸", "腰", "札", "その他", "紋章", "証" };
        string[] jobs = new string[] { "戦士", "僧侶", "魔使", "武闘", "盗賊", "旅芸", "バト", "パラ", "魔戦", "レン", "賢者", "スパ", "まも", "どう", "踊り", "占い", "天地", "遊び", "デス" };
        string[] regists = new string[] { "呪いガード", "即死ガード", "闇ダメージ" };


        List<Dictionary<string, string>> itemDictionary = new List<Dictionary<string, string>>();


        Dictionary<string, string> replaceWordDic = new Dictionary<string, string>();
        List<string[]> settingData = new List<string[]>();

        string name = "";
        string equip = "";
        List<string> detailList = new List<string>();

        Form2 entryWindow = new Form2();
        Form3 updateWindow = new Form3();

        public Form1() {
            InitializeComponent();

            if(File.Exists(replaceWordFileName)) {
                string[] replaceWords = File.ReadAllLines(replaceWordFileName);
                for(int i = 0; i < replaceWords.Length; i++) {
                    string[] words = replaceWords[i].Split(new string[] { "!_!" }, StringSplitOptions.RemoveEmptyEntries);
                    replaceWordDic[words[0]] = words[1];
                }
            }
            if (File.Exists(_itemDicFile)) {
                string[] lines = File.ReadAllLines(_itemDicFile);
                for (int i = 0; i < lines.Length; i++) {
                    string[] line = lines[i].Split(new char[] { '\t' });
                    Dictionary<string, string> item = new Dictionary<string, string>();
                    for (int j = 0; j < line.Length; j++) {
                        string[] parts = line[j].Split(new string[] { "!_!" }, StringSplitOptions.RemoveEmptyEntries);
                        if(parts.Length > 1) {
                            item[parts[0]] = parts[1];
                        }
                        else {
                            item[parts[0]] = "";
                        }
                    }
                    itemDictionary.Add(item);
                }
            }

            loadUserData();
            loadPositionData();

            selectEquip.Checked = true;
            selectUser.Checked = true;

            jobList.Items.AddRange(jobs);
            jobList.SelectedIndex = 0;

            for (int i = 0; i < regists.Length; i++) {
                Control[] comboBox = this.Controls.Find("registBox" + i, true);
                List<string> items = new List<string>();
                items.Add(regists[i] + " 指定なし");
                for (int j = 1; j <= 10; j++) {
                    items.Add(regists[i] + " " + (j * 10) + "%以上");
                }
                ((System.Windows.Forms.ComboBox)comboBox[0]).Items.AddRange(items.ToArray());
                ((System.Windows.Forms.ComboBox)comboBox[0]).SelectedIndex = 0;
            }

            initialize();
        }

        private async void initialize() {
            googleOcr = new GoogleVisionApiOCR();
            googleOcr.initialize(@"C:\Users\tsutsumi\Downloads\try-apis-8b2095f28b0e.json");
            windowsOcr = new WindowsOCR();
            azureOcr = new AzureComputerVisionApiOCR();
            await Task.Run(() => azureOcr.initialize(@"C:\Users\tsutsumi\Downloads\azure.txt"));

            await Task.Run(() => windowsOcr.initialize(""));
            tesseractOcr = new TesseractOCR();
            tesseractOcr.initialize("");
            ocrRadioWindows.Checked = true;
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
            Bitmap img = new Bitmap(imageFileName);
            string text = ocr.GetTextFromImage(img, "ja-JP").Replace("\n", "\r\n");
            img.Dispose();
            if(InvokeRequired) {
                Invoke((MethodInvoker)delegate {
                    logTextBox1.Text = text;
                    foreach (string key in replaceWordDic.Keys) {
                        text = text.Replace(key, replaceWordDic[key]);
                    }
                    logTextBox2.Text = text;

                    logger.Info("読み取り結果：" + Environment.NewLine + logTextBox1.Text);
                });
            }
            else {
                logTextBox1.Text = text;
                foreach (string key in replaceWordDic.Keys) {
                    text = text.Replace(key, replaceWordDic[key]);
                }
                logTextBox2.Text = text;

                logger.Info("読み取り結果：" + Environment.NewLine + logTextBox1.Text);
            }
            return new BeltEntity(text);
        }

        private async void button1_Click(object sender, EventArgs e) {
            screenCapture();
            if(ocrRadioWindows.Checked || ocrRadioAzure.Checked) {
                await Task.Run(() => GetOCRTest());
            }
            else {
                BeltEntity entity = GetOCRTest();
            }
        }


        private void Form1_Load(object sender, EventArgs e) {

        }

        private void button2_Click(object sender, EventArgs e) {
            if(numericUpDown1.Value != 0) {
                numericUpDown1.Value -= 1;
            }
            savePositionData();
        }

        private void button4_Click(object sender, EventArgs e) {
            numericUpDown1.Value += 1;
            savePositionData();
        }

        private void button3_Click(object sender, EventArgs e) {
            if (numericUpDown2.Value != 0) {
                numericUpDown2.Value -= 1;
            }
            savePositionData();
        }

        private void button5_Click(object sender, EventArgs e) {
            numericUpDown2.Value += 1;
            savePositionData();
        }

        private void screenCapture() {
            this.SendToBack();

            // スクリーンショット
            Bitmap captureImage = new System.Drawing.Bitmap((int)numericUpDown3.Value, (int)numericUpDown4.Value);
            //Graphicsの作成
            Graphics g = Graphics.FromImage(captureImage);
            //画面全体をコピーする
            g.CopyFromScreen(new Point((int)numericUpDown1.Value, (int)numericUpDown2.Value), new Point(0, 0), captureImage.Size);
            //解放
            g.Dispose();

            captureImage.Save(imageFileName);

            //表示
            pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
            pictureBox1.Image = captureImage;

            this.TopMost = true;
            this.TopMost = false;
        }

        private void numericUpDown_ValueChanged(object sender, EventArgs e) {
            screenCapture();
            savePositionData();
        }

        //private void outputSetting() {
        //    File.WriteAllText(settingFileName, numericUpDown1.Value + "," + numericUpDown2.Value + "," + numericUpDown3.Value + "," + numericUpDown4.Value);
        //}

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
            button1_Click(null,null);
            button12_Click(null, null);

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
                equip = "アイテム";
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
                string[] namehead = new string[] { "EO", "ED", "EE", "の", "N回", "E回", "回", "囚", "图", "NO", "NG", "NE", "v3", "①", "②", "D", "E", "S", "N", "O", "@", "3", "2" };
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
                if(name.Length > 0) {
                    for (int i = 0; i < namehead.Length; i++) {
                        if (name.Substring(0, namehead[i].Length) == namehead[i]) {
                            name = name.Substring(namehead[i].Length);
                            break;
                        }
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
S輝石のベルト+4
腰アクセサリーレア度 A
とりどりの宝石が
さまざまな効果を
生み出すベルト
取り引き不可
Lv 1以上装備可
追加効果
輝石効果:金かいしん率 +1.0%
輝石効果:金きいだい H P +10
輝石効果:金见いガード+10.0%
秘石効果:こうげきカ +8
戦士 僧侶 魔使 武闘 盗賊 旅芸 バト パラ 魔戦 レン 賢者 スパ
まも どう 踊り 占い 天地 遊び デス
O装備できる仲間モンスターを見る
";

            logTextBox1.Text = text;
            foreach (string key in replaceWordDic.Keys) {
                text = text.Replace(key, replaceWordDic[key]);
            }
            logTextBox2.Text = text;
            
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

        private void loadPositionData() {
            int settingCount = 3;
            if (File.Exists(settingFileName)) {
                int index = -1;
                settingData.Clear();
                string[] setting = File.ReadAllLines(settingFileName);
                for (int i = 0; i < setting.Length; i++) {
                    string[] settingParts = setting[i].Split(new char[] { ',' });
                    settingData.Add(settingParts);
                    if (settingParts[4] == "+") {
                        index = i;
                    }
                }
                if (setting.Length < settingCount) {
                    for (int i = 0; i < settingCount - setting.Length; i++) {
                        settingData.Add(new string[] { "0", "0", "1", "1", "" });
                    }
                }
                if (index != -1) {
                    Control[] radioList = this.Controls.Find("positionSetting" + index, true);
                    ((System.Windows.Forms.RadioButton)radioList[0]).Checked = true;
                }
            }
            else {
                settingData.Add(new string[] { "0", "0", "1", "1", "+" });
                for (int i = 0; i < settingCount - 1; i++) {
                    settingData.Add(new string[] { "0", "0", "1", "1", "" });
                }
                positionSetting0.Checked = true;
            }
        }

        private void savePositionData() {
            int index = - 1;
            if(positionSetting0.Checked) {
                index = 0;
            }
            else if(positionSetting1.Checked) {
                index = 1;
            }
            else {
                index = 2;
            }
            settingData[index] = new string[] { numericUpDown1.Value.ToString(), numericUpDown2.Value.ToString(), numericUpDown3.Value.ToString(), numericUpDown4.Value.ToString(), "+" };

            StringBuilder sb = new StringBuilder();
            for(int i = 0; i < settingData.Count; i++) {
                sb.Append(string.Join(",", settingData[i]) + Environment.NewLine);
            }
            File.WriteAllText(settingFileName, sb.ToString());
        }

        private void button9_Click(object sender, EventArgs e) {
            replaceWordDic[textBox1.Text] = textBox2.Text;

            string text = logTextBox2.Text;
            foreach (string key in replaceWordDic.Keys) {
                text = text.Replace(key, replaceWordDic[key]);
            }
            logTextBox2.Text = text;
            
            outputReplaceWords();
        }

        private void button12_Click(object sender, EventArgs e) {
            analyze(logTextBox2.Text);

            entryWindow.setItems(name, equip, listBox1.SelectedItem.ToString(), detailList);
            entryWindow.ShowDialog();
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e) {
            saveUserData();
        }

        private void ocrRadio_CheckedChanged(object sender, EventArgs e) {
            if(ocrRadioGoogle.Checked) {
                ocr = googleOcr;
            }
            else if(ocrRadioTesseract.Checked) {
                ocr = tesseractOcr;
            }
            else if (ocrRadioAzure.Checked) {
                ocr = azureOcr;
            }
            else {
                ocr = windowsOcr;
            }
        }

        private void positionSetting_CheckedChanged(object sender, EventArgs e) {
            numericUpDown1.ValueChanged -= numericUpDown_ValueChanged;
            numericUpDown2.ValueChanged -= numericUpDown_ValueChanged;
            numericUpDown3.ValueChanged -= numericUpDown_ValueChanged;
            int index = int.Parse(((System.Windows.Forms.RadioButton)sender).Text.Replace("設定", "")) -1;
            for (int i = 0; i < settingData.Count; i++) {
                if (i == index) {
                    settingData[i][4] = "+";
                    numericUpDown1.Value = int.Parse(settingData[i][0]);
                    numericUpDown2.Value = int.Parse(settingData[i][1]);
                    numericUpDown3.Value = int.Parse(settingData[i][2]);
                    numericUpDown4.Value = int.Parse(settingData[i][3]);
                }
                else {
                    settingData[i][4] = "";
                }
            }
            numericUpDown1.ValueChanged += numericUpDown_ValueChanged;
            numericUpDown2.ValueChanged += numericUpDown_ValueChanged;
            numericUpDown3.ValueChanged += numericUpDown_ValueChanged;
        }


        private void searchRadioChange(object sender, EventArgs e) {
            if(selectEquip.Checked) {
                equipList.Items.Clear();
                equipList.Items.AddRange(equips);
            }
            else {
                List<string> itemList = new List<string>();
                if (allUser.Checked) {
                    for (int i = 0; i < listBox1.Items.Count; i++) {
                        if (File.Exists(listBox1.Items[i] + _itemDataFile)) {
                            string[] data = File.ReadAllLines(listBox1.Items[i] + _itemDataFile);
                            itemList.AddRange(data.Select(x => x.Split(new char[] { ',' })[0]).ToList());
                        }
                    }
                }
                else {
                    if (File.Exists(listBox1.SelectedItem + _itemDataFile)) {
                        string[] data = File.ReadAllLines(listBox1.SelectedItem + _itemDataFile);
                        itemList.AddRange(data.Select(x => x.Split(new char[] { ',' })[0]).ToList());
                    }
                }
                equipList.Items.Clear();
                equipList.Items.AddRange(itemList.Distinct().ToArray());
            }
        }

        private void equipList_SelectedIndexChanged(object sender, EventArgs e) {
            string selectEquipItem = equipList.SelectedItem.ToString();
            resultList.Items.Clear();
            abilityList.Items.Clear();
            List<string> users = new List<string>();
            if (allUser.Checked) {
                for (int i = 0; i < listBox1.Items.Count; i++) {
                    users.Add(listBox1.Items[i].ToString());
                }
            }
            else {
                users.Add(listBox1.SelectedItem.ToString());
            }

            if (selectEquip.Checked) {
                List<string> ability = new List<string>();

                for(int i = 0; i < users.Count; i++) {
                    if (File.Exists(users[i] + _equipDataFile)) {
                        string[] data = File.ReadAllLines(users[i] + _equipDataFile);
                        List<string> aa = data.Where(x => (x.Split(new char[] { '\t' })[1]) == selectEquipItem).ToList();
                        List<string[]> ab = aa.Select(x => (x.Split(new char[] { '\t' })[2]).Split(new char[] { ' ' })).ToList();
                        List<string[]> bb = ab.Select(x => x.Where(y =>
                        y.Contains("錬金:") ||
                        y.Contains("合成:") ||
                        y.Contains("伝承:") ||
                        y.Contains("輝石:") ||
                        y.Contains("秘石:") ||
                        y.Contains("戦神:") ||
                        y.Contains("鬼石:"))
                        .Select(y => y
                        .Replace("0", "")
                        .Replace("1", "")
                        .Replace("2", "")
                        .Replace("3", "")
                        .Replace("4", "")
                        .Replace("5", "")
                        .Replace("6", "")
                        .Replace("7", "")
                        .Replace("8", "")
                        .Replace("9", "")
                        .Replace("(", "")
                        .Replace(")", "")
                        .Replace("+", "")
                        .Replace("-", "")
                        .Replace(".", "")
                        .Replace("%", "")
                        ).ToArray()).ToList();

                        bb.ForEach(x => ability.AddRange(x));
                        abilityList.Items.AddRange(ability.Distinct().ToArray());
                        resultList.Items.AddRange(aa.Select(x => users[i] + "\t" + x).ToArray());
                    }
                }

            }
            else {
                Dictionary<string, int> allCount = new Dictionary<string, int>();
                for (int i = 0; i < users.Count; i++) {
                    if (File.Exists(users[i] + _itemDataFile)) {
                        string[] data = File.ReadAllLines(users[i] + _itemDataFile);
                        List<string[]> aa = data.Where(x => (x.Split(new char[] { ',' })[0]) == selectEquipItem).Select(x => x.Split(new char[] { ',' })).ToList();
                        var bb = aa.GroupBy(x => x[0]).Select(x => new { Name = x.Key, Sum = x.Sum(y => int.Parse(y[1])) }).ToList();
                        foreach(var result in bb) {
                            resultList.Items.Add(users[i] + " " + result.Name + " " + result.Sum + "こ");
                            if(!allCount.ContainsKey(result.Name)) {
                                allCount.Add(result.Name, 0);
                            }
                            allCount[result.Name] += result.Sum;
                        }
                    }
                }
                foreach(string name in allCount.Keys) {
                    resultList.Items.Add("　合計" + " " + name + " " + allCount[name] + "こ");
                }

            }
        }

        private void abilityList_SelectedIndexChanged(object sender, EventArgs e) {
            string selectEquipItem = equipList.SelectedItem.ToString();
            resultList.Items.Clear();
            List<string> users = new List<string>();
            if (allUser.Checked) {
                for (int i = 0; i < listBox1.Items.Count; i++) {
                    users.Add(listBox1.Items[i].ToString());
                }
            }
            else {
                users.Add(listBox1.SelectedItem.ToString());
            }

            if (selectEquip.Checked) {
                string selectAbilityItem = abilityList.SelectedItem.ToString();

                for (int i = 0; i < users.Count; i++) {
                    if (File.Exists(users[i] + _equipDataFile)) {
                        string[] data = File.ReadAllLines(users[i] + _equipDataFile);
                        List<string> aa = data.Where(x => (x.Split(new char[] { '\t' })[1]) == selectEquipItem).ToList();
                        for (int j = 0; j < aa.Count; j++) {
                            string[] parts = aa[j].Split(new char[] { '\t' });
                            string[] ability = parts[2].Split(new char[] { ' ' }).Where(y =>
                                y.Contains("錬金:") ||
                                y.Contains("合成:") ||
                                y.Contains("伝承:") ||
                                y.Contains("輝石:") ||
                                y.Contains("秘石:") ||
                                y.Contains("戦神:") ||
                                y.Contains("鬼石:"))
                                .Select(y => y
                                .Replace("0", "")
                                .Replace("1", "")
                                .Replace("2", "")
                                .Replace("3", "")
                                .Replace("4", "")
                                .Replace("5", "")
                                .Replace("6", "")
                                .Replace("7", "")
                                .Replace("8", "")
                                .Replace("9", "")
                                .Replace("(", "")
                                .Replace(")", "")
                                .Replace("+", "")
                                .Replace("-", "")
                                .Replace(".", "")
                                .Replace("%", "")
                                ).ToArray();
                            if (ability.Where(x => x == selectAbilityItem).Count() > 0) {
                                resultList.Items.Add(users[i] + "\t" + aa[j]);
                            }
                        }
                    }
                }
            }
            else {

            }

        }

        private void resultList_DoubleClick(object sender, EventArgs e) {
            string selectedItem = ((ListBox)sender).SelectedItem.ToString();
            if(selectedItem.Length > 0) {
                string[] parts = selectedItem.Split(new char[] { '\t' });
                if(parts.Length > 3) {
                    string user = parts[0];
                    string name = parts[1];
                    string equip = parts[2];
                    List<string> detailList = new List<string>();
                    string[] detail = parts[3].Split(new char[] { ' ' });
                    updateWindow.setItems(name, equip, user, detail.ToList());
                }
                else {
                    parts = selectedItem.Split(new char[] { ' ' });
                    if(parts.Length < 3) {
                        return;
                    }
                    string user = parts[0];
                    string equip = parts[1];
                    string count = parts[2];
                    if(!listBox1.Items.Contains(user)) {
                        return;
                    }
                    updateWindow.setItems("", equip, user, count);
                }
                updateWindow.Show();
            }


        }

        private void button13_Click(object sender, EventArgs e) {
            string[] setEquipUrls = new string[] {
                "http://bazaar.d-quest-10.com/list/sp_set/lv_1.html" 
            };

            string[] equipUrls = new string[] { 
                "http://bazaar.d-quest-10.com/list/d_head/lv_1.html", 
                "http://bazaar.d-quest-10.com/list/d_upper/lv_1.html",
                "http://bazaar.d-quest-10.com/list/d_lower/lv_1.html",
                "http://bazaar.d-quest-10.com/list/d_arm/lv_1.html",
                "http://bazaar.d-quest-10.com/list/d_leg/lv_1.html",
                "http://bazaar.d-quest-10.com/list/d_shield/lv_1.html",
                "http://bazaar.d-quest-10.com/list/w_hand/lv_1.html",
                "http://bazaar.d-quest-10.com/list/w_both/lv_1.html",
                "http://bazaar.d-quest-10.com/list/w_short/lv_1.html",
                "http://bazaar.d-quest-10.com/list/w_spear/lv_1.html",
                "http://bazaar.d-quest-10.com/list/w_axe/lv_1.html",
                "http://bazaar.d-quest-10.com/list/w_claw/lv_1.html",
                "http://bazaar.d-quest-10.com/list/w_whip/lv_1.html",
                "http://bazaar.d-quest-10.com/list/w_stick/lv_1.html",
                "http://bazaar.d-quest-10.com/list/w_cane/lv_1.html",
                "http://bazaar.d-quest-10.com/list/w_club/lv_1.html",
                "http://bazaar.d-quest-10.com/list/w_fan/lv_1.html",
                "http://bazaar.d-quest-10.com/list/w_hammer/lv_1.html",
                "http://bazaar.d-quest-10.com/list/w_bow/lv_1.html",
                "http://bazaar.d-quest-10.com/list/w_boomerang/lv_1.html",
                "http://bazaar.d-quest-10.com/list/w_falx/lv_1.html"
            };

            string[] equipAccessoryUrls = new string[] {
                "http://bazaar.d-quest-10.com/list/d_accessory/pop_2.html"
            };

            string[] setHeader = new string[] { "LV", "セット名", "必要装備", "守備", "重さ", "攻魔", "回魔", "器用", "早さ", "おしゃれ", "セット特殊効果", "セット効果", "装備職" };
            string[] equipHeader = new string[] { "アイテム名","LV","分類","出品数","最安値","★","★★","★★★","広場", "効果", "装備職", "空白" };
            string[] accessoryHeader = new string[] { "アイテム名", "分類", "出品数", "最安値", "店買価格", "店売価格", "広場", "効果", "空白" };

            List<Dictionary<string, string>> itemList = new List<Dictionary<string, string>>();
            for(int i = 0; i < setEquipUrls.Length; i++ ) {
                itemList.AddRange(getItemData(setHeader, setEquipUrls[i]));
            }

            for (int i = 0; i < equipUrls.Length; i++) {
                itemList.AddRange(getItemData(equipHeader, equipUrls[i]));
            }

            for (int i = 0; i < equipAccessoryUrls.Length; i++) {
                itemList.AddRange(getItemData(accessoryHeader, equipAccessoryUrls[i]));
            }

            itemDictionary = itemList;

            StringBuilder allItemString = new StringBuilder();
            for (int i = 0; i < itemList.Count; i++) {
                StringBuilder sb = new StringBuilder();
                foreach (string key in itemList[i].Keys) {
                    if(sb.Length != 0) {
                        sb.Append("\t");
                    }
                    sb.Append(key + "!_!" + itemList[i][key]);
                }
                allItemString.Append(sb.ToString() + Environment.NewLine);
            }
            File.WriteAllText(_itemDicFile, allItemString.ToString());

            int a = 0;
        }

        private List<Dictionary<string, string>> getItemData(string[] header, string url) {
            List<Dictionary<string, string>> equipSetList = new List<Dictionary<string, string>>();

            // 指定したサイトのHTMLをストリームで取得する
            WebRequest req = WebRequest.Create(url);
            var doc = default(IHtmlDocument);
            using (WebResponse res = req.GetResponse())
            using (Stream stream = res.GetResponseStream()) {
                var parser = new HtmlParser();
                doc = parser.ParseDocument(stream);
            }

            int headerIndex = 0;

            AngleSharp.Dom.IHtmlCollection<AngleSharp.Dom.IElement> urlElements = doc.QuerySelectorAll("table");
            for (int i = 0; i < urlElements.Count(); i++) {
                if (urlElements[i].InnerHtml.Contains(header[0])) {
                    AngleSharp.Dom.IHtmlCollection<AngleSharp.Dom.IElement> lines = urlElements[i].QuerySelectorAll("tr");
                    Dictionary<string, string> equipSet = new Dictionary<string, string>();
                    for (int j = 0; j < lines.Count(); j++) {
                        if (lines[j].InnerHtml.Contains(header[0])) {
                            continue;
                        }
                        AngleSharp.Dom.IHtmlCollection<AngleSharp.Dom.IElement> columns = lines[j].QuerySelectorAll("td");
                        for (int k = 0; k < columns.Count(); k++) {
                            string text = "";
                            /*
                            if (columns[k].InnerHtml.Contains("href")) {
                                AngleSharp.Dom.IHtmlCollection<AngleSharp.Dom.IElement> parts = columns[k].QuerySelectorAll("a");
                                text = string.Join(",", parts.Select(x => x.InnerHtml).ToArray());
                            }
                            else if (columns[k].InnerHtml.Contains("div")) {
                                AngleSharp.Dom.IHtmlCollection<AngleSharp.Dom.IElement> parts = columns[k].QuerySelectorAll("div");
                                text = string.Join(",", parts.Select(x => x.InnerHtml).ToArray());
                            }
                            else {
                            */

                            text = columns[k].InnerHtml;
                            text = text.Replace("\r\n", " ");
                            text = text.Replace("\n", " ");
                            text = text.Replace("<br>", ",");
                            text = text.Replace("</div><div ", "</div>,<div ");
                            text = Regex.Replace(text, @"<(([^>]|\n)*)>", "");


                            if (header[headerIndex] != "空白" && header[headerIndex] != "装備職" && header[headerIndex] != "セット効果" && text.Length == 0) {
                                continue;
                            }
                            if(text.Contains("adsbygoogle")) {
                                continue;
                            }
                            //}
                            equipSet[header[headerIndex++]] = text;
                            if(headerIndex == header.Length) {
                                equipSetList.Add(equipSet);
                                equipSet = new Dictionary<string, string>();
                                headerIndex = 0;
                            }
                        }
                    }
                }
            }
            return equipSetList;
        }

        private void registSearch() {
            /*
            int jobIndex = jobList.SelectedIndex;
            int registIndex = registList.SelectedIndex;

            string job = "全職業";
            string regist = "";
            if(jobIndex > 0) {
                job = jobList.SelectedItem.ToString();
            }
            if(registIndex > 0) {
                regist = registList.SelectedItem.ToString();
            }

            string[] data = File.ReadAllLines(listBox1.SelectedItem.ToString() + _equipDataFile);
            */

        }

        private void button14_Click(object sender, EventArgs e) {
            string job = jobList.SelectedItem.ToString();
            string user = listBox1.SelectedItem.ToString();

            int registSelectCount = 9;

            Dictionary<string, float> targetRegistList = new Dictionary<string, float>();
            Dictionary<string, float> orbRegistList = new Dictionary<string, float>();

            for (int i = 0; i < registSelectCount; i++ ) {
                if(regists.Length > i) {
                    Control[] orbRegist = this.Controls.Find("registAppend" + i, true);
                    orbRegistList[regists[i]] = float.Parse(((System.Windows.Forms.NumericUpDown)orbRegist[0]).Value.ToString());
                }

                Control[] comboBox = this.Controls.Find("registBox" + i, true);
                if(((System.Windows.Forms.ComboBox)comboBox[0]).SelectedIndex >= 0) {
                    string regist = ((System.Windows.Forms.ComboBox)comboBox[0]).SelectedItem.ToString();
                    if (regist.Length != 0 && !regist.Contains("指定なし")) {
                        if(regist.IndexOf("ガード") > 0) {
                            string aa = regist.Substring(regist.IndexOf("ガード") + 4, regist.IndexOf("%") - (regist.IndexOf("ガード") + 4));
                            targetRegistList[regist.Substring(0, regist.IndexOf("ガード") + 3)] = float.Parse(aa);
                        }
                        else {
                            string aa = regist.Substring(regist.IndexOf("ダメージ") + 5, regist.IndexOf("%") - (regist.IndexOf("ダメージ") + 5));
                            targetRegistList[regist.Substring(0, regist.IndexOf("ダメージ") + 4)] = float.Parse(aa);
                        }
                    }
                }
            }

            string[] registParts = new string[] { "アタマ", "からだ上", "からだ下", "ウデ", "足", "盾", "アクセサリー（顔）", "アクセサリー（指）", "アクセサリー（腰）", "アクセサリー（他）" };
            string[] registInternalParts = new string[] { "アタマ", "からだ上", "からだ下", "ウデ", "足", "盾", "顔", "指", "腰", "その他" };
            string[] bodyParts = new string[] { "アタマ", "からだ上", "からだ下", "足" };
            string[] appendParts = new string[] { };
            if(includeFace.Checked) {
                Array.Resize(ref bodyParts, bodyParts.Length + 1);
                bodyParts[bodyParts.Length - 1] = "顔";
                Array.Resize(ref appendParts, appendParts.Length + 1);
                appendParts[appendParts.Length - 1] = "顔";
            }
            if (includeFinger.Checked) {
                Array.Resize(ref bodyParts, bodyParts.Length + 1);
                bodyParts[bodyParts.Length - 1] = "指";
                Array.Resize(ref appendParts, appendParts.Length + 1);
                appendParts[appendParts.Length - 1] = "指";
            }
            if (includeOther.Checked) {
                Array.Resize(ref bodyParts, bodyParts.Length + 1);
                bodyParts[bodyParts.Length - 1] = "その他";
                Array.Resize(ref appendParts, appendParts.Length + 1);
                appendParts[appendParts.Length - 1] = "その他";
            }
            if (includeShield.Checked) {
                Array.Resize(ref bodyParts, bodyParts.Length + 1);
                bodyParts[bodyParts.Length - 1] = "盾";
                Array.Resize(ref appendParts, appendParts.Length + 1);
                appendParts[appendParts.Length - 1] = "盾";
            }
            if (includeWaist.Checked) {
                Array.Resize(ref bodyParts, bodyParts.Length + 1);
                bodyParts[bodyParts.Length - 1] = "腰";
                Array.Resize(ref appendParts, appendParts.Length + 1);
                appendParts[appendParts.Length - 1] = "腰";
            }

            Dictionary<string, string> replaceParts = new Dictionary<string, string>();
            for(int i = 0; i < registParts.Length; i++) {
                replaceParts[registInternalParts[i]] = registParts[i];
            }

            List<Dictionary<string, string>> haveEquipList = new List<Dictionary<string, string>>();
            string[] data = File.ReadAllLines(listBox1.SelectedItem.ToString() + _equipDataFile);
            for(int i = 0; i < data.Length; i++) {
                Dictionary<string, string> item = new Dictionary<string, string>();
                string[] parts = data[i].Split(new char[] { '\t' });
                string name = parts[0];
                string equip = parts[1];
                item["アイテム名"] = name;
                item["分類"] = equip;
                item["効果"] = parts[2];
                string[] ability = parts[2].Split(new char[] { ' ' });
                for(int j = 0; j < ability.Length; j++) {
                    if(ability[j].Contains("ガード")) {
                        string kind = ability[j].Substring(ability[j].IndexOf(":") + 1, (ability[j].IndexOf("ガード") + 3) - (ability[j].IndexOf(":") + 1));
                        string grade = ability[j].Substring(ability[j].IndexOf("+") + 1, ability[j].Length - 1 - (ability[j].IndexOf("+") + 1)); // %を除外する
                        float nGrade = Calc.Analyze(grade.Replace("(", "").Replace(")", "")).Calc(null);
                        if(!item.ContainsKey(kind)) {
                            item[kind] = "0";
                        }
                        item[kind] = (float.Parse(item[kind]) + nGrade).ToString();
                    }
                    else if (ability[j].Contains("減")) {
                        string kind = ability[j].Substring(ability[j].IndexOf(":") + 1, (ability[j].IndexOf("ダメージ") + 4) - (ability[j].IndexOf(":") + 1));
                        string grade = ability[j].Substring(ability[j].IndexOf("ダメージ") + 4, ability[j].Length - 2 - (ability[j].IndexOf("ダメージ") + 4)); // %減を除外する
                        float nGrade = Calc.Analyze(grade.Replace("(", "").Replace(")", "")).Calc(null);
                        if (!item.ContainsKey(kind)) {
                            item[kind] = "0";
                        }
                        item[kind] = (float.Parse(item[kind]) + nGrade).ToString();
                    }
                }
                haveEquipList.Add(item);
            }


            List<Dictionary<string, string>> setEquips = itemDictionary.Where(x => x.ContainsKey("セット効果")).Where(x => x["装備職"].Contains(job)).OrderByDescending(x => int.Parse(x["LV"])).ToList();
            List<Dictionary<string, string>> allEquips = itemDictionary.Where(x => !x.ContainsKey("セット効果")).Where(x => (!x.ContainsKey("装備職") ||  (x.ContainsKey("装備職") && x["装備職"].Contains(job))) && registParts.Contains(x["分類"])).OrderBy(x => int.Parse(x.ContainsKey("LV") ? x["LV"] : "1")).ToList();

            if(onlySetEquip.Checked) {
                // まずレベルの高いセット装備から探す
                for (int i = 0; i < setEquips.Count; i++) {
                    Dictionary<string, string> setEquip = new Dictionary<string, string>(setEquips[i]);

                    string[] setAbility = setEquip["セット特殊効果"].Replace("、", ",").Split(new char[] { ',' });
                    for (int j = 0; j < setAbility.Length; j++) {
                        string[] abilityList = setAbility[j].Split(new char[] { '|' });
                        for (int k = 0; k < abilityList.Length; k++) {
                            if (abilityList[k].Contains("ガード")) {
                                string kind = abilityList[k].Substring(0, (abilityList[k].IndexOf("ガード") + 3) - (abilityList[k].IndexOf(":") + 1));
                                string grade = abilityList[k].Substring(abilityList[k].IndexOf("+") + 1, abilityList[k].Length - 1 - (abilityList[k].IndexOf("+") + 1)); // %を除外する
                                float nGrade = Calc.Analyze(grade.Replace("(", "").Replace(")", "")).Calc(null);
                                if (!setEquip.ContainsKey(kind)) {
                                    setEquip[kind] = "0";
                                }
                                setEquip[kind] = (float.Parse(setEquip[kind]) + nGrade).ToString();
                            }
                            else if (!abilityList[k].Contains("軽減") && abilityList[k].Contains("減")) {
                                string kind = abilityList[k].Substring(0, (abilityList[k].IndexOf("ダメージ") + 4) - (abilityList[k].IndexOf(":") + 1));
                                string grade = abilityList[k].Substring(abilityList[k].IndexOf("ダメージ") + 4, abilityList[k].Length - 2 - (abilityList[k].IndexOf("ダメージ") + 4)); // %減を除外する
                                float nGrade = Calc.Analyze(grade.Replace("(", "").Replace(")", "")).Calc(null);
                                if (!setEquip.ContainsKey(kind)) {
                                    setEquip[kind] = "0";
                                }
                                setEquip[kind] = (float.Parse(setEquip[kind]) + nGrade).ToString();
                            }
                        }
                    }

                    Dictionary<string, int> partsCheckList = new Dictionary<string, int>();
                    for (int j = 0; j < bodyParts.Length; j++) {
                        partsCheckList[bodyParts[j]] = 0;
                    }
                    bool haveSet = true;
                    List<Dictionary<string, string>> haveSetList = new List<Dictionary<string, string>>();
                    string[] needEquips = setEquip["必要装備"].Split(new char[] { ',' });
                    for (int j = 0; j < needEquips.Length; j++) {
                        Dictionary<string, string> equip = allEquips.Where(x => x["アイテム名"] == needEquips[j]).FirstOrDefault();
                        partsCheckList[equip["分類"]] = 1;

                        if (haveEquipList.Where(x => x["アイテム名"].Contains(needEquips[j])).Count() == 0) {
                            haveSet = false;
                        }
                        else {
                            haveSetList.AddRange(haveEquipList.Where(x => x["アイテム名"].Contains(needEquips[j])).ToList());
                        }
                    }
                    if (haveSet) {
                        displayEquipSet(user, bodyParts, appendParts, partsCheckList, allEquips, replaceParts, haveEquipList, haveSetList, targetRegistList, setEquip, orbRegistList);
                    }
                }
            }
            else {
                Dictionary<string, int> partsCheckList = new Dictionary<string, int>();
                for (int j = 0; j < bodyParts.Length; j++) {
                    partsCheckList[bodyParts[j]] = 0;
                }
                List<Dictionary<string, string>> haveSetList = new List<Dictionary<string, string>>();

                displayEquipSet(user, bodyParts, appendParts, partsCheckList, allEquips, replaceParts, haveEquipList, haveSetList, targetRegistList, null, orbRegistList);
            }


        }

        private void displayEquipSet(string user, string[] bodyParts, string[] appendParts, Dictionary<string, int> partsCheckList, List<Dictionary<string, string>> allEquips, Dictionary<string, string> replaceParts, List<Dictionary<string, string>> haveEquipList, List<Dictionary<string, string>> haveSetList, Dictionary<string, float> targetRegistList, Dictionary<string, string> setEquip, Dictionary<string, float> orbRegistList) {
            // セットに含まれていない装備を検索
            List<Dictionary<string, string>> nonSetList = new List<Dictionary<string, string>>();
            List<string> nonParts = partsCheckList.Where(x => x.Value == 0).Select(x => x.Key).ToList();
            for (int j = 0; j < nonParts.Count; j++) {
                nonSetList.AddRange(allEquips.Where(x => x["分類"] == replaceParts[nonParts[j]]).ToList());
            }
            nonSetList = nonSetList.OrderByDescending(x => x.ContainsKey("LV") ? int.Parse(x["LV"]) : 1).ToList();
            for (int j = 0; j < nonSetList.Count; j++) {
                if (appendParts.Select(x => replaceParts[x]).Contains(nonSetList[j]["分類"])) {
                    haveSetList.AddRange(haveEquipList.Where(x => x["アイテム名"].Contains(nonSetList[j]["アイテム名"]) && (x["効果"].Contains("ガード") || x["効果"].Contains("減"))).ToList());
                }
                else {
                    haveSetList.AddRange(haveEquipList.Where(x => x["アイテム名"].Contains(nonSetList[j]["アイテム名"])).ToList());
                }
            }

            List<List<Dictionary<string, string>>> allCheckEquipList = new List<List<Dictionary<string, string>>>();
            // 手持ちの装備がそろった

            checkEquipList(haveSetList, bodyParts.ToList(), null, 0, ref allCheckEquipList);


            //List<List<Dictionary<string, string>>> checkOkEquipList = new List<List<Dictionary<string, string>>>();
            List<Dictionary<string, float>> registList = new List<Dictionary<string, float>>();

            List<bool> checkList = new List<bool>();
            for (int j = 0; j < allCheckEquipList.Count; j++) {
                registList.Add(new Dictionary<string, float>());
                List<Dictionary<string, string>> equipSet = allCheckEquipList[j];
                Dictionary<string, float> prevTargetRegistList = new Dictionary<string, float>(targetRegistList);
                for (int k = 0; k < equipSet.Count; k++) {
                    Dictionary<string, string> equip = equipSet[k];
                    foreach (string key in targetRegistList.Keys) {
                        if (equip.ContainsKey(key)) {
                            prevTargetRegistList[key] -= float.Parse(equip[key]);
                        }
                    }
                    foreach (string key in equip.Keys.Where(x => x.Contains("ガード") || x.Contains("ダメージ"))) {
                        if (!registList[j].ContainsKey(key)) {
                            registList[j].Add(key, 0);
                        }
                        registList[j][key] += float.Parse(equip[key]);
                    }

                }
                if(setEquip != null) {
                    foreach (string key in setEquip.Keys.Where(x => x.Contains("ガード") || x.Contains("ダメージ"))) {
                        if (!registList[j].ContainsKey(key)) {
                            registList[j].Add(key, 0);
                        }
                        registList[j][key] += float.Parse(setEquip[key]);
                    }
                }

                checkList.Add(true);
                foreach (string key in prevTargetRegistList.Keys) {
                    if (prevTargetRegistList[key] - orbRegistList[key] > 0) {
                        checkList[j] = false;
                    }
                }
            }

            StringBuilder sb = new StringBuilder();

            int index = 1;
            for (int j = 0; j < checkList.Count; j++) {
                if (checkList[j] == true) {
                    sb.Append((index++) + "件目" + Environment.NewLine);
                    for (int k = 0; k < allCheckEquipList[j].Count; k++) {
                        sb.Append(user + "\t");
                        sb.Append(allCheckEquipList[j][k]["アイテム名"] + "\t");
                        sb.Append(allCheckEquipList[j][k]["分類"] + "\t");
                        sb.Append(allCheckEquipList[j][k]["効果"] + "\t");
                        sb.Append(Environment.NewLine);
                    }
                    sb.Append("　全体の耐性:");
                    foreach (string key in registList[j].Keys) {
                        string orbString = "";
                        float orbNum = 0;
                        if (orbRegistList.ContainsKey(key)) {
                            orbNum = orbRegistList[key];
                            if (orbNum > 0) {
                                orbString = "(宝珠" + orbRegistList[key] + ")";
                            }
                        }
                        sb.Append(key + (registList[j][key] + orbNum) + orbString + "% ");
                    }
                    sb.Append(Environment.NewLine);
                    sb.Append(Environment.NewLine);
                }
            }
            resultList.Items.Clear();
            resultList.Items.AddRange(sb.ToString().Split(new string[] { Environment.NewLine }, StringSplitOptions.None));
        }


        private void checkEquipList(List<Dictionary<string, string>> haveSetList, List<string> bodyParts, List<Dictionary<string, string>> setEquipList, int index, ref List<List<Dictionary<string, string>>> allCheckEquipList) {
            if(index >= bodyParts.Count) {
                allCheckEquipList.Add(setEquipList);
                return;
            }

            if(setEquipList == null) {
                setEquipList = new List<Dictionary<string, string>>();
            }
            int prevIndex = index;
            List<Dictionary<string, string>> prevList = new List<Dictionary<string, string>>(setEquipList);
            string nowParts = bodyParts[index];
            List<Dictionary<string, string>> partsList = haveSetList.Where(x => x["分類"] == nowParts).ToList();
            for(int i = 0; i < partsList.Count; i++) {
                index = prevIndex;
                setEquipList = new List<Dictionary<string, string>>(prevList);
                /*
                if(setEquipList == null) {
                    setEquipList = new List<Dictionary<string, string>>();
                }
                */
                setEquipList.Add(partsList[i]);
                checkEquipList(haveSetList, bodyParts, setEquipList, ++index, ref allCheckEquipList);
            }
            return;
        }

        private void button17_Click(object sender, EventArgs e) {
            if (numericUpDown4.Value != 0) {
                numericUpDown4.Value -= 1;
            }
            savePositionData();
        }

        private void button18_Click(object sender, EventArgs e) {
            if (numericUpDown3.Value != 0) {
                numericUpDown3.Value -= 1;
            }
            savePositionData();
        }

        private void button16_Click(object sender, EventArgs e) {
            numericUpDown3.Value += 1;
            savePositionData();
        }

        private void button15_Click(object sender, EventArgs e) {
            numericUpDown4.Value += 1;
            savePositionData();
        }
    }
}
