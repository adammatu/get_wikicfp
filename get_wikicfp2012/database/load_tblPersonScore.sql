delete from tblPersonScore
go
bulk insert tblPersonScore from 'D:\work\phd\!\lines\tblPersonScore.csv' with ( FIELDTERMINATOR = ',', ROWTERMINATOR = '\n' )