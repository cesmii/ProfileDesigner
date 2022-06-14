namespace CESMII.ProfileDesigner.Data.Repositories
{
    using System;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Threading.Tasks;
    using CESMII.ProfileDesigner.Data.Entities;

    public interface IRepository<TEntity> : IDisposable where TEntity : AbstractEntity
    {
        /// <summary>
        /// Gets collection of the items.
        /// </summary>
        IQueryable<TEntity> Collection { get; }

        TEntity GetByID(int id);
        TEntity GetByID(long id);

        /// <summary>
        /// Get all entities in a given DBSet. These must inherit from AbstractEntity.
        /// </summary>
        /// <returns>A list of the type passed in.</returns>
        IQueryable<TEntity> GetAll();

        IQueryable<TEntity> FindByCondition(Expression<Func<TEntity, bool>> expression, bool cacheOnly = false);

        /// <summary>
        /// Execute a stored procedure with a return int
        /// </summary>
        /// <param name="query">The query to execute.</param>
        /// <param name="parameters">Additional parameters to execute.</param>
        Task<int> ExecStoredProcedureAsync(string query, params object[] parameters);
        void StartTransaction();
        Task CommitTransactionAsync();
        void RollbackTransaction();

        /// <summary>
        /// Add an entry to the database set of <T>AbstractEntity</T>
        /// </summary>
        /// <param name="entity">The Generic Entity type. Must be an abstract entity.</param>
        /// <returns>Task of number of records updated.</returns>
        Task<int> AddAsync(TEntity entity);

        /// <summary>
        /// Add an entry to the database set of <T>AbstractEntity</T> synchronously
        /// </summary>
        /// <param name="entity">The Generic Entity type. Must be an abstract entity.</param>
        /// <returns>Task of number of records updated.</returns>
        int Add(TEntity entity);

        /// <summary>
        /// Update an entry async.
        /// </summary>
        /// <param name="entity">The Generic Entity type. Must be an abstract entity.</param>
        /// <returns>Task for update. Imagine this should return the number of records.</returns>
        Task UpdateAsync(TEntity entity);

        /// <summary>
        /// Update an entry synch.
        /// </summary>
        /// <param name="entity">The Generic Entity type. Must be an abstract entity.</param>
        void Update(TEntity entity);

        /// <summary>
        /// Attaches an entity to the context so that it can be found on future queries even if it has not been saved yet (helps handling recursive references)
        /// </summary>
        /// <param name="entity"></param>
        void Attach(TEntity entity);

        /// <summary>
        /// Async save to the database.
        /// </summary>
        /// <returns>The Generic Entity type of the repository. Must be an abstract entity.</returns>
        Task<int> SaveChanges();

        Task<int> Delete(TEntity entity);
        Task LoadIntoCacheAsync(Expression<Func<TEntity, bool>> predicate);
    }
}