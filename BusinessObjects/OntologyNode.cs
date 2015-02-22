using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HISWebClient.BusinessObjects
{
    public class OntologyNode
    {
        private readonly ObservableCollection<OntologyNode> _childs = new ObservableCollection<OntologyNode>();

        public OntologyNode()
            : this(null)
        {

        }

        public OntologyNode(string text)
        {
            
            title = text;           
            _childs.CollectionChanged += _childs_CollectionChanged;
        }

        public OntologyNode(string id, string text, bool isFolder)
        {
            key = id; 
            title = text;
            folder = isFolder;
            _childs.CollectionChanged += _childs_CollectionChanged;
        }

        void _childs_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
            {
                //foreach (OntologyNode item in e.NewItems)
                    //item.Parent = this;
            }
        }

        //public OntologyNode Parent { get; set; }
        public string key { get; set; }
        public string title { get; set; }
        public bool folder { get; set; }

        public ObservableCollection<OntologyNode> children
        {
            get { return _childs; }
        }

        public bool HasChild(string name)
        {
            return OntologyTree.FindNode(name, children) != null;
        }

        public override string ToString()
        {
            return title;
        }
    }
}
