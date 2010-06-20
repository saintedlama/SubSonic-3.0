using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SubSonic.Repository;
using SubSonic.SqlGeneration.Schema;
using SubSonic.DataProviders;
using SubSonic.Query;

namespace SubSonic.Runner
{
    public class Program
    {
        private static int Count = 10000;

        private static SimpleRepository Repo;

        static void Main(string[] args)
        {
            var provider = ProviderFactory.GetProvider("WestWind");
            //provider.Log = Console.Out;

            Repo = new SimpleRepository(provider, SimpleRepositoryOptions.RunMigrations);

            try
            {
                var qry = new CodingHorror(provider, "DROP TABLE MyOrders").Execute();
            }
            catch { }

            try
            {
                new CodingHorror(provider, "DROP TABLE MyOrderLines").Execute();
            }
            catch { }

            for (int i = 0; i < Count; i++)
            {
                SetupOrders();
            }

            var t1 = DateTime.Now;

            var orders = Repo.All<MyOrder>().ToList();

            long j = 0;

            foreach (var oder in orders)
            {
                j+=oder.OrderLines.Count;
                Console.WriteLine("State " + oder.OrderState.State);
            }

            var t2 = DateTime.Now;

            Console.WriteLine("Selecting {0} Order took {1} ms", Count, (t2 - t1).TotalMilliseconds);

            Console.ReadLine();
        }

        private static void SetupOrders()
        {
            var o = new MyOrder { Key = "MKL" };
            Repo.Add(o);

            var s = new MyOrderState { State = 1 };
            s.MyOrderId = o.Id;
            Repo.Add(s);

            Repo.Add(new MyOrderLine
            {
                MyOrderId = o.Id
            });
            Repo.Add(new MyOrderLine
            {
                MyOrderId = o.Id
            });
            Repo.Add(new MyOrderLine
            {
                MyOrderId = o.Id
            });
        }
    }

    

    public class MyOrder
    {
        public int Id { get; set; }
        public string Key { get; set; }

        [SubSonicReferencesAttribute]
        public virtual IList<MyOrderLine> OrderLines { get; set; }

        [SubSonicReferencesAttribute]
        public virtual MyOrderState OrderState { get; set; }
    }

    public class MyOrderLine
    {
        public int Id { get; set; }
        public int MyOrderId { get; set; }

        public string DoIt()
        {
            return String.Format("Id={0}, MyOrderId={1}", Id, MyOrderId);
        }
    }

    public class MyOrderState
    {
        public int Id { get; set; }
        public int MyOrderId { get; set; }

        public int State { get; set; }
    }
}
