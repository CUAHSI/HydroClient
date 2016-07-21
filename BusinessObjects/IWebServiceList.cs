using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HISWebClient.BusinessObjects
{
    interface IWebServicesList
    {
        IEnumerable<WebServiceNode> GetWebServices();
    }
}
