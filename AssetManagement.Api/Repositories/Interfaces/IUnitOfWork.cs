namespace AssetManagement.Api.Repositories.Interfaces;

// Commits every change the repositories made in this request as ONE transaction.
// Repositories only stage changes (Add/Remove/modify tracked entities);
// the service decides when the whole unit of work is saved.
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync();
}
