using System.Data;
using MySqlConnector;
using Worsoon.Attributes;
using Worsoon.Core;
using Worsoon.ESql.Apis;

namespace ESql.Drivers.MySqlDriver.ApiImpl;

public class DeletorBuilder<T>: BaseDisposer,IDeletor<T> where T : class, new()
{
    private string _whereExpression = string.Empty;

    private MySqlConnection? _conn = null;
    private MySqlCommand? _commander = null;
    
    public string? ConnectString { get; set; }
    public bool? PrintSql { get; set; }
    
    public DeletorBuilder<T> Build()
    {
        _conn = new MySqlConnection(ConnectString);
        _commander = _conn.CreateCommand();
        _commander.Parameters.Clear();
        return this;
    }
    public DeletorBuilder<T> AddWhereExpression(string expression)
    {
        this._whereExpression = expression;
        return this;
    }
    public int ExecuteNoneQuery()
    {
        if (_conn == null) return -1;
        
        try
        {
            if (_conn.State == ConnectionState.Broken || 
                _conn.State is ConnectionState.Closed or ConnectionState.Connecting)
                _conn.Open();
            if (_commander != null)
            {
                _commander.CommandText = ToSql();

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
            if (_conn.State == ConnectionState.Broken ||
                _conn.State == ConnectionState.Closed ||
                _conn.State == ConnectionState.Connecting)
                await _conn.OpenAsync();
            if (_commander != null)
            {
                _commander.CommandText = ToSql();

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

    public IDeletor<T> Where(string pattern)
    {
        _whereExpression = pattern;
        return this;
    }
    public IDeletor<T> Set(string pattern, object o)
    {
        _commander?.Parameters.Add(new MySqlParameter(pattern, o));
        return this;
    }
    public string ToSql()
    {
        var pool = Pool.StringBuilder.Get();

        var type = typeof(T);
        var tbName = (type.GetCustomAttributes(typeof(HelloAttribute), false).FirstOrDefault() as HelloAttribute)?.Name;
        if (tbName.IsNullOrEmpty()) tbName = type.Name;
        
        pool.Append($"DELETE FROM {tbName}");

        if (!_whereExpression.IsNullOrEmpty())
            pool.Append($" WHERE {_whereExpression}");

        return pool.Put(true);
    }

    public override void OnDisposing()
    {
        _commander?.Dispose();
        _conn?.Close();
        _conn?.Dispose();
        base.Dispose();
    }
}