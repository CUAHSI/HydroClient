namespace HISWebClient.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class QclSourceAndMethod : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.DM_TimeSeries", "QCLID", c => c.String());
            AddColumn("dbo.DM_TimeSeries", "QCLDesc", c => c.String());
            AddColumn("dbo.DM_TimeSeries", "SourceOrg", c => c.String());
            AddColumn("dbo.DM_TimeSeries", "SourceId", c => c.String());
            AddColumn("dbo.DM_TimeSeries", "SourceDesc", c => c.String());
            AddColumn("dbo.DM_TimeSeries", "MethodId", c => c.String());
            AddColumn("dbo.DM_TimeSeries", "MethodDesc", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.DM_TimeSeries", "MethodDesc");
            DropColumn("dbo.DM_TimeSeries", "MethodId");
            DropColumn("dbo.DM_TimeSeries", "SourceDesc");
            DropColumn("dbo.DM_TimeSeries", "SourceId");
            DropColumn("dbo.DM_TimeSeries", "SourceOrg");
            DropColumn("dbo.DM_TimeSeries", "QCLDesc");
            DropColumn("dbo.DM_TimeSeries", "QCLID");
        }
    }
}
