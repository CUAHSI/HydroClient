using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data;
using System.Runtime.Serialization;

namespace CUAHSI.Models
{
    [DataContract]
    public class HydroTag
    {
        [DataMember]
        public int TagID { get; set; }

        [DataMember]
        public string TagName { get; set; }

        [DataMember]
        public int TagCollection { get; set; }

        [DataMember]
        public string TagCollectionName { get; set; }

        [DataMember]
        /// <summary>
        /// User ID of user that created this tag.
        /// </summary>
        public int TagCreator { get; set; }

        [DataMember]
        public string TagCreatorName { get; set; }

        [DataMember]
        public string TagCreationDate { get; set; }

        [DataMember]
        public bool isOwner { get; set; }

        public HydroTag(DataRow r)
        {
            TagID = Convert.ToInt32(r.ItemArray[0]);
            TagName = r.ItemArray[1].ToString();
            TagCreator = Convert.ToInt32(r.ItemArray[2]);
            TagCreatorName = r.ItemArray[3].ToString();
            TagCreationDate = r.ItemArray[4].ToString();
            isOwner = Convert.ToBoolean(r.ItemArray[5]);
        }

        public override string ToString()
        {
            return TagName;
        }
    }
}