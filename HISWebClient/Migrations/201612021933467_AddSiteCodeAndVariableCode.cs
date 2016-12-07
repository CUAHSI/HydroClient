namespace HISWebClient.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddSiteCodeAndVariableCode : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.DM_TimeSeries", "SiteCode", c => c.String());
            AddColumn("dbo.DM_TimeSeries", "VariableCode", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.DM_TimeSeries", "VariableCode");
            DropColumn("dbo.DM_TimeSeries", "SiteCode");
        }
    }
}
