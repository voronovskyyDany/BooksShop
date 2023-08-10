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
    public class CommentRepository : Repository<Comment>, ICommentRepository
    {

        public CommentRepository(ApplicationDbContext db) : base(db){}

        public void Update(Comment comment)
        {
            _db?.Comments?.Update(comment);
        }
    }
}
