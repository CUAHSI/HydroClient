namespace HISWebClient.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class test2 : DbMigration
    {
        public override void Up()
        {
            RenameColumn(table: "dbo.DM_TimeSeries", name: "UserTimeSeries_UserEmail", newName: "UserEmail");
            RenameIndex(table: "dbo.DM_TimeSeries", name: "IX_UserTimeSeries_UserEmail", newName: "IX_UserEmail");
            DropColumn("dbo.DM_TimeSeries", "Email");
        }
        
        public override void Down()
        {
            AddColumn("dbo.DM_TimeSeries", "Email", c => c.String());
            RenameIndex(table: "dbo.DM_TimeSeries", name: "IX_UserEmail", newName: "IX_UserTimeSeries_UserEmail");
            RenameColumn(table: "dbo.DM_TimeSeries", name: "UserEmail", newName: "UserTimeSeries_UserEmail");
        }
    }
}
