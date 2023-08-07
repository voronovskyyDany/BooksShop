using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.ViewModels
{
    public class ProductsCategoryViewModel
    {
        public List<Product> Products { get; set; }
        public Category Category { get; set; }
    }
}
