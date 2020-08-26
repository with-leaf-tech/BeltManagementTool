using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Windows.Globalization;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage.Streams;

namespace OcrClassLibrary {
    abstract public class OcrClassBase {

        protected OcrResult Recognize(string filename, string baseLang) {
            Task<OcrResult> result = OcrMain(filename, baseLang);
            result.Wait();
            return result.Result;
        }
        protected OcrResult Recognize(string baseLang) {
            Task<OcrResult> result = OcrMain(baseLang);
            result.Wait();
            return result.Result;
        }
        protected OcrResult Recognize(Bitmap bitmap, string baseLang) {
            Task<OcrResult> result = OcrMain(bitmap, baseLang);
            result.Wait();
            return result.Result;
        }

        private async Task<OcrResult> OcrMain(string filename, string baseLang) {
            OcrEngine ocrEngine = OcrEngine.TryCreateFromLanguage(new Language(baseLang));
            var bitmap = await LoadImage(filename);
            var ocrResult = await ocrEngine.RecognizeAsync(bitmap);
            return ocrResult;
        }

        private async Task<OcrResult> OcrMain(string baseLang) {
            SoftwareBitmap bitmap = null;
            BitmapSource image = System.Windows.Clipboard.GetImage();
            System.Windows.Media.Imaging.BitmapFrame bitmapFrame;
            var encoder = new BmpBitmapEncoder();
            encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(image));
            using (var stream = new MemoryStream()) {
                encoder.Save(stream);
                stream.Seek(0, SeekOrigin.Begin);
                var decoder = System.Windows.Media.Imaging.BitmapDecoder.Create(stream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                bitmapFrame = decoder.Frames[0];
                bitmap = ConvertFrom(bitmapFrame).Result;
            }

            OcrEngine ocrEngine = OcrEngine.TryCreateFromLanguage(new Language(baseLang));
            var ocrResult = await ocrEngine.RecognizeAsync(bitmap);
            return ocrResult;
        }
        private async Task<OcrResult> OcrMain(Bitmap bitmap, string baseLang) {
            OcrEngine ocrEngine = OcrEngine.TryCreateFromLanguage(new Language(baseLang));

            MemoryStream ms = new MemoryStream();
            bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            var stream = await ConvertToRandomAccessStream(ms);
            var bb = await LoadImage(stream);

            var ocrResult = await ocrEngine.RecognizeAsync(bb);
            return ocrResult;
        }

        public async Task<SoftwareBitmap> ConvertFrom(System.Windows.Media.Imaging.BitmapFrame sourceBitmap) {
            // BitmapFrameをBMP形式のバイト配列に変換
            byte[] bitmapBytes;
            var encoder = new BmpBitmapEncoder(); // ここは.NET用のエンコーダーを使う
            encoder.Frames.Add(sourceBitmap);
            using (var memoryStream = new MemoryStream()) {
                encoder.Save(memoryStream);
                bitmapBytes = memoryStream.ToArray();
            }

            // バイト配列をUWPのIRandomAccessStreamに変換
            using (var randomAccessStream = new InMemoryRandomAccessStream()) {
                using (var outputStream = randomAccessStream.GetOutputStreamAt(0))
                using (var writer = new DataWriter(outputStream)) {
                    writer.WriteBytes(bitmapBytes);
                    await writer.StoreAsync();
                    await outputStream.FlushAsync();
                }

                // IRandomAccessStreamをSoftwareBitmapに変換
                // （ここはUWP APIのデコーダーを使う）
                var decoder = await Windows.Graphics.Imaging.BitmapDecoder.CreateAsync(randomAccessStream);
                var softwareBitmap
                  = await decoder.GetSoftwareBitmapAsync(BitmapPixelFormat.Bgra8,
                                                         BitmapAlphaMode.Premultiplied);
                return softwareBitmap;
            }
        }

        private async Task<SoftwareBitmap> LoadImage(string path) {
            var fs = System.IO.File.OpenRead(path);
            var buf = new byte[fs.Length];
            fs.Read(buf, 0, (int)fs.Length);
            var mem = new MemoryStream(buf);
            mem.Position = 0;

            var stream = await ConvertToRandomAccessStream(mem);
            var bitmap = await LoadImage(stream);
            return bitmap;
        }
        private async Task<SoftwareBitmap> LoadImage(IRandomAccessStream stream) {
            var decoder = await Windows.Graphics.Imaging.BitmapDecoder.CreateAsync(stream);
            var bitmap = await decoder.GetSoftwareBitmapAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
            return bitmap;
        }
        private async Task<IRandomAccessStream> ConvertToRandomAccessStream(MemoryStream memoryStream) {
            var randomAccessStream = new InMemoryRandomAccessStream();
            var outputStream = randomAccessStream.GetOutputStreamAt(0);
            var dw = new DataWriter(outputStream);
            var task = new Task(() => dw.WriteBytes(memoryStream.ToArray()));
            task.Start();
            await task;
            await dw.StoreAsync();
            await outputStream.FlushAsync();
            return randomAccessStream;
        }


    }
}
