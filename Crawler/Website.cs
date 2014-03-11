using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Crawler
{
    public class Website : TableEntity
    {
        public string url;
        public string modified;

        public Website(string title, string url, string modified)
        {
            this.PartitionKey = "website";
            this.RowKey = title.ToLower().Trim();
            this.url = WebUtility.UrlEncode(url);
            this.modified = WebUtility.UrlEncode(modified);

        }

        public string getUrl() {
            return WebUtility.UrlDecode(url);
        }

        public string getModified() {
            return WebUtility.UrlDecode(modified);
        }
    }
}
