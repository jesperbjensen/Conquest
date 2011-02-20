CREATE TABLE Maneuvers
(
   ID INT IDENTITY(1,1) PRIMARY KEY CLUSTERED,
   TypeKey nvarchar(100),
   Player nvarchar(255),
   Value int,
   Points int,
   CreatedAt datetime
)

CREATE TABLE Medallions
(
   ID INT IDENTITY(1,1) PRIMARY KEY CLUSTERED,
   TypeKey nvarchar(100),
   Player nvarchar(255),
   Amount int,
   CreatedAt datetime,
   UserNotified bit
)

CREATE INDEX IX_Maneuvers ON Maneuvers(TypeKey,Player,Value,Points); 
