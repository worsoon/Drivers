using System.Data;
using System.Data.SQLite;
using System.Reflection;
using Worsoon.Attributes;
using Worsoon.Core;
using Worsoon.ESql.Apis;

namespace ESql.Drivers.SQLiteDriver.ApiImpl;

public class UpdatorBuilder<T>: BaseDisposer,IUpdator<T> where T : class, new()
{
    private string _whereExpression = string.Empty;
    private SQLiteConnection? _conn = null;
    private SQLiteCommand? _commander = null;

    private T? _entity;
    private IEnumerable<T>? _ranges = null;
    private readonly Type _thisType = typeof(T);
    
    public string? SqlStatement { get; set; }
    public string? ConnectStrings { get; set; }
    public bool? PrintSQL { get; set; }

    public UpdatorBuilder(string sqlStatement)
    {
        SqlStatement = sqlStatement;
    }
    public UpdatorBuilder<T> Add(T entity)
    {
        _entity = entity;
        return this;
    }

    public IUpdator<T> AddRange(IEnumerable<T> range)
    {
        _ranges = range;
        return this;
    }
    public int ExecuteNoneQuery()
    {
        if (_conn == null) return -1;
        
        try
        {
            if (_conn.State is ConnectionState.Broken or ConnectionState.Closed or ConnectionState.Connecting)
                _conn.Open();
            if (_commander != null)
            {
                _commander.CommandText = ToSql();
                if(PrintSQL??false)
                    AssertX.Info(_commander.CommandText);
                return _commander.ExecuteNonQuery();
            }
        }
        catch (Exception e)
        {
            AssertX.Error(e);
        }

        _commander?.Parameters.Clear();

        return -1;
    }
    public async Task<int> ExecuteNoneQueryAsync()
    {
        if (_conn == null) return -1;
        
        try
        {
            if (_conn.State is ConnectionState.Broken or ConnectionState.Closed or ConnectionState.Connecting)
                await _conn.OpenAsync();
            if (_commander != null)
            {
                _commander.CommandText = ToSql();
                if(PrintSQL??false)
                    AssertX.Info(_commander.CommandText);
                AssertX.Info(_commander.CommandText);

                return await _commander.ExecuteNonQueryAsync();
            }
        }
        catch (Exception e)
        {
            AssertX.Error(e);
        }

        _commander?.Parameters.Clear();

        return -1;
    }

    public IUpdator<T> Where(string pattern)
    {
        _whereExpression = pattern;
        return this;
    }
    public IUpdator<T> Set(string pattern, object o)
    {
        _commander?.Parameters.Add(new SQLiteParameter(pattern, o));
        return this;
    }

    IUpdator<T> IUpdator<T>.Add(T entity)
    {
        return Add(entity);
    }

    public string ToSql()
    {
        if (SqlStatement.IsNullOrEmpty())
            return string.Empty;
        
        var pool = Pool.StringBuilder.Get();

        var tbName = (_thisType.GetCustomAttributes<HelloAttribute>().FirstOrDefault())?.Name;
        if (tbName.IsNullOrEmpty()) tbName = _thisType.Name;
        
        pool.Append($"UPDATE {tbName} SET ");

        if (!SqlStatement.IsNullOrEmpty())
            pool.Append(SqlStatement);

        if (!_whereExpression.IsNullOrEmpty())
            pool.Append($" WHERE {_whereExpression}");

        return pool.Put(true);
    }

    public IUpdator<T> Build()
    {
        _conn = new SQLiteConnection(ConnectStrings);
        _commander = _conn.CreateCommand();
        _commander.Parameters.Clear();
        return this;
    }
    public override void OnDisposing()
    {
        _commander?.Dispose();
        _conn?.Close();
        _conn?.Dispose();
        Dispose();
    }
}