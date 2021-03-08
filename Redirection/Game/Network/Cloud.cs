using Dan200.Core.Async;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Dan200.Game.Network
{
    public static class Cloud
    {
        private static readonly String BASE_URL = "http://www.redirectiongame.com/api/";

        public static Promise<string[]> GetMOTD()
        {
            var request = new SimplePromise<string[]>();
            new Task(delegate
            {
                try
                {
                    string[] results = GET("motd_get.php");
                    if (results.Length >= 1)
                    {
                        request.Succeed(results);
                    }
                    else
                    {
                        request.Succeed(null);
                    }
                }
                catch (Exception e)
                {
                    request.Fail(e.Message);
                }
            }).Start();
            return request;
        }

        private static string[] GET(string subURL)
        {
            return MakeRequest("GET", subURL, null);
        }

        private static string[] POST(string subURL, string postData)
        {
            return MakeRequest("POST", subURL, postData);
        }

        private static string[] MakeRequest(string method, string subURL, string data)
        {
            try
            {
                string fullURL = BASE_URL + subURL;
                WebRequest request = HttpWebRequest.Create(fullURL);
                request.Method = method;
                if (data != null)
                {
                    Stream requestStream = request.GetRequestStream();
                    using (StreamWriter writer = new StreamWriter(requestStream))
                    {
                        writer.Write(data);
                    }
                }
                using (WebResponse response = request.GetResponse())
                {
                    Stream responseStream = response.GetResponseStream();
                    using (StreamReader reader = new StreamReader(responseStream, Encoding.UTF8))
                    {
                        string firstLine = reader.ReadLine();
                        if (!firstLine.Equals("Success"))
                        {
                            if (firstLine.Equals("Error"))
                            {
                                string error = reader.ReadLine();
                                throw new Exception(error);
                            }
                            else
                            {
                                throw new IOException("Server Error");
                            }
                        }

                        List<string> results = new List<string>(32);
                        while (reader.Peek() >= 0)
                        {
                            results.Add(reader.ReadLine());
                        }
                        return results.ToArray();
                    }
                }
            }
            catch (WebException)
            {
                throw new IOException("Network Error");
            }
        }
    }
}
