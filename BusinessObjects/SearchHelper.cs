using DotSpatial.Data;
using DotSpatial.Topology;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HISWebClient.Util;

namespace HISWebClient.DataLayer
{
	/// <summary>
	/// Helper class - converts the search results list to a GIS Feature set that can be
	/// shown in map
	/// </summary>
	public static class SearchHelper
	{
		private static void PopulateDataRow(HISWebClient.BusinessObjects.Models.SeriesDataCartModel.SeriesDataCart series, DataRow row)
		{
			row["DataSource"] = series.ServCode;
			row["SiteName"] = series.SiteName;
			row["VarName"] = series.VariableName;
			row["SiteCode"] = series.SiteCode;
			row["VarCode"] = series.VariableCode;
			row["Keyword"] = series.ConceptKeyword;
			row["ValueCount"] = series.ValueCount;
			row["StartDate"] = series.BeginDate.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
			row["EndDate"] = series.EndDate.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
			row["ServiceURL"] = series.ServURL;
			row["ServiceCode"] = series.ServCode;
			row["DataType"] = series.DataType;
			row["ValueType"] = series.ValueType;
			row["SampleMed"] = series.SampleMedium;
			row["TimeUnits"] = series.TimeUnit;
			row["TimeSupport"] = series.TimeSupport;
			row["Latitude"] = series.Latitude;
			row["Longitude"] = series.Longitude;
			row["IsRegular"] = series.IsRegular;
			row["Units"] = series.VariableUnits;
		}

		/// <summary>
		/// Adds the necessary attribute columns to the featureSet's attribute table
		/// </summary>
		private static FeatureSet CreateEmptyFeatureSet()
		{
			var fs = new FeatureSet(FeatureType.Point);

			var tab = fs.DataTable;
			tab.Columns.Add(new DataColumn("DataSource", typeof(string)));
			tab.Columns.Add(new DataColumn("SiteName", typeof(string)));
			tab.Columns.Add(new DataColumn("VarName", typeof(string)));
			tab.Columns.Add(new DataColumn("SiteCode", typeof(string)));
			tab.Columns.Add(new DataColumn("VarCode", typeof(string)));
			tab.Columns.Add(new DataColumn("Keyword", typeof(string)));
			tab.Columns.Add(new DataColumn("ValueCount", typeof(int)));
			tab.Columns.Add(new DataColumn("StartDate", typeof(string)));
			tab.Columns.Add(new DataColumn("EndDate", typeof(string)));
			tab.Columns.Add(new DataColumn("ServiceURL", typeof(string)));
			tab.Columns.Add(new DataColumn("ServiceCode", typeof(string)));
			tab.Columns.Add(new DataColumn("DataType", typeof(string)));
			tab.Columns.Add(new DataColumn("ValueType", typeof(string)));
			tab.Columns.Add(new DataColumn("SampleMed", typeof(string)));
			tab.Columns.Add(new DataColumn("TimeUnits", typeof(string)));
			tab.Columns.Add(new DataColumn("TimeSupport", typeof(double)));
			tab.Columns.Add(new DataColumn("Latitude", typeof(double)));
			tab.Columns.Add(new DataColumn("Longitude", typeof(double)));
			tab.Columns.Add(new DataColumn("IsRegular", typeof(bool)));
			tab.Columns.Add(new DataColumn("Units", typeof(string)));
		  
			return fs;
		}

		
		/// <summary>
		/// Divides the search bounding box into several 'tiles' to prevent
		/// </summary>
		/// <param name="bigBoundingBox">the original bounding box</param>
		/// <param name="tileWidth">The tile width in decimal degrees</param>
		/// <param name="tileHeight">The tile height (south-north) in decimal degrees</param>
		/// <returns></returns>
		public static List<Extent> CreateTiles(Extent bigBoundingBox, double tileWidth, double tileHeight)
		{
			var tiles = new List<Extent>();
			double fullWidth = Math.Abs(bigBoundingBox.MaxX - bigBoundingBox.MinX);
			double fullHeight = Math.Abs(bigBoundingBox.MaxY - bigBoundingBox.MinY);

			if (fullWidth < tileWidth || fullHeight < tileHeight)
			{
				tiles.Add(bigBoundingBox);
				return tiles;
			}

			double yll = bigBoundingBox.MinY; //y-coordinate of the tile's lower left corner
			var numColumns = (int)(Math.Ceiling(fullWidth / tileWidth));
			var numRows = (int)(Math.Ceiling(fullHeight / tileHeight));
			var lastTileWidth = fullWidth - ((numColumns - 1) * tileWidth);
			var lastTileHeight = fullHeight - ((numRows - 1) * tileHeight);
			int r;

			for (r = 0; r < numRows; r++)
			{
				double xll = bigBoundingBox.MinX; //x-coordinate of the tile's lower left corner

				if (r == numRows - 1)
				{
					tileHeight = lastTileHeight;
				}

				int c;
				for (c = 0; c < numColumns; c++)
				{
					var newTile = c == (numColumns - 1) ? new Extent(xll, yll, xll + lastTileWidth, yll + tileHeight) :
														  new Extent(xll, yll, xll + tileWidth, yll + tileHeight);
					tiles.Add(newTile);
					xll = xll + tileWidth;
				}
				yll = yll + tileHeight;
			}
			return tiles;
		}

		public static SearchResult ToFeatureSetsByDataSource(IEnumerable<HISWebClient.BusinessObjects.Models.SeriesDataCartModel.SeriesDataCart> seriesList)
		{
			if (seriesList == null) throw new ArgumentNullException("seriesList");

			var resultCollection = new List<SearchResultItem>();
			foreach(var dataCart in seriesList)
			{
				IFeatureSet featureSet;
				var searchItem = resultCollection.FirstOrDefault(item => item.ServiceCode == dataCart.ServCode);
				if (searchItem == null)
				{
					featureSet =  CreateEmptyFeatureSet();
					searchItem = new SearchResultItem(dataCart.ServCode, featureSet);
					resultCollection.Add(searchItem);
				}
				featureSet = searchItem.FeatureSet;
				AddToFeatureSet(dataCart, featureSet);
			}

			return new SearchResult(resultCollection);
		}

		/// <summary>
		/// Clips the list of series by the polygon
		/// </summary>
		/// <param name="fullSeriesList">List of series</param>
		/// <param name="polygon">the polygon shape</param>
		/// <returns>a new list of series metadata that is only within the polygon</returns>
		public static IEnumerable<HISWebClient.BusinessObjects.Models.SeriesDataCartModel.SeriesDataCart> ClipByPolygon(IEnumerable<HISWebClient.BusinessObjects.Models.SeriesDataCartModel.SeriesDataCart> fullSeriesList, IFeature polygon)
		{
			var newList = new List<HISWebClient.BusinessObjects.Models.SeriesDataCartModel.SeriesDataCart>();
			
			foreach (HISWebClient.BusinessObjects.Models.SeriesDataCartModel.SeriesDataCart series in fullSeriesList)
			{
				double lat = series.Latitude;
				double lon = series.Longitude;
				var coord = new Coordinate(lon, lat);
				if (polygon.Intersects(coord))
				{
					newList.Add(series);
				}
			}
			return newList;
		}

		/// <summary>
		/// Updates  BeginDate/EndDate/ValueCount in the SeriesDataCart to the user-specified range
		/// </summary>
		/// <param name="series">Series to update</param>
		/// <param name="startDate">User-specified startDate</param>
		/// <param name="endDate">User-specified endDate</param>
		public static void UpdateDataCartToDateInterval(HISWebClient.BusinessObjects.Models.SeriesDataCartModel.SeriesDataCart series, DateTime beginDate, DateTime endDate)
		{
			//Initialize/validate input parameters...
			if ((null == series) ||
				(0 >= series.ValueCount) ||				//No values in series
				(! series.IsRegular) ||					//Data is instantaneous - cannot calculate collection interval
				(null == series.BeginDate) ||
				(null == series.EndDate) ||
				(series.EndDate <= series.BeginDate) ||	//Series End date earlier than Begin date
				(null == beginDate) ||
				(null == endDate) ||
				(endDate <= beginDate) ||				//Search End date earlier than Begin date
				(series.BeginDate > endDate) ||			//Series begins after search ends
				(series.EndDate < beginDate))			//Series ends before search begins
            {
				//Input parameter(s) invalid - return early...
				series.ValueCount = 0;	//No value count estimate made
				return;
			}

			//Determine the search begin and end dates...
			var searchBeginDate = (series.BeginDate < beginDate) ? beginDate : series.BeginDate;
			var searchEndDate = (series.EndDate > endDate) ? endDate : series.EndDate;

			//Calculate total and search time spans
			TimeSpan tsTotal = series.EndDate - series.BeginDate;
			TimeSpan tsSearch = searchEndDate - searchBeginDate;

			//Calculate estimated value count:  Total value count * (search time span / total time span)
			//NOTE: The use of MidpointRouding.AwayFromZero ensures values like 4.5 round to 5 (not 4)
			//		See MicroSoft documentation on Math.Round(Double) and Math.Round(Double, MidpointRounding) for more information
			int vcTotal = series.ValueCount;
			series.ValueCount = Convert.ToInt32( Math.Round(vcTotal * (tsSearch.TotalMilliseconds / tsTotal.TotalMilliseconds), MidpointRounding.AwayFromZero));

			//Set series begin and and dates for search...
			series.BeginDate = searchBeginDate;
			series.EndDate = searchEndDate;
		}

		/// <summary>
		/// Check input series Begin and End dates against input Start and End dates.  
		///   Series dates within input Start and End Dates - return true, return false otherwise
		/// </summary>
		public static bool IsSeriesInDateRange(HISWebClient.BusinessObjects.Models.SeriesDataCartModel.SeriesDataCart series, DateTime startDate, DateTime endDate)
		{
			if ((startDate <= series.BeginDate) && (endDate >= series.EndDate))
			{
				//Series dates within input date range - return true...
				return true;
			}

			return false;
		}

