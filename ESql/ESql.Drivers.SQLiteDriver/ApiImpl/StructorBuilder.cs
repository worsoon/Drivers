using System.Data.SQLite;
using System.Reflection;
using Worsoon.Attributes;
using Worsoon.Core;
using Worsoon.ESql.Apis;

namespace ESql.Drivers.SQLiteDriver.ApiImpl;

public class StructorBuilder<T> : IStructor<T>
{
    private readonly Type _type = typeof(T);

    public string? ConnectString { get; set; }
    public bool PrintSql { get; set; }

    public IStructor<T> Build() => this;

    public bool HasTable()
    {
        try
        {
            var table = _type.GetCustomAttribute<HelloAttribute>();
            if (table == null) return false;

            var tbName = table.Name;
            if (tbName.IsNullOrEmpty()) return false;

            using var conn = new SQLiteConnection(ConnectString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText =
                "SELECT count(*) FROM sqlite_master WHERE type=\"table\" AND name = \"{0}\"".Format(tbName);

            return cmd.ExecuteScalar().ToInt() > 0;
        }
        catch (Exception e)
        {
            AssertX.Error(e);
            return false;
        }
    }

    public bool CreateTable()
    {
        if (HasTable())
        {
            if (PrintSql)
                AssertX.Info("用户表已存在！");
            return true;
        }

        var table = _type.GetCustomAttribute<HelloAttribute>();

        var tbName = table == null ? _type.Name : table.Name;

        var pool = Pool.StringBuilder.Get();
        pool.AppendFormat("CREATE TABLE IF NOT EXISTS {0}(\n", tbName);

        PropertyInfo[] props = _type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Default);

        //分配批次
        /*
         * 找出来所有的被标记为[ForeignEntity]的实体，这些实体中有定义参考属性*/
        var foreign = props.Where(e => e.GetCustomAttribute<HelloAttribute>() != null).ToArray();
        var normalInfo = props.Where(e => !foreign.Contains(e)).ToArray();

        foreach (PropertyInfo prop in normalInfo)
        {
            var custom = prop.GetCustomAttribute<HelloAttribute>();
            switch (prop.PropertyType.Name)
            {
                case "Int16":
                    pool.AppendFormat("{0} INTEGER", prop.Name);
                    break;
                case "Int32":
                    pool.AppendFormat("{0} INTEGER", prop.Name);
                    break;
                case "Int64":
                    pool.AppendFormat("{0} INTEGER", prop.Name);
                    break;
                case "Boolean":
                    pool.AppendFormat("{0} BOOLEAN", prop.Name);
                    break;
                case "Double":
                    pool.AppendFormat("{0} DOUBLE", prop.Name);
                    break;
                case "String":
                {
                    var length = custom?.StringLength;
                    pool.AppendFormat("{0} VARCHAR({1})", prop.Name, (length > 0 ? length : 64));
                }
                    break;
                case "DateTime":
                    pool.AppendFormat("{0} DATETIME", prop.Name);
                    break;
            }

            //处理主键
            if (custom?.IsPrimaryKey ?? false)
                pool.Append(" PRIMARY KEY");
            if (custom?.IsIdentity ?? false)
                pool.Append(" AUTOINCREMENT");

            //处理约束
            if (custom?.Unique ?? false)
                pool.Append(" UNIQUE");
            if (custom?.NotNULL ?? false)
                pool.Append(" NOT NULL");

            pool.Append(",\n");

            //创建索引
            // if (custom?.Index != null && !custom.Index.IsNullOrEmpty())
            //     pool.AppendFormat("INDEX {0}(`{1}`),".Format(custom.Index, prop.Name));
        }

        pool.Remove(pool.Length - 2, 1);
        pool.Append(")");
        var sql = pool.Put(true);

        if (PrintSql)
            AssertX.Info($"用户表不存在，正在自动创建：{sql}");
        using var conn = new SQLiteConnection(ConnectString);
        conn.Open();
        using var cmd = conn.CreateCommand();

        cmd.CommandText = sql;
        return cmd.ExecuteNonQuery() > 0;
    }

    [Obsolete(message: "尚未实现")]
    public bool AlterTable()
    {
        if (!HasTable())
        {
            AssertX.Info("用户表不存在，执行创建表操作！");
            return true;
        }

        var table = _type.GetCustomAttribute<HelloAttribute>();
        var tbName = table == null ? _type.Name : table.Name;

        var pool = Pool.StringBuilder.Get();
        pool.AppendFormat("ALTER TABLE IF NOT EXISTS {0}(\n", tbName);

        PropertyInfo[] props = _type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Default);
        PropertyInfo[] normalInfo =
            _type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Default);


        foreach (PropertyInfo prop in props)
        {
            var p = prop.PropertyType.Name;
            var custom = prop.GetCustomAttribute<HelloAttribute>();

            switch (prop.PropertyType.Name)
            {
                case "Int16":
                    pool.AppendFormat("{0} INTEGER", prop.Name);
                    break;
                case "Int32":
                    pool.AppendFormat("{0} INTEGER", prop.Name);
                    break;
                case "Int64":
                    pool.AppendFormat("{0} INTEGER", prop.Name);
                    break;
                case "Boolean":
                    pool.AppendFormat("{0} BOOLEAN", prop.Name);
                    break;
                case "Double":
                    pool.AppendFormat("{0} DOUBLE", prop.Name);
                    break;
                case "String":
                {
                    var length = custom?.StringLength ?? 0;
                    pool.AppendFormat("{0} VARCHAR({1})", prop.Name, (length > 0 ? length : 64));
                }
                    break;
                case "DateTime":
                    pool.AppendFormat("{0} DATETIME", prop.Name);
                    break;
            }

            //处理主键
            if (custom?.IsPrimaryKey ?? false)
                pool.Append(" PRIMARY KEY");
            if (custom?.IsIdentity ?? false)
                pool.Append(" AUTOINCREMENT");

            //处理约束
            if (custom?.Unique ?? false)
                pool.Append(" UNIQUE");
            if (custom?.NotNULL ?? false)
                pool.Append(" NOT NULL");

            pool.Append(",\n");
        }

        pool.Remove(pool.Length - 2, 1);
        pool.Append(")");
        var sql = pool.Put(true);

        if (PrintSql)
            AssertX.Info($"编辑表：{sql}");
        // using var conn = new SqliteConnection(_connectString);
        // conn.Open();
        // using var cmd = conn.CreateCommand();
        //
        // cmd.CommandText = sql;
        // return cmd.ExecuteNonQuery() > 0;
        return false;
    }

    [Obsolete("尚未实现")]
    public bool SyncTable()
    {
        if (PrintSql)
            AssertX.Info("表格同步");
        return HasTable() ? AlterTable() : CreateTable();
    }

    public bool DropTable()
    {
        var tbName = typeof(T).GetCustomAttribute<HelloAttribute>()?.Name;
        tbName = tbName.IsNullOrEmpty() ? typeof(T).Name : tbName;

        var sql = $"DROP TABLE IF EXISTS {tbName};";

        using var conn = new SQLiteConnection(ConnectString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        if (PrintSql)
            AssertX.Info(sql);

        return cmd.ExecuteNonQuery() > 0;
    }
}