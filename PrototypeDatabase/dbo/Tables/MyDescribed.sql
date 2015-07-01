CREATE TABLE [dbo].[MyDescribed] (
    [Id]                    CHAR (25)       NOT NULL,
    [DateCreatedUtc]        DATETIME2 (7)   NOT NULL,
    [CreatedBy]             NVARCHAR (100)  NOT NULL,
    [DateModifiedUtc]       DATETIME2 (7)   NULL,
    [ModifiedBy]            NVARCHAR (100)  NULL,
    [Name]                  NVARCHAR (100)  NOT NULL,
    [Label]                 NVARCHAR (255)  NULL,
    [Description]           NVARCHAR (2048) NULL,
    [MyNullableIntProperty] INT             NULL,
    [MyBoolProperty]        BIT             NOT NULL,
    [MyTextProperty]        NVARCHAR (MAX)  NULL,
    CONSTRAINT [PK_MyDescribed] PRIMARY KEY CLUSTERED ([Id] ASC)
);

