using System;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Web;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FaceMagic_Console
{
    class Program
    {
        static void Main(string[] args)
        {
            Face[] Person = new Face[Directory.GetFiles("Photo").Length];
            Console.WriteLine(Directory.GetFiles("Photo").Length);
            UploadFile();
            Console.WriteLine("Hit ENTER to exit...");
            Console.ReadLine();
        }

        static async void UploadFile()
        {
            var client = new HttpClient();
            var queryString = HttpUtility.ParseQueryString(string.Empty);
            // Request headers
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", "eb07e36b635143a4ad8245b8584f7494");
            // Request parameters
            queryString["returnFaceId"] = "true";
            queryString["returnFaceLandmarks"] = "false";
            queryString["returnFaceAttributes"] = "gender,age";
            var uri = "https://api.cognitive.azure.cn/face/v1.0/detect?" + queryString;
            HttpResponseMessage response;
            using (var content = new ByteArrayContent(File.ReadAllBytes("Photo\\1.jpg")))
            {
                content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                response = await client.PostAsync(uri, content);
            }
            string J_Result = await response.Content.ReadAsStringAsync();
            //Delete []
            J_Result = J_Result.Substring(0, J_Result.Length - 1);
            J_Result = J_Result.Substring(1);
            JObject Result_A = JObject.Parse(J_Result);
            string T_Result = Result_A["faceId"].ToString();
            Console.WriteLine(T_Result);
        }
    }

    public class Face
    {

        public string ID_F { get; set; }
        public string Gender_F { get; set; }
        public string Age_F { get; set; }
        public Face(string id_f, string gender_f, string age_f)
        {
            this.ID_F = id_f;
            this.Age_F = age_f;
            this.Gender_F = gender_f;
        }
    }
}
