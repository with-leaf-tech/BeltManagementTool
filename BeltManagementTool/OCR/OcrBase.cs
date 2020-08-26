using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeltManagementTool.OCR {
    abstract public class OcrBase {

        public virtual void initialize(string accessKey) {
        }

        public virtual string GetTextFromImage(System.Drawing.Bitmap bitmap, string baseLang) {
            return "";
        }

        protected byte[] ImageToByte(System.Drawing.Image img) {
            System.Drawing.ImageConverter converter = new System.Drawing.ImageConverter();
            return (byte[])converter.ConvertTo(img, typeof(byte[]));
        }


    }
}
