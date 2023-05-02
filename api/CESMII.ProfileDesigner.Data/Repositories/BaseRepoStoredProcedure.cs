namespace CESMII.ProfileDesigner.Data.Repositories
{
    using System;
    using System.Linq;
    using System.Collections.Generic;

    using Microsoft.EntityFrameworkCore;

    using CESMII.ProfileDesigner.Data.Entities;

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

        public IQueryable<TEntity> ExecStoredFunction(string fnName, int? timeout, int? skip, int? take, List<OrderBySimple> orderBys, params object[] parameters)
        {
            var orderByExpr = BuildOrderByList(orderBys);
            if (!string.IsNullOrEmpty(orderByExpr)) orderByExpr = $" ORDER BY {orderByExpr}";

            var sql = $"SELECT * FROM {fnName}({BuildParametersList(parameters == null ? 0 : parameters.Length)}) {orderByExpr}";

            //handle paging
            if (take.HasValue)
            {
                sql += $" LIMIT {take}";
            }
            if (skip.HasValue)
            {
                sql += $" OFFSET {skip}";
            }

            //wrap call in timeout setting if needed
            var existingTimeout = _context.Database.GetCommandTimeout();
            if (timeout.HasValue)
            {
                _context.Database.SetCommandTimeout(timeout);
            }
            try
            {
                return _context.Set<TEntity>().FromSqlRaw(sql, parameters);
            }
            finally
            {
                //restore timeout setting - if needed
                if (timeout.HasValue)
                {
                    _context.Database.SetCommandTimeout(existingTimeout);
                }
            }
        }

        public int ExecStoredFunctionCount(string fnName, params object[] parameters)
        {
            //assemble count call
            var sql = $"SELECT COUNT(*) FROM {fnName} ({BuildParametersList(parameters == null ? 0 : parameters.Length)})";
            return _context.Set<StoredProcedureCount>().FromSqlRaw(sql, parameters).ToList().FirstOrDefault().NumRows;
        }

        private string BuildParametersList(int length)
        {
            if (length == 0) return "";
            var result = new System.Text.StringBuilder();
            for (int i = 0; i < length; i++)
            {
                if (i > 0) result.Append($",");
                result.Append($"{{{i}}}");
            }
            return result.ToString();
        }

        private string BuildOrderByList(List<OrderBySimple> orderBys)
        {
            var result = new System.Text.StringBuilder();
            if (orderBys != null)
            {
                var counter = 0;
                foreach (var orderBy in orderBys)
                {
                    if (counter > 0) result.Append($",");
                    var desc = orderBy.IsDescending ? " DESC" : "";
                    result.Append($"{orderBy.FieldName}{desc}");
                    counter++;
                }
            }
            return result.ToString();
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

}