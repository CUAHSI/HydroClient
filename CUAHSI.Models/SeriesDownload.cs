using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using System.Text;


namespace CUAHSI.Models
{
    [DataContract]
    public class SeriesDownload
    {
        /// <summary>
        /// SeriesID used for client-side indexing
        /// </summary>
        [DataMember]
        public int SeriesID { get; set; }

        /// <summary>
        /// Publicly-navigable link to the Series you are downloading.
        /// </summary>
        [DataMember]
        public string Uri { get; set; }

        public SeriesDownload()
        { }
    }
}
