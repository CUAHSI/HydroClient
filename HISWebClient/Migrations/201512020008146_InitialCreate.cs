namespace HISWebClient.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class InitialCreate : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.DM_TimeSeries",
                c => new
                    {
                        TimeSeriesId = c.Int(nullable: false, identity: true),
                        UserEmail = c.String(maxLength: 128),
                        Status = c.Int(nullable: false),
                        Organization = c.String(),
                        ServiceTitle = c.String(),
                        Keyword = c.String(),
                        DataType = c.String(),
                        ValueType = c.String(),
                        SampleMedium = c.String(),
                        StartDate = c.DateTime(nullable: false),
                        EndDate = c.DateTime(nullable: false),
                        ValueCount = c.Int(nullable: false),
                        SiteName = c.String(),
                        VariableName = c.String(),
                        TimeSupport = c.Double(nullable: false),
                        TimeUnit = c.String(),
                        SeriesId = c.Int(nullable: false),
                        WaterOneFlowURI = c.String(),
                        TimeSeriesRequestId = c.String(),
                    })
                .PrimaryKey(t => t.TimeSeriesId)
                .ForeignKey("dbo.DM_UserTimeSeries", t => t.UserEmail)
                .Index(t => t.UserEmail);
            
            CreateTable(
                "dbo.DM_UserTimeSeries",
                c => new
                    {
                        UserEmail = c.String(nullable: false, maxLength: 128),
                    })
                .PrimaryKey(t => t.UserEmail);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.DM_TimeSeries", "UserEmail", "dbo.DM_UserTimeSeries");
            DropIndex("dbo.DM_TimeSeries", new[] { "UserEmail" });
            DropTable("dbo.DM_UserTimeSeries");
            DropTable("dbo.DM_TimeSeries");
        }
    }
}
