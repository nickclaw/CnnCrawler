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

namespace AccessPoint
{
    /// <summary>
    /// Summary description for Service
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
    // [System.Web.Script.Services.ScriptService]
    public class Service : System.Web.Services.WebService
    {
        private CloudTable table;
        private CloudQueue commandQueue;

        public Service()
        {
            string connectionString = CloudConfigurationManager.GetSetting("Microsoft.ServiceBus.ConnectionString");
            CloudStorageAccount storage = CloudStorageAccount.Parse(connectionString);
            CloudQueueClient queueClient = storage.CreateCloudQueueClient();
            commandQueue = queueClient.GetQueueReference("commandqueue");
            commandQueue.CreateIfNotExists();

            CloudTableClient tableClient = storage.CreateCloudTableClient();
            table = tableClient.GetTableReference("urltable");
            table.CreateIfNotExists();
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
            TableQuery<Website> query = new TableQuery<Website>().Where(combineFilter(Regex.Split(word, "\\s").ToList()));

            List<string> list = new List<string>();
            foreach(Website site in table.ExecuteQuery(query)) {
                list.Add(WebUtility.UrlDecode(site.RowKey));
            }
            return list;
        }
    }
}
