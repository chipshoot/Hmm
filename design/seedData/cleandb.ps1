$dbServer = "CHEETAHFAST\SQLEXPRESS"
$db="HmmRepo"

invoke-sqlcmd -serverInstance $dbServer -database $db -inputFile "G:\Projects2\Hmm\design\database\cleanDatabase.sql"
