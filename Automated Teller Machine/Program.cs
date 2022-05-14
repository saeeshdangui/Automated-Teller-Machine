using Automated_Teller_Machine.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Automated_Teller_Machine
{
    class Program
    {
        static readonly string WonResult = "Won";
        static readonly string LostResult = "Lost";

        static List<Inventory> inventories = new List<Inventory>();
        static List<Bet> bets = new List<Bet>();
        static Dictionary<int, int> payoutBills = new Dictionary<int, int>();
        static readonly string winningCamelRegex = "w [0-9]+";
        static readonly string betRegex = "[0-9]+ [0-9]+";

        static List<Camel> camels = new List<Camel>
            {
                new Camel { Id = 1, Name = "That Darn Gray Cat", Odds = 5, Result = "Won" },
                new Camel { Id = 2, Name = "Fort Utopia", Odds = 10, Result = "Lost" },
                new Camel { Id = 3, Name = "Count Sheep", Odds = 9, Result = "Lost" },
                new Camel { Id = 4, Name = "Ms Traitour", Odds = 4, Result = "Lost" },
                new Camel { Id = 5, Name = "Real Princess", Odds = 3, Result = "Lost" },
                new Camel { Id = 6, Name = "Pa Kettle", Odds = 5, Result = "Lost" },
                new Camel { Id = 7, Name = "Gin Stinger", Odds = 6, Result = "Lost" },
            };

        static void Main(string[] args)
        {
            bool exit = false;
            int camelId = 0;
            string wager = string.Empty;
            string option = string.Empty;

            RestockCashInventory();

            do
            {
                PrintOptions();

                string input = GetCommand().ToLower();

                if (Regex.IsMatch(input, winningCamelRegex))
                {
                    option = "w";
                }
                else if (Regex.IsMatch(input, betRegex))
                {
                    option = "b";
                }
                else
                {
                    option = input;
                }

                switch (option)
                {
                    case "r":
                        RestockCashInventory();
                        break;
                    case "w":
                        camelId = Convert.ToInt32(input.Split(' ')[1]);
                        SetWiningCamelNumber(camelId);
                        break;
                    case "b":
                        var inputArray = input.Split(' ');
                        wager = inputArray[1];
                        int.TryParse(inputArray[0], out camelId);
                        SetBetOnCamel(wager, camelId);
                        break;
                    case "q":
                        exit = true;
                        break;
                    default:
                        Console.WriteLine(string.Format(ErrorMessages.InvalidCommand, option));
                        break;
                }

            } while (exit == false);
        }

        private static void PrintOptions()
        {
            Console.WriteLine("Inventory:");
            foreach (Inventory inventory in inventories)
            {
                Console.WriteLine($"${inventory.Denomination},{inventory.Quantity}");
            }
            Console.WriteLine("Camels:");
            foreach (Camel camel in camels)
            {
                Console.WriteLine($"{camel.Id},{camel.Name},{camel.Odds},{camel.Result}");
            }
        }

        private static string GetCommand()
        {
            string option = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(option))
            {
                return GetCommand();
            }
            return option;
        }

        private static void RestockCashInventory()
        {
            inventories = new List<Inventory>
            {
                new Inventory { Denomination = 1, Quantity = 10 },
                new Inventory { Denomination = 5, Quantity = 10 },
                new Inventory { Denomination = 10, Quantity = 10 },
                new Inventory { Denomination = 20, Quantity = 10 },
                new Inventory { Denomination = 100, Quantity = 10 },
            };
        }

        private static void SetWiningCamelNumber(int winningCamelId)
        {
            Camel previousWinnerCamel = camels.Find(x => x.Result.Equals(WonResult));

            if (previousWinnerCamel != null && previousWinnerCamel.Id != winningCamelId)
            {
                // Set previously won camel to lost
                previousWinnerCamel.Result = LostResult;

                Camel wonCamel = camels.Find(x => x.Id == winningCamelId);

                if (wonCamel == null)
                {
                    Console.WriteLine(string.Format(ErrorMessages.InvalidCamelNumber, winningCamelId));
                }

                wonCamel.Result = WonResult;
            }
        }

        private static void SetBetOnCamel(string wager, int camelId)
        {
            int.TryParse(wager, out int amount);

            if (amount == 0)
            {
                Console.WriteLine(string.Format(ErrorMessages.InvalidBet, wager));
            }
            else
            {
                Camel camel = camels.Find(x => x.Id == camelId);

                if (camel == null)
                {
                    Console.WriteLine(string.Format(ErrorMessages.InvalidCamelNumber, camelId));
                }
                else
                {
                    Bet bet = bets.Find(x => x.CamelId == camelId);

                    if (bet == null)
                    {
                        bets.Add(new Bet { Amount = amount, CamelId = camelId });
                    }
                    else
                    {
                        bet.Amount = amount;
                    }
                    
                    FindWinningCamel(camelId);
                }
            }
        }

        private static void FindWinningCamel(int camelId)
        {
            Camel camel = camels.Find(x => x.Id == camelId);
            Bet bet = bets.Find(x => x.CamelId == camelId);

            if (bet == null || (camel != null && camel.Result.Equals(LostResult)))
            {
                Console.WriteLine($"No Payout: {camel.Name}");
                return;
            }
            else if (camel == null)
            {
                Console.WriteLine(string.Format(ErrorMessages.InvalidCamelNumber, camelId));
            }
            else
            {
                int winningAmount = camel.Odds * bet.Amount;
                Payout(camel, winningAmount);
            }
        }

        private static void Payout(Camel camel, int winningAmount)
        {
            Console.WriteLine($"Payout: {camel.Name},${winningAmount}");

            if (GetTotalInventoryAmount() < winningAmount)
            {
                Console.WriteLine(string.Format(ErrorMessages.InsufficientFunds, winningAmount));
                return;
            }

            Console.WriteLine("Dispensing:");

            InitializePayoutBills();
            CalculateBillsForPayout(winningAmount);

            foreach (var payoutBill in payoutBills.OrderBy(x => x.Key))
            {
                Console.WriteLine($"${payoutBill.Key},{payoutBill.Value}");
            }
        }

        private static void CalculateBillsForPayout(int winningAmount)
        {
            if (winningAmount == 0)
            {
                return;
            }

            int highestDenominationInInventoryDenomination = inventories.Where(x => x.Quantity > 0 && winningAmount >= x.Denomination).Max(x => x.Denomination);
            Inventory highestDenominationInInventoy = inventories.Find(x => x.Denomination == highestDenominationInInventoryDenomination);

            payoutBills[highestDenominationInInventoryDenomination] += 1;
            highestDenominationInInventoy.Quantity -= 1;

            CalculateBillsForPayout(winningAmount - highestDenominationInInventoryDenomination);
        }

        private static void InitializePayoutBills()
        {
            payoutBills = new Dictionary<int, int>();
            foreach (Inventory inventory in inventories)
            {
                payoutBills.Add(inventory.Denomination, 0);
            }
        }

        private static int GetTotalInventoryAmount()
        {
            return inventories.Sum(x => x.Denomination * x.Quantity);
        }
    }
}
