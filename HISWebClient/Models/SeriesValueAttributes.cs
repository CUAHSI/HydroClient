using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HISWebClient.Models
{
	//This is a change to make SmartGit happy (like THAT is really possible...)
	//[Serializable]
	//public class BaseAttributes
	//{
	//	public BaseAttributes()
	//	{
	//		Selected = false;
	//	}

	//	public BaseAttributes( bool selected)
	//	{
	//		Selected = selected;
	//	}

	//	public bool Selected { get; set; }
	//}

	//[Serializable]
	//public class SourceAttributes : BaseAttributes
	//{
	//	public SourceAttributes() : base() {}

	//	public SourceAttributes( bool selected, 
	//							 int originId, 
	//							 string organization, 
	//							 string description,
	//							 string citation ) : base(selected)
	//	{
	//		OriginId = originId;
	//		Organization = organization;
	//		Description = description;
	//		Citation = citation;
	//	}

	//	public int OriginId { get; set; }

	//	public string Organization { get; set; }

	//	public string Description { get; set; }

	//	public string Citation { get; set; }
	//}


	//[Serializable]
	//public class MethodAttributes : BaseAttributes
	//{
	//	public MethodAttributes() : base() {}

	//	public MethodAttributes( bool selected,
	//							 int code,
	//							 string description, 
	//							 string link) : base(selected)
	//	{
	//		Code = code;
	//		Description = description;
	//		Link = link;
	//	}

	//	public int Code { get; set; }

	//	public string Description { get; set; }

	//	public string Link { get; set; }
	//}

	//[Serializable]
	//public class QualityControlLevelAttributes : BaseAttributes
	//{
	//	public QualityControlLevelAttributes() : base() {}

	//	public QualityControlLevelAttributes( bool selected,
	//										  string code,
	//										  string definition,
	//										  string explanation) : base(selected)
	//	{
	//		Code = code;
	//		Definition = definition;
	//		Explanation = explanation;
	//	}

	//	public string Code { get; set; }
	
	//	public string Definition { get; set; }

	//	public string Explanation { get; set; } 
	//}

	//A simple class recording the method, quality control level and source attributes of the values for a particular series id
	//[Serializable]
	//public class SeriesValueAttributes
	//{

	//	//Constructors...
	//	public SeriesValueAttributes() 
	//	{
	//		MethodCodesToAttributes = new Dictionary<int, MethodAttributes>();
	//		QualityControlLevelCodesToAttributes = new Dictionary<string, QualityControlLevelAttributes>();
	//		SourceCodesToAttributes = new Dictionary<int, SourceAttributes>();
	//	}

	//	public SeriesValueAttributes(Dictionary<int, MethodAttributes> methodCodesToAttributes = null,
	//								 Dictionary<string, QualityControlLevelAttributes> qualityControlLevelCodesToAttributes = null,
	//								 Dictionary<int, SourceAttributes> sourceCodesToAttributes = null)
	//		: this()
	//	{
	//		if (null != methodCodesToAttributes)
	//		{
	//			MethodCodesToAttributes = methodCodesToAttributes;
	//		}

	//		if (null != qualityControlLevelCodesToAttributes)
	//		{
	//			QualityControlLevelCodesToAttributes = qualityControlLevelCodesToAttributes;
	//		}

	//		if (null != sourceCodesToAttributes)
	//		{
	//			SourceCodesToAttributes = sourceCodesToAttributes;
	//		}
	//	}

	//	//Members...
	//	public Dictionary<int, MethodAttributes> MethodCodesToAttributes { get; set; }

	//	public Dictionary<string, QualityControlLevelAttributes> QualityControlLevelCodesToAttributes { get; set; }

	//	public Dictionary<int, SourceAttributes> SourceCodesToAttributes { get; set; }
	//}
}