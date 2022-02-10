namespace CESMII.ProfileDesigner.Data.Entities
{
    using System;
    using System.Linq.Expressions;

    public class OrderByExpression<TEntity> where TEntity : AbstractEntity
    {
        public Expression<Func<TEntity, object>> Expression { get; set; }
        public bool IsDescending { get; set; } = false;
    }

}