using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class UserRepository : IRepository<User>
{
    private readonly AppDbContext _db;
    public UserRepository(AppDbContext db) => _db = db;

    public async Task AddAsync(User entity) => await _db.Users.AddAsync(entity);

    public Task<User?> GetByIdAsync(Guid id) => _db.Users.Include(u => u.RefreshTokens).FirstOrDefaultAsync(u => u.Id == id);

    public Task<User?> SingleOrDefaultAsync(Func<User, bool> predicate) => Task.FromResult(_db.Users.AsEnumerable().SingleOrDefault(predicate));

    public async Task<IEnumerable<User>> ListAsync() => await _db.Users.Include(u => u.RefreshTokens).ToListAsync();

    public void Remove(User entity) => _db.Users.Remove(entity);

    public void Update(User entity) => _db.Users.Update(entity);
}
