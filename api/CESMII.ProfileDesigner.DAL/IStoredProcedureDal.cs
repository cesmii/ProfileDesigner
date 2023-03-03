namespace CESMII.ProfileDesigner.DAL
{
    using System;
    using System.Collections.Generic;

    using CESMII.ProfileDesigner.DAL.Models;
    using CESMII.ProfileDesigner.Data.Entities;


    /// <summary>
    /// Used to call a stored function in DB and return rows of data
    /// Calling code needs knowledge of query and columns returned. 
    /// Order by values should use raw column names returned by stored function
    /// For instance, see <see cref="Data.Entities.ProfileTypeDefinitionSimple"/> for column names to use in order by expression.
    /// </summary>
    public interface IStoredProcedureDal<TModel> : IDisposable where TModel : AbstractModel
    {
        DALResult<TModel> GetItemsAll(string fnName, bool returnCount, List<OrderBySimple> orderBys, params object[] parameters);
        DALResult<TModel> GetItemsPaged(string fnName, int? skip, int? take, bool returnCount, List<OrderBySimple> orderBys, params object[] parameters);
        int Count(string fnName, params object[] parameters);
    }
}