using System.Data;
using System.Reflection;
using MySqlConnector;
using Worsoon.Attributes;
using Worsoon.Core;
using Worsoon.ESql;
using Worsoon.ESql.Apis;
namespace ESql.Drivers.MySqlDriver.ApiImpl;

public class SelectorBuilder<T> : BaseDisposer, ISelector<T> where T : class, new()
{
    private string _whereExpression = string.Empty;
    private string _selectExpression = "*";
    private int _skip = 0, _take = 1;

    private MySqlConnection? _conn;
    private MySqlCommand? _commander;

    public IESql? ESqlInstance;
    public string? ConnectStrings { get; set; }
    public bool? TableAsync { get; set; }
    public bool? SqlPrinter { get; set; }

    private void OnExecuting()
    {
        if (TableAsync ?? false)
            ESqlInstance!.Struct<T>().SyncTable();
    }

    public SelectorBuilder(string selectExpression = "*")
    {
        _selectExpression = selectExpression;
    }

    public SelectorBuilder<T> Build()
    {
        _conn = new MySqlConnection(ConnectStrings);
        _commander = _conn.CreateCommand();
        _commander.Parameters.Clear();
        return this;
    }

    public SelectorBuilder<T> AddWhereExpression(string expression)
    {
        this._whereExpression = expression;
        return this;
    }

    public SelectorBuilder<T> AddSkipAndTake(int skip, int take)
    {
        _skip = skip;
        _take = take;
        return this;
    }

    public string ToSql()
    {
        var pool = Pool.StringBuilder.Get();

        pool.Append($"SELECT {_selectExpression} FROM ");
        var type = typeof(T);
        var tbName = (type.GetCustomAttribute<HelloAttribute>())?.TableName;
        if (tbName.IsNullOrEmpty()) tbName = type.Name;
        pool.Append(tbName);

        if (!_whereExpression.IsNullOrEmpty())
            pool.Append($" WHERE {_whereExpression}");

        if (_skip > 0 || _take > 1)
            pool.Append($" LIMIT {(_take)} OFFSET {_skip}");

        return pool.Put(true);
    }

    public int ExecuteNoneQuery() => 0;

    public async Task<int> ExecuteNoneQueryAsync() => await Task.Run(ExecuteNoneQuery);

    public object? ExecuteScalar()
    {
        OnExecuting();
        if (_conn == null) return null;
        try
        {
            if (_conn.State == (ConnectionState.Broken | ConnectionState.Closed | ConnectionState.Connecting))
                _conn.Open();

            _commander!.CommandText = ToSql();

            if (SqlPrinter ?? false)
                AssertX.Info(_commander?.CommandText);
            return _commander?.ExecuteNonQuery();
        }
        catch (Exception e)
        {
            AssertX.Error(e);
        }

        _commander?.Parameters.Clear();

        return null;
    }

    public async Task<object?> ExecuteScalarAsync()
    {
        OnExecuting();
        if (_conn == null) return null;
        try
        {
            if (_conn.State is (ConnectionState.Broken or ConnectionState.Closed or ConnectionState.Connecting))
                await _conn.OpenAsync();

            _commander!.CommandText = ToSql();
            if (SqlPrinter ?? false)
                AssertX.Info(_commander.CommandText);

            return await _commander?.ExecuteScalarAsync()!;
        }
        catch (Exception e)
        {
            AssertX.Error(e);
        }

        _commander?.Parameters.Clear();

        return null;
    }

    public DataSet? ExecuteSet()
    {
        OnExecuting();
        if (_conn == null) return null;
        try
        {
            if (_conn.State is (ConnectionState.Broken or ConnectionState.Closed or ConnectionState.Connecting))
                _conn.Open();
            var set = new DataSet();
            if (_commander != null)
            {
                _commander.CommandText = ToSql();
                if (SqlPrinter ?? false)
                    AssertX.Info(_commander.CommandText);
                MySqlDataAdapter adapter = new MySqlDataAdapter(_commander);
                adapter.Fill(set);
            }

            return set;
        }
        catch (Exception e)
        {
            AssertX.Error(e);
        }

        _commander?.Parameters.Clear();

        return null;
    }

