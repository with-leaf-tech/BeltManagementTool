using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace OcrClassLibrary {
    public class OcrClassLibrary : OcrClassBase {
        Capture cap = new Capture();

        public string OcrFromImage(string path, string baseLang) {
            StringBuilder sb = new StringBuilder();
            var result = Recognize(path, baseLang);
            foreach (var l in result.Lines) {
                sb.Append(l.Text);
            }
            return sb.ToString();
        }

        public string OcrFromClipBoard(string baseLang) {
            StringBuilder sb = new StringBuilder();
            var result = Recognize(baseLang);
            foreach (var l in result.Lines) {
                sb.Append(l.Text);
            }
            return sb.ToString();
        }


        public void CameraStart(int deviceNum) {
            cap.Initialize(deviceNum);
        }

        public void CameraEnd() {
            cap.CloseInterfaces();
        }

        public void CaptureImage() {
            SendKeys.SendWait("^{PRTSC}");
            //次のようにすると、アクティブなウィンドウのイメージをコピー
            //SendKeys.SendWait("%{PRTSC}");
            //SendKeys.SendWait("{PRTSC}");

            //DoEventsを呼び出したほうがよい場合があるらしい
            Application.DoEvents();
            //System.Threading.Thread.Sleep(5000);
            //クリップボードにあるデータの取得
            IDataObject d = Clipboard.GetDataObject();
            //クリップボードにデータがあったか確認
            if (d != null) {
                //ビットマップデータ形式に関連付けられているデータを取得
                Image img = (Image)d.GetData(DataFormats.Bitmap);
                if (img != null) {
                    //img.Save(@"C:\Temp2\cap3.jpg", System.Drawing.Imaging.ImageFormat.Jpeg);

                    //画面のイメージデータは大きいため、
                    //用がなくなればクリップボードから削除した方がいいかもしれない
                    //Clipboard.SetDataObject(new DataObject());
                }
            }
        }


        public string OcrFromCamera(string baseLang) {
            Bitmap bitmap = cap.CaptureImage();
            StringBuilder sb = new StringBuilder();
            var result = Recognize(bitmap, baseLang);
            foreach (var l in result.Lines) {
                sb.Append(l.Text);
            }
            return sb.ToString();
        }

        public string OcrFromBitmap(Bitmap bitmap, string baseLang) {
            StringBuilder sb = new StringBuilder();
            var result = Recognize(bitmap, baseLang);
            foreach (var l in result.Lines) {
                sb.Append(l.Text);
            }
            return sb.ToString();
        }


    }
}
