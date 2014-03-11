using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Crawler
{
    public class Data : TableEntity
    {
        public string data { get; set; }

        public Data()
        {
            this.PartitionKey = "data";
            this.RowKey = "";
            this.data = "";
        }

        public Data(string type, string data)
        {
            this.PartitionKey = "data";
            this.RowKey = type;
            this.data = WebUtility.UrlEncode(data);
        }
    }
}
