using System;
using System.Collections.Generic;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading;
using System.Linq;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Web;
using System.Runtime.Serialization;

namespace TestAppAzure {
    static class Program {
        // OCRの結果

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

        // Add your Computer Vision subscription key and endpoint to your environment variables.
        static string subscriptionKey = "xxxxxxx";

        static string endpoint = "xxxxxx";

        // the OCR method endpoint
        static string uriBase = endpoint + "vision/v2.0/ocr";

        static async Task Main() {
            // Get the path and filename to process from the user.
            Console.WriteLine("Optical Character Recognition:");
            Console.Write("Enter the path to an image with text you wish to read: ");
            string imageFilePath = @"C:\Tools\test.png";

            if (File.Exists(imageFilePath)) {
                // Call the REST API method.
                Console.WriteLine("\nWait a moment for the results to appear.\n");
                await MakeOCRRequest(imageFilePath);
            }
            else {
                Console.WriteLine("\nInvalid file path");
            }
            Console.WriteLine("\nPress Enter to exit...");
            Console.ReadLine();
        }

        /// <summary>
        /// Gets the text visible in the specified image file by using
        /// the Computer Vision REST API.
        /// </summary>
        /// <param name="imageFilePath">The image file with printed text.</param>
        static async Task MakeOCRRequest(string imageFilePath) {
            try {
                HttpClient client = new HttpClient();

                // Request headers.
                client.DefaultRequestHeaders.Add(
                    "Ocp-Apim-Subscription-Key", subscriptionKey);

                // Request parameters. 
                // The language parameter doesn't specify a language, so the 
                // method detects it automatically.
                // The detectOrientation parameter is set to true, so the method detects and
                // and corrects text orientation before detecting text.
                string requestParameters = "language=unk&detectOrientation=true";

                // Assemble the URI for the REST API method.
                string uri = uriBase + "?" + requestParameters;

                HttpResponseMessage response;

                // Read the contents of the specified local image
                // into a byte array.
                byte[] byteData = GetImageAsByteArray(imageFilePath);

                // Add the byte array as an octet stream to the request body.
                using (ByteArrayContent content = new ByteArrayContent(byteData)) {
                    // This example uses the "application/octet-stream" content type.
                    // The other content types you can use are "application/json"
                    // and "multipart/form-data".
                    content.Headers.ContentType =
                        new MediaTypeHeaderValue("application/octet-stream");

                    // Asynchronously call the REST API method.
                    response = await client.PostAsync(uri, content);
                }

                // Asynchronously get the JSON response.
                string contentString = await response.Content.ReadAsStringAsync();
                JToken aaa = JToken.Parse(contentString);

                IResult weatherForecast = Deserialize<IResult>(contentString);

                StringBuilder sb = new StringBuilder();
                for(int i = 0; i < weatherForecast.regions.Length; i++) {
                    IRegion region = weatherForecast.regions[i];
                    for(int j = 0; j < region.lines.Length; j++) {
                        ILine line = region.lines[j];
                        for(int k = 0; k < line.words.Length; k++) {
                            IWord word = line.words[k];
                            sb.Append(word.text);
                        }
                        sb.Append(Environment.NewLine);
                    }
                }

                Console.WriteLine("\nResponse:\n\n{0}\n", sb.ToString());


                // Display the JSON response.
                //Console.WriteLine("\nResponse:\n\n{0}\n", JToken.Parse(contentString).ToString());
            }
            catch (Exception e) {
                Console.WriteLine("\n" + e.Message);
            }
        }
        static public T Deserialize<T>(string json) {
            T result;

            System.Runtime.Serialization.Json.DataContractJsonSerializer serializer
                        = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(T));

            using (System.IO.MemoryStream stream
                = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(json))) {
                result = (T)serializer.ReadObject(stream);
            }

            return result;

        }

        /// <summary>
        /// Returns the contents of the specified file as a byte array.
        /// </summary>
        /// <param name="imageFilePath">The image file to read.</param>
        /// <returns>The byte array of the image data.</returns>
        static byte[] GetImageAsByteArray(string imageFilePath) {
            // Open a read-only file stream for the specified file.
            using (FileStream fileStream =
                new FileStream(imageFilePath, FileMode.Open, FileAccess.Read)) {
                // Read the file's contents into a byte array.
                BinaryReader binaryReader = new BinaryReader(fileStream);
                return binaryReader.ReadBytes((int)fileStream.Length);
            }
        }
    }
}