		/// <summary>
		/// Adds series to an existing feature set
		/// </summary>
		/// <param name="series">Series</param>
		/// <param name="fs">Feature set</param>
		private static void AddToFeatureSet(HISWebClient.BusinessObjects.Models.SeriesDataCartModel.SeriesDataCart series, IFeatureSet fs)
		{
			double lat = series.Latitude;
			double lon = series.Longitude;
			var coord = new Coordinate(lon, lat);

			var f = new Feature(FeatureType.Point, new[] {coord});
			fs.Features.Add(f);

			var row = f.DataRow;
			PopulateDataRow(series, row);
		}

//		//BC - 17-Oct-2016 - Retain for possible future use...
//		/// <summary>
//		/// Updates  BeginDate/EndDate/ValueCount in the SeriesDataCart to the user-specified range
//		/// </summary>
//		/// <param name="series">Series to update</param>
//		/// <param name="startDate">User-specified startDate</param>
//		/// <param name="endDate">User-specified endDate</param>
//		public static void UpdateDataCartToDateInterval(HISWebClient.BusinessObjects.Models.SeriesDataCartModel.SeriesDataCart series, DateTime startDate, DateTime endDate)
//		{
//			//Initialize/validate input parameters...
//			if ((null == series) ||
//				 (null == startDate) ||
//				 (null == endDate) ||
//				 (endDate < startDate) ||						//End date earlier than start date
//				 (0 >= series.ValueCount) ||					//No values in series
//				 (0.0 >= series.TimeSupport) ||					//Data is instantaneous - cannot calculate collection interval
//				 (String.IsNullOrWhiteSpace(series.TimeUnit)))	//Time unit empty - cannot calculate collection interval
//			{
//				//Input parameter(s) invalid - return early...
//				series.ValueCount = 0;	//No value count estimate made
//				return;
//			}

//			// Update BeginDate/EndDate/ValueCount to the user-specified range
//			var seriesStartDate = (null == series.BeginDate) ? startDate : (series.BeginDate < startDate) ? startDate : series.BeginDate;
//			var seriesEndDate = (null == series.EndDate) ? endDate : (series.EndDate > endDate) ? endDate : series.EndDate;

//			//Check series data type...
//			//Controlled vocabulary data type values
//			//Source: http://his.cuahsi.org/mastercvreg/edit_cv11.aspx?tbl=DataTypeCV&id=789577851
//			string seriesDataType = (String.IsNullOrWhiteSpace(series.DataType)) ? "unknown" : series.DataType.ToLower();

//			switch (seriesDataType)
//			{
//				case "constant over interval":
//				case "continuous":
//				case "regular sampling":
//					//Data type supports value count estimation - take no action
//					break;
//				case "average":
//				case "best easy systematic estimator":
//				case "categorical":
//				case "cumulative":
//				case "incremental":
//				case "maximum":
//				case "median":
//				case "minimum":
//				case "mode":
//				case "sporadic":
//				case "standarddeviation":
//				case "unknown":
//				case "variance":
//					//Data type does NOT support value count estimation - reset value count
//					series.ValueCount = 0;
//					break;
//				default:
//					//Unknown data type - reset value count
//					series.ValueCount = 0;
//#if (DEBUG)
//					if (EnvironmentContext.LocalEnvironment())
//					{
//						//write to file...
//						using (System.IO.FileStream fs = new System.IO.FileStream(@"C:\CUAHSI\UnknownDataTypes.txt",
//																				   System.IO.FileMode.OpenOrCreate,
//																				   System.IO.FileAccess.ReadWrite,
//																				   System.IO.FileShare.ReadWrite))
//						{
//							string toWrite = String.Format("Service Code: {0} - Data Type: {1}", series.ServCode, seriesDataType);

//							var sr = new System.IO.StreamReader(fs);
//							bool bFound = false;

//							while (-1 != sr.Peek())
//							{
//								string read = sr.ReadLine();
//								if (toWrite == read)
//								{
//									bFound = true;
//									break;
//								}
//							}

//							if (!bFound)
//							{
//								var sw = new System.IO.StreamWriter(fs);
//								sw.WriteLine(toWrite);
//								sw.Flush();
//							}
//						}
//					}
//#endif
//					break;
//			}

//			if (0 < series.ValueCount)
//			{
//				//Can estimate value count (data is not instantaneous) - determine data collection interval
//				string seriesTimeUnit = series.TimeUnit.ToLower();
//				int interval = Convert.ToInt32(series.TimeSupport);
//				TimeSpan tsInterval = new TimeSpan();

//				switch (seriesTimeUnit)
//				{
//					case "common year":
//						tsInterval = new TimeSpan((interval * 365), 0, 0, 0, 0);
//						break;
//					case "month":
//						tsInterval = new TimeSpan((interval * 31), 0, 0, 0, 0);
//						break;
//					case "week":
//						tsInterval = new TimeSpan((interval * 7), 0, 0, 0, 0);
//						break;
//					case "day":
//						tsInterval = new TimeSpan(interval, 0, 0, 0, 0);
//						break;
//					case "hour":
//						tsInterval = new TimeSpan(0, interval, 0, 0, 0);
//						break;
//					case "minute":
//						tsInterval = new TimeSpan(0, 0, interval, 0, 0);
//						break;
//					case "second":
//						tsInterval = new TimeSpan(0, 0, 0, interval, 0);
//						break;
//					case "millisecond":
//						tsInterval = new TimeSpan(0, 0, 0, 0, interval);
//						break;
//					default:
//						//Unknown time unit - reset value count
//						series.ValueCount = 0;
//						break;
//				}

//				if (0 < series.ValueCount)
//				{
//					//Data collection interval established  - value count estimate = total time / interval time
//					TimeSpan tsTotal = seriesEndDate - seriesStartDate;

//					series.ValueCount = Convert.ToInt32(tsTotal.TotalMilliseconds / tsInterval.TotalMilliseconds);
//				}
//			}

//			series.BeginDate = seriesStartDate;
//			series.EndDate = seriesEndDate;
//		}
	}
}

