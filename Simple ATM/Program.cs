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
        logIn(); // authenticate userID and PIN
    }

    static void logIn()
    {
        Console.WriteLine($"\tWelcome to TopaZ Banking! \nEnter your User ID:");
        string inputID = Console.ReadLine();
        Console.WriteLine("Enter your pin:");
        string inputPIN = ReadPassword();

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

    static string ReadPassword()
    {
        string inputPIN = string.Empty;
        ConsoleKeyInfo key;
        do
        {
            key = Console.ReadKey(true); // Read key but don't display it
            // if key isn't enter, backspace or non-char key
            if (key.Key != ConsoleKey.Enter && key.Key != ConsoleKey.Backspace && key.Key != ConsoleKey.Escape)
            {
                inputPIN += key.KeyChar;
                Console.Write("*");
            }
            else if (key.Key == ConsoleKey.Backspace && inputPIN.Length > 0)
            {
                inputPIN = inputPIN[0..^1]; // Remove last char
                Console.Write("\b \b"); // Move curses 1 space back, print space, move 1 space back again
            }
        } while (key.Key != ConsoleKey.Enter);

        return inputPIN;
    }

    static void displayAccounts(string ID) // displays all account belonging to user
    {
        Console.Clear();
        Console.WriteLine($"Accounts belonging to {userName}!");

        
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

                    var accNo = reader.GetString(2);
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
                Withdraw(accID, ID);
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
                userID = "";
                userName = "";
                logIn();
                break;

            default:
                displayAccounts(userID);
                break;
        }
    }

    static void Withdraw(string accID, string ID)
    {
        Console.WriteLine("Enter withdrawal amount");
        decimal withAmount = decimal.Parse(Console.ReadLine());
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