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
    public interface IRepositoryStoredProcedure<TEntity> : IDisposable where TEntity : AbstractEntity
    {
        IQueryable<TEntity> ExecStoredFunction(string fnName, int? timeout, int? skip, int? take, List<OrderBySimple> orderBys, params object[] parameters);
        int ExecStoredFunctionCount(string fnName, params object[] parameters);  //TBD - make this a long at some point
    }

}