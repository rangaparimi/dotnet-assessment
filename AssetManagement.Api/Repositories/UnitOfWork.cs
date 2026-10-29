using AssetManagement.Api.Data;
using AssetManagement.Api.Repositories.Interfaces;

namespace AssetManagement.Api.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _db;

    public UnitOfWork(AppDbContext db)
    {
        _db = db;
    }

    // All repositories share the same scoped AppDbContext, so this saves
    // everything they staged in a single transaction
    public Task<int> SaveChangesAsync() => _db.SaveChangesAsync(); //saves all the changes made in the database in a single transaction
}
