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
        private static SimpleRepository Repo;

        static void Main(string[] args)
        {
            var provider = ProviderFactory.GetProvider("WestWind");

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
    
            for (int i = 0; i < 10000; i++)
            {
                SetupOrders();
            }

            var t1 = DateTime.Now;

            var orders = Repo.All<MyOrder>().ToList();

            var t2 = DateTime.Now;

            Console.WriteLine("Selecting 10000 Order took {0}ms", (t2 - t1).TotalMilliseconds);

            Console.ReadLine();
        }

        private static void SetupOrders()
        {
            var o = new MyOrder { Key = "MKL" };
            Repo.Add(o);

            Repo.Add(new MyOrderLine
            {
                OrderId = o.Id
            });
            Repo.Add(new MyOrderLine
            {
                OrderId = o.Id
            });
            Repo.Add(new MyOrderLine
            {
                OrderId = o.Id
            });
        }
    }

    

    public class MyOrder
    {
        public int Id { get; set; }
        public string Key { get; set; }
        public IList<MyOrderLine> OrderLines { get; set; }
    }

    public class MyOrderLine
    {
        public int Id { get; set; }

        [SubSonicForeignKey()]
        public int OrderId { get; set; }
    }
}