    public async Task<DataSet?> ExecuteSetAsync()
    {
        OnExecuting();
        if (_conn == null) return null;
        try
        {
            if (_conn.State is (ConnectionState.Broken or ConnectionState.Closed or ConnectionState.Connecting))
                await _conn.OpenAsync();
            var set = new DataSet();
            if (_commander != null)
            {
                _commander.CommandText = ToSql();
                if (SqlPrinter ?? false)
                    AssertX.Info(_commander.CommandText);
                MySqlDataAdapter adapter = new MySqlDataAdapter(_commander);
                adapter.Fill(set);
            }

            return set;
        }
        catch (Exception e)
        {
            AssertX.Error(e);
        }

        _commander?.Parameters.Clear();

        return null;
    }

    public T? ToEntity() => ToList()?.FirstOrDefault();

    public async Task<T?> ToEntityAsync() => (await ExecuteSetAsync())?.Tables[0].ToEntities<T>().FirstOrDefault();

    public IEnumerable<T>? ToList() => ExecuteSet()?.Tables[0].ToEntities<T>();

    public async Task<IEnumerable<T>?> ToListAsync() => (await ExecuteSetAsync())?.Tables[0].ToEntities<T>();

    public ISelector<T> Where(string pattern)
    {
        _whereExpression = pattern;
        return this;
    }

    public ISelector<T> Set(string pattern, object o)
    {
        _commander?.Parameters.Add(new MySqlParameter(pattern, o));
        return this;
    }

    public ISelector<T> Skip(int skip)
    {
        _skip = skip;
        return this;
    }

    public ISelector<T> Take(int take)
    {
        _take = take;
        return this;
    }

    public int Count()
    {
        var tbName = typeof(T).GetCustomAttribute<HelloAttribute>()?.TableName;
        if (tbName.IsNullOrEmpty())
            tbName = typeof(T).Name;

        var pool = Pool.StringBuilder.Get();
        pool.Append("SELECT count(*) FROM ");
        pool.AppendFormat("{0} ", tbName);

        if (!_whereExpression.IsNullOrEmpty())
            pool.AppendFormat("WHERE {0}", _whereExpression);

        var query = pool.Put(true);

        OnExecuting();

        if (_conn == null) return 0;
        try
        {
            if (_conn.State is (ConnectionState.Broken or ConnectionState.Closed or ConnectionState.Connecting))
                _conn.Open();
            if (_commander != null)
            {
                _commander.CommandText = query;
                if (SqlPrinter ?? false)
                    AssertX.Info(_commander.CommandText);
                return _commander.ExecuteScalar().ToInt();
            }
        }
        catch (Exception e)
        {
            AssertX.Error(e);
        }

        _commander?.Parameters.Clear();
        return 0;
    }

    public async Task<int> CountAsync()
    {
        var tbName = typeof(T).GetCustomAttribute<HelloAttribute>()?.TableName;
        if (tbName.IsNullOrEmpty())
            tbName = typeof(T).Name;

        var pool = Pool.StringBuilder.Get();
        pool.Append("SELECT count(*) FROM ");
        pool.AppendFormat("{0} ", tbName);

        if (!_whereExpression.IsNullOrEmpty())
            pool.AppendFormat("WHERE {0}", _whereExpression);

        var query = pool.Put(true);

        OnExecuting();

        if (_conn == null) return 0;
        try
        {
            if (_conn.State is (ConnectionState.Broken or ConnectionState.Closed or ConnectionState.Connecting))
                await _conn.OpenAsync();
            if (_commander != null)
            {
                _commander.CommandText = query;
                return (await _commander.ExecuteScalarAsync()).ToInt();
            }
        }
        catch (Exception e)
        {
            AssertX.Error(e);
        }

        _commander?.Parameters.Clear();

        return 0;
    }

    public bool Any() => Count() > 0;

    public async Task<bool> AnyAsync() => (await CountAsync()) > 0;

    public override void OnDisposing()
    {
        _commander?.Dispose();
        _conn?.Close();
        _conn?.Dispose();
    }
}