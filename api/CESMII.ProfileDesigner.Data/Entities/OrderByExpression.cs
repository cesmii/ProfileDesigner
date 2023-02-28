namespace CESMII.ProfileDesigner.Data.Entities
{
    using System;
    using System.Linq.Expressions;

    /// <summary>
    /// This works in Linq expressions / EF seamlessly
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    public class OrderByExpression<TEntity> where TEntity : AbstractEntity
    {
        public Expression<Func<TEntity, object>> Expression { get; set; }
        public bool IsDescending { get; set; } = false;
    }

    /// <summary>
    /// This is a simplistic order by object used to manually generate SQL to append to a
    /// stored function where an order by is appended to the execute statement
    /// </summary>
    public class OrderBySimple
    {
        public string FieldName { get; set; }
        public bool IsDescending { get; set; } = false;
    }


}