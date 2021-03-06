USE [master]
GO
/****** Object:  Database [dbScience3]    Script Date: 2015-01-19 21:23:39 ******/
CREATE DATABASE [dbScience3]
 CONTAINMENT = NONE
 ON  PRIMARY 
( NAME = N'dbScience3', FILENAME = N'D:\Microsoft SQL Server\MSSQL11.SQLEXPRESS\MSSQL\DATA\dbScience3.mdf' , SIZE = 10240KB , MAXSIZE = UNLIMITED, FILEGROWTH = 1024KB )
 LOG ON 
( NAME = N'dbScience3_log', FILENAME = N'D:\Microsoft SQL Server\MSSQL11.SQLEXPRESS\MSSQL\DATA\dbScience3_log.ldf' , SIZE = 1024KB , MAXSIZE = 2048GB , FILEGROWTH = 10%)
GO
ALTER DATABASE [dbScience3] SET COMPATIBILITY_LEVEL = 100
GO
IF (1 = FULLTEXTSERVICEPROPERTY('IsFullTextInstalled'))
begin
EXEC [dbScience3].[dbo].[sp_fulltext_database] @action = 'enable'
end
GO
ALTER DATABASE [dbScience3] SET ANSI_NULL_DEFAULT OFF 
GO
ALTER DATABASE [dbScience3] SET ANSI_NULLS OFF 
GO
ALTER DATABASE [dbScience3] SET ANSI_PADDING OFF 
GO
ALTER DATABASE [dbScience3] SET ANSI_WARNINGS OFF 
GO
ALTER DATABASE [dbScience3] SET ARITHABORT OFF 
GO
ALTER DATABASE [dbScience3] SET AUTO_CLOSE OFF 
GO
ALTER DATABASE [dbScience3] SET AUTO_CREATE_STATISTICS ON 
GO
ALTER DATABASE [dbScience3] SET AUTO_SHRINK OFF 
GO
ALTER DATABASE [dbScience3] SET AUTO_UPDATE_STATISTICS ON 
GO
ALTER DATABASE [dbScience3] SET CURSOR_CLOSE_ON_COMMIT OFF 
GO
ALTER DATABASE [dbScience3] SET CURSOR_DEFAULT  GLOBAL 
GO
ALTER DATABASE [dbScience3] SET CONCAT_NULL_YIELDS_NULL OFF 
GO
ALTER DATABASE [dbScience3] SET NUMERIC_ROUNDABORT OFF 
GO
ALTER DATABASE [dbScience3] SET QUOTED_IDENTIFIER OFF 
GO
ALTER DATABASE [dbScience3] SET RECURSIVE_TRIGGERS OFF 
GO
ALTER DATABASE [dbScience3] SET  DISABLE_BROKER 
GO
ALTER DATABASE [dbScience3] SET AUTO_UPDATE_STATISTICS_ASYNC OFF 
GO
ALTER DATABASE [dbScience3] SET DATE_CORRELATION_OPTIMIZATION OFF 
GO
ALTER DATABASE [dbScience3] SET TRUSTWORTHY OFF 
GO
ALTER DATABASE [dbScience3] SET ALLOW_SNAPSHOT_ISOLATION OFF 
GO
ALTER DATABASE [dbScience3] SET PARAMETERIZATION SIMPLE 
GO
ALTER DATABASE [dbScience3] SET READ_COMMITTED_SNAPSHOT OFF 
GO
ALTER DATABASE [dbScience3] SET HONOR_BROKER_PRIORITY OFF 
GO
ALTER DATABASE [dbScience3] SET RECOVERY BULK_LOGGED 
GO
ALTER DATABASE [dbScience3] SET  MULTI_USER 
GO
ALTER DATABASE [dbScience3] SET PAGE_VERIFY CHECKSUM  
GO
ALTER DATABASE [dbScience3] SET DB_CHAINING OFF 
GO
ALTER DATABASE [dbScience3] SET FILESTREAM( NON_TRANSACTED_ACCESS = OFF ) 
GO
ALTER DATABASE [dbScience3] SET TARGET_RECOVERY_TIME = 0 SECONDS 
GO
USE [dbScience3]
GO
/****** Object:  User [starter]    Script Date: 2015-01-19 21:23:39 ******/
CREATE USER [starter] WITHOUT LOGIN WITH DEFAULT_SCHEMA=[dbo]
GO
ALTER ROLE [db_owner] ADD MEMBER [starter]
GO
ALTER ROLE [db_securityadmin] ADD MEMBER [starter]
GO
ALTER ROLE [db_datareader] ADD MEMBER [starter]
GO
ALTER ROLE [db_datawriter] ADD MEMBER [starter]
GO
/****** Object:  Table [dbo].[tblAffiliation]    Script Date: 2015-01-19 21:23:40 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[tblAffiliation](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[Person_ID] [int] NULL,
	[Affiliation] [varchar](2000) NULL
) ON [PRIMARY]

GO
SET ANSI_PADDING OFF
GO
/****** Object:  Table [dbo].[tblConference]    Script Date: 2015-01-19 21:23:40 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[tblConference](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[Name] [varchar](200) NULL
) ON [PRIMARY]

GO
SET ANSI_PADDING OFF
GO
/****** Object:  Table [dbo].[tblEvent]    Script Date: 2015-01-19 21:23:40 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[tblEvent](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[EventGroup_ID] [int] NULL,
	[Name] [varchar](2000) NULL,
	[Type] [int] NULL,
	[Key] [varchar](200) NULL,
	[Url] [varchar](2000) NULL,
	[Conference_ID] [int] NULL,
	[Score] [float] NULL,
	[ScoreNow] [float] NULL
) ON [PRIMARY]

GO
SET ANSI_PADDING OFF
GO
/****** Object:  Table [dbo].[tblEventGroup]    Script Date: 2015-01-19 21:23:40 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[tblEventGroup](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[Name] [varchar](200) NULL,
	[Type] [int] NULL,
	[Date] [date] NULL,
	[Url] [varchar](2000) NULL,
	[Conference_ID] [int] NULL,
	[Group] [varchar](200) NULL
) ON [PRIMARY]

GO
SET ANSI_PADDING OFF
GO
/****** Object:  Table [dbo].[tblLink]    Script Date: 2015-01-19 21:23:40 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[tblLink](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[Person_ID] [int] NULL,
	[Event_ID] [int] NULL
) ON [PRIMARY]

GO
/****** Object:  Table [dbo].[tblLinkReason]    Script Date: 2015-01-19 21:23:40 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[tblLinkReason](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[Link_ID] [int] NULL,
	[Reason] [int] NULL,
	[ReasonLink_ID] [int] NULL
) ON [PRIMARY]

GO
/****** Object:  Table [dbo].[tblPerson]    Script Date: 2015-01-19 21:23:40 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[tblPerson](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[Name] [varchar](200) NULL,
	[Affiliation] [varchar](2000) NULL,
	[OPI] [int] NULL,
	[StartYear] [int] NULL,
	[HIndex] [int] NULL
) ON [PRIMARY]

GO
SET ANSI_PADDING OFF
GO
/****** Object:  Table [dbo].[tblPersonOPI]    Script Date: 2015-01-19 21:23:40 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[tblPersonOPI](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[Name] [varchar](200) NULL,
	[OPI] [int] NULL
) ON [PRIMARY]

GO
SET ANSI_PADDING OFF
GO
/****** Object:  Table [dbo].[tblPersonScore]    Script Date: 2015-01-19 21:23:40 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[tblPersonScore](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[Person_ID] [int] NULL,
	[Year] [int] NULL,
	[Score] [float] NULL,
	[ConnectionCount] [int] NULL
) ON [PRIMARY]

GO
/****** Object:  Table [dbo].[tblReference]    Script Date: 2015-01-19 21:23:40 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[tblReference](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[Event_ID] [int] NULL,
	[ReferenceEvent_ID] [int] NULL
) ON [PRIMARY]

GO
/****** Object:  Index [ClusteredIndex-20130305-165723]    Script Date: 2015-01-19 21:23:40 ******/
CREATE CLUSTERED INDEX [ClusteredIndex-20130305-165723] ON [dbo].[tblEvent]
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [ClusteredIndex-20130305-165658]    Script Date: 2015-01-19 21:23:40 ******/
CREATE CLUSTERED INDEX [ClusteredIndex-20130305-165658] ON [dbo].[tblLink]
(
	[Person_ID] ASC,
	[Event_ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [ClusteredIndex-20130305-165421]    Script Date: 2015-01-19 21:23:40 ******/
CREATE CLUSTERED INDEX [ClusteredIndex-20130305-165421] ON [dbo].[tblPerson]
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [ClusteredIndex-20130305-165506]    Script Date: 2015-01-19 21:23:40 ******/
CREATE CLUSTERED INDEX [ClusteredIndex-20130305-165506] ON [dbo].[tblPersonScore]
(
	[ID] ASC,
	[Person_ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [NonClusteredIndex-20130307-214917]    Script Date: 2015-01-19 21:23:40 ******/
CREATE NONCLUSTERED INDEX [NonClusteredIndex-20130307-214917] ON [dbo].[tblEvent]
(
	[Type] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
USE [master]
GO
ALTER DATABASE [dbScience3] SET  READ_WRITE 
GO
