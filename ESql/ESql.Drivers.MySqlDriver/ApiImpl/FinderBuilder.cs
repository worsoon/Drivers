using Worsoon.Core;
using Worsoon.Core.Extension;
using Worsoon.ESql;
using Worsoon.ESql.Apis;

namespace ESql.Drivers.SQLiteDriver.ApiImpl;

public class FinderBuilder<T> : IFinder<T> where T : class, new()
{
    public IESql? ESqlInstance { get; set; }
    public ICacheService? CacheService { get; set; }

    /// <summary>
    /// 查找一个实体对象
    /// </summary>
    /// <param name="expression">实体属性</param>
    /// <returns></returns>
    public T? Find(Func<T, bool> expression, bool autoBind = false)
    {
        var entity = CacheService?.Select<IEnumerable<T>>(typeof(T).Name)?.Where(expression).FirstOrDefault() ??
                     default(T);
        if (entity == null && ESqlInstance != null)
        {
            CacheService?.Add(typeof(T).Name, ESqlInstance.Select<T>().ToList());
            entity = CacheService?.Select<IEnumerable<T>>(typeof(T).Name)?.Where(expression).FirstOrDefault() ??
                     default;
        }

        return entity;
    }

    /// <summary>
    /// 查找一个列表
    /// </summary>
    /// <param name="expression">查找表达式</param>
    /// <returns></returns>
    public IEnumerable<T>? Finds(Func<T, bool> expression, bool autoBind = false)
    {
        var entity = CacheService?.Select<List<T>>(typeof(T).Name)?.Where(expression).ToList();

        if (entity.IsNullOrEmpty() && ESqlInstance != null)
        {
            CacheService?.Add(typeof(T).Name, ESqlInstance.Select<T>().ToList());
            entity = CacheService?.Select<List<T>>(typeof(T).Name)?.Where(expression).ToList();
        }

        return entity;
    }

    /// <summary>
    /// 异步查找对象
    /// </summary>
    /// <param name="expression">表达式</param>
    /// <returns></returns>
    public async Task<T?> FindAsync(Func<T, bool> expression, bool autoBind = false)
    {
        var entity = CacheService?.Select<List<T>>(typeof(T).Name)?.Where(expression).FirstOrDefault();

        if (entity != null || ESqlInstance == null) return entity;
        CacheService?.Add(typeof(T).Name, await ESqlInstance.Select<T>().ToListAsync());
        entity = CacheService?.Select<List<T>>(typeof(T).Name)?.Where(expression).FirstOrDefault();

        return entity;
    }

    /// <summary>
    /// 异步查找列表
    /// </summary>
    /// <param name="expression">表达式</param>
    /// <returns></returns>
    public async Task<IEnumerable<T>?> FindsAsync(Func<T, bool> expression, bool autoBind = false)
    {
        var entity = CacheService?.Select<List<T>>(typeof(T).Name)?.Where(expression).ToList();

        if (!entity.IsNullOrEmpty() || ESqlInstance == null) return entity;
        CacheService?.Add(typeof(T).Name, await ESqlInstance.Select<T>().ToListAsync());
        entity = CacheService?.Select<List<T>>(typeof(T).Name)?.Where(expression).ToList();

        return entity;
    }

    /// <summary>
    /// 检查是否包含元素
    /// </summary>
    /// <param name="expression">表达式</param>
    /// <returns></returns>
    public bool Any(Func<T, bool> expression) =>
        (CacheService?.Select<IEnumerable<T>>(typeof(T).Name) ?? Array.Empty<T>()).Count(expression) > 0;

    /// <summary>
    /// 异步检查是否包含元素
    /// </summary>
    /// <param name="expression"></param>
    /// <returns></returns>
    public async Task<bool> AnyAsync(Func<T, bool> expression) => await Task.Run(() => Any(expression));

    public int Count(Func<T, bool> expression)
        => CacheService?.Select<IEnumerable<T>>(typeof(T).Name)?.Count(expression) ?? 0;

    public async Task<int> CountAsync(Func<T, bool> expression)
        => await Task.Run(() => CacheService?.Select<IEnumerable<T>>(typeof(T).Name)?.Count(expression) ?? 0);

    /// <summary>
    /// 异步更新对象缓存
    /// </summary>
    public async Task UpdateAsync()
    {
        if (ESqlInstance != null)
        {
            CacheService?.Delete(typeof(T).Name);
            CacheService?.Add(typeof(T).Name, await ESqlInstance.Select<T>().ToListAsync());
        }
    }

    /// <summary>
    /// 更新对象缓存
    /// </summary>
    public void Update()
    {
        if (ESqlInstance != null)
        {
            CacheService?.Delete(typeof(T).Name);
            CacheService?.Add(typeof(T).Name, ESqlInstance.Select<T>().ToList());
        }
    }

    public IFinder<T> Build() => this;
}