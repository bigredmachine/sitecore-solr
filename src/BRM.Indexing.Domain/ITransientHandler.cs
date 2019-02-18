using System;

namespace BRM.Indexing.Domain
{
    public interface ITransientHandler
    {
        TResult Execute<TResult>(Func<TResult> action);
    }
}