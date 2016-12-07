namespace HISWebClient.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddLatAndLong : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.DM_TimeSeries", "Latitude", c => c.Double(nullable: false));
            AddColumn("dbo.DM_TimeSeries", "Longitude", c => c.Double(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.DM_TimeSeries", "Longitude");
            DropColumn("dbo.DM_TimeSeries", "Latitude");
        }
    }
}
