using System;
using System.Text;
using System.Collections.Generic;
using System.Web;
using System.IO;
using System.Xml;
using System.Linq;
using System.Net;
using System.Xml.Serialization;
using ASCOM;
using ASCOM.Controls;
using ASCOM.Utilities.Video;
using ASCOM.DeviceInterface;
using ASCOM.DriverAccess;
using ASCOM.Interface;
using ASCOM.Helper;
using ComputerBeacon.Json;
using CodeScales.Http;
using CodeScales.Http.Methods;
using CodeScales.Http.Entity;
using CodeScales.Http.Entity.Mime;
using CodeScales.Http.Common;
using Occultationis;

namespace Occultationis
{
    class JANClient
    {
        static string url = "http://nova.astrometry.net";
        static string apikey = "amblhcpsjipelcuu";
        static string sesin = "";
        static string fileName = "";
        static string[] jobids = { "" };
        static string[] jobstaties = { "" };
        static string[] user_images = { "" };
        static string userid = "";
        static string delimiter = ";";
        static int waitingTime = 10000;
        static int howManyTimes = 10;
        static string outString = "./";
        static bool ascom = false;
        static double racenter;
        static double deccenter;
        static bool debug = true;

        private void setConfig()
        {

        }

        public string replaceSuffix(string filename, string suffix)
        {
            int lastPointPosition = filename.LastIndexOf(".");
            if (lastPointPosition < 0)
            {
                Console.WriteLine("The nameof image file is NOT correct: " + filename);
                // Exit
            }
            StringBuilder sb = new StringBuilder();
            sb.Append(filename.Substring(0, lastPointPosition + 1));
            sb.Append(suffix);
            return sb.ToString();
        }

        public static JsonObject getLoginJSON(string s)
        {
            JsonObject obj = new JsonObject();
            obj.Add("apikey", s);
            return obj;
        }

        public WebRequest makePost(string url, string data, string contentType)
        {
            WebRequest request = WebRequest.Create(url);
            request.Credentials = CredentialCache.DefaultCredentials;
            request.Method = "POST";
            request.ContentLength = data.Length;
            request.ContentType = "application/x-www-form-urlencoded";
            Stream dataStream = request.GetRequestStream();
            dataStream.Write(Encoding.ASCII.GetBytes(data), 0, Encoding.ASCII.GetByteCount(data));
            Console.WriteLine(data);
            dataStream.Close();

            return request;
        }

        public string loginPost()
        {
            // WebRequest request  = WebRequest.Create( url + "/api/login" );
            JsonObject jsonObject = getLoginJSON(apikey);
            String input = "&request-json=" + jsonObject.ToString();

            WebRequest request = makePost(url + "/api/login", input, "application/x-www-form-urlencoded");
            WebResponse response = request.GetResponse();
            Console.WriteLine(((HttpWebResponse)response).StatusCode);
            Console.WriteLine(((HttpWebResponse)response).StatusDescription);
            Stream dataStream = ((HttpWebResponse)response).GetResponseStream();
            StreamReader reader = new StreamReader(dataStream);
            String serverResponse = reader.ReadToEnd();
            Console.WriteLine(serverResponse);
            reader.Close();
            dataStream.Close();
            response.Close();

            return jsonGetValue(serverResponse, "session");

        }

        public string jsonGetValue(string sjson, string key)
        {
            string value;
            object init;

            JsonObject jsonObj = new JsonObject(sjson);
            jsonObj.TryGetValue(key, out init);

            return init.ToString();
        }

        public static JsonObject getUploadJSON(string session)
        {
            JsonObject obj = new JsonObject();

            obj.Add("publicity_visible", "y");
            obj.Add("allow_modifications", "d");
            obj.Add("session", session);
            obj.Add("allow_commercial_use", "d");

            return obj;
        }

        public string postFile(string session, string fileName)
        {
            int GOOD_RETURN_CODE = 200;
            string subIdLocal = "";

            FileStream imageFile = File.OpenRead(fileName);

            CodeScales.Http.HttpClient httpClient = new CodeScales.Http.HttpClient();
            HttpPost httpPost = new HttpPost(new Uri(url + "/api/upload/"));


            JsonObject jsonObject = getUploadJSON(session);
            MultipartEntity reqEntity = new MultipartEntity();

            StringBody stringBody = new StringBody(Encoding.UTF8, "request-json", jsonObject.ToString());
            reqEntity.AddBody(stringBody);
            FileInfo fileInfo = new FileInfo(fileName);
            FileBody fileBody = new FileBody("file", fileName, fileInfo);
            reqEntity.AddBody(fileBody);

            httpPost.Entity = reqEntity;
            HttpResponse response = httpClient.Execute(httpPost);

            String result = "";

            int respCode = response.ResponseCode;
            if (respCode == GOOD_RETURN_CODE)
            {
                HttpEntity entity = response.Entity;
                String content = EntityUtils.ToString(entity);
                string success = jsonGetValue(content, "status");
                subIdLocal = jsonGetValue(content, "subid");
                string hash = jsonGetValue(content, "hash");
            }

            return subIdLocal;
        }

