namespace CESMII.ProfileDesigner.Data.Repositories
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Threading.Tasks;

    using CESMII.ProfileDesigner.Common.Utils;
    using CESMII.ProfileDesigner.Data.Entities;

    /// <summary>
    /// Get data from static JSON files loaded into individual files.
    /// Make this as similar in structure as possible to the real repo. 
    /// </summary>
    /// <remarks>
    /// This is just a stop gap until we come to a decision on the data storage approach.
    /// NOTE - This does not account or handle potential for locked file scenarios. 
    /// </remarks>
    /// <typeparam name="TEntity"></typeparam>
    public interface IMockRepository<TEntity> : IDisposable where TEntity : AbstractEntity
    {
        /// <summary>
        /// Gets collection of the items.
        /// </summary>
        //IQueryable<TEntity> Collection { get; }

        TEntity GetByID(int id);
        //TEntity GetByID(long id);

        /// <summary>
        /// Get all entities in a given DBSet. These must inherit from AbstractEntity.
        /// </summary>
        /// <returns>A list of the type passed in.</returns>
        IQueryable<TEntity> GetAll();

        IQueryable<TEntity> FindByCondition(Expression<Func<TEntity, bool>> expression);

        /// <summary>
        /// Execute a stored procedure with a return type of <T>AbstractEntity</T>
        /// </summary>
        /// <param name="query">The query to execute.</param>
        /// <param name="parameters">Additional parameters to execute.</param>
        /// <returns>A list of TEntity.</returns>
        //IQueryable<TEntity> ExecStoredProcedure(string query, params object[] parameters);

        /// <summary>
        /// Add an entry to the database set of <T>AbstractEntity</T>
        /// </summary>
        /// <param name="entity">The Generic Entity type. Must be an abstract entity.</param>
        /// <returns>Task of number of records updated.</returns>
        //Task<int> Add(TEntity entity);

        /// <summary>
        /// Update an entry.
        /// </summary>
        /// <param name="entity">The Generic Entity type. Must be an abstract entity.</param>
        /// <returns>Task for update. Imagine this should return the number of records.</returns>
        //Task Update(TEntity entity);

        /// <summary>
        /// Async save to the database.
        /// </summary>
        /// <returns>The Generic Entity type of the repository. Must be an abstract entity.</returns>
        //Task<int> SaveChanges();

        //Task<int> Delete(TEntity entity);
    }

    public class MockRepo<TEntity>: IMockRepository<TEntity> where TEntity : AbstractEntity
    {
        protected bool _disposed = false;
        
        //get path of the executing data dll and the hardcoded files live in a folder called mock data 
        string _path = System.Reflection.Assembly.GetExecutingAssembly().AssemblyDirectory();
        string _tEntityTypeName = typeof(TEntity).ToString();
        IQueryable<TEntity> _data;

        public MockRepo() {

            //TBD - some DALs may not have a JSON file yet...check for file existence first and don't throw exception in c'tor. 
            // Note if they try to call the methods below, they will get null exception on _data. 
            string fileName = $@"{_path}\mockdata\{_tEntityTypeName}.json";
            if (!File.Exists(fileName)) return;

            //load the data into class
            string json = File.ReadAllText(fileName);
            //deserialize into .NET structure
            _data = Newtonsoft.Json.JsonConvert.DeserializeObject<List<TEntity>>(json).AsQueryable();
        }

        public IQueryable<TEntity> GetAll()
        {
            return _data;
        }

        public IQueryable<TEntity> FindByCondition(Expression<Func<TEntity, bool>> expression)
        {
            return _data.Where(expression);
        }

        public TEntity GetByID(int id)
        {
            return _data.First(x => x.ID.Equals(id));
        }

        public virtual void Dispose()
        {
            if (_disposed) return;
            
            //clean up resources

            //set flag so we only run dispose once.
            _disposed = true;
        }


    }
}