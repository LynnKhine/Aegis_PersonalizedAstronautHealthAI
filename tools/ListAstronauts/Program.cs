using Microsoft.Data.Sqlite;

var dbPath = args.Length > 0 ? args[0] : "aegis.db";
using var conn = new SqliteConnection($"Data Source={dbPath}");
conn.Open();
using var cmd = conn.CreateCommand();
cmd.CommandText = "SELECT Id, Name, NASAId FROM Astronauts ORDER BY Name;";
using var reader = cmd.ExecuteReader();
Console.WriteLine($"{"Id",-38} | {"Name",-20} | NASAId");
Console.WriteLine(new string('-', 70));
while (reader.Read())
    Console.WriteLine($"{reader.GetString(0),-38} | {reader.GetString(1),-20} | {reader.GetString(2)}");
