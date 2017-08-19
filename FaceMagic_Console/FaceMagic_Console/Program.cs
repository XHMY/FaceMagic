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
            Console.WriteLine("Please chose which work mode would you want." +
                "\n\"matchPerson\": It is useful to find a known person's other photos." +
                "\n\"matchFace\": It can be used in the cases like searching celebrity-looking faces." +
                "\nChoes matchPerson Mode input \"1\"    Choes matchFace Mode input \"2\"" +
                "\nIf you can't understand, please input \"1\".");
            MyValue.Mode = Console.ReadLine();
            if (MyValue.Mode=="2")
            {
                Console.WriteLine("Please input the threshold, If you can't understand, please input 0.6.");
                Console.WriteLine("(  0 < Threshold < 1  )");
                MyValue.Threshold = double.Parse(Console.ReadLine());
                MyValue.Mode = "matchFace";
            }
            else
            {
                MyValue.Mode = "matchPerson";
                MyValue.Threshold = 0.001;
            }
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();        
            sw.Start();
            string[] R_Picture = Directory.GetFiles("Error");
            foreach (string file in R_Picture)
            {
                File.Delete(file);
            }
            List <Face>  Person= new List<Face>();
            StreamReader MyKey = new StreamReader("API_Key.txt");
            MyValue.API_Key = MyKey.ReadToEnd();
            MyKey.Close();
            Console.WriteLine("\nWould you want to read file from \"JSON_Value.txt\"?" +
                "    Hit \"Enter\" to read the file." +
                "\n(If you can't understand or want to upload all file, please Hit \"Space\")");
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
                    foreach (Face T_FaceValue in MyValue.TA_FaceValue)
                    {
                        Console.WriteLine("You have upload {0} successful.", T_FaceValue.Directory_F);
                        Person.Add(T_FaceValue);
                    }
                    
                }
                
            }
            else
            {
                Person= Get_FaceBasicInformation();  
            }          
            Person = Get_Confidence(Person);
            sw.Stop();
            TimeSpan ts2 = sw.Elapsed;
            Console.WriteLine("\nIt took {0} min to search in {1} photos.", ts2.TotalMinutes,Person.Count);
            EndConsoleOutput();
        }

        static void EndConsoleOutput()
        {
            Console.WriteLine("\nFaceMagic v"+ System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString());
            Console.WriteLine("Build Date: Thu 08/17/2017");
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
            MyValue.T_FileJSON = MyValue.T_FileJSON.Substring(1,MyValue.T_FileJSON.Length-1);
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
            JArray[] j_FaceIds = new JArray[Person.Count / 1000 + 1];
            for (int i = 0; i < Person.Count / 1000 + 1; i++)
            {
                j_FaceIds[i] = new JArray();
            }            
            int t_count = 0;
            int t_Serial = 0;
            var sam_Faces = from sam_Person in Person
                           where sam_Person.Sample_F == "true"
                           select new { sam_Person.ID_F };      
            foreach (Face people in Person)
            {
                if (people.Sample_F=="false")
                {
                    t_count++;
                    j_FaceIds[t_Serial].Add(people.ID_F);
                    if (t_count==998)
                    {
                        t_Serial++;
                        t_count = 0;
                    }
                }                
            }
            MyValue.Finish = "";
            foreach (var sam_faceid in sam_Faces)
            {
                foreach (JArray JFid in j_FaceIds)
                {
                    API_FindSimilar(sam_faceid.ToString().Substring(9,sam_faceid.ToString().Length-11), JFid, MyValue.Mode);
                    while (MyValue.Finish != "OK")
                    {
                        System.Threading.Thread.Sleep(200);
                    }
                    MyValue.Finish = "";
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
                }                
            }
            var _orderedPerson = Person.OrderByDescending(p => p.Confidence_F);        
            MyValue.Count = 0;
            var SamePeople = Person.Where(p => p.Confidence_F > MyValue.Threshold);
            string[] R_Picture = Directory.GetFiles("Result");
            foreach (string file in R_Picture)
            {
                File.Delete(file);
            }
            foreach (Face sp in SamePeople)
            {
                if (sp.Sample_F == "false")
                {
                    MyValue.Count++;
                    File.Copy(Path.GetFullPath(sp.Directory_F), "Result\\"
                        + sp.Confidence_F
                        + "  " + sp.Name_F
                        + sp.Directory_F.Substring(sp.Directory_F.Length - 4), true);
                }
            }
            Console.WriteLine("\nNow, we can show you the confidence of the sample face with each face in the group.");
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
            Console.WriteLine("\nWe have found {0} pictures have the similar face with sample picture." +
                "\nYou can see it in the float \"Result\"", MyValue.Count);
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
                MyValue.T_Timeout = false;
                int time = 0;
                API_Detect(MPD);
                while (MyValue.Finish != "OK")
                {
                    time++;
                    System.Threading.Thread.Sleep(200);
                    if (time==150)
                    {
                        MyValue.T_Timeout = true;
                        break;
                    }
                }
                MyValue.Finish = "";
                if (MyValue.T_FindFace == true & MyValue.T_Timeout==false)
                {
                    int t_count = 0;
                    foreach (Face T_FaceValue in MyValue.TA_FaceValue)
                    {
                        if (t_count==0)
                        {
                            Console.WriteLine("You have upload {0} successful.", T_FaceValue.Directory_F);
                        }
                        Person.Add(T_FaceValue);
                        t_count++;
                    }
                    W_FileJSON.Append("|");
                    W_FileJSON.Append(MyValue.TB_FileJSON);
                    MyValue.Count++;
                }
                else
                {
                    if (MyValue.T_Timeout==true)
                    {
                        Console.WriteLine("Time out!!! Fail to upload {0}",MPD);
                        File.Copy(Path.GetFullPath(MPD), "Error\\"
                        + "TimeOut  "
                        + MPD.Substring(11), true);
                    }
                    else
                    {
                        Console.WriteLine("We can't find face in file {0}, please recheck this picture. ", MPD.ToString());
                    }                   
                }
            }
            MyValue.T_Sample = "true";
            foreach (string MSD in MySampleDicere)
            {
                MyValue.T_Timeout = false;
                int time = 0;
                API_Detect(MSD);
                while (MyValue.Finish != "OK")
                {
                    time++;
                    System.Threading.Thread.Sleep(200);
                    if (time == 150)
                    {
                        MyValue.T_Timeout = true;
                        break;
                    }
                }
                MyValue.Finish = "";
                if (MyValue.T_FindFace == true & MyValue.T_Timeout == false)
                {
                    int t_count = 0;
                    foreach (Face T_FaceValue in MyValue.TA_FaceValue)
                    {
                        if (t_count == 0)
                        {
                            Console.WriteLine("You have upload {0} successful.", T_FaceValue.Directory_F);
                        }
                        Person.Add(T_FaceValue);
                        t_count++;
                    }
                    W_FileJSON.Append("|");
                    W_FileJSON.Append(MyValue.TB_FileJSON);
                    MyValue.Count++;
                }
                else
                {
                    if (MyValue.T_Timeout == true)
                    {
                        Console.WriteLine("Time out!!! Fail to upload {0}", MSD);
                        File.Copy(Path.GetFullPath(MSD), "Error\\"
                        + "TimeOut  "
                        + MSD.Substring(11), true);
                    }
                    else
                    {
                        Console.WriteLine("We can't find face in file {0}, please recheck this picture. ", MSD.ToString());
                    }
                }
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
            MyValue.TB_FileJSON = new StringBuilder();
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
            //Console.WriteLine(response.ToString());
            string J_Result = await response.Content.ReadAsStringAsync();            
            if (J_Result == "[]")
            {
                MyValue.T_FindFace = false;
                File.Copy(Path.GetFullPath(T_Directory), "Error\\"
                        + T_Directory.Substring(11),true);
            }
            else
            {
                J_Result = J_Result.Substring(0, J_Result.Length - 1);
                J_Result = J_Result.Substring(1);
                J_Result = J_Result.Replace("},{", "}|{");
                string[] JA_Resule = J_Result.Split('|');
                MyValue.TA_FaceValue = new Face[JA_Resule.Length];
                int tCount = 0;
                foreach (string JR in JA_Resule)
                {
                    JObject Result_A = JObject.Parse(JR);
                    int found1 = 0;
                    int found2 = 0;
                    found1 = T_Directory.IndexOf("\\");
                    found2 = T_Directory.IndexOf(".");
                    string name_P;
                    if (JA_Resule.Length == 1)
                    {
                        name_P = "";
                    }
                    else
                    {
                        name_P = " Face-" + tCount;
                    }
                    Result_A.Add("Name", T_Directory.Substring(found1 + 1, found2 - found1 - 1)+name_P);
                    Result_A.Add("Path", T_Directory);
                    Result_A.Add("Sample", MyValue.T_Sample);
                    MyValue.TB_FileJSON.Append(Result_A.ToString());
                    if (JA_Resule.Length - tCount > 1) MyValue.TB_FileJSON.Append("|");                    
                    MyValue.T_FaceValue = new Face(Result_A["faceId"].ToString(),
                        Result_A["faceAttributes"]["gender"].ToString(),
                        Result_A["faceAttributes"]["age"].ToString(),
                        Result_A["Name"].ToString(),
                        Result_A["Path"].ToString(),
                        Result_A["Sample"].ToString());
                    MyValue.TA_FaceValue[tCount] = MyValue.T_FaceValue;
                    tCount++;
                }
                MyValue.T_FindFace = true;
                //Console.WriteLine(await response.Content.ReadAsStringAsync());//outut test
                //Delete []
                
            }            
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
        public static string Mode { get; set; }
        public static string API_Key { get; set; }
        public static string T_Sample { get; set; }
        public static string Finish { get; set; }
        public static int Count { get; set; }
        public static Face T_FaceValue { get; set; }
        public static string T_Directory { get; set; }
        public static string T_FileJSON { get; set; }
        public static bool T_FindFace { get; set; }
        public static Face[] TA_FaceValue { get; set; }
        public static StringBuilder TB_FileJSON { get; set; }
        public static double Threshold { get; set; }
        public static bool T_Timeout { get; set; }
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
