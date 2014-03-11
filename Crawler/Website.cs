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
    public class Website : TableEntity
    {
        public string url;
        public string title;

        public Website()
        {

        }

        public Website(string keyword, string url)
        {
            this.RowKey = WebUtility.UrlEncode(url);
            this.PartitionKey = keyword;
        }

        public string getTitle()
        {
            return WebUtility.UrlDecode(title);
        }

        public string getUrl() {
            return WebUtility.UrlDecode(url);
        }
    }
}
