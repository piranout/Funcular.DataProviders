CREATE TABLE [dbo].[MyLedgerEntryModification]
(
	[Id] CHAR(25) NOT NULL PRIMARY KEY, 
	[LedgerEntryId] CHAR(25) NOT NULL,
    [DateCreatedUtc] DATETIME2 NOT NULL DEFAULT GETUTCDATE(), 
    [ItemAmount] MONEY NOT NULL, 
    [Reason] NVARCHAR(50) NOT NULL, 
    CONSTRAINT [FK_MyLedgerEntryModification_MyLedgerEntry] FOREIGN KEY (LedgerEntryId) REFERENCES [MyLedgerEntry]([Id])
	)
