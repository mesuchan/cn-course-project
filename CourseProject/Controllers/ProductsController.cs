using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CourseProject.Models;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace CourseProject.Controllers
{
    [Route("api/products")]
    public class ProductsController : Controller
    {
        private readonly Context context;
        private readonly UserManager<IdentityUser> userManager;

        public ProductsController(Context context, UserManager<IdentityUser> userManager)
        {
            this.context = context;
            this.userManager = userManager;
        }

        // GET: api/<controller>
        [HttpGet]
        public IEnumerable<Product> Get()
        {
            return context.Products.Select(p => p);
        }

        // GET: api/<controller>
        [HttpGet("{id}")]
        public ActionResult<Product> Get(int id)
        {
            var product = context.Products.Select(p => p).Include(p => p.Sizes).Where(p => p.ProductId == id).FirstOrDefault();
            if (product == null)
                return NotFound();
            return product;
        }

        [Authorize]
        [HttpPost("purchase")]
        public async Task<ActionResult> PurchaseAsync([FromBody] Purchase value)
        {
            var user = await userManager.GetUserAsync(HttpContext.User);
            var customer = await Customer.GetOrCreateAsync(context, user);
            var purchase = new Purchase
            {
                Customer = customer
            };
            context.Add(purchase);
            await context.SaveChangesAsync();
            foreach (var pp in value.PurchaseProducts)
            {
                pp.PurchaseId = purchase.PurchaseId;
                
                var size = context.Sizes.Find(pp.ProductId, pp.Size);
                size.Quantity--;
                context.Update(size);

                var v = context.Vieweds.Find(customer.CustomerId, pp.ProductId);
                if (v != null)
                {
                    context.Remove(v);
                    context.SaveChanges();
                }
            }
            context.AddRange(value.PurchaseProducts);

            await context.SaveChangesAsync();
            return Ok(purchase);
        }
        
        [Authorize]
        [HttpPost("{id}")]
        public async Task<ActionResult> View(int id)
        {
            var p = context.Products.Find(id);
            if (p == null)
                return NotFound();

            var user = await userManager.GetUserAsync(HttpContext.User);
            var customer = await Customer.GetOrCreateAsync(context, user);

            var v = context.Vieweds.Find(customer.CustomerId, id);
            if (v == null)
            {
                v = new Viewed { CustomerId = customer.CustomerId, ProductId = id, Count = 0 };
                context.Add(v);
            }
            else
                context.Update(v);

            v.Count++;

            await context.SaveChangesAsync();

            return Ok();
        }

        [Authorize]
        [HttpGet("recommended")]
        public async Task<ActionResult> Recommended()
        {
            var user = await userManager.GetUserAsync(HttpContext.User);
            var customer = await Customer.GetOrCreateAsync(context, user);

            var r = from v in context.Vieweds.Include(v => v.Product)
                    where v.CustomerId == customer.CustomerId
                    orderby v.Count descending
                    select v.Product;

            return Ok(r);
        }

        [HttpGet("export")]
        public ActionResult Export()
        {
            var tmp = context.Products.Include(p => p.Sizes).Include(p => p.Fabrics).Select(p => new { p.ProductId, Name = p.Name.Trim(' ', '\n'), p.Price, p.Color, p.Country, p.Description });

            StringWriter streamWriter = new StringWriter();
            using (CsvWriter csvWriter = new CsvWriter(streamWriter, new CsvConfiguration(new System.Globalization.CultureInfo("ru-RU", false))))
            {
                csvWriter.Configuration.Delimiter = ";";
                csvWriter.WriteRecords(tmp);
            }

            return Ok(streamWriter.ToString());
        }
    }
}
