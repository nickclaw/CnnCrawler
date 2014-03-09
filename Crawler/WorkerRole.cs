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
using System.Diagnostics;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using System.IO;
using System.Text.RegularExpressions;

namespace Crawler
{
    public class WorkerRole : RoleEntryPoint
    {
        public CloudQueue commandQueue;
        public CloudQueue urlQueue;
        public CloudTable table;

        public Dictionary<string, DomainValidator> domainValidators = new Dictionary<string, DomainValidator>();

        public override void Run()
        {
            // This is a sample worker implementation. Replace with your logic.
            Trace.TraceInformation("Crawler entry point called", "Information");

            while (true)
            {
                Thread.Sleep(10000);

                CloudQueueMessage command = commandQueue.GetMessage();
                if (command != null)
                {
                    commandQueue.DeleteMessage(command);
                    // handle command
                }
                else if (true) // can run
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
            // Set the maximum number of concurrent connections 
            ServicePointManager.DefaultConnectionLimit = 12;

            string connectionString = CloudConfigurationManager.GetSetting("Microsoft.ServiceBus.ConnectionString");
            CloudStorageAccount storage = CloudStorageAccount.Parse(connectionString);
            CloudQueueClient queueClient = storage.CreateCloudQueueClient();
            commandQueue = queueClient.GetQueueReference("commandqueue");
            urlQueue = queueClient.GetQueueReference("urlqueue");
            commandQueue.CreateIfNotExists();
            urlQueue.CreateIfNotExists();

            CloudTableClient tableClient = storage.CreateCloudTableClient();
            table = tableClient.GetTableReference("urltable");
            table.CreateIfNotExists();

            urlQueue.AddMessage(new CloudQueueMessage("http://www.cnn.com/index.html"));

            DomainValidator wwwValidator = new DomainValidator("www");
            foreach (string url in wwwValidator.getSitemaps())
            {

            }

            return base.OnStart();
        }

        private void handleUrl(string url) {
            string page = requestPage(url);
            if (url.EndsWith(".xml")) {
                parseSitemap(page);
            }
            else if (url.EndsWith(".html"))
            {
                parseWebpage(page);
            }
        }

        private void parseSitemap(string sitemap)
        {
            foreach (Match url in Regex.Matches(sitemap, "<loc>http://([a-z]*?)\\.cnn\\.com(.*?)</loc>"))
            {
                addUrl(url.Groups[1].Value, url.Groups[2].Value);
            }
        }

        private void parseWebpage(string page)
        {
            foreach (Match url in Regex.Matches(page, ""))
            {

            }
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

        private void addUrl(string domain, string path)
        {
            if (!domainValidators.ContainsKey(domain))
            {
                domainValidators.Add(domain, new DomainValidator(domain));
                // add sitemaps
            }

            if (domainValidators[domain].isValid(path))
            {
                // enqueue url
            }
        }
    }
}
