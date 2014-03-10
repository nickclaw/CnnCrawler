using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Crawler
{
    public class DomainValidator
    {   
        private string domain;
        private List<string> forbidden;
        private List<string> sitemaps;

        public DomainValidator(string dmn) {
            domain = dmn;
            forbidden = new List<string>();
            sitemaps = new List<string>();

            string robots = requestPage("http://" + domain + ".cnn.com/robots.txt");
            foreach (Match url in Regex.Matches(robots, "Sitemap: (.*)"))
            {
                sitemaps.Add(url.Groups[1].Value);
            }

            foreach (Match url in Regex.Matches(robots, "Disallow: (.*)"))
            {
                forbidden.Add(url.Groups[1].Value);
            }
        }

        public List<string> getSitemaps()
        {
            return sitemaps;
        }

        public List<string> getForbidden()
        {
            return forbidden;
        }

        public bool isValid(string path)
        {
            foreach (string forbiddenPath in forbidden)
            {
                if (forbiddenPath.StartsWith(path))
                {
                    return false;
                }
            }

            return true;
        }

        private string requestPage(string url)
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
    }
}
