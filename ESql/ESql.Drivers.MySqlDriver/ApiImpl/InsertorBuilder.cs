using System.Data;
using System.Reflection;
using MySqlConnector;
using Worsoon.Attributes;
using Worsoon.Core;
using Worsoon.ESql;
using Worsoon.ESql.Apis;

namespace ESql.Drivers.SQLiteDriver.ApiImpl;

public class InsertorBuilder<T> : BaseDisposer, IInsertor<T> where T : class, new()
{
    private DataTable _table = new DataTable();

    private MySqlConnection? _conn;
    private MySqlCommand? _commander;
    private readonly string _statement;
    private readonly PropertyInfo[] _propertyInfos;

    public IESql? Instance { get; set; }
    public string? ConnectStrings { get; set; }
    public bool? WithTableSync { get; set; }
    public bool? PrintSQL { get; set; }

    public InsertorBuilder(string statement)
    {
        _statement = statement;
        _propertyInfos = typeof(T).GetProperties(BindingFlags.Default | BindingFlags.Instance | BindingFlags.Public);
    }

    public InsertorBuilder<T> Build()
    {
        _conn = new MySqlConnection(ConnectStrings);
        _commander = _conn.CreateCommand();
        _commander.Parameters.Clear();
        return this;
    }

    public int ExecuteNoneQuery()
    {
        if (WithTableSync ?? false)
            Instance!.Struct<T>().SyncTable();
        if (_table.Rows?.Count > 0)
        {
            var bulkConnection = new MySqlConnection(ConnectStrings);
            try
            {
                bulkConnection?.Open();
                var trans = bulkConnection?.BeginTransaction();
                using var bulkCommander = bulkConnection?.CreateCommand();
                if (bulkConnection != null)
                {
                    foreach (DataRow row in _table.Rows)
                    {
                        using var cmd = trans!.Connection.CreateCommand();
                        cmd.CommandText = "";

                        cmd.Dispose();
                    }
                }

                trans?.Commit();
                bulkCommander?.Dispose();
                return _table.Rows.Count;
            }
            catch (Exception e)
            {
                AssertX.Error(e);
                return -1;
            }
            finally
            {
                bulkConnection?.Close();
                bulkConnection?.Dispose();
                _table.Rows.Clear();
                _table.Clear();
            }
        }

        try
        {
            if (_conn is { State: ConnectionState.Closed or ConnectionState.Broken or ConnectionState.Connecting })
                _conn.Open();
            if (_commander != null)
            {
                _commander.CommandText = ToSql();
                AssertX.Info($"插入数据：{ToSql()}");
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
        if (WithTableSync ?? false)
            Instance!.Struct<T>().SyncTable();
        if (_table.Rows?.Count > 0)
        {
            var bulkConnection = _conn?.Clone();
            try
            {
                await bulkConnection?.OpenAsync()!;
                var trans = await bulkConnection.BeginTransactionAsync();
                await using var bulkCommander = bulkConnection.CreateCommand();
                MySqlBulkCopy copy = new MySqlBulkCopy(bulkConnection, trans)
                {
                    DestinationTableName = _table.TableName
                };
                await copy.WriteToServerAsync(_table);
                await trans?.CommitAsync()!;
                await bulkCommander!.DisposeAsync();
                return _table.Rows.Count;
            
            }
            catch (Exception e)
            {
                AssertX.Error(e);
                return -1;
            }
            finally
            {
                await bulkConnection?.CloseAsync()!;
                await bulkConnection.DisposeAsync();
                _table.Rows.Clear();
                _table.Clear();
            }
        }

        try
        {
            if (_conn is { State: ConnectionState.Closed or ConnectionState.Broken or ConnectionState.Connecting })
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

    public IInsertor<T> Add(T entity)
    {
        foreach (var prop in _propertyInfos)
            if (prop.GetCustomAttribute<HelloAttribute>() != null)
            {
                var p = prop.GetCustomAttribute<HelloAttribute>();

                if (p is { IsIdentity: true, IsPrimaryKey: true })
                    continue;
                Set($"@{prop.Name}", prop?.GetValue(entity!));
            }
            else
                Set($"@{prop.Name}", prop?.GetValue(entity!));

        return this;
    }

    public IInsertor<T> AddTable(DataTable table)
    {
        _table = table;
        return this;
    }

    public IInsertor<T> AddRange(List<T> list)
    {
        _table.Rows.Clear();
        _table.Columns.Clear();
        
        foreach (var prop in _propertyInfos)
            _table.Columns.Add(prop.Name, prop.PropertyType);

        _table.TableName = typeof(T).GetCustomAttribute<HelloAttribute>()?.TableName;
        if (_table.TableName.IsNullOrEmpty()) _table.TableName = typeof(T).Name;

        foreach (var l in list)
        {
            DataRow row = _table.Rows.Add();
            foreach (PropertyInfo prop in _propertyInfos)
                row[prop.Name] = prop.GetValue(l);
        }

        return this;
    }

    public IInsertor<T> Set(string pattern, object? o)
    {
        _commander?.Parameters.Add(new MySqlParameter(pattern, o));
        return this;
    }

    public int ExecuteIndentity()
    {
        var res = ExecuteNoneQuery();

        if (res > 0)
        {
            var type = typeof(T);
            var tbName = type.GetCustomAttribute<HelloAttribute>()?.TableName ?? type.Name;

            if (_conn == null) return 0;
            try
            {
                if (_conn.State is (ConnectionState.Broken or ConnectionState.Closed or ConnectionState.Connecting))
                    _conn.Open();
                if (_commander != null)
                {
                    _commander.CommandText = "SELECT seq FROM sqlite_sequence WHERE name= '{0}'".Format(tbName);
                    if (PrintSQL ?? false)
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

        return 0;
    }

    public async Task<int> ExecuteIndentityAsync()
    {
        var res = await ExecuteNoneQueryAsync();

        if (res > 0)
        {
            var type = typeof(T);
            var tbName = type.GetCustomAttribute<HelloAttribute>()?.TableName ?? type.Name;
            if (_conn == null) return 0;
            try
            {
                if (_conn.State is (ConnectionState.Broken or ConnectionState.Closed or ConnectionState.Connecting))
                    await _conn.OpenAsync();
                if (_commander != null)
                {
                    _commander.CommandText = "SELECT seq FROM sqlite_sequence WHERE name= '{0}'".Format(tbName);
                    if (PrintSQL ?? false)
                        AssertX.Info(_commander.CommandText);
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
        else return 0;
    }

    public string ToSql()
    {
        if (_statement.IsNullOrEmpty())
        {
            var pool = Pool.StringBuilder.Get();

            var type = typeof(T);
            var tbName = type.GetCustomAttribute<HelloAttribute>()?.TableName ?? type.Name;

            var valueExpression = string.Empty;
            pool.AppendFormat("INSERT INTO {0}(", tbName);

            bool hasNoId = true;
            try
            {
                //首先找到主键
                var pk = _propertyInfos.FirstOrDefault(e =>
                    e.GetCustomAttribute<HelloAttribute>()?.IsPrimaryKey ?? false);

                //如果主键是int类型，且标记了自动增长，那么就不用插入Id了
                if (pk != null)
                {
                    var pki = pk.GetCustomAttribute<HelloAttribute>();
                    if (pki is { IsIdentity: true })
                        hasNoId = false;
                }
                else
                {
                    AssertX.Fatal($"表 {tbName} 没有设置主键，必须设置主键才有效！");
                }
            }
            catch (Exception e)
            {
                AssertX.Fatal(e);
            }

            //如果没有主键，那么
            foreach (PropertyInfo property in _propertyInfos)
                if (property.Name.ToLower() != "id")
                {
                    pool.Append($"{property.Name},");
                    valueExpression += $"@{property.Name},";
                }
                else
                {
                    if (hasNoId)
                    {
                        pool.Append($"{property.Name},");
                        valueExpression += $"@{property.Name}";
                    }
                }

            var statement = pool.Put(true);
            pool.Clear();
            statement = statement.Left(statement.Length - 1);
            pool.Append(statement);
            pool.AppendFormat(") VALUES({0})", valueExpression.Left(valueExpression.Length - 1));

            return pool.Put(true);
        }

        return _statement;
    }

    public override void OnDisposing()
    {
        _commander?.Dispose();
        _conn?.Close();
        _conn?.Dispose();
    }
}