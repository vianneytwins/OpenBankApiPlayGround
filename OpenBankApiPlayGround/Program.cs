namespace OpenBankApiPlayGround
{
	using System;

	using Connectors.OBP;

	using Newtonsoft.Json;
	using System.Linq;
	using System.Collections.Generic;
	using System.IO;

	public class Program
	{
		/*
static string bankId = "gh.29.fr";
static string accountId = "09beccaf-0e0d-4771-be6a-42206fe06c33";
static string password = "79a0cb";
static string user_name = "elise.fr.29@example.com";
*/
		/*
		// OpenSandbox
		private const string HttpsApisandboxOpenbankprojectCom = "https://apisandbox.openbankproject.com/";
		static string consumerKey = "ztgbqrbnrzkbgl0eyz5ji5lwxspjgm45rbmoaa0i";
		private const string UserFilePath = @"french_users.json";*/
		
		//SG Sandbox
		private const string HttpsApisandboxOpenbankprojectCom = "https://socgen-p-api.openbankproject.com/";
		static string consumerKey = "n2hyr3muugtzrrkvqz1hrovge4etjdshisxy4wj0";
		private const string UserFilePath = @"sg_users.json";
		
		static void Main(string[] args)
		{
			//Console.BufferHeight = short.MaxValue - 1;

			var client = new ObpRestClient(HttpsApisandboxOpenbankprojectCom);

			var users = GetUsers();

			foreach (var user in users)
			{
				client.Login(user.email, user.password, consumerKey);
				Console.WriteLine("");
				Console.WriteLine("==============================");
				Console.WriteLine("stats for :" + user.email);

				GetMinAndMaxDateOfTransactions(client);
			}
			//Test(client, bankId, accountId, viewId, transactionRequestType, description, currency);

			Console.ReadLine();
		}

		private static List<ObpUser> GetUsers()
		{
			return JsonConvert.DeserializeObject<List<ObpUser>>(File.ReadAllText(UserFilePath));
		}

		private static void Test(
			ObpRestClient client,
			string bankId,
			string accountId,
			string viewId,
			string transactionRequestType,
			string description,
			string currency)
		{
			Test("GetAccountByIdCore", () => client.GetAccountByIdCore(bankId, accountId));
			//Test("GetAccountsAtBank", () => client.GetAccountsAtBank(bankId));
			Test("GetAccountByIdFull", () => client.GetAccountByIdFull(bankId, accountId, viewId));
			Test("GetAccounts", client.GetAccounts);
			//Test("GetTransactionById",() => client.GetTransactionById(bankId, accountId, viewId, "f64ae303-5be6-45e1-9bba-9a7dbbf11ccf"));
			Test("GetTransactionsForAccountCore", () => client.GetTransactionsForAccountCore(bankId, accountId));
			Test("GetTransactionsForAccountFull", () => client.GetTransactionsForAccountFull(bankId, accountId, viewId));
			Test("GetAccountsAtAllBanksPublic", client.GetAccountsAtAllBanksPublic);
			Test("GetTransactionTypesOfferedByBankResponse", () => client.GetTransactionTypesOfferedByBankResponse(bankId));
			Test(
				"GetTransactionRequestTypesForAccount",
				() => client.GetTransactionRequestTypesForAccount(bankId, accountId, viewId));
			Test("GetTransactionRequests", () => client.GetTransactionRequests(bankId, accountId, viewId));
			Test("GetRoles", client.GetRoles);

			/*Test(
				"CreateTransactionRequest",
				() =>
				client.CreateTransactionRequest(
					bankId,
					accountId,
					viewId,
					transactionRequestType,
					new CreateTransactionRequestRequest
					{
						To =
								new TransactionRequestAccount
								{
									AccountId = accountId,
									BankId = bankId
								},
						Description = description,
						Value =
								new AmountOfMoney
								{
									Amount = 1000m,
									Currency = currency
								}
					}));*/
			
		}

		public static void Test(string name, Func<object> method)
		{
			Console.WriteLine(name);
			Console.ReadLine();
			var result = method();
			Console.WriteLine(JsonConvert.SerializeObject(result, Formatting.Indented));
			Console.WriteLine();
		}

		static void GetMinAndMaxDateOfTransactions(ObpRestClient client)
		{
			var accounts = client.GetAccounts();
			foreach (var account in accounts)
			{
				var accountBalance = client.GetAccountByIdCore(account.BankId, account.Id);
				Console.WriteLine("your account "+ accountBalance.Label+ " of type "+ accountBalance.Type +" has "+ accountBalance.Balance.Amount +" "+accountBalance.Balance.Currency);
				                 
				var result = client.GetTransactionsForAccountCore(account.BankId, account.Id);
				
				if (result != null && result.Transactions.Length > 0)
				{
					var transactions = result.Transactions;
					Console.WriteLine("stats for this account : Bank -" + account.BankId + " - account id: " + account.Id);
					
					GetMinAndMaxTransactionsDates(transactions);
					
					SumAllIncomesAndExpenses(transactions);

					GetIncomesAndExpenseByMonth(transactions);

					//GetReccuringIncomesAndExpenses(transactions);
					
					Console.WriteLine("");
				}
				else
				{
					Console.WriteLine("this account is empty : Bank -" + account.BankId + " - account id: "+ account.Id);
				}
			}
		}

		private static void GetReccuringIncomesAndExpenses(TransactionCore[] transactions)
		{
			var recurringTransactions =
            				//transactions.GroupBy(x => x.Counterparty.Id).Where(x => x.Count()> 1);
						transactions.GroupBy(x => x.Details.Value.Amount +"_"+ x.Counterparty.Id).Where(x => x.Count()> 1);
			foreach (var counterparty in recurringTransactions)
			{
				Console.WriteLine("this Amount " + counterparty.Key + " has " + counterparty.Count() + " occurences");
				foreach (var transactionCore in counterparty)
				{
					//TODO faire une plus belle methode de tostring sur transactionCore..ou extension
					Console.WriteLine("cpty" + transactionCore.Counterparty.Holder.Name + ", desc:" + transactionCore.Details.Description + ", date : " + transactionCore.Details.Completed);
				}
			}
			//enelever les non-monthly en faisant un groupby month avant et enlevant ce qui sont pas dedans ?
		}

		private static void SumAllIncomesAndExpenses(TransactionCore[] transactions)
		{
			Console.WriteLine("Incomes " +
			                  transactions.Where(x => x.Details.Value.Amount > 0).Sum(x => x.Details.Value.Amount));
			Console.WriteLine(
				"Spending " + transactions.Where(x => x.Details.Value.Amount < 0).Sum(x => x.Details.Value.Amount));
		}

		private static void GetIncomesAndExpenseByMonth(TransactionCore[] transactions)
		{
			var incomeGroupbyMonth =
				transactions.Where(x => x.Details.Value.Amount > 0).GroupBy(x => x.Details.Completed.Month);
			foreach (var month in incomeGroupbyMonth)
			{
				Console.WriteLine("this month " + month.Key + " you've earned " + month.Sum(x => x.Details.Value.Amount));
			}

			var spendingGroupbyMonth =
				transactions.Where(x => x.Details.Value.Amount < 0).ToLookup(x => x.Details.Completed.Month);
			foreach (var month in spendingGroupbyMonth)
			{
				Console.WriteLine("this month " + month.Key + " you've spend " + month.Sum(x => x.Details.Value.Amount));
			}
		}

		private static void GetMinAndMaxTransactionsDates(TransactionCore[] transactions)
		{
			Console.WriteLine(transactions.Min(x => x.Details.Completed));
			Console.WriteLine(transactions.Max(x => x.Details.Completed));
		}
		
		// TODO : compare my average spending to others
		private string GetCompareMySpendingToOthers()
		{
			return String.Empty;
		}
	}
}
