using MySql.Data.MySqlClient;

namespace Library
{
    public static class DbHelper
    {
        private const string Server   = "localhost";
        private const string Port     = "3306";
        private const string DbUser   = "db";
        private const string DbPass   = "123456";
        private const string DbName   = "library";

        private static string Cs =>
            $"Server={Server};Port={Port};Database={DbName};Uid={DbUser};Pwd={DbPass};" +
            "CharSet=utf8mb4;SslMode=None;AllowPublicKeyRetrieval=True;";

        private static string CsNoDb =>
            $"Server={Server};Port={Port};Uid={DbUser};Pwd={DbPass};" +
            "CharSet=utf8mb4;SslMode=None;AllowPublicKeyRetrieval=True;";

        public static MySqlConnection GetConnection() => new MySqlConnection(Cs);

        public static void InitializeDatabase()
        {
            // Step 1: create database if it does not exist
            using (var conn = new MySqlConnection(CsNoDb))
            {
                conn.Open();
                Exec(conn, null,
                    $"CREATE DATABASE IF NOT EXISTS `{DbName}` " +
                    "CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci");
            }

            // Step 2: create tables
            using (var conn = GetConnection())
            {
                conn.Open();

                Exec(conn, null, @"
                    CREATE TABLE IF NOT EXISTS Books (
                        Id       INT          PRIMARY KEY AUTO_INCREMENT,
                        Name     VARCHAR(255) NOT NULL,
                        Author   VARCHAR(255) DEFAULT '',
                        Category VARCHAR(100) DEFAULT '',
                        Quantity INT          DEFAULT 0
                    ) ENGINE=InnoDB");

                Exec(conn, null, @"
                    CREATE TABLE IF NOT EXISTS Users (
                        Id    INT          PRIMARY KEY AUTO_INCREMENT,
                        Name  VARCHAR(255) NOT NULL,
                        Phone VARCHAR(50)  DEFAULT '',
                        Email VARCHAR(255) DEFAULT ''
                    ) ENGINE=InnoDB");

                Exec(conn, null, @"
                    CREATE TABLE IF NOT EXISTS Borrows (
                        Id         INT      PRIMARY KEY AUTO_INCREMENT,
                        UserId     INT      NOT NULL,
                        BorrowDate DATETIME DEFAULT NOW(),
                        ReturnDate DATETIME NULL,
                        Status     VARCHAR(20) DEFAULT 'borrowing',
                        FOREIGN KEY (UserId) REFERENCES Users(Id)
                    ) ENGINE=InnoDB");

                Exec(conn, null, @"
                    CREATE TABLE IF NOT EXISTS BorrowDetails (
                        Id       INT PRIMARY KEY AUTO_INCREMENT,
                        BorrowId INT NOT NULL,
                        BookId   INT NOT NULL,
                        Quantity INT DEFAULT 1,
                        FOREIGN KEY (BorrowId) REFERENCES Borrows(Id),
                        FOREIGN KEY (BookId)   REFERENCES Books(Id)
                    ) ENGINE=InnoDB");
            }
        }

        public static void Exec(MySqlConnection conn, MySqlTransaction tx, string sql)
        {
            using (var cmd = new MySqlCommand(sql, conn, tx))
                cmd.ExecuteNonQuery();
        }
    }
}
