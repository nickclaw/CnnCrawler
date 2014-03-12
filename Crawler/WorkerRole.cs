using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using System.IO;
using System.Text.RegularExpressions;

namespace Crawler
{
    public class WorkerRole : RoleEntryPoint
    {
        public TableHelper helper;

        public CloudQueue commandQueue;
        public CloudQueue urlQueue;

        public Dictionary<string, DomainValidator> domainValidators = new Dictionary<string, DomainValidator>();

        public override void Run()
        {
            Debug.WriteLine("Run()");

            bool canRun = !helper.isRunning("false");

            // This is a sample worker implementation. Replace with your logic.
            Trace.TraceInformation("Crawler entry point called", "Information");

            while (true)
            {
                Debug.WriteLine("``---------------------RUNNING------------------");
                Thread.Sleep(500);

                CloudQueueMessage command = commandQueue.GetMessage();
                if (command != null)
                {
                    commandQueue.DeleteMessage(command);
                    Debug.WriteLine("############## COMMAND: " + command.AsString + " ############");
                    if (command.AsString == "start")
                    {
                        canRun = helper.isRunning("true");
                    }
                    else if (command.AsString == "stop")
                    {
                        canRun = !helper.isRunning("false");
                    }
                }
                else if (canRun) // can run
                {
                    CloudQueueMessage url = urlQueue.GetMessage();
                    if (url != null)
                    {
                        urlQueue.DeleteMessage(url);
                        handleUrl(url.AsString);
                    }
                }
            }
        }

        public override bool OnStart()
        {
            Debug.WriteLine("OnStart()");

            // Set the maximum number of concurrent connections 
            ServicePointManager.DefaultConnectionLimit = 12;

            string connectionString = CloudConfigurationManager.GetSetting("Microsoft.ServiceBus.ConnectionString");
            CloudStorageAccount storage = CloudStorageAccount.Parse(connectionString);
            CloudQueueClient queueClient = storage.CreateCloudQueueClient();
            commandQueue = queueClient.GetQueueReference("commandqueue");
            urlQueue = queueClient.GetQueueReference("urlqueue");
            commandQueue.CreateIfNotExists();
            urlQueue.CreateIfNotExists();

            // urlQueue.Clear(); // TODO temporary

            CloudTableClient tableClient = storage.CreateCloudTableClient();
            helper = new TableHelper(tableClient);

            addUrl("www", "/index.html");

            return base.OnStart();
        }

        private void handleUrl(string url) {
            Debug.WriteLine("Handling: " + url);
            string page = requestPage(url);
            if (page == null)
            {
                // errors already been logged
            } 
            else if (url.EndsWith(".xml"))
            {
                parseSitemap(page);
            }
            else
            {
                string domain = Regex.Match(url, "http://(.*?)\\.").Groups[1].Value;
                parseWebpage(page, domain, url);
            }
        }

        private void parseSitemap(string sitemap)
        {
            // check the sitemap for <loc></loc> urls, add them
            foreach (Match url in Regex.Matches(sitemap, "<loc>http://([a-z]*?)\\.cnn\\.com(.*?)</loc>"))
            {

                Match match = Regex.Match(url.Groups[2].Value, "(\\d{4})-\\d{2}\\.xml");
                if (!match.Success || match.Groups[1].Value == "2014")
                {
                    addUrl(url.Groups[1].Value, url.Groups[2].Value);
                }
            }
        }

        private void parseWebpage(string page, string currentDomain, string currentUrl)
        {

            Match siteMatch = Regex.Match(page, "<title>(.*?)(?:[-<].*)itle>");
            string site = siteMatch.Groups[1].Value;

            helper.storeUrl(Regex.Split(Regex.Replace(site.Trim(), "[^a-zA-Z0-9\\s\\.]+", " ").ToLower(), "\\s"), currentUrl);

            Debug.WriteLine("Title: " + site);

            // now that we've parsed the page of information we want, check it for links to follow
            foreach (Match url in Regex.Matches(page, "href=\"(?:http://([a-z]*?)\\.cnn\\.com)?([^\"]*?\\.html.*?)[\"#]"))
            {
                string domain = url.Groups[1].Value;
                if (domain.Length == 0)
                {
                    domain = currentDomain;
                }
                addUrl(domain, url.Groups[2].Value);
            }
        }

        private string requestPage(string url)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Timeout = 20000;
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
                helper.registerError(e.Message);
                return null;
            }
        }

        private void addUrl(string domain, string path)
        {   
            // if the domain hasn't been visited
            // parse it's robots.txt and add sitemaps to the queue
            if (!domainValidators.ContainsKey(domain))
            {
                domainValidators.Add(domain, new DomainValidator(domain));
                foreach (string sitemap in domainValidators[domain].getSitemaps())
                {
                    urlQueue.AddMessage(new CloudQueueMessage(sitemap));
                }
            }

            // now that it definitely exists, make sure the current path is allowed
            // if it is, enqueue the url
            if (domainValidators[domain].isValid(path))
            {
                Debug.WriteLine("Added Url: " + path);
                urlQueue.AddMessage(new CloudQueueMessage("http://" + domain + ".cnn.com" + path));
                helper.incrementQueueSize();
            }
        }
    }
}
