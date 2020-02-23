namespace BRM.Indexing.Domain
{
    using System;

    public interface ITransientHandler
    {
        TResult Execute<TResult>(Func<TResult> action);
    }
}