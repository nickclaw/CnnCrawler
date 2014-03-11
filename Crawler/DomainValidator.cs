using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Crawler
{
    public class DomainValidator
    {   
        private string domain;
        private List<string> forbidden;
        private List<string> sitemaps;
        private HashSet<string> visited;

        public DomainValidator(string dmn) {
            domain = dmn;
            forbidden = new List<string>();
            sitemaps = new List<string>();
            visited = new HashSet<string>();

            string robots = requestPage("http://" + domain + ".cnn.com/robots.txt");
            if (robots != null)
            {
                foreach (Match url in Regex.Matches(robots, "Sitemap: (.*)"))
                {
                    Debug.WriteLine("Domain - " + domain + ": " + url.Groups[1].Value);
                    sitemaps.Add(url.Groups[1].Value);
                }

                foreach (Match url in Regex.Matches(robots, "Disallow: (.*)"))
                {
                    forbidden.Add(url.Groups[1].Value);
                }
            }
        }

        public List<string> getSitemaps()
        {
            return sitemaps;
        }

        public bool isValid(string path)
        {
            if ((!path.Contains(".html") && !path.Contains(".xml")) || visited.Contains(path))
            {
                return false;
            }

            foreach (string forbiddenPath in forbidden)
            {
                if (forbiddenPath.StartsWith(path))
                {
                    return false;
                }
            }

            visited.Add(path);

            return true;
        }

        private string requestPage(string url)
        {
            Debug.WriteLine("Requesting page: " + url);
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "GET";
                WebResponse response = request.GetResponse();
                StreamReader sr = new StreamReader(response.GetResponseStream(), System.Text.Encoding.UTF8);
                string result = sr.ReadToEnd();
                sr.Close();
                response.Close();
                return result;
            }
            catch (Exception e)
            {
                return null;
            }
        }
    }
}
