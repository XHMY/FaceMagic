using System;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Web;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Text;
using System.Collections.Generic;
using System.Linq;

namespace FaceMagic_Console
{
    class Program
    {
        static void Main(string[] args)
        {
            List <Face>  Person= new List<Face>();
            StreamReader MyKey = new StreamReader("API_Key.txt");
            MyValue.API_Key = MyKey.ReadToEnd();
            MyKey.Close();
            Console.WriteLine("Would you want to read file from \"JSON_Value.txt\"?" +
                "    Hit \"Enter\" to read the file." +
                "\n(If you Don't know what I said,please Hit \"Space\")");
            var Hit = Console.ReadKey();
            Console.Clear();
            if (Hit.KeyChar=='\r')
            {
                Person = ReadJSONFile("JSON_Value.txt");
                MyValue.T_Sample = "true";
                string[] MySampleDicere = Directory.GetFiles("Sample");
                foreach (var MSD in MySampleDicere)
                {
                    MyValue.Finish = "";
                    API_Detect(MSD);
                    while (MyValue.Finish != "OK")
                    {
                        System.Threading.Thread.Sleep(200);
                    }
                    Console.WriteLine("You have upload {0} successful.", MyValue.T_FaceValue.Directory_F);
                    Person.Add(MyValue.T_FaceValue);
                }
                
            }
            else
            {
                Person= Get_FaceBasicInformation();  
            }          
            Person = Get_Confidence(Person);
            EndConsoleOutput();
        }

        static void EndConsoleOutput()
        {
            Console.WriteLine("\n################--------Designed by ZengYF--------################");
            Console.WriteLine("\n---------Power by Microsoft---------");
            Console.WriteLine("\nLearn more about this App in \"https://github.com/XHMY/FaceMagic\"");
            Console.WriteLine("\n\nHit ENTER to exit...");
            Console.ReadLine();
        }

