/* InnerSky SQL Server 2022 schema + seed script */
SET NOCOUNT ON;

IF DB_ID(N'InnerSky') IS NULL
BEGIN
    CREATE DATABASE [InnerSky];
END;
GO

USE [InnerSky];
GO

IF OBJECT_ID(N'dbo.EmotionProfileComponents', N'U') IS NOT NULL DROP TABLE dbo.EmotionProfileComponents;
IF OBJECT_ID(N'dbo.EmotionProfiles', N'U') IS NOT NULL DROP TABLE dbo.EmotionProfiles;
IF OBJECT_ID(N'dbo.Dyads', N'U') IS NOT NULL DROP TABLE dbo.Dyads;
IF OBJECT_ID(N'dbo.EmotionIntensityNames', N'U') IS NOT NULL DROP TABLE dbo.EmotionIntensityNames;
IF OBJECT_ID(N'dbo.BaseEmotions', N'U') IS NOT NULL DROP TABLE dbo.BaseEmotions;
GO

CREATE TABLE dbo.BaseEmotions
(
    EmotionId       NVARCHAR(32) NOT NULL PRIMARY KEY,
    Label           NVARCHAR(64) NOT NULL,
    WheelOrder      TINYINT NOT NULL UNIQUE
);

CREATE TABLE dbo.EmotionIntensityNames
(
    EmotionId       NVARCHAR(32) NOT NULL,
    IntensityLevel  TINYINT NOT NULL,
    DisplayName     NVARCHAR(64) NOT NULL,
    CONSTRAINT PK_EmotionIntensityNames PRIMARY KEY (EmotionId, IntensityLevel),
    CONSTRAINT CK_EmotionIntensityNames_IntensityLevel CHECK (IntensityLevel BETWEEN 0 AND 2),
    CONSTRAINT FK_EmotionIntensityNames_BaseEmotions FOREIGN KEY (EmotionId) REFERENCES dbo.BaseEmotions (EmotionId)
);

CREATE TABLE dbo.Dyads
(
    DyadId          NVARCHAR(32) NOT NULL PRIMARY KEY,
    Label           NVARCHAR(64) NOT NULL,
    EmotionAId      NVARCHAR(32) NOT NULL,
    EmotionBId      NVARCHAR(32) NOT NULL,
    CONSTRAINT FK_Dyads_EmotionA FOREIGN KEY (EmotionAId) REFERENCES dbo.BaseEmotions (EmotionId),
    CONSTRAINT FK_Dyads_EmotionB FOREIGN KEY (EmotionBId) REFERENCES dbo.BaseEmotions (EmotionId)
);

CREATE TABLE dbo.EmotionProfiles
(
    Id              UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_EmotionProfiles PRIMARY KEY,
    Name            NVARCHAR(200) NOT NULL,
    CreatedUtc      DATETIME2(0) NOT NULL CONSTRAINT DF_EmotionProfiles_CreatedUtc DEFAULT (SYSUTCDATETIME())
);

CREATE TABLE dbo.EmotionProfileComponents
(
    Id              BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_EmotionProfileComponents PRIMARY KEY,
    ProfileId       UNIQUEIDENTIFIER NOT NULL,
    EmotionId       NVARCHAR(32) NOT NULL,
    IntensityLevel  TINYINT NOT NULL,
    CONSTRAINT FK_EmotionProfileComponents_Profiles FOREIGN KEY (ProfileId) REFERENCES dbo.EmotionProfiles (Id) ON DELETE CASCADE,
    CONSTRAINT FK_EmotionProfileComponents_BaseEmotions FOREIGN KEY (EmotionId) REFERENCES dbo.BaseEmotions (EmotionId),
    CONSTRAINT CK_EmotionProfileComponents_IntensityLevel CHECK (IntensityLevel BETWEEN 0 AND 2)
);
GO

INSERT INTO dbo.BaseEmotions (EmotionId, Label, WheelOrder) VALUES
(N'joy', N'Joy', 1),
(N'trust', N'Trust', 2),
(N'fear', N'Fear', 3),
(N'surprise', N'Surprise', 4),
(N'sadness', N'Sadness', 5),
(N'disgust', N'Disgust', 6),
(N'anger', N'Anger', 7),
(N'anticipation', N'Anticipation', 8);

INSERT INTO dbo.EmotionIntensityNames (EmotionId, IntensityLevel, DisplayName) VALUES
(N'joy', 0, N'Serenity'), (N'joy', 1, N'Joy'), (N'joy', 2, N'Ecstasy'),
(N'trust', 0, N'Acceptance'), (N'trust', 1, N'Trust'), (N'trust', 2, N'Admiration'),
(N'fear', 0, N'Apprehension'), (N'fear', 1, N'Fear'), (N'fear', 2, N'Terror'),
(N'surprise', 0, N'Distraction'), (N'surprise', 1, N'Surprise'), (N'surprise', 2, N'Amazement'),
(N'sadness', 0, N'Pensiveness'), (N'sadness', 1, N'Sadness'), (N'sadness', 2, N'Grief'),
(N'disgust', 0, N'Boredom'), (N'disgust', 1, N'Disgust'), (N'disgust', 2, N'Loathing'),
(N'anger', 0, N'Annoyance'), (N'anger', 1, N'Anger'), (N'anger', 2, N'Rage'),
(N'anticipation', 0, N'Interest'), (N'anticipation', 1, N'Anticipation'), (N'anticipation', 2, N'Vigilance');

INSERT INTO dbo.Dyads (DyadId, Label, EmotionAId, EmotionBId) VALUES
(N'optimism', N'Optimism', N'anticipation', N'joy'),
(N'love', N'Love', N'joy', N'trust'),
(N'submission', N'Submission', N'trust', N'fear'),
(N'awe', N'Awe', N'fear', N'surprise'),
(N'disapproval', N'Disapproval', N'surprise', N'sadness'),
(N'remorse', N'Remorse', N'sadness', N'disgust'),
(N'contempt', N'Contempt', N'disgust', N'anger'),
(N'aggressiveness', N'Aggressiveness', N'anger', N'anticipation'),
(N'hope', N'Hope', N'anticipation', N'trust'),
(N'guilt', N'Guilt', N'joy', N'fear'),
(N'curiosity', N'Curiosity', N'trust', N'surprise'),
(N'despair', N'Despair', N'fear', N'sadness'),
(N'unbelief', N'Unbelief', N'surprise', N'disgust'),
(N'envy', N'Envy', N'sadness', N'anger'),
(N'cynicism', N'Cynicism', N'disgust', N'anticipation'),
(N'pride', N'Pride', N'anger', N'joy'),
(N'dominance', N'Dominance', N'anger', N'trust'),
(N'anxiety', N'Anxiety', N'anticipation', N'fear'),
(N'delight', N'Delight', N'joy', N'surprise'),
(N'sentimentality', N'Sentimentality', N'trust', N'sadness'),
(N'shame', N'Shame', N'fear', N'disgust'),
(N'outrage', N'Outrage', N'surprise', N'anger'),
(N'pessimism', N'Pessimism', N'sadness', N'anticipation'),
(N'morbidness', N'Morbidness', N'disgust', N'joy');
GO
