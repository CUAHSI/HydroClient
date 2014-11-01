using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace HISWebClient.BusinessObjects
{
    public abstract class ObservableObject<T> : INotifyPropertyChanged
    {
        #region Implementation of INotifyPropertyChanged

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Called when [property changed].
        /// </summary>
        /// <param name="property">The property.</param>
        protected virtual void NotifyPropertyChanged(Expression<Func<object>> property)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(NameHelper.Name(property)));
        }

        #endregion
    }
}
