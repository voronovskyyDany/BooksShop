using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.ViewModels
{
    public class HomeViewModel
    {
        public List<Product> Products { get; set; }
        public List<Product> Recomended { get; set; }
        public List<Product> History { get; set; }
        public List<Category> Categories { get; set; }
    }
}
