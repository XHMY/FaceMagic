using System;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Web;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;
using System.Collections.Generic;


namespace FaceMagic_Console
{
    class Program
    {
        static void Main(string[] args)
        {
            List <Face>  Person= new List<Face>();
            Person= Get_FaceBasicInformation();
            EndConsoleOutput();
        }
        static void EndConsoleOutput()
        {
            Console.WriteLine("\n################--------Designed by ZengYF--------################");
            Console.WriteLine("\n---------Power by Microsoft---------");
            Console.WriteLine("\nLearn more about this App in \"github.com/XHMY/FaceMagic\"");
            Console.WriteLine("\n\nHit ENTER to exit...");
            Console.ReadLine();
        }
        static List<Face> Get_FaceBasicInformation()
        {
            List<Face> Person = new List<Face>();  //Creat a new list called Person
            StringBuilder W_FileJSON = new StringBuilder(); //W_FileJSON is the String be written into the TXT file.
            W_FileJSON.Clear();
            MyValue.Finish = "";
            string[] MyPhotoDicrectory = Directory.GetFiles("Photo"); //MyPhotoDicrectory can help API upload all the file in the float photo.
            MyValue.Count = 0;            
            //MyValue.Person = new Face[Directory.GetFiles("Photo").Length];
            //Console.WriteLine(Directory.GetFiles("Photo").Length);
            //Make sure that we has got the right amount of the picture.
            foreach (string MPD in MyPhotoDicrectory)
            {
                
                MyValue.T_Directory = MPD; //Transmit the directory infromation to MyValue
                API_Detect();
                while (MyValue.Finish != "OK")
                {
                    System.Threading.Thread.Sleep(200);
                }
                MyValue.Finish = "";
                Console.WriteLine("You have upload {0} successful.", MyValue.T_FaceValue.Name_F);
                //MyValue.Person[MyValue.Count] = MyValue.T_FaceValue;                
                Person.Add(MyValue.T_FaceValue);
                W_FileJSON.Append("{\"Name\":\"");                
                W_FileJSON.Append(MyValue.T_FaceValue.Name_F);
                W_FileJSON.Append("\"");
                W_FileJSON.Append(MyValue.T_FileJSON);
                W_FileJSON.Append("}");                
                MyValue.Count++;
            }
            Console.WriteLine("***********----------------SUCCESS----------------***********");
            foreach (Face people in Person)
            {
                Console.WriteLine("FileName:{0} || Gender:{1} || Age:{2} || FaceID:{3}",
                    people.Name_F,
                    people.Gender_F,
                    people.Age_F,
                    people.ID_F);
            }
            StreamWriter WriteJSON_TXT = new StreamWriter("JSON_Value.txt");
            WriteJSON_TXT.Write(W_FileJSON.ToString());
            WriteJSON_TXT.Close();
            Console.WriteLine("Warning:The FaceID will expire after 24 hour!!!");
            return Person;
        }


        static async void API_Detect()
        {
            var client = new HttpClient();
            var queryString = HttpUtility.ParseQueryString(string.Empty);
            // Request headers
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", "eb07e36b635143a4ad8245b8584f7494");
            // Request parameters
            queryString["returnFaceId"] = "true";
            queryString["returnFaceLandmarks"] = "false";
            queryString["returnFaceAttributes"] = "gender,age,smile,glasses";
            var uri = "https://api.cognitive.azure.cn/face/v1.0/detect?" + queryString;
            HttpResponseMessage response;
            using (var content = new ByteArrayContent(File.ReadAllBytes(MyValue.T_Directory)))
            {
                content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                response = await client.PostAsync(uri, content);
                
            }
            //Console.WriteLine(response.StatusCode.ToString());//outut test   
            string J_Result = await response.Content.ReadAsStringAsync();
            //Console.WriteLine(await response.Content.ReadAsStringAsync());//outut test
            //Delete []
            J_Result = J_Result.Substring(0, J_Result.Length - 1);
            J_Result = J_Result.Substring(1);
            JObject Result_A = JObject.Parse(J_Result);
            MyValue.T_FileJSON = J_Result.ToString();
            //Console.WriteLine(Result_A["faceAttributes"]["gender"].ToString());//outut test
            int found1 = 0;
            int found2 = 0;
            found1 = MyValue.T_Directory.IndexOf("\\");
            found2 = MyValue.T_Directory.IndexOf(".");
            //MyValue.T_Directory.Substring(1);            
            MyValue.T_FaceValue = new Face(Result_A["faceId"].ToString(),
                Result_A["faceAttributes"]["gender"].ToString(),
                Result_A["faceAttributes"]["age"].ToString(),
                MyValue.T_Directory.Substring(found1+1,found2-found1-1));
            MyValue.Finish = response.StatusCode.ToString();
            //string T_Result = Result_A["faceId"].ToString();
            //Console.WriteLine(T_Result);
            
        }

    }
    public class MyValue
    {
        
        public static string Finish { get; set; }
        public static int Count { get; set; }
        public static Face T_FaceValue { get; set; }
        public static string T_Directory { get; set; }
        public static string T_FileJSON { get; set; }
        //public static Face[] Person { get; set; }
        
    }
    public class Face
    {
        public string Name_F { get; set; }
        public string ID_F { get; set; }
        public string Gender_F { get; set; }
        public string Age_F { get; set; }

        public Face(string id_f, string gender_f, string age_f, string name_f)
        {
            this.ID_F = id_f;
            this.Age_F = age_f;
            this.Gender_F = gender_f;
            this.Name_F = name_f;

        }
    }
}
