using MySql.Data.MySqlClient;
using Mysqlx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsFormsApp1.Utilities
{
    public static class DBUtilities
    {
        public static int Execute(string sql, MySqlConnection connection,
            Action<Exception> onError = null, params (string, string)[] parameters)
        {
            int res = 0;
            try
            {
                if(connection.State == System.Data.ConnectionState.Closed)
                    connection.Open();
                var com = new MySqlCommand(sql, connection);

                foreach (var p in parameters)
                {
                    com.Parameters.AddWithValue(p.Item1, p.Item2);
                }

                res = com.ExecuteNonQuery();

                com.Dispose();
            }
            catch (Exception e)
            {
                onError?.Invoke(e);
            }

            return res;
        }

        public static void ExecuteReader(string sql,
            MySqlConnection connection,
            Action<MySqlDataReader> read,
            Action<Exception> onError = null,
            params (string, string)[] parameters)
        {
            try
            {

                if (connection.State == System.Data.ConnectionState.Closed)
                    connection.Open();
                var com = new MySqlCommand(sql, connection);

                foreach (var p in parameters)
                {
                    com.Parameters.AddWithValue(p.Item1, p.Item2);
                }

                var r = com.ExecuteReader();

                while (r.Read())
                {
                    read(r);
                }

                r.Close();
                r.DisposeAsync().Wait();

                com.Dispose();
            }
            catch (Exception e)
            {
                onError(e);
            }

        }

        public static List<string> GetAllTables(string dbName,
            MySqlConnection connection, Action<Exception> onError)
        {
            List<string> res = new List<string>();

            Execute($"USE {dbName}", connection, onError);

            ExecuteReader($"SHOW TABLES", connection,
                (r) => { res.Add(r.GetString(0)); });

            return res;
        }

        public static List<string> GetAllColumns(string tableName, MySqlConnection connection
             , Action<Exception> onError)
        {
            List<string> res = new List<string>();

            ExecuteReader($"DESCRIBE {tableName}", connection,
                r => res.Add(r.GetString(0)), onError);

            return res;
        }

        public static List<(string, string)> GetAllColumnsAndTypes(string tableName, MySqlConnection connection
             , Action<Exception> onError)
        {
            List<(string, string)> res = new List<(string, string)>();

            ExecuteReader($"SELECT COLUMN_NAME, DATA_TYPE from INFORMATION_SCHEMA.COLUMNS c\r\nwhere TABLE_SCHEMA = 'TanksDb' AND TABLE_NAME = '{tableName}'", connection,
                r => res.Add((r.GetString(0), r.GetString(1))), onError);

            return res;
        }

        public static List<(string, string)> GetAllFKAndTablesWithFK(string tablename, MySqlConnection connection,
            Action<Exception> onError)
        {
            List<(string, string)> res = new List<(string, string)>();

            ExecuteReader($"SELECT k.REFERENCED_COLUMN_NAME, k.REFERENCED_TABLE_NAME \r\nFROM information_schema.TABLE_CONSTRAINTS i \r\nLEFT JOIN information_schema.KEY_COLUMN_USAGE k ON i.CONSTRAINT_NAME = k.CONSTRAINT_NAME \r\nWHERE i.CONSTRAINT_TYPE = 'FOREIGN KEY' \r\nAND i.TABLE_SCHEMA = DATABASE()\r\nAND i.TABLE_NAME = '{tablename}';", connection,
                r => res.Add((r.GetString(0), r.GetString(1))), onError);

            return res;
        }
    }
}
