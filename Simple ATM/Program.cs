using System.Data.SqlClient;
using System.Collections;
using System.Data;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Simple_ATM;

class Program
{
    static string connectionString = $"Server=(localdb)\\MSSQLLocalDB";
    static string userID = "";
    static string userName = "";
    static string userPIN = "";
    static CultureInfo braaiGeld = CultureInfo.GetCultureInfo("en-ZA");
    static void Main(string[] args)
    {
        logIn(); // authenticate userID and PIN
    }

    static void logIn()
    {
        Console.WriteLine($"\tWelcome to TopaZ Banking! \nEnter your User ID:");
        string inputID = Console.ReadLine();
        Console.WriteLine("Enter your pin:");
        string inputPIN = HideInput();

        // Convert PIN into array of bytes
        byte[] pinBytes = Encoding.UTF8.GetBytes(inputPIN);
        //Create hash value from the array of bytes
        byte[] hashValue = SHA256.HashData(pinBytes);
        //Convert back into string to be usable in query
        inputPIN = Convert.ToHexString(hashValue);


        using (var conn = new SqlConnection(connectionString))
        {
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.Parameters.AddWithValue("@inputID", inputID);
            cmd.Parameters.AddWithValue("@inputPIN", inputPIN);
            cmd.CommandText =
                $"SELECT * FROM ShitBank.dbo.Users WHERE userID = @inputID AND userPIN = @inputPIN";

            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    userID = reader.GetString(0);
                    userName = reader.GetString(1);
                    userPIN = reader.GetString(2);
                }
            }

            conn.Close();
        }

        // only proceed if inputs match DB value
        if (inputID == userID && userID != "" && userPIN == inputPIN && userPIN != "") displayAccounts(userID);
        else
        {
            Console.Clear();
            Console.WriteLine("Invalid credentials. Press any key to return to login menu.");
            Console.ReadKey();
            Console.Clear();
            logIn();
        }
    }

    static string HideInput()
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
        string[] allAccounts = { };

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

                    Console.WriteLine($"{account} {userID} {accNo} {balance:C}");
                }
            }

            conn.Close();
        }

        Console.WriteLine("Enter account ID to perform transactions");
        string acc = Console.ReadLine();
        VerifyAcc(acc, ID);
    }

    static void VerifyAcc(string acc, string ID)
    {
        using (var conn = new SqlConnection(connectionString))
        {
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = $"SELECT * FROM ShitBank.dbo.Accounts where accountID = @acc and userID = @ID";
            cmd.Parameters.AddWithValue("@acc", acc);
            cmd.Parameters.AddWithValue("@ID", ID);
            if (acc == Convert.ToString(cmd.ExecuteScalar()) && acc != "") accMenu(acc, ID);
            else
            {
                Console.WriteLine("Invalid option. Press any key to continue");
                Console.ReadKey();
                displayAccounts(ID);
            }

            conn.Close();
        }
    }

    static void accMenu(string accID, string ID)
    {
        Console.WriteLine($@"Choose an option for Acc ID: {accID}
        1) Withdraw money
        2) Deposit money
        3) View Transactions
        4) View Accounts
        5) Log out");

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
        Console.Clear();
        // Console.WriteLine("{transID} {transType} {amount:C} {timeStamp}");
        using (var conn = new SqlConnection(connectionString))
        {
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.Parameters.AddWithValue("@accID", accId);
            cmd.CommandText = $"SELECT  * FROM ShitBank.dbo.Transactions WHERE accountID = @accID";
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    var transID = reader.GetString(0);
                    var transType = reader.GetString(2);
                    var amount = reader.GetDecimal(3);
                    var timeStamp = reader.GetDateTime(4);
                    if (transType == "D") transType = "Deposit";
                    if (transType == "W") transType = "Withdrawal";
                    Console.WriteLine($"\n{transID} {transType} {amount:c} {timeStamp}\t");
                }

                Console.WriteLine($"\n");
                Console.WriteLine("Press any key to return to account functions");
                Console.ReadKey();
            }

            conn.Close();
            accMenu(accId, ID);
        }
    }
}