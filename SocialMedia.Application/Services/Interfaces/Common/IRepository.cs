using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocialMedia.Application.Services.Interfaces.Common
{
    public interface IRepository<T> where T:class
    {
        Task<T?> GetByIdAsync(object id);
        void Add(T entity);
        void Update(T entity);
        void Delete(T entity);
        Task<bool> SaveChangesAsync();
    }
}
