using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace search_image
{
    public class Program2
    {
        // Replace <Subscription Key> with your valid subscription key.
        const string subscriptionKey = "275e69c6e73b4958924ee9496cdb5e23";

        // You must use the same region in your REST call as you used to
        // get your subscription keys. For example, if you got your
        // subscription keys from westus, replace "westcentralus" in the URL
        // below with "westus".
        //
        // Free trial subscription keys are generated in the westcentralus region.
        // If you use a free trial subscription key, you shouldn't need to change
        // this region.
        const string uriBase =
            "https://westcentralus.api.cognitive.microsoft.com/vision/v2.0/analyze";
        
        public static void readfile()
        {
            // Get the path and filename to process from the user.
            Console.WriteLine("Analyze an image:");
            Console.Write("Enter the path to the image you wish to analyze: ");

            string folder_path = Form1.folder_path;
            string imageFilePath = folder_path+"\\Pictures"; //储存图像的文件夹名称
            DirectoryInfo folder = new DirectoryInfo(imageFilePath);

            foreach (FileInfo file in folder.GetFiles("*.jpg"))
            {
                Console.WriteLine(file.FullName);
                if (File.Exists(file.FullName))
                {
                    // Make the REST API call.
                    Console.WriteLine("\nWait a moment for the results to appear.\n");
                    MakeAnalysisRequest(file.FullName).Wait();
                    
                }
                else
                {
                    Console.WriteLine("\nInvalid file path");
                }
                Console.WriteLine("\nPress Enter to exit...");
                Console.ReadLine();
            }
            
        }

        /// <summary>
        /// Gets the analysis of the specified image file by using
        /// the Computer Vision REST API.
        /// </summary>
        /// <param name="imageFilePath">The image file to analyze.</param>
        public static async Task MakeAnalysisRequest(string imageFilePath)
        {
            try
            {
                HttpClient client = new HttpClient();

                // Request headers.
                client.DefaultRequestHeaders.Add(
                    "Ocp-Apim-Subscription-Key", subscriptionKey);

                // Request parameters. A third optional parameter is "details".
                string requestParameters =
                    "visualFeatures=Categories,Description,Color";

                // Assemble the URI for the REST API Call.
                string uri = uriBase + "?" + requestParameters;

                HttpResponseMessage response;

                // Request body. Posts a locally stored JPEG image.
                byte[] byteData = GetImageAsByteArray(imageFilePath);

                using (ByteArrayContent content = new ByteArrayContent(byteData))
                {
                    // This example uses content type "application/octet-stream".
                    // The other content types you can use are "application/json"
                    // and "multipart/form-data".
                    content.Headers.ContentType =
                        new MediaTypeHeaderValue("application/octet-stream");

                    // Make the REST API call.
                    Console.WriteLine("before await");
                    //response = await client.PostAsync(uri, content);
                    response = client.PostAsync(uri, content).Result;//.Content.ReadAsStringAsync().Result;
                    //return response;
                    Console.WriteLine("after readfile");
                }

                // Get the JSON response.
                string contentString = await response.Content.ReadAsStringAsync();
                string folder_path = Form1.folder_path;
                // Display the JSON response.
                Console.WriteLine("\nResponse:\n\n{0}\n",
                    JToken.Parse(contentString).ToString());
                string file = imageFilePath.Substring(imageFilePath.LastIndexOf("\\") + 1);
                string name = file.Substring(0, file.LastIndexOf("."));
                string[] pathpart = { folder_path+"\\Imagedata\\", name, ".txt" };//图片数据文件夹
                string imagedata = String.Join("",pathpart);
                
                System.IO.File.WriteAllText(imagedata,contentString);
            }
            catch (Exception e)
            {
                Console.WriteLine("\n" + e.Message);
            }
        }

        /// <summary>
        /// Returns the contents of the specified file as a byte array.
        /// </summary>
        /// <param name="imageFilePath">The image file to read.</param>
        /// <returns>The byte array of the image data.</returns>
        public static byte[] GetImageAsByteArray(string imageFilePath)
        {
            using (FileStream fileStream =
                new FileStream(imageFilePath, FileMode.Open, FileAccess.Read))
            {
                BinaryReader binaryReader = new BinaryReader(fileStream);
                return binaryReader.ReadBytes((int)fileStream.Length);
            }
        }
    }
}