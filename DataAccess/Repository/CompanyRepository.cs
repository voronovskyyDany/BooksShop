using DataAccess.Data;
using DataAccess.Repository.IRepository;
using Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.Repository
{
public class CompanyRepository : Repository<Company>, ICompanyRepository
{

    public CompanyRepository(ApplicationDbContext db) : base(db){}

    public void Update(Company company)
    {
        _db?.Companies?.Update(company);
    }
}
}
