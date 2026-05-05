using System.Threading.Tasks;
using Domain.Interfaces;
using Infrastructure.Data;

namespace Infrastructure;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _db;
    public UnitOfWork(AppDbContext db) => _db = db;
    public Task<int> SaveChangesAsync() => _db.SaveChangesAsync();
}
