using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlightBooking.Migrations
{
    /// <inheritdoc />
    public partial class UpdateMessagesTableUseIsRead : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Thêm cột IsRead nếu chưa có
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Messages') AND name = 'IsRead')
                BEGIN
                    ALTER TABLE [dbo].[Messages]
                    ADD [IsRead] [bit] NOT NULL DEFAULT (0);
                END
            ");

            // Migrate dữ liệu từ Status sang IsRead (nếu có cột Status)
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Messages') AND name = 'Status')
                BEGIN
                    DECLARE @MigrateSQL NVARCHAR(MAX) = N'
                        UPDATE [dbo].[Messages]
                        SET [IsRead] = CASE 
                            WHEN [Status] = ''READ'' THEN 1
                            WHEN [Status] = ''SENT'' THEN 0
                            ELSE 0
                        END;
                    ';
                    EXEC sp_executesql @MigrateSQL;
                END
            ");

            // Đảm bảo có cột IsAutoReply
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Messages') AND name = 'IsAutoReply')
                BEGIN
                    ALTER TABLE [dbo].[Messages]
                    ADD [IsAutoReply] [bit] NOT NULL DEFAULT (0);
                END
            ");

            // Đảm bảo có cột ReadAt
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Messages') AND name = 'ReadAt')
                BEGIN
                    ALTER TABLE [dbo].[Messages]
                    ADD [ReadAt] [datetime2](7) NULL;
                END
            ");

            // Tạo bảng nếu chưa tồn tại
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Messages')
                BEGIN
                    CREATE TABLE [dbo].[Messages](
                        [MessageId] [int] IDENTITY(1,1) NOT NULL,
                        [UserId] [int] NOT NULL,
                        [Content] [nvarchar](2000) NOT NULL,
                        [SenderType] [nvarchar](20) NOT NULL,
                        [IsRead] [bit] NOT NULL DEFAULT ((0)),
                        [IsAutoReply] [bit] NOT NULL DEFAULT ((0)),
                        [CreatedAt] [datetime2](7) NOT NULL DEFAULT (sysdatetime()),
                        [ReadAt] [datetime2](7) NULL,
                        PRIMARY KEY CLUSTERED ([MessageId] ASC)
                    );

                    IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Messages_Users')
                    BEGIN
                        ALTER TABLE [dbo].[Messages]
                        ADD CONSTRAINT [FK_Messages_Users] FOREIGN KEY([UserId])
                        REFERENCES [dbo].[users]([user_id]);
                    END

                    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Messages_UserId_CreatedAt' AND object_id = OBJECT_ID('Messages'))
                    BEGIN
                        CREATE INDEX [IX_Messages_UserId_CreatedAt] ON [dbo].[Messages]([UserId], [CreatedAt]);
                    END

                    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Messages_SenderType' AND object_id = OBJECT_ID('Messages'))
                    BEGIN
                        CREATE INDEX [IX_Messages_SenderType] ON [dbo].[Messages]([SenderType]);
                    END
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Rollback: Có thể thêm lại cột Status nếu cần
            // migrationBuilder.Sql(@"
            //     IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Messages') AND name = 'Status')
            //     BEGIN
            //         ALTER TABLE [dbo].[Messages]
            //         ADD [Status] [nvarchar](20) NOT NULL DEFAULT ('SENT');
            //         
            //         UPDATE [dbo].[Messages]
            //         SET [Status] = CASE WHEN [IsRead] = 1 THEN 'READ' ELSE 'SENT' END;
            //     END
            // ");
        }
    }
}






