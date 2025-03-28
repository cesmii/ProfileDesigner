﻿namespace CESMII.ProfileDesigner.Data.Repositories
{
    using System;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Threading.Tasks;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;

    using CESMII.ProfileDesigner.Data.Entities;

    //static class CacheCounters
    //{
    //    public static long cacheHitCount = 0;
    //    public static long cacheMissCount = 0;
    //    public static long dbMissCount = 0;
    //    public static List<object> dbMisses = new List<object>();
    //    public static List<object> cacheMisses = new List<object>();
    //}

    public class BaseRepo<TEntity, TContext> : IRepository<TEntity> where TEntity : AbstractEntity where TContext : DbContext
    {
        protected bool _disposed = false;
        private readonly DbContext _context;

        //private IQueryable<TEntity> _collection;

        public BaseRepo(TContext context, IConfiguration configuration)
        {
            // Grab a DI of the context provided in startup.
            _context = context;

            // To allow for larger data sets for certain clients, extend the timeout period.
            //_context.Database.SetCommandTimeout(int.Parse(configuration["DataRepoSettings:CommandTimeout"]));
            
            // Tell the context to include the entity type provided.
            _context.Set<TEntity>();
        }

        // Be careful of lifecycle (e.g. make sure to destroy this as the user will start sharing the collection.
        //public IQueryable<TEntity> Collection => _collection ?? (_collection = _context.Set<TEntity>());
        public IQueryable<TEntity> Collection => _context.Set<TEntity>();

        public void StartTransaction()
        {
            if (_context.Database.CurrentTransaction != null)
            {
                return;
            }
            _context.Database.BeginTransaction();
            _context.ChangeTracker.AutoDetectChangesEnabled = false;
        }

        public async Task CommitTransactionAsync()
        {
            _context.ChangeTracker.DetectChanges();
            await _context.SaveChangesAsync();
            _context.ChangeTracker.AutoDetectChangesEnabled = true;
            await _context.Database.CommitTransactionAsync();
            //if (!_context.ChangeTracker.HasChanges())
            //{
            //    _context.ChangeTracker.Clear();
            //}
        }

        public void RollbackTransaction()
        {
            if (_context.Database.CurrentTransaction == null)
            {
                return;
            }
            _context.Database.RollbackTransaction();
        }

        public TEntity GetByID(int id)
        {
            // TODO DOUBLE CHECK THIS ASA WELL
            // return Collection.Single(t => t.ID == id);
            return _context.Set<TEntity>().Find(id);
        }

        public TEntity GetByID(long id)
        {
            // TODO DOUBLE CHECK THIS ASA WELL
            // return Collection.Single(t => t.ID == id);
            return _context.Set<TEntity>().Find(id);
        }

        public IQueryable<TEntity> GetAll()
        {
            return _context.Set<TEntity>();
        }

        public IQueryable<TEntity> FindByCondition(Expression<Func<TEntity, bool>> expression, bool cacheOnly = false)
        {
            if (_context.Database.CurrentTransaction != null || cacheOnly)
            {
                // Use the Tracked Changes first if under transaction: doesn't work reliably otherwise (ChangeTracker collection changed exception)
                IQueryable<TEntity> query;
                int retryCount = 50;
                do
                {
                    try
                    {
                        query = _context.Set<TEntity>().Local.AsQueryable().Where(expression);
                        if (cacheOnly || query?.Any() == true)
                        {
                            //CacheCounters.cacheHitCount++;
                            return query.AsQueryable();
                        }
                        retryCount = 0;
                    }
                    catch (InvalidOperationException)
                    {
                        // For some reason (likely on-demand data loading of data) this throws due to modified collection
                        retryCount--;
                        if (retryCount <= 0)
                        {
                            throw;
                        }
                    }
                } while (retryCount > 0);
            }
            var query2 = _context.Set<TEntity>().Where(expression);
            //if (!query2.Any())
            //{
            //    CacheCounters.dbMissCount++;
            //    CacheCounters.dbMisses.Add(expression);
            //}
            //else
            //{
            //    CacheCounters.cacheMissCount++;
            //    CacheCounters.cacheMisses.AddRange(query2.ToList());
            //}
            return query2;
        }

        public Task<int> ExecStoredProcedureAsync(string query, int? timeout, params object[] parameters)
        {
            var existingTimeout = _context.Database.GetCommandTimeout();
            if (timeout.HasValue)
            {
                _context.Database.SetCommandTimeout(timeout);
            }
            try
            {
                return _context.Database.ExecuteSqlRawAsync(query, parameters);
                //return _context.Set<TEntity>().FromSqlRaw(query, parameters);
            }
            finally {
                //restore timeout setting - if needed
                if (timeout.HasValue)
                {
                    _context.Database.SetCommandTimeout(existingTimeout);
                }
            }
        }
        public async Task<int> AddAsync(TEntity entity)
        {
            var entry = _context.Entry(entity);
            if (entry?.State == EntityState.Detached)
            {
                // Only add if not already tracked
                await _context.Set<TEntity>().AddAsync(entity);
            }
            if (_context.Database.CurrentTransaction != null) return 0;
            return await _context.SaveChangesAsync();
        }

        public int Add(TEntity entity)
        {
            _context.Set<TEntity>().Add(entity);
            if (_context.Database.CurrentTransaction != null) return 0;
            return _context.SaveChanges();
        }

        public async Task UpdateAsync(TEntity entity)
        {
            if (entity.ID != null && entity.ID != 0)
            {
                _context.Set<TEntity>().Update(entity);
            }
            else
            {
                // Entity not written yet: updating in the cache is sufficient
            }
            if (_context.Database.CurrentTransaction != null) return;
            await _context.SaveChangesAsync();
        }

        public void Update(TEntity entity)
        {
            _context.Set<TEntity>().Update(entity);
            if (_context.Database.CurrentTransaction != null) return;
            _context.SaveChanges();
        }

        public void Attach(TEntity entity)
        {
            _context.Attach(entity);
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public async Task<int> DeleteAsync(TEntity entity)
        {
            _context.Remove(entity);
            if (_context.Database.CurrentTransaction != null) return 0;
            return await _context.SaveChangesAsync();
        }

        public async Task ReloadAsync(TEntity entity)
        {
            await _context.Entry(entity).ReloadAsync();
        }

        public virtual void Dispose()
        {
            if (_disposed) return;
            //clean up resources
            _context.Dispose();
            //set flag so we only run dispose once.
            _disposed = true;
        }

        public Task LoadIntoCacheAsync(Expression<Func<TEntity, bool>> predicate)
        {
            return _context.Set<TEntity>().Where(predicate).LoadAsync();
        }
    }
}