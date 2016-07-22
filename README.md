# HydroClient
Generally available source code for the CUAHSI HydroClient Web Application 

A Visual Studio 2013 ASP.NET MVC 'solution' for the CUAHSI HydroClient.

To run the project locally, define the following values in web.config:

Connection Strings:

	DefaultConnection 	- SQL Server connection string to HydroClient Users database
	local-DBLog 	      - SQL Server connection string to local logging database
	deploy-DBLog 	      - SQL Server connection string to logging database 

App Settings 'Key-Value' pairs:

	ServiceUrl 		          - HisCentral server v1.0 URL
	ServiceUrl1_1_Endpoint	- HisCentral server v1.1 URL

	BlobStorageViaPrimaryAccessKey 	 - Azure blob storage account name and primary key
	BlobStorageViaSecondaryAccessKey - Azure blob storage account name and secondary key

	blobContainer - Azure blob storage container name


To run securely under Microsoft Azure, define the same values in the web app instance.
