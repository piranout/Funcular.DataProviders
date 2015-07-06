CREATE TABLE [dbo].[DescribedThing] (
    [Id]                    CHAR (25)       NOT NULL,
    [DateCreatedUtc]        DATETIME2 (7)   NOT NULL DEFAULT GETUTCDATE(),
    [CreatedBy]             NVARCHAR (100)  NOT NULL,
    [DateModifiedUtc]       DATETIME2 (7)   NULL,
    [ModifiedBy]            NVARCHAR (100)  NULL,
    [Name]                  NVARCHAR (100)  NOT NULL,
    [Label]                 NVARCHAR (255)  NULL,
    [Description]           NVARCHAR (2048) NULL,
    [NullableIntProperty] INT             NULL,
    [BoolProperty]        BIT             NOT NULL,
    [TextProperty]        NVARCHAR (MAX)  NULL,
    CONSTRAINT [PK_DescribedThing] PRIMARY KEY CLUSTERED ([Id] ASC)
);

