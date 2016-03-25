namespace HISWebClient.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddedStatusMessage : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.ExportTaskData", "Status", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.ExportTaskData", "Status");
        }
    }
}
