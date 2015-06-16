delete from tblLinkReason
go
bulk insert tblLinkReason from 'd:\\work\\phd\\!\\lines\\tblLinkReason.csv' with ( FIELDTERMINATOR = ',', ROWTERMINATOR = '\n' )