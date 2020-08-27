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

        //Bitmap captureImage = null;
        GoogleVisionApiOCR googleOcr = null;
        WindowsOCR windowsOcr = null;
        TesseractOCR tesseractOcr = null;
        OcrBase ocr = null;

        string settingFileName = "setting.txt";
        string replaceWordFileName = "replaceWord.txt";
        string userFileName = "user.txt";
        string imageFileName = "image.png";

        private string _itemDataFile = "_item.txt";
        private string _equipDataFile = "_equip.txt";
        
        string[] equips = new string[] { "鎌", "両手杖", "アタマ", "からだ上", "からだ下", "ウデ", "足", "顔", "首", "指", "胸", "腰", "札", "その他", "紋章", "証" };

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
            loadUserData();
            loadPositionData();

            selectEquip.Checked = true;
            selectUser.Checked = true;

            initialize();
        }

        private async void initialize() {
            googleOcr = new GoogleVisionApiOCR();
            googleOcr.initialize(@"C:\Users\tsutsumi\Downloads\try-apis-8b2095f28b0e.json");
            windowsOcr = new WindowsOCR();

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
            if(ocrRadioWindows.Checked) {
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
2神域の杖+3
両手杖レア度 B
死んでも早詠みの 使い込み。店売り不可
効果が残りうる
行動しやすい杖
Lv 99以上装備可
追加効果
錬金石D赤の練金石
基礎効果:死亡時 50.0%で早詠みの杖が消えない
基礎効果:2.0%でタ-ン消費なし(試合無効)
錬金効果:呪文ぼうそう率 +1.4(-0.9)%
錬金効果:攻撃時 4%でマヒ
錬金効果:攻撃時 4%でルカニ
できのよさ: 攻撃魔力 +6
戦士 僧侶 魔使 武闘 盗賊 旅芸 バト パラ 魔戦 レン 買賢者 スパ
まも どう 踊り 占い 天地 遊び デス
0錬金強化を見る
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
                        List<string[]> aa = data.Where(x => (x.Split(new char[] { '\t' })[1]) == selectEquipItem).Select(x => (x.Split(new char[] { '\t' })[2]).Split(new char[] { ' ' })).ToList();
                        List<string[]> bb = aa.Select(x => x.Where(y =>
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
                    string user = parts[0];
                    string equip = parts[1];
                    string count = parts[2];
                    updateWindow.setItems("", equip, user, count);
                }
                updateWindow.Show();
            }


        }
    }
}