        static List<Face> ReadJSONFile(string directory)
        {
            List<Face> Person = new List<Face>();
            StreamReader ReadJSON_TXT = new StreamReader(directory);
            MyValue.T_FileJSON = ReadJSON_TXT.ReadToEnd();
            ReadJSON_TXT.Close();
            MyValue.T_FileJSON = MyValue.T_FileJSON.Substring(0,MyValue.T_FileJSON.Length-1);
            JObject T_OJSON = new JObject(); 
            string[] aT_FileJSON = MyValue.T_FileJSON.Split('|');
            foreach (string json in aT_FileJSON)
            {
                T_OJSON = JObject.Parse(json);
                MyValue.T_FaceValue = new Face(T_OJSON["faceId"].ToString(),
                T_OJSON["faceAttributes"]["gender"].ToString(),
                T_OJSON["faceAttributes"]["age"].ToString(),
                T_OJSON["Name"].ToString(),
                T_OJSON["Path"].ToString(),
                T_OJSON["Sample"].ToString());
                if (MyValue.T_FaceValue.Sample_F=="false")
                {
                    Person.Add(MyValue.T_FaceValue);
                }
                
            }
            return Person;
        }
        static List<Face> Get_Confidence(List<Face> Person)
        {
            JArray j_FaceIds = new JArray();
            var sam_Faces = from sam_Person in Person
                           where sam_Person.Sample_F == "true"
                           select new { sam_Person.ID_F };      
            foreach (Face people in Person)
            {
                if (people.Sample_F=="false")
                {
                    j_FaceIds.Add(people.ID_F);
                }                
            }
            MyValue.Finish = "";
            foreach (var sam_faceid in sam_Faces)
            {                
                API_FindSimilar(sam_faceid.ToString().Substring(9,sam_faceid.ToString().Length-11), j_FaceIds, "matchFace");
                while (MyValue.Finish != "OK")
                {
                    System.Threading.Thread.Sleep(200);
                }
            }
            string MyJSON_T = MyValue.T_FileJSON.Replace("},{", "}|{");
            string[] MyJSON = MyJSON_T.Split('|');
                        
            foreach (string MJ in MyJSON)
            {
                JObject json = new JObject();
                json = JObject.Parse(MJ);
                var c_result = Person.Where(p => p.ID_F == json["faceId"].ToString());
                foreach (Face SCR in c_result)
                {
                    SCR.Confidence_F = double.Parse(json["confidence"].ToString());
                }
            }
            var _orderedPerson = Person.OrderByDescending(p => p.Confidence_F);
            Console.WriteLine("\nNow, we can show you the similarity of the sample face with each of the group face.");
            foreach (Face people in _orderedPerson)
            {
                if (people.Sample_F=="false")
                {
                    Console.WriteLine("Name: {0}  Gender: {1}  Age: {2}  Confidence: {3}", people.Name_F,people.Gender_F,people.Age_F, people.Confidence_F);
                }
                else
                {
                    Console.WriteLine("\nSampleFaceName: {0}  Gender: {1}  Age: {2}", people.Name_F, people.Gender_F, people.Age_F);
                }
            }
            MyValue.Count = 0;
            var SamePeople = Person.Where(p=>p.Confidence_F >0.7);
            foreach (Face sp in SamePeople)
            {
                if (sp.Sample_F=="false")
                {
                    MyValue.Count++;
                    File.Copy(Path.GetFullPath(sp.Directory_F), "Result\\" 
                        +sp.Name_F
                        +sp.Directory_F.Substring(sp.Directory_F.Length-4),true);
                }
            }
            Console.WriteLine("\nWe have found {0} picture have the same with sample picture." +
                "\nYou can see it in the float \"Result\"",MyValue.Count);
            return Person;
        }
        static List<Face> Get_FaceBasicInformation()
        {
            List<Face> Person = new List<Face>();  //Creat a new list called Person
            StringBuilder W_FileJSON = new StringBuilder(); //W_FileJSON is the String be written into the TXT file.
            W_FileJSON.Clear();
            MyValue.Finish = "";
            string[] MyPhotoDicrectory = Directory.GetFiles("PhotoGroup"); //MyPhotoDicrectory can help API upload all the file in the float photo.
            string[] MySampleDicere = Directory.GetFiles("Sample");
            MyValue.Count = 0;
            MyValue.T_Sample = "false";
            foreach (string MPD in MyPhotoDicrectory)
            {   
                API_Detect(MPD);
                while (MyValue.Finish != "OK")
                {
                    System.Threading.Thread.Sleep(200);
                }
                MyValue.Finish = "";
                Console.WriteLine("You have upload {0} successful.", MyValue.T_FaceValue.Directory_F);
                Person.Add(MyValue.T_FaceValue);
                W_FileJSON.Append(MyValue.T_FileJSON);
                W_FileJSON.Append("|");
                MyValue.Count++;
            }
            MyValue.T_Sample = "true";
            foreach (string MSD in MySampleDicere)
            {
                API_Detect(MSD);
                while (MyValue.Finish != "OK")
                {
                    System.Threading.Thread.Sleep(200);
                }
                MyValue.Finish = "";
                Console.WriteLine("You have upload {0} successful.", MyValue.T_FaceValue.Directory_F);            
                Person.Add(MyValue.T_FaceValue);
                W_FileJSON.Append(MyValue.T_FileJSON);
                W_FileJSON.Append("|");
                MyValue.Count++;
            }
            Console.WriteLine("***********----------------SUCCESS----------------***********");
            foreach (Face people in Person)
            {
                Console.WriteLine("Name:{0} || Gender:{1} || Age:{2} || FaceID:{3}",
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

        static async void API_Detect(string T_Directory)
        {
            var client = new HttpClient();
            var queryString = HttpUtility.ParseQueryString(string.Empty);
            // Request headers
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", MyValue.API_Key);
            // Request parameters
            queryString["returnFaceId"] = "true";
            queryString["returnFaceLandmarks"] = "false";
            queryString["returnFaceAttributes"] = "gender,age,smile,glasses";
            var uri = "https://api.cognitive.azure.cn/face/v1.0/detect?" + queryString;
            HttpResponseMessage response;
            using (var content = new ByteArrayContent(File.ReadAllBytes(T_Directory)))
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
            int found1 = 0;
            int found2 = 0;
            found1 = T_Directory.IndexOf("\\");
            found2 = T_Directory.IndexOf(".");
            Result_A.Add("Name", T_Directory.Substring(found1 + 1, found2 - found1 - 1));
            Result_A.Add("Path", T_Directory);
            Result_A.Add("Sample", MyValue.T_Sample);
            MyValue.T_FileJSON = Result_A.ToString();
            //Console.WriteLine(Result_A["faceAttributes"]["gender"].ToString());//outut test                      
            MyValue.T_FaceValue = new Face(Result_A["faceId"].ToString(),
                Result_A["faceAttributes"]["gender"].ToString(),
                Result_A["faceAttributes"]["age"].ToString(),
                Result_A["Name"].ToString(),
                Result_A["Path"].ToString(),
                Result_A["Sample"].ToString());
            MyValue.Finish = response.StatusCode.ToString();   
        }

        static async void API_FindSimilar(string T_FaceId , JArray FaceIds,string mode)
        {
            JObject Body_J = new JObject();
            Body_J.Add("faceId",T_FaceId);            
            Body_J.Add("faceIds", FaceIds);
            Body_J.Add("mode",mode);
            var client = new HttpClient();
            var queryString = HttpUtility.ParseQueryString(string.Empty);

            // Request headers
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", MyValue.API_Key);

            var uri = "https://api.cognitive.azure.cn/face/v1.0/findsimilars?" + queryString;

            HttpResponseMessage response;

            // Request body
            byte[] byteData = Encoding.UTF8.GetBytes(Body_J.ToString());

            using (var content = new ByteArrayContent(byteData))
            {
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                response = await client.PostAsync(uri, content);
            }
            MyValue.T_FileJSON = await response.Content.ReadAsStringAsync();
            MyValue.T_FileJSON = MyValue.T_FileJSON.Substring(0, MyValue.T_FileJSON.Length - 1);
            MyValue.T_FileJSON = MyValue.T_FileJSON.Substring(1);
            
            MyValue.Finish = response.StatusCode.ToString();
        }
    }
    public class MyValue
    {
        public static string API_Key { get; set; }
        public static string T_Sample { get; set; }
        public static string Finish { get; set; }
        public static int Count { get; set; }
        public static Face T_FaceValue { get; set; }
        public static string T_Directory { get; set; }
        public static string T_FileJSON { get; set; }
        
    }
    public class Face
    {
        public string Name_F { get; set; }
        public string ID_F { get; set; }
        public string Gender_F { get; set; }
        public string Age_F { get; set; }
        public string Directory_F { get; set;}
        public string Sample_F { get; set; }
        public double Confidence_F { get; set; }
        public string Same_F { get; set; }

        public Face(string id_f, string gender_f, string age_f, string name_f ,string directory_f, string sample_f)
        {
            this.ID_F = id_f;
            this.Age_F = age_f;
            this.Gender_F = gender_f;
            this.Name_F = name_f;
            this.Directory_F = directory_f;
            this.Sample_F = sample_f;
        }

        public Face(string id_f, double confidence_f)
        {
            this.ID_F = id_f;
            this.Confidence_F = confidence_f; 
        }

        public Face(string id_f, string same_f)
        {
            this.ID_F = id_f;
            this.Same_F = same_f;
        }
    }
}
