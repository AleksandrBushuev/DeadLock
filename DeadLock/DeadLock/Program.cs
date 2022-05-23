using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace DeadLock
{
    public class Account
    {
        public string Name { get; }
        private double Balance;
        public Account(string name, double balance)
        {
            Name = name;
            Balance = balance;
        }

        public void WithdrawMoney(double amount)
        {
            Balance -= amount;
        }
        public void DepositMoney(double amount)
        {
            Balance += amount;
        }
    }

    public class AccountManager
    {
        private Account FromAccount;
        private Account ToAccount;
        private double TransferAmount;
        public AccountManager(Account AccountFrom, Account AccountTo, double AmountTransfer)
        {
            FromAccount = AccountFrom;
            ToAccount = AccountTo;
            TransferAmount = AmountTransfer;
        }
        public void FundTransfer()
        {
            Console.WriteLine($"{Thread.CurrentThread.Name} начинает блокировку лицевого счета {FromAccount.Name}");
            lock (FromAccount)
            {
                Console.WriteLine($"{Thread.CurrentThread.Name} заблокировал личевой счет {FromAccount.Name}");
                Console.WriteLine($"{Thread.CurrentThread.Name} выполняет подготовку к переводу");
                Thread.Sleep(1000);
                Console.WriteLine($"{Thread.CurrentThread.Name} начинает блокировку лицевого счета  {ToAccount.Name}");
                lock (ToAccount)
                {
                    Thread.Sleep(1000);
                    Console.WriteLine($"{Thread.CurrentThread.Name} заблокировал личевой счет {ToAccount.Name}");
                    Console.WriteLine($"{Thread.CurrentThread.Name}  перевод денежных средств...");
                    FromAccount.WithdrawMoney(TransferAmount);
                    ToAccount.DepositMoney(TransferAmount);
                    Console.WriteLine($"{Thread.CurrentThread.Name}  перевод завершен");
                }
            }
        }

        public void FundTransferNoBlock()
        {
            Console.WriteLine($"{Thread.CurrentThread.Name} начинает блокировку лицевого счета {FromAccount.Name}");
            lock (FromAccount)
            {
                Console.WriteLine($"{Thread.CurrentThread.Name} заблокировал лицевой счет {FromAccount.Name}");
                Console.WriteLine($"{Thread.CurrentThread.Name} выполняет подготовку к переводу");
                Thread.Sleep(1000);
                Console.WriteLine($"{Thread.CurrentThread.Name} начинает блокировку лицевого счета  {ToAccount.Name}");

                if (Monitor.TryEnter(ToAccount, 3000))
                {
                    Console.WriteLine($"{Thread.CurrentThread.Name} заблокировал лицевой счет {ToAccount.Name}");
                    try
                    {
                        Console.WriteLine($"{Thread.CurrentThread.Name}  перевод денежных средств...");
                        FromAccount.WithdrawMoney(TransferAmount);
                        ToAccount.DepositMoney(TransferAmount);
                        Console.WriteLine($"{Thread.CurrentThread.Name}  перевод завершен");
                    }
                    finally
                    {
                        Monitor.Exit(ToAccount);
                    }
                }
                else
                {
                    Console.WriteLine($"{Thread.CurrentThread.Name} не удалось заблокировать {ToAccount.Name}");
                }
            }
        }

    }

    class Program
    {
        static void Main(string[] args)
        {
            Account accoun1 = new Account("Отправитель", 5000);
            Account accoun2 = new Account("Получатель", 3000);
            AccountManager accountManager1 = new AccountManager(accoun1, accoun2, 5000);

            Task task1 = new Task(() =>
            {
                Thread.CurrentThread.Name = "AccountManager1";
                accountManager1.FundTransfer();
                //accountManager1.FundTransferNoBlock();
            });


            AccountManager accountManager2 = new AccountManager(accoun2, accoun1, 6000);
            Task task2 = new Task(() =>
            {
                Thread.CurrentThread.Name = "AccountManager2";
                accountManager2.FundTransfer();
                //accountManager2.FundTransferNoBlock();
            });

            task1.Start();
            task2.Start();

            task1.Wait();
            task2.Wait();


            Console.WriteLine("Main метод завершен");
            Console.ReadKey();
        }
    }
}
