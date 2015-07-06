CREATE TABLE [dbo].[TransactionItemAmendment]
(
	[Id] CHAR(25) NOT NULL PRIMARY KEY, 
	[TransactionItemId] CHAR(25) NOT NULL,
    [DateCreatedUtc] DATETIME2 NOT NULL DEFAULT GETUTCDATE(), 
    [ItemAmount] MONEY NOT NULL, 
    [Reason] NVARCHAR(50) NOT NULL, 
    CONSTRAINT [FK_TransactionItemAmendment_TransactionItem] FOREIGN KEY (TransactionItemId) REFERENCES [TransactionItem]([Id])
	)
