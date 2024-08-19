using System.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;
using System.Globalization;

namespace Simple_ATM;

class Program
{
    static string connectionString = $"Server=(localdb)\\MSSQLLocalDB";
    static string userID = "";
    static string userName = "";
    static string userPIN = "";


    private static string topazFont = @"
  _______                ______  ____              _    _             
 |__   __|              |___  / |  _ \            | |  (_)            
    | | ___  _ __   __ _   / /  | |_) | __ _ _ __ | | ___ _ __   __ _ 
    | |/ _ \| '_ \ / _` | / /   |  _ < / _` | '_ \| |/ / | '_ \ / _` |
    | | (_) | |_) | (_| |/ /__  | |_) | (_| | | | |   <| | | | | (_| |
    |_|\___/| .__/ \__,_/_____| |____/ \__,_|_| |_|_|\_\_|_| |_|\__, |
            | |                                                  __/ |
            |_|                                                 |___/ 
";

    static void Main(string[] args)
    {
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.Cyan;
        LogIn(); // authenticate userID and PIN
    }

    static void LogIn()
    {
        Console.Clear();
        Console.WriteLine(topazFont);
        Console.WriteLine($"Enter your User ID:");
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

            try
            {
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        userID = reader.GetString(0);
                        userName = reader.GetString(1);
                        userPIN = reader.GetString(2);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Please enter valid input. Press any key to try again");
                Console.ReadKey();
                Console.Clear();
                LogIn();
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

    static void DisplayAccounts(string uID)
    {
        Console.Clear();
        Console.WriteLine(topazFont);
        Console.WriteLine($"Accounts belonging to {userName}:");
        Console.WriteLine($"\t{"| Acc ID",-7} {"| Acc Type",-10} {"| Acc Number",-12} {"| Balance"}");
        Console.WriteLine("||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||");
        using (var conn = new SqlConnection(connectionString))
        {
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = $"SELECT * FROM TopazBanking.dbo.Accounts where userID = @ID";
            cmd.Parameters.AddWithValue("@ID", uID);

            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    var accID = reader.GetInt32(0);
                    var accType = reader.GetString(1);
                    var accNo = reader.GetString(3);
                    var balance = reader.GetDecimal(4);
                    //Console.WriteLine($"\t{account,-7} {accType,-10} {accNo,-12} {balance:C}");
                    Console.WriteLine("\t| {0,-7}| {1,-10}| {2,-12}| {3, 12}", accID, accType, accNo,
                        balance.ToString("C", CultureInfo.GetCultureInfo("en-ZA")));
                }
            }

            conn.Close();
        }

        Console.WriteLine("Enter Acc ID to perform transactions");
        string accInput = Console.ReadLine();
        try
        {
            Convert.ToInt32(accInput).Equals(typeof(Int32)); // ensure input can be converted to integer
        }
        catch (Exception e)
        {
            Console.WriteLine("Account choice must be an integer. Press any key to continue");
            Console.ReadKey();
            DisplayAccounts(uID);
        }

        int accOption = Convert.ToInt32(accInput);
        Console.Clear();
        VerifyAcc(accOption, uID);
    }

    static void VerifyAcc(int accID, string uID)
    {
        using (var conn = new SqlConnection(connectionString))
        {
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = $"SELECT * FROM TopazBanking.dbo.Accounts where accountID = @accID and userID = @uID";
            cmd.Parameters.AddWithValue("@accID", accID);
            cmd.Parameters.AddWithValue("@uID", uID);
            if (accID == Convert.ToInt32(cmd.ExecuteScalar()) && accID != 0) AccMenu(accID, uID);
            else
            {
                Console.WriteLine("Invalid option. Press any key to continue");
                Console.ReadKey();
                DisplayAccounts(uID);
            }

            conn.Close();
        }
    }

    static void AccMenu(int accID, string uID)
    {
        Console.Clear();
        Console.WriteLine(topazFont);
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
                Withdraw(accID, userID);
                break;
            case "2":
                Deposit(accID, userID);
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
                Console.WriteLine("Invalid option. Press any key to try again");
                Console.ReadKey();
                Console.Clear();
                AccMenu(accID, userID);
                break;
        }
    }

    static void Deposit(int accID, string uID)
    {
        Console.WriteLine("Please enter deposit amount:");
        decimal depositAmount;
        while (!decimal.TryParse(Console.ReadLine(), out depositAmount) || depositAmount <= 0)
        {
            Console.WriteLine("Invalid deposit amount.");
            Deposit(accID, userID);
            return;
        }

        using (var conn = new SqlConnection(connectionString))
        {
            conn.Open();
            SqlCommand cmd = conn.CreateCommand();

            cmd.CommandText =
                $"UPDATE TopazBanking.dbo.Accounts SET balance = balance + {depositAmount} WHERE accountID = {accID} and userID = '{userID}'";
            cmd.ExecuteNonQuery();

            cmd.CommandText =
                $"INSERT INTO TopazBanking.dbo.Transactions VALUES ('{accID}', 'DEPOSIT', '{depositAmount}', '{DateTime.Now}')";
            cmd.ExecuteNonQuery();
            conn.Close();

            Console.WriteLine("Deposit successful.");
            Console.WriteLine("Press any key to return to account functions");
            Console.ReadKey();
            Console.Clear();
            DisplayAccounts(uID);
        }
    }

    static void Withdraw(int accID, string uID)
    {
        Console.WriteLine("Please enter withdrawal amount:");
        decimal withdrawalAmount;
        while (!decimal.TryParse(Console.ReadLine(), out withdrawalAmount) || withdrawalAmount <= 0)
        {
            Console.WriteLine("Invalid withdrawal amount.");
            Withdraw(accID, userID);
            return; // return to what?
        }

        using (var conn = new SqlConnection(connectionString))
        {
            conn.Open();
            SqlCommand cmd = conn.CreateCommand();
            cmd.CommandText =
                $"SELECT balance FROM TopazBanking.dbo.Accounts WHERE accountID = {accID} and userID = '{userID}'";

            decimal currentBalance = (decimal)cmd.ExecuteScalar();

            if (currentBalance >= withdrawalAmount)
            {
                cmd.CommandText =
                    $"UPDATE TopazBanking.dbo.Accounts SET balance = balance - {withdrawalAmount} WHERE accountID = {accID} and userID = '{uID}'";
                cmd.ExecuteNonQuery();

                cmd.CommandText =
                    $"INSERT INTO TopazBanking.dbo.Transactions VALUES ('{accID}', 'WITHDRAWAL', '{withdrawalAmount}', '{DateTime.Now}')";
                cmd.ExecuteNonQuery();

                Console.WriteLine("Withdrawal successful.");
                Console.WriteLine("Press any key to return to account functions");
                Console.ReadKey();
                Console.Clear();
                DisplayAccounts(userID);
            }
            else
            {
                Console.WriteLine("Insufficient funds.");
            }

            conn.Close();
        }
    }

    static void AccTransactions(int accID, string uID)
    {
        Console.Clear();
        Console.WriteLine($"Transactions for account {accID}");
        Console.WriteLine($"\t| {"ID",4}\t| {"Date of transaction",20}\t| {"Acc Type",-9}\t| Amount");
        Console.WriteLine($"\t|||||||||||||||||||||||||||||||||||||||||||||||||||||||||");
        using (var conn = new SqlConnection(connectionString))
        {
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = $"SELECT * FROM TopazBanking.dbo.Transactions WHERE accountID = {accID}";
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    var transID = reader.GetInt32(0);
                    var transType = reader.GetString(2);
                    var transAmount = reader.GetDecimal(3);
                    var transDate = reader.GetDateTime(4);
                    Console.WriteLine("\t| {0, 4}\t| {1, 20}\t| {2, -9}\t| {3, 10}", transID,
                        transDate.ToString("G", CultureInfo.GetCultureInfo("en-ZA")), transType,
                        transAmount.ToString("C", CultureInfo.GetCultureInfo("en-ZA")));
                    //Console.WriteLine($"\t|{transID, 4}\t|{transDate, 20}\t|{transType, -10}\t|{transAmount:C}");
                }

                if (!reader.HasRows) Console.WriteLine("\n\tNo transactions found.");
                Console.WriteLine($"\nPress any key to return to account functions");
                Console.ReadKey();
            }

            conn.Close();

            AccMenu(accID, uID);
        }
    }
}