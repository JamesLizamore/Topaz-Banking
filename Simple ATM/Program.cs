using System.Data.SqlClient;

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

    static void Main(string[] args)
    {
        LogIn(); // authenticate userID and PIN
    }

    static void LogIn()
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
                $"SELECT * FROM TopazBanking.dbo.Users WHERE userID = @inputID AND userPIN = @inputPIN";

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
        if (inputID == userID && userID != "" && userPIN == inputPIN && userPIN != "") DisplayAccounts(userID);
        else
        {
            Console.Clear();
            Console.WriteLine("Invalid credentials. Press any key to return to login menu.");
            Console.ReadKey();
            Console.Clear();
            LogIn();
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

    static void DisplayAccounts(string ID) // displays all account belonging to user
    {
        Console.Clear();
        Console.WriteLine($"Accounts belonging to {userName}!");

        using (var conn = new SqlConnection(connectionString))
        {
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = $"SELECT * FROM TopazBanking.dbo.Accounts where userID = @ID";
            cmd.Parameters.AddWithValue("@ID", ID);

            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    var account = reader.GetInt32(0);
                    var accType = reader.GetString(1);
                    var accNo = reader.GetString(3);
                    var balance = reader.GetDecimal(4);

                    Console.WriteLine($"{account} {accType} {accNo} {balance:C}");
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
            cmd.CommandText = $"SELECT * FROM TopazBanking.dbo.Accounts where accountID = @acc and userID = @ID";
            cmd.Parameters.AddWithValue("@acc", acc);
            cmd.Parameters.AddWithValue("@ID", ID);
            if (acc == Convert.ToString(cmd.ExecuteScalar()) && acc != "") AccMenu(acc, ID);
            else
            {
                Console.WriteLine("Invalid option. Press any key to continue");
                Console.ReadKey();
                DisplayAccounts(ID);
            }

            conn.Close();
        }
    }

    static void AccMenu(string accID, string ID)
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
                Withdraw(Convert.ToInt32(accID), ID);
                break;
            case "2":
                Console.WriteLine("Money shall now enter your account!");
                break;
            case "3":
                AccTransactions(accID, userID);
                break;
            case "4":
                DisplayAccounts(userID);
                break;
            case "5":
                userID = "";
                userName = "";
                LogIn();
                break;

            default:
                DisplayAccounts(userID);
                break;
        }
    }

    static void Withdraw(int accID, string ID)
    {
        Console.WriteLine("Please enter withdrawal amount:");
        decimal withdrawalAmount;
        while (!decimal.TryParse(Console.ReadLine(), out withdrawalAmount) || withdrawalAmount <= 0)
        {
            Console.WriteLine("Invalid withdrawal amount.");
            Withdraw(accID, ID);
            return; // return to what?
        }

        using (var conn = new SqlConnection(connectionString))
        {
            conn.Open();
            SqlCommand cmd = conn.CreateCommand();
            cmd.CommandText = $"SELECT balance FROM TopazBanking.dbo.Accounts WHERE accountID = @{accID}";
            cmd.Parameters.AddWithValue("@accID", accID);
            decimal currentBalance = (decimal)cmd.ExecuteScalar();

            if (currentBalance >= withdrawalAmount)
            {
                cmd.CommandText =
                    "UPDATE TopazBanking.dbo.Accounts SET balance = balance - @withdrawalAmount WHERE accountID = @accID";
                cmd.Parameters.AddWithValue("@withdrawalAmount", withdrawalAmount);
                cmd.ExecuteNonQuery();

                cmd.CommandText =
                    $"INSERT INTO Transactions VALUES ('{accID}', 'WITHDRAWAL', '{withdrawalAmount}', '{DateTime.Now}')";

                cmd.ExecuteNonQuery();

                Console.WriteLine("Withdrawal successful.");
                Console.WriteLine("Press any key to return to account functions");
                Console.ReadKey();
                Console.Clear();
                DisplayAccounts(ID); // changed var global userID to local ID
            }
            else
            {
                Console.WriteLine("Insufficient funds.");
            }
            conn.Close();
        }
    }

    static void AccTransactions(string accId, string ID)
    {
        Console.Clear();
        // Console.WriteLine("{transID} {transType} {amount:C} {timeStamp}");
        using (var conn = new SqlConnection(connectionString))
        {
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.Parameters.AddWithValue("@accID", accId);
            cmd.CommandText = $"SELECT  * FROM TopazBanking.dbo.Transactions WHERE accountID = @accID";
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    var transID = reader.GetString(0);
                    var transType = reader.GetString(2);
                    var amount = reader.GetDecimal(3);
                    var timeStamp = reader.GetDateTime(4);

                    Console.WriteLine($"\n{transID} {transType} {amount:c} {timeStamp}\t");
                }

                if (!reader.HasRows) Console.WriteLine("No transactions found.");
                Console.WriteLine($"\n");
                Console.WriteLine("Press any key to return to account functions");
                Console.ReadKey();
            }

            conn.Close();
            AccMenu(accId, ID);
        }
    }
}