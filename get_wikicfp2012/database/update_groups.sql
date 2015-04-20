-- name groups
update tblEventGroup set [Group] = rtrim(left(Name,CHARINDEX(' ',Name))) where [Type]=10
--
go
-- add groups for non-conferences
update tblEventGroup set [Group] = c.Name, Conference_ID = c.ID
from
tblConference c
join tblEvent e on e.Conference_ID=c.ID
join tblEventGroup eg on e.EventGroup_ID=eg.ID
where eg.[Type]<>10

