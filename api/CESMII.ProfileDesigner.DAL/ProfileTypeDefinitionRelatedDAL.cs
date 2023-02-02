namespace CESMII.ProfileDesigner.DAL
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.EntityFrameworkCore;

    using NLog;

    using CESMII.ProfileDesigner.DAL.Models;
    using CESMII.ProfileDesigner.Data.Entities;
    using CESMII.ProfileDesigner.Common.Enums;



    public enum StoreProcedureTypeEnum
    {
        TypeDefDescendants,
        TypeDefAncestors,
        TypeDefDependencies,
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    public interface IRepositoryStoredProcedure<TEntity> : IDisposable where TEntity : AbstractEntity
    {
        IQueryable<TEntity> ExecStoredProcedureAsync(string sql, params object[] parameters);
        //DALResult<TModel> ExecuteStoredFunction(StoreProcedureTypeEnum fnType, List<string> arguments, 
        //    int? skip = null, int? take = null);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <typeparam name="TContext"></typeparam>
    public class BaseRepoStoredProcedure<TEntity, TContext> : IRepositoryStoredProcedure<TEntity> where TContext : DbContext where TEntity : AbstractEntity
    {
        protected bool _disposed = false;
        //private readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private readonly DbContext _context;

        public BaseRepoStoredProcedure(TContext context) //, IConfiguration configuration)
        {
            _context = context;
            //TBD - we may tap into this type of thing in case we need to extend timeout
            //_context.Database.SetCommandTimeout(int.Parse(configuration["ReportSettings:CommandTimeout"]));
        }
        public IQueryable<TEntity> ExecStoredProcedureAsync(string sql, params object[] parameters)
        {
            return _context.Set<TEntity>().FromSqlRaw(sql, parameters);
        }

        public void Dispose()
        {
            if (_disposed) return;
            //clean up resources
            _context.Dispose();
            //set flag so we only run dispose once.
            _disposed = true;
        }

    }

    public interface IStoredProcedureDal<TModel> : IDisposable where TModel : AbstractModel
    {
        DALResult<TModel> ExecuteStoredProcedureGetItems(int? skip = null, int? take = null, bool returnCount = false, params object[] parameters);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <typeparam name="TModel"></typeparam>
    public abstract class BaseStoredProcedureDAL<TEntity, TModel> where TEntity : AbstractEntity, new() where TModel : AbstractModel
    {
        protected bool _disposed = false;
        protected static readonly Logger _logger = LogManager.GetCurrentClassLogger();
        protected readonly IRepositoryStoredProcedure<TEntity> _repo;

        protected BaseStoredProcedureDAL(IRepositoryStoredProcedure<TEntity> repo)
        {
            _repo = repo;
        }

        /// <summary>
        /// Map from entity retrieved from db to model used by front end. 
        /// </summary>
        protected virtual TModel MapToModel(TEntity entity)
        {
            throw new NotImplementedException();
        }

        protected virtual DALResult<TModel> ExecuteStoredProcedureGetItems(string sql, int? skip = null, int? take = null, bool returnCount = false, params object[] parameters)
        {
            var query = _repo.ExecStoredProcedureAsync(sql, parameters);
            var count = returnCount ? query.Count() : 0;

            //handle paging
            IQueryable<TEntity> data;
            if (skip.HasValue && take.HasValue) data = query.Skip(skip.Value).Take(take.Value);
            else if (skip.HasValue) data = query.Skip(skip.Value);
            else if (take.HasValue) data = query.Take(take.Value);
            else data = query;
            
            var result = new DALResult<TModel>
            {
                Count = count
            };
            result.Data = MapToModels(data.ToList());
            return result;
        }

        /// <summary>
        /// Map from entity retrieved from db to model used by front end. 
        /// </summary>
        protected virtual List<TModel> MapToModels(List<TEntity> entities)
        {
            var result = new List<TModel>();

            foreach (var item in entities)
            {
                result.Add(MapToModel(item));
            }
            return result;
        }

        public virtual void Dispose()
        {
            if (_disposed) return;
            //clean up resources
            _repo.Dispose();
            //set flag so we only run dispose once.
            _disposed = true;
        }

    }

    /// <summary>
    /// 
    /// </summary>
    public class ProfileTypeDefinitionRelatedDAL : 
        BaseStoredProcedureDAL<ProfileTypeDefinitionSimple, ProfileTypeDefinitionSimpleModel>, 
        IStoredProcedureDal<ProfileTypeDefinitionSimpleModel>
    {
        public ProfileTypeDefinitionRelatedDAL(IRepositoryStoredProcedure<ProfileTypeDefinitionSimple> repo) : base(repo)
        {
        }

        public DALResult<ProfileTypeDefinitionSimpleModel> ExecuteStoredProcedureGetItems(int? skip = null, int? take = null, bool returnCount = false, params object[] parameters)
        {
            string sql = "SELECT * FROM public.fn_profile_type_definition_get_descendants({0}, {1}) d;";
            return base.ExecuteStoredProcedureGetItems(sql, skip, take, returnCount, parameters);
        }

        protected override ProfileTypeDefinitionSimpleModel MapToModel(ProfileTypeDefinitionSimple entity)
        {
            if (entity != null)
            {
                return new ProfileTypeDefinitionSimpleModel
                {
                    ID = entity.ID,
                    Name = entity.Name,
                    BrowseName = entity.BrowseName,
                    Profile = new ProfileModel() { ID = entity.ProfileId, Namespace = entity.ProfileNamespace, Version = entity.ProfileVersion },
                    Description = entity.Description,
                    Type = new LookupItemModel() { ID = entity.TypeId, Name = entity.TypeName } ,
                    Author = new UserSimpleModel() { ID = entity.AuthorId },
                    OpcNodeId = entity.OpcNodeId,
                    IsAbstract = entity.IsAbstract,
                    Level = entity.Level
                };
            }
            else
            {
                return null;
            }

        }

        /*
        #region Stored Function Calls
        /// <summary>
        /// </summary>
        /// <param name="items"></param>
        /// <param name="orgId"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public DALResult<ProfileTypeDefinitionRelatedModel_NEW> ExecuteStoredFunction(StoreProcedureTypeEnum fnType, int id, UserToken userId, int? skip = null, int? take = null)
        {
            var s1 = new Npgsql.NpgsqlParameter("_date", NpgsqlDbType.Date)
            {
                Direction = System.Data.ParameterDirection.Input,
                Value = DateTime.Now.Date
            };

            var command = await _context..Set<ProfileTypeDefinitionRelatedModel_NEW>().from.ex.FromSqlRaw($"SELECT * from get_aggregate_balance(@s1)", s1).ToListAsync();

            var command = await _context.Database.FromSqlRaw($"SELECT * from get_aggregate_balance(@s1)", s1).ToListAsync();


            string entityName = upsertType.ToString();
            string upsertSproc;

            string parameters = 
            string fnCall = $"SELECT * FROM public.fn_profile_type_definition_get_descendants({id}, {userId.UserId}) d " + ;
                              "WHERE d.level < 2
            ORDER BY d.level, d.name
            LIMIT 10 OFFSET 10;

            switch (upsertType)
            {
                case StoreProcedureTypeEnum.TypeDefDescendants:
                    upsertSproc = $"amz.Save{entityName} @orgId, @profileId, @entryDate, @data";
                    break;
                case StoreProcedureTypeEnum.TypeDefDependencies:
                case StoreProcedureTypeEnum.TypeDefAncestors:
                    upsertSproc = $"amz.Save{entityName} @orgId, @profileId, @entryDate, @advTypeId, @data";
                    break;
                default:
                    upsertSproc = $"amz.Save{entityName} @orgId, @profileId, @data";
                    break;
            }

            int count = 0;
            int errCount = 0;
            foreach (var item in items)
            {
                string json = JsonConvert.SerializeObject(item);
                try
                {
                    SqlParameter[] parms;
                    switch (upsertType)
                    {
                        case UpsertTypeEnum.ProductAdDailyInfo:
                            parms = GetSqlParametersEtl(orgId, profileId, userId, json, entryDate, null).ToArray();
                            break;
                        case UpsertTypeEnum.KeywordDailyInfo:
                        case UpsertTypeEnum.TargetDailyInfo:
                        case UpsertTypeEnum.KeywordSearchTermDailyInfo:
                        case UpsertTypeEnum.TargetSearchTermDailyInfo:
                            parms = GetSqlParametersEtl(orgId, profileId, userId, json, entryDate, advTypeId).ToArray();
                            break;
                        default:
                            parms = GetSqlParametersEtl(orgId, profileId, userId, json, null, null).ToArray();
                            break;
                    }
                    var result = this.ExecStoredProcedureUpsert(upsertSproc, parms);
                    if (result == 0)
                    {
                        //return value of 0 indicates an issue. 
                        _logger.Log(NLog.LogLevel.Error, $"AmazonEtlDAL.UpsertItems.{entityName} - item was not upserted. Check JSON format: {json}");
                        errCount++;
                    }
                    else
                    {
                        count++;
                    }
                }
                catch (Exception ex)
                {
                    //return value of 0 indicates an issue. 
                    _logger.Log(NLog.LogLevel.Error, $"AmazonEtlDAL.UpsertItems.{entityName} - unexpected error.  Item: {json}. {System.Environment.NewLine}Exception:{ex} ");
                    errCount++;
                }
            }
            _logger.Log(NLog.LogLevel.Info, $"AmazonEtlDAL.UpsertItems.{entityName} - {count} items processed.");
            if (errCount > 0)
            {
                _logger.Log(NLog.LogLevel.Info, $"AmazonEtlDAL.UpsertItems.{entityName} - {errCount} items failed.");
            }
            return count;
        }

        public int ExecStoredProcedureUpsert(string query, params object[] parameters)
        {
            return _context.Database.ExecuteSqlCommand($"exec {query}", parameters);
        }

        protected virtual List<SqlParameter> GetSqlParametersEtl(long orgId, long profileId, long userId, string json)
        {
            return GetSqlParametersEtl(orgId, profileId, userId, json, null, null);
        }

        protected virtual List<SqlParameter> GetSqlParametersEtl
        (
            long orgId,
            long profileId,
            long userId,
            string json,
            DateTime? entryDate,
            int? advTypeId
        )
        {
            List<SqlParameter> retList = new List<SqlParameter>
            {
                //TBD - change this and the stored procs over to big int
                new SqlParameter("orgId", SqlDbType.Int) { Value = orgId },
                new SqlParameter("profileId", SqlDbType.BigInt) { Value = profileId },
                new SqlParameter("data", SqlDbType.VarChar) { Value = json }
                //new SqlParameter("userId", SqlDbType.Int) { Value = userId }
            };

            if (entryDate.HasValue)
            {
                retList.Add(new SqlParameter("entryDate", SqlDbType.Date) { Value = entryDate.Value });
            }

            if (advTypeId.HasValue)
            {
                retList.Add(new SqlParameter("advTypeId", SqlDbType.Int) { Value = advTypeId.Value });
            }

            return retList;
        }

        #endregion
        */
    }

}