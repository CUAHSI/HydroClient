using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CUAHSI.Models.Comparison
{
    /// <summary>
    /// Implements fast equality comparison for OntologyItems based on ConceptID, which is guaranteed within CUAHSIData API to be globally unique (constrained at the data tier)
    /// </summary>
    public class SimpleOntologyItemComparer : IEqualityComparer<OntologyItem>
    {
        public bool Equals(OntologyItem x, OntologyItem y)
        {
            if (Object.ReferenceEquals(x, y)) return true;

            if (Object.ReferenceEquals(x, null) ||
                Object.ReferenceEquals(y, null))
                return false;

            return x.Id == y.Id;
        }

        public int GetHashCode(OntologyItem o)
        {
            if (Object.ReferenceEquals(o, null)) return 0;

            return o.Id.GetHashCode();
        }
    }
}