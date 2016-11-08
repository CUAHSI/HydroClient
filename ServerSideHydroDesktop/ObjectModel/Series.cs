using System;
using System.Collections.Generic;

using System.Linq;

namespace ServerSideHydroDesktop.ObjectModel
{
    /// <summary>
    /// Represents a time series. The time series is a combination of a specific site, variable,
    /// method, source and quality control level.
    /// </summary>
    public class Series : BaseEntity
    {
        #region Constructors

        /// <summary>
        /// Creates a new series with properties set to default.
        /// </summary>
        public Series()
        {
            DataValueList = new List<DataValue>();
            ThemeList = new List<Theme>();
            Method = Method.Unknown;
            Source = Source.Unknown;
            QualityControlLevel = QualityControlLevel.Unknown;
        }

        /// <summary>
        /// Creates a new data series associated with the specific site, variable,
        /// method, quality control level and source. This series will contain zero
        /// data values after creation.
        /// </summary>
        /// <param name="site">the observation site (location of measurement)</param>
        /// <param name="variable">the observed variable</param>
        /// <param name="method">the observation method</param>
        /// <param name="qualControl">the quality control level of observed values</param>
        /// <param name="source">the source of the data values for this series</param>
        public Series(Site site, Variable variable, Method method, QualityControlLevel qualControl, Source source) : this()
        {
            Site = site;
            Variable = variable;
            Method = method;
            QualityControlLevel = qualControl;
            Source = source;
        }

        /// <summary>
        /// Creates a copy of the original series. If copyDataValues is set to true,
        /// then the data values are also copied. 
        /// The new series shares the same site, variable, source, method and quality
        /// control level. The new series does not belong to any data theme.
        /// </summary>
        /// <param name="original">The original series</param>
        /// <param name="copyDataValues">if set to true, then all data values are copied</param>
        public Series(Series original, bool copyDataValues = true) : this()
        {
            //TODO: need to include series provenance information
            
            CreationDateTime = DateTime.Now;
            IsCategorical = original.IsCategorical;
            Method = original.Method;
            QualityControlLevel = original.QualityControlLevel;
            Source = original.Source;
            UpdateDateTime = DateTime.Now;
            Variable = original.Variable;

            //to copy the data values
            if (copyDataValues)
            {
                foreach (DataValue originalDataValue in original.DataValueList)
                {
                    AddDataValue(originalDataValue.Copy());
                }
            }
        }

        #endregion

        #region Properties

        /// <summary>
        /// True if the series represents categorical data
        /// </summary>
        public virtual bool IsCategorical { get; set; }

        /// <summary>
        /// The local time when the first value of the series was measured
        /// </summary>
        public virtual DateTime BeginDateTime 
		{
			//Read only...
			get
			{
				return (DataValueList == null) ? DateTime.MinValue : DataValueList.Min(dv => dv.LocalDateTime);
			}
		}

        /// <summary>
        /// The local time when the last value of the series was measured
        /// </summary>
        public virtual DateTime EndDateTime 
		{
			//Read only...
			get
			{
				return (DataValueList == null) ? DateTime.MaxValue : DataValueList.Max(dv => dv.LocalDateTime);
			}
		}

        /// <summary>
        /// Gets the begin date time of series in UTC
        /// </summary>
        public virtual DateTime BeginDateTimeUTC
		{
			//Read only...
			get
			{
				return (DataValueList == null) ? DateTime.MinValue : DataValueList.Min(dv => dv.DateTimeUTC);
			}
			
		}

        /// <summary>
        /// Gets the end date time of the series in UTC
        /// </summary>
        public virtual DateTime EndDateTimeUTC
		{ 
			//Read only...
			get
			{
				return (DataValueList == null) ? DateTime.MaxValue : DataValueList.Max(dv => dv.DateTimeUTC);
			}
		}

        /// <summary>
        /// The number of data values in this series
        /// </summary>
        public virtual int ValueCount
		{ 
			//Read only - list count is the value count...
			get
			{
				return DataValueList == null ? 0 : DataValueList.Count;
			}
		}

        /// <summary>
        /// The time when the series has been saved to the HydroDesktop 
        /// repository
        /// </summary>
        public virtual DateTime CreationDateTime { get; set; }

        /// <summary>
        /// A 'Subscribed' Data series may be regularly updated by appending data
        /// </summary>
        public virtual bool Subscribed { get; set; }

        /// <summary>
        /// The time when this data series was last updated (its data values were changed)
        /// </summary>
        public virtual DateTime UpdateDateTime { get; set; }
        
        /// <summary>
        /// Time when this series was last checked
        /// </summary>
        public virtual DateTime LastCheckedDateTime { get; set; }

        /// <summary>
        /// The site where the data is measured
        /// </summary>
        public virtual Site Site { get; set; }

        /// <summary>
        /// The measured variable
        /// </summary>
        public virtual Variable Variable { get; set; }

        /// <summary>
        /// The method of measurement
        /// </summary>
        public virtual Method Method { get; set; }      

        /// <summary>
        /// The primary source of the data
        /// </summary>
        public virtual Source Source { get; set; }      

        /// <summary>
        /// The primary quality control level of the data
        /// </summary>
        public virtual QualityControlLevel QualityControlLevel { get; set; }

        /// <summary>
        /// The list of all values belonging to this data series
        /// </summary>
        public virtual IList<DataValue> DataValueList { get; protected set; }

        /// <summary>
        /// The list of all themes containing this series
        /// </summary>
        public virtual IList<Theme> ThemeList { get; protected set; }
     
        #endregion

        #region Public methods

        /// <summary>
        /// String representation of the series
        /// <returns>SiteName | VariableName | DataType</returns>
        /// </summary>
        public override string ToString()
        {
            return Site.Name + "|" + Variable.Name + "|" + Variable.DataType;
        }

        /// <summary>
        /// Associates an existing data value with this data series
        /// </summary>
        /// <param name="val"></param>
        public virtual void AddDataValue(DataValue val)
        {
			if (null != val)
			{
            DataValueList.Add(val);
            val.Series = this;
			}
        }

        /// <summary>
        /// Adds a data value to the end of this series
        /// </summary>
        /// <param name="time">the local observation time of the data value</param>
        /// <param name="value">the observation value</param>
        /// <returns>the DataValue object</returns>
        public virtual void AddDataValue(DateTime time, double value)
        {
            var val = new DataValue(value, time, 0.0);
            AddDataValue(val);
        }

        /// <summary>
        /// Adds a data value to the end of this series
        /// </summary>
        /// <param name="time">the local observation time of the data value</param>
        /// <param name="value">the observed value</param>
        /// <param name="utcOffset">the difference between UTC and local time</param>
        /// <param name="qualifier">the qualifier (contains information about specific
        ///   observation conditions</param>
        /// <returns>the DataValue object</returns>
        public virtual void AddDataValue(DateTime time, double value, double utcOffset, Qualifier qualifier)
        {
            var val = new DataValue(value, time, utcOffset) {Qualifier = qualifier};
            AddDataValue(val);
        }

        #endregion
    }
}
