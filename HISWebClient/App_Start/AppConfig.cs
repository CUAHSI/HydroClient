using HISWebClient.Models;
using Owin;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;

namespace HISWebClient
{
    public class AppConfig
    {
        public void Initialize(IAppBuilder app)
        {
			//Useless code???
			//var ontologyHelper = new OntologyHelper();
			//var defaultOntology = ConfigurationManager.AppSettings["DefaultOntology"];
			//var s = ontologyHelper.getOntology("defaultOntology");
        }
        
    }
}