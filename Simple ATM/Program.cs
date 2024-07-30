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
        string userID = "";

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
            Console.Clear();
            Console.WriteLine($"Welcome, {userName}!");
            displayAccounts(userID);
        }
    }

    static void displayAccounts(string ID)
    {
        
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
                    
                    var accNO = reader.GetString(2);
                    var balance = reader.GetDecimal(3);

                    Console.WriteLine($"{account} {userID} {accNO} {balance:c}");
                }
            }
            conn.Close();
        }
        Console.WriteLine("Enter account ID to perform transactions");
        string acc = Console.ReadLine();
        accMenu(acc);
    }
    
    static void accMenu(string accNum)
    {
        Console.WriteLine($@"Choose an option for Acc Num: {accNum}
        1) Withdraw money
        2) Deposit money
        3) View Accounts
        4) Log out");
        //Console.WriteLine($"\n\n Enter 0 to return to previous menu");
        var accOption = Console.ReadLine();
        switch (accOption)
        {
            case "1":
                Console.WriteLine("Money shall now leave you account!");
                break;
            case "2":
                Console.WriteLine("Money shall no enter your account!");
                break;
            case "3":
                displayAccounts(userID);
                break;
            case "4":
                Environment.Exit(0);
                break;
                
            default:
                accMenu(accNum);
                break;
        }
    }
}