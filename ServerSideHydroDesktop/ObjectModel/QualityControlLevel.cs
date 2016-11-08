
namespace ServerSideHydroDesktop.ObjectModel
{
    /// <summary>
    /// Specifies the quality control level (raw data, approved data)
    /// </summary>
    public class QualityControlLevel : BaseEntity
    {
		public QualityControlLevel()
		{
			OriginId = 0;
			Code = Constants.Unknown;
			Definition = Constants.Unknown;
			Explanation = Constants.Unknown;

			neverSet = true;
		}

        /// <summary>
        /// The original identifier of the quality control level specified by a
        /// web service. This is an optional property. Set this property to 0 if not
        /// used.
        /// </summary>
		private int _originId;
		public virtual int OriginId
		{ 
			get
			{
				return _originId;
			}
			
			set
			{
				_originId = value;
				neverSet = false;
			}
		}    
        
        /// <summary>
        /// Quality control level code specified by the web service
        /// </summary>
        public virtual string Code { get; set; }
        /// <summary>
        /// Quality control level definition specified
        /// </summary>
        public virtual string Definition { get; set; }

        /// <summary>
        /// Quality control level explanation
        /// </summary>
        public virtual string Explanation { get; set; }

        /// <summary>
        /// Returns a string representation of the quality control level
        /// </summary>
        /// <returns>quality control level definition string</returns>
        public override string ToString()
        {
            return Definition;
        }

        /// <summary>
        /// When the quality control level is unknown or unspecified
        /// </summary>
        public static QualityControlLevel Unknown
        {
            get
            {
                return new QualityControlLevel
                {
                    Code = Constants.Unknown,
                    Definition = Constants.Unknown,
                    Explanation = Constants.Unknown,
                    OriginId = 0,
					neverSet = true
                };
            }
        }
    }   
}
