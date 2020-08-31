using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace BeltManagementTool.OCR {
    class AzureComputerVisionApiOCR : OcrBase {

        [Serializable]
        class IResult {
            public string language;
            public float textAngle;
            public string orientation;
            // 複数のリージョン
            public IRegion[] regions;
        }

        // リージョン
        [Serializable]
        class IRegion {
            public string boundingBox;
            // 複数行を持つ
            public ILine[] lines;
        }

        // 行
        [Serializable]
        class ILine {
            public string boundingBox;
            public IWord[] words;
        }

        // 単語
        [Serializable]
        class IWord {
            public string boundingBox;
            public string text;
        }

        HttpClient client = new HttpClient();
        string uri = "";

        public AzureComputerVisionApiOCR() {

        }

        public override void initialize(string accessKey) {
            string[] keys = File.ReadAllLines(accessKey);
            string subscriptionKey = keys[0];
            string endpoint = keys[1];
            string uriBase = endpoint + "vision/v3.0/ocr";

            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", subscriptionKey);
            string requestParameters = "language=unk&detectOrientation=true";
            uri = uriBase + "?" + requestParameters;
        }

        public override string GetTextFromImage(System.Drawing.Bitmap bitmap, string baseLang) {
            string contentString = GetTextAsync(bitmap).Result;

            IResult weatherForecast = Deserialize<IResult>(contentString);

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < weatherForecast.regions.Length; i++) {
                IRegion region = weatherForecast.regions[i];
                for (int j = 0; j < region.lines.Length; j++) {
                    ILine line = region.lines[j];
                    for (int k = 0; k < line.words.Length; k++) {
                        IWord word = line.words[k];
                        sb.Append(word.text);
                    }
                    sb.Append(Environment.NewLine);
                }
            }
            return sb.ToString();
        }

        public async Task<string> GetTextAsync(System.Drawing.Bitmap bitmap) {
            HttpResponseMessage response;

            using (ByteArrayContent content = new ByteArrayContent(ImageToByte(bitmap))) {
                content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

                response = await client.PostAsync(uri, content);
            }

            string contentString = await response.Content.ReadAsStringAsync();
            return contentString;
        }

        public T Deserialize<T>(string json) {
            T result;

            System.Runtime.Serialization.Json.DataContractJsonSerializer serializer
                        = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(T));

            using (System.IO.MemoryStream stream
                = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(json))) {
                result = (T)serializer.ReadObject(stream);
            }

            return result;

        }
    }
}
