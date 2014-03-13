using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Crawler
{
    public class TableHelper
    {

        private Queue<string> lastTenUrls;
        private Queue<string> lastTenErrors;
        private int crawledSize;
        private int queueSize;
        private int errorSize;

        private CloudTable urlTable;
        private CloudTable errorTable;
        private CloudTable dataTable;

        public TableHelper(CloudTableClient client)
        {
            urlTable = client.GetTableReference("urltable");
            dataTable = client.GetTableReference("datatable");
            errorTable = client.GetTableReference("errortable");

            urlTable.CreateIfNotExists();
            dataTable.CreateIfNotExists();
            errorTable.CreateIfNotExists();

            lastTenErrors = new Queue<string>();
            lastTenUrls = new Queue<string>();
            crawledSize = int.Parse(get(dataTable, "data", "count", "0"));
            queueSize = int.Parse(get(dataTable, "data", "queuesize", "0"));
            errorSize = int.Parse(get(dataTable, "data", "errorsize", "0"));
        }

        public bool isRunning(string isrunning)
        {
            try
            {
                dataTable.Execute(
                    TableOperation.InsertOrReplace(new Data("isrunning", isrunning))
                );
                return true;
            }
            catch (Exception e)
            {
                registerError(e.Message);
                return false;
            }
        }

        public void incrementQueueSize()
        {
            queueSize++;
            if (queueSize % 5 == 0)
            {
                try
                {
                    dataTable.Execute(
                        TableOperation.InsertOrReplace(new Data("queuesize", queueSize.ToString()))
                    );
                }
                catch (Exception e)
                {
                    registerError(e.Message);
                }
            }
        }

        public void storeUrl(string[] keywords, string path)
        {
            foreach(string keyword in keywords) {
                try
                {
                    urlTable.Execute(
                        TableOperation.InsertOrMerge(new Website(keyword, path))
                    );
                }
                catch (Exception e)
                {
                    registerError(e.Message);
                }
            }

            lastTenUrls.Enqueue(path);
            if (lastTenUrls.Count > 10)
            {
                lastTenUrls.Dequeue();
            }
            crawledSize++;

            if (crawledSize % 5 == 0)
            {
                try
                {
                    dataTable.Execute(
                        TableOperation.InsertOrReplace(new Data("count", crawledSize.ToString()))
                    );
                    dataTable.Execute(
                        TableOperation.InsertOrReplace(new Data("lastten", String.Join("|", lastTenUrls.ToArray())))
                    );
                }
                catch (Exception e)
                {
                    registerError(e.Message);
                }
            }
        }

        public void Clear()
        {
            errorSize = 0;
            queueSize = 0;
            crawledSize = 0;
            lastTenErrors.Clear();
            lastTenUrls.Clear();
        }

        public void registerError(string error)
        {
            errorSize++;
            lastTenErrors.Enqueue(error);
            if (lastTenErrors.Count > 10)
            {
                lastTenErrors.Dequeue();
            }

            try
            {
               errorTable.Execute(
                    TableOperation.InsertOrReplace(new Data("error" + errorSize, WebUtility.UrlEncode(error)))
               );
               dataTable.Execute(
                    TableOperation.InsertOrReplace(new Data("lasttenerror", WebUtility.UrlEncode(String.Join("|", lastTenErrors.ToArray<string>()))))
               );
               dataTable.Execute(
                  TableOperation.InsertOrReplace(new Data("errorsize", errorSize.ToString()))
               );
            }
            catch(Exception e) {}
        }

        private string get(CloudTable table, string partition, string row, string alt)
        {
            List<Data> results = table.ExecuteQuery(
                new TableQuery<Data>().Where(TableQuery.CombineFilters(
                    TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partition),
                    TableOperators.And,
                    TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, row)
                ))
            ).ToList<Data>();

            if (results.Count > 0)
            {
                return results[0].data;
            }

            return alt;
        }
    }
}
