using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CourseProject.Models;

using Autofac;
using Microsoft.EntityFrameworkCore;

namespace ShopGrabber
{
    class Program
    {
        static void Main(string[] args)
        {
            var builder = new ContainerBuilder();

            builder.RegisterType<HMGrabber>().As<IGrabber>();
            builder.RegisterType<GenerateCustomerRandom>().As<ICustomerGenerator>();
            builder.RegisterType<GeneratePurchaseRandom>().As<IPurchaseGenerator>();
            builder.RegisterType<GeneratePurchaseProduct>().As<IPurchaseProductGenerator>();
            var optionsBuilder = new DbContextOptionsBuilder<Context>();
            optionsBuilder.UseSqlServer("Server=DESKTOP-3JGDKKC;Database=shop;User=elena;Password=1234;Trusted_Connection=True;");
            builder.RegisterInstance(new Context(optionsBuilder.Options));

            builder.RegisterType<EFPutter>().As<IPutter>();

            using (var container = builder.Build())
            {
                var grabber = container.Resolve<IGrabber>();
                List<Product> products = grabber.GrabProducts(100);
                Console.WriteLine("Products were done!!");
                var customersGenerator = container.Resolve<ICustomerGenerator>();
                List<Customer> customers = customersGenerator.GenerateCustomers(100);
                Console.WriteLine("Customers were done!!");
                var purchasesGenerator = container.Resolve<IPurchaseGenerator>();
                List<Purchase> purchases = purchasesGenerator.GeneratePurchase(100);
                Console.WriteLine("Purchases were done!!");
                var purchaseProductsGenerator = container.Resolve<IPurchaseProductGenerator>();
                List<PurchaseProduct> purchaseProducts = purchaseProductsGenerator.GeneratePurchaseProducts(100);
                Console.WriteLine("ProductPurchases were done!!");

                var putter = container.Resolve<IPutter>();
                putter.PutProducts(products);
                putter.PutSizes(products);
                putter.PutFabrics(products);
                //putter.PutCustomers(customers);
                //putter.PutPurchases(purchases);
                //putter.PutPurchaseProducts(purchaseProducts);
            }
        }
    }
}