        public string[] getJobids(String subID)
        {
            StringBuilder builder = new StringBuilder();
            String results = "";

            CodeScales.Http.HttpClient httpClient = new CodeScales.Http.HttpClient();
            HttpGet httpget = new HttpGet(new Uri(url + "/api/submissions/" + subID));
            HttpResponse response = httpClient.Execute(httpget);
            HttpEntity entity = response.Entity;
            results = EntityUtils.ToString(entity);
            if (debug)
            {
                Console.WriteLine("getJobsID gets:" + response.ResponseCode);
                Console.WriteLine("getJobsID     :" + results);
            }



            String jobs = jsonGetValue(results, "jobs");
            jobs = jobs.Replace("[", "");
            jobs = jobs.Replace("]", "");

            if (debug)
            {
                Console.WriteLine("Jobs: " + jobs);
            }

            char[] separator = { ',' };
            string[] jobidsLocal = jobs.Split(separator);

            if (debug)
            {
                if (jobidsLocal.Length >= 1)
                {
                    Console.WriteLine("Job IDs:");
                    for (int i = 0; i < jobidsLocal.Length; i++)
                    {

                        Console.WriteLine(jobidsLocal[i]);
                    }
                }
            }
            userid = jsonGetValue(results, "user");
            if (debug)
            {
                Console.WriteLine("\nUserID: " + userid);

            }

            String user_imageslocal = jsonGetValue(results, "user_images");
            user_imageslocal = user_imageslocal.Replace("[", "");
            user_imageslocal = user_imageslocal.Replace("]", "");
            if (debug)
            {
                Console.WriteLine("User_images line: " + user_imageslocal);
            }
            user_images = user_imageslocal.Split(separator);
            if (debug)
            {
                if (user_images.Length >= 1)
                {
                    Console.WriteLine("User images:");
                    for (int i = 0; i < user_images.Length; i++)
                    {

                        Console.WriteLine(user_images[i]);
                    }
                }
            }

            return jobidsLocal;
        }

        public CalibrationData getCalibration(string jobid)
        {
            StringBuilder builder = new StringBuilder();
            String result = "";

            CodeScales.Http.HttpClient httpClient = new CodeScales.Http.HttpClient();
            HttpGet httpGet = new HttpGet(new Uri(url + "/api/jobs/" + jobid + "/calibration"));
            HttpResponse response = httpClient.Execute(httpGet);
            HttpEntity entity = response.Entity;
            String content = EntityUtils.ToString(entity);
            if (debug)
            {
                Console.WriteLine("getCalibration gets: " + content);
            }
            String parity = jsonGetValue(content, "parity");
            String orientation = jsonGetValue(content, "orientation");
            String pixscale = jsonGetValue(content, "pixscale");
            String radius = jsonGetValue(content, "radius");
            String radecimal = jsonGetValue(content, "ra");
            String decdecimal = jsonGetValue(content, "dec");

            CalibrationData res = new CalibrationData();
            res.parity = Double.Parse(parity);
            res.orientation = Double.Parse(orientation);
            res.pixscale = Double.Parse(pixscale);
            res.radius = Double.Parse(radius);
            res.ra = Double.Parse(radecimal);
            res.dec = Double.Parse(decdecimal);

            return res;

        }

        public string[] getJobStaties()
        {
            String[] staties = new String[jobids.Length];
            for (int i = 0; i < jobids.Length; i++)
            {
                CodeScales.Http.HttpClient httpClient = new CodeScales.Http.HttpClient();
                HttpGet httpGet = new HttpGet(new Uri(url + "/api/jobs/" + jobids[i]));
                HttpResponse response = httpClient.Execute(httpGet);
                HttpEntity entity = response.Entity;
                String results = EntityUtils.ToString(entity);
                if (debug)
                {
                    Console.WriteLine("getJobStaties gets:" + response.ResponseCode);
                    Console.WriteLine("getJobStaties    :" + results);
                }
                String status = jsonGetValue(results, "status");
                if (debug)
                {
                    if (jobids[i] != "")
                    {
                        Console.WriteLine(jobids[i] + "    " + status);
                    }
                }
                staties[i] = status;

            }
            return staties;
        }

        public bool testResult()
        {
            bool b = false;

            for (int i = 0; i < jobids.Length; i++)
            {
                if ((jobids[i] != "") && (jobstaties[i].Equals("success")))
                {
                    b = true;
                }
            }
            return b;
        }

        public CalibrationData waitResult(string subID)
        {
            CalibrationData cal = new CalibrationData();
            for (int i = 0; i < howManyTimes; i++)
            {
                System.Threading.Thread.Sleep(waitingTime);
                jobids = getJobids(subID);
                jobstaties = getJobStaties();
                Console.Write(".");
                Boolean b = testResult();
                if (b)
                {
                    break;
                }

            }

            for (int i = 0; i < jobids.Length; i++)
            {
                cal = getCalibration(jobids[i]);
            }
            return cal;
        }

        public CalibrationData imageCalibration(string nameFile)
        {
            String sessionID = loginPost();
            String sidl = postFile(sessionID, nameFile);
            CalibrationData calibrationData = waitResult(sidl);

            return calibrationData;
        }
    }
    
}
