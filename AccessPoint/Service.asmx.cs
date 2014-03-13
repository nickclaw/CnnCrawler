using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Services;
using Crawler;
using Microsoft.WindowsAzure.Storage.Table.DataServices;
using System.Diagnostics;
using System.Net;
using System.Text.RegularExpressions;
using System.Configuration;
using System.IO;
using System.Web.Hosting;

namespace AccessPoint
{
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
    // [System.Web.Script.Services.ScriptService]
    public class Service : System.Web.Services.WebService
    {
        private static CloudTable table;
        private static CloudTable dataTable;
        private static CloudQueue commandQueue;
        private static CloudQueue urlQueue;

        private static TrieNode root;

        private static Dictionary<string, List<string>> autoCache;        
        private static Dictionary<string, List<string>> searchCache;
        private static bool created = false;

        

        public Service()
        {
            if (created == false)
            {
                string connectionString = connectionString = ConfigurationManager.ConnectionStrings["StorageConnectionString"].ConnectionString;
                CloudStorageAccount storage = CloudStorageAccount.Parse(connectionString);
                CloudQueueClient queueClient = storage.CreateCloudQueueClient();
                commandQueue = queueClient.GetQueueReference("commandqueue");
                commandQueue.CreateIfNotExists();
                urlQueue = queueClient.GetQueueReference("urlqueue");
                urlQueue.CreateIfNotExists();

                CloudTableClient tableClient = storage.CreateCloudTableClient();
                table = tableClient.GetTableReference("urltable");
                table.CreateIfNotExists();
                dataTable = tableClient.GetTableReference("datatable");
                dataTable.CreateIfNotExists();

                searchCache = new Dictionary<string, List<string>>();
                autoCache = new Dictionary<string, List<string>>();

                root = new TrieNode(null);

                StreamReader reader = new StreamReader(HostingEnvironment.ApplicationPhysicalPath + "million.txt");
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    root.build(line.ToLower());
                }

                created = true;
            }
        }

        [WebMethod]
        public List<string> AutoComplete(string word)
        {
            if (autoCache.ContainsKey(word))
            {
                return autoCache[word];
            }
            else
            {
                List<string> list = root.search(word.ToLower().Replace(' ', '_'), 10);
                autoCache.Add(word, list);
                if (autoCache.Count > 100)
                {
                    autoCache.Clear();
                }
                return list;
            }
        }

        private string combineFilter(List<string> words) {
            string word = words[0];
            words.RemoveAt(0);

            string next;
            if (words.Count == 0)
            {
                next = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, word);
            }
            else
            {
                next = combineFilter(words);
            }

            return TableQuery.CombineFilters(
                TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, word),
                TableOperators.Or,
                next
            );
        }

        [WebMethod]
        public List<string> SearchUrl(string word)
        {
            List<string> list;
            if (searchCache.ContainsKey(word))
            {
                return searchCache[word];
            }
            else
            {
                TableQuery<Website> query = new TableQuery<Website>().Where(combineFilter(Regex.Split(word, "\\s").ToList()));
                
                // get and sort
                List<Website> webList = table.ExecuteQuery(query).ToList<Website>();
                webList.Sort(delegate(Website a, Website b) {
                    return b.count - a.count;
                });

                list = new List<string>();
                foreach (Website site in webList)
                {
                    list.Add(WebUtility.UrlDecode(site.RowKey));
                }

                searchCache.Add(word, list);
                if (searchCache.Count > 100)
                {
                    searchCache.Clear();
                }
            }

            return list;
        }

        [WebMethod]
        public bool Command(string command)
        {
            if (command == "clear")
            {
                autoCache.Clear();
                searchCache.Clear();
            }
            commandQueue.AddMessage(new CloudQueueMessage(command));
            return true;
        }

        [WebMethod]
        public int SearchCacheSize()
        {
            return searchCache.Count;
        }

        [WebMethod]
        public int AutoCacheSize()
        {
            return autoCache.Count;
        }

        [WebMethod]
        public int QueueSize()
        {
            string result = getData("queuesize");
            if (result != null)
            {
                return int.Parse(result);
            }
            else
            {
                return 0;
            }
        }
        
        [WebMethod]
        public int CrawledSize()
        {
            string result = getData("count");
            if (result != null)
            {
                return int.Parse(result);
            }
            else
            {
                return 0;
            }
        }

        [WebMethod]
        public int ErrorSize()
        {
            string result = getData("errorsize");
            if (result != null)
            {
                return int.Parse(result);
            }
            else
            {
                return 0;
            }
        }

        [WebMethod]
        public List<string> LastTen()
        {
            string result = getData("lastten");

            if (result != null)
            {
                return result.Split('|').ToList<string>();
            }
            else
            {
                return new List<string>();
            }
        }

        [WebMethod]
        public List<string> LastTenErrors()
        {
            string result = getData("lasttenerror");

            if (result != null)
            {
                return WebUtility.UrlDecode(result).Split('|').ToList<string>();
            }
            else
            {
                return new List<string>();
            }
        }


        [WebMethod]
        public long GetRam()
        {
            return GC.GetTotalMemory(true) / 1048576;
        }

        [WebMethod]
        public string IsRunning()
        {
            string result = getData("isrunning");
            if (result != null)
            {
                return result;
            }
            else
            {
                return "false";
            }
        }

        [WebMethod]
        public bool RegisterClick(string url)
        {
            TableQuery<Website> query = new TableQuery<Website>().Where(
                TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, WebUtility.UrlEncode(url))
            );

            Debug.WriteLine("############### REGISTER ##################");
            foreach(Website site in table.ExecuteQuery(query)) {
                Debug.WriteLine("TESTING");
                Debug.WriteLine(site.count);
                if (site.count == null) {
                    site.count = 1;
                } else {
                    site.count = site.count + 1;
                }
                try {
                    table.Execute(
                        TableOperation.InsertOrMerge(site) 
                    );
                } catch (Exception e){}
            }

            return true;
        }

        private string getData(string type) {
            TableQuery<Data> query = new TableQuery<Data>().Where(
                TableQuery.CombineFilters(
                    TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "data"),
                    TableOperators.And,
                    TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, type)
                )
            );

            List<Data> results = dataTable.ExecuteQuery(query).ToList<Data>();
            if (results.Count > 0)
            {
                return WebUtility.UrlDecode(results[0].data);
            }
            else
            {
                return null;
            }
        }
    }
}
