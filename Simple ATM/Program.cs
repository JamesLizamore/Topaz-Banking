using System.Data.SqlClient;
using System.Collections;
using System.Data;
using System.Globalization;

namespace Simple_ATM;

class Program
{
    static string connectionString = $"Server=(localdb)\\MSSQLLocalDB";
    static string userID = "";
    static string userName = "";

    static void Main(string[] args)
    {
        //

        logIn();
    }

    static void logIn()
    {
        Console.WriteLine($"\tWelcome to TopaZ Banking! \nEnter your User ID:");
        string inputID = Console.ReadLine();
        Console.WriteLine("Enter your pin:");
        string inputPIN = Console.ReadLine();

        using (var conn = new SqlConnection(connectionString))
        {
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.Parameters.AddWithValue("@inputID", inputID);
            cmd.Parameters.AddWithValue("@inputPIN", inputPIN);
            cmd.CommandText =
                $"SELECT userID, userName FROM ShitBank.dbo.Users WHERE userID = @inputID AND userPIN = @inputPIN";

            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    userID = reader.GetString(0);
                    userName = reader.GetString(1);
                    
                }
                
            }
            conn.Close();
        }
        displayAccounts(userID);
    }

    static void displayAccounts(string ID)
    {
        Console.Clear();
        Console.WriteLine($"Accounts belonging to {userName}!");

        string? accNo;
        using (var conn = new SqlConnection(connectionString))
        {
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = $"SELECT * FROM ShitBank.dbo.Accounts where userID = @ID";
            cmd.Parameters.AddWithValue("@ID", ID);

            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    var account = reader.GetString(0);
                    
                    accNo = reader.GetString(2);
                    var balance = reader.GetDecimal(3);

                    Console.WriteLine($"{account} {userID} {accNo} {balance:c}");
                }
            }
            conn.Close();
        }
        Console.WriteLine("Enter account ID to perform transactions");
        string acc = Console.ReadLine();
        accMenu(acc, ID);
    }
    
    static void accMenu(string accID, string ID)
    {
        Console.WriteLine($@"Choose an option for Acc ID: {ID}
        1) Withdraw money
        2) Deposit money
        3) View Transactions
        4) View Accounts
        5) Log out");
        //Console.WriteLine($"\n\n Enter 0 to return to previous menu");
        var accOption = Console.ReadLine();
        switch (accOption)
        {
            case "1":
                Console.WriteLine("Money shall now leave you account!");
                break;
            case "2":
                Console.WriteLine("Money shall now enter your account!");
                break;
            case "3":
                accTransactions(accID, userID);
                break;
            case "4":
                displayAccounts(userID);
                break;
            case "5":
                logIn();
                break;
                
            default:
                displayAccounts(userID);
                break;
        }
    }

    static void accTransactions(string accId, string ID)
    {
        using (var conn = new SqlConnection(connectionString))
        {
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.Parameters.AddWithValue("@accID", accId);
            cmd.CommandText = $"SELECT  * FROM ShitBank.dbo.Transactions WHERE accountID = @accID";
        }
    }
}