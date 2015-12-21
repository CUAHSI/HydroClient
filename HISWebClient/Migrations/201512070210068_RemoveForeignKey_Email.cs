namespace HISWebClient.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class RemoveForeignKey_Email : DbMigration
    {
        public override void Up()
        {
            RenameColumn(table: "dbo.DM_TimeSeries", name: "UserEmail", newName: "UserTimeSeries_UserEmail");
            RenameIndex(table: "dbo.DM_TimeSeries", name: "IX_UserEmail", newName: "IX_UserTimeSeries_UserEmail");
            AddColumn("dbo.DM_TimeSeries", "Email", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.DM_TimeSeries", "Email");
            RenameIndex(table: "dbo.DM_TimeSeries", name: "IX_UserTimeSeries_UserEmail", newName: "IX_UserEmail");
            RenameColumn(table: "dbo.DM_TimeSeries", name: "UserTimeSeries_UserEmail", newName: "UserEmail");
        }
    }
}
