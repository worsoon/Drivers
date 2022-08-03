using ESql.Drivers.SqliteDriver.ApiImpl;
using ESql.Drivers.SQLiteDriver.ApiImpl;
using Worsoon.Core;
using Worsoon.ESql;
using Worsoon.ESql.Apis;

namespace ESql.Drivers.SQLiteDriver;

public class ESqlSqliteBuilder : IESql
{
    private string _connString = string.Empty;
    private bool _tableSync = false;
    private bool _printSQL = false;
    private bool _sqlPrinter = false;
    private ICacheService? _cacheService;

    public ESqlSqliteBuilder()
    {
    }

    public ESqlSqliteBuilder(string connString, bool tableSync, bool sqlprinter, ICacheService cacheService)
    {
        _connString = connString;
        _tableSync = tableSync;
        _printSQL = sqlprinter;
        _cacheService = cacheService;
    }

    public ISelector<T> Select<T>(string expression = "*") where T : class, new() =>
        new SelectorBuilder<T>(expression)
        {
            ConnectStrings = _connString, TableAsync = _tableSync, ESqlInstance = this,
            SqlPrinter = _printSQL
        }.Build();

    public IDeletor<T> Delete<T>() where T : class, new() => new DeletorBuilder<T>()
        {
            ConnectString = _connString,
            PrintSql = _printSQL
        }
        .Build();

    public IUpdator<T> Update<T>(string updateCondition) where T : class, new() =>
        new UpdatorBuilder<T>(updateCondition)
            { ConnectStrings = _connString, PrintSQL = _printSQL, SqlStatement = updateCondition }.Build();

    public IInsertor<T> Insert<T>(string statement) where T : class, new() => new InsertorBuilder<T>(statement)
    {
        WithTableSync = _tableSync,
        ConnectStrings = _connString,
        Instance = this,
        PrintSQL = _sqlPrinter
    }.Build();

    public IQuerier<T> Query<T>(string statement) where T : class, new()
    {
        throw new NotImplementedException();
    }

    public IFinder<T> Find<T>() where T : class, new() => new FinderBuilder<T>()
    {
        ESqlInstance = this, CacheService
            = _cacheService
    }.Build();

    public IStructor<T> Struct<T>() where T : class, new() => new StructorBuilder<T>()
        { PrintSql = _printSQL, ConnectString = _connString }.Build();

    public IQuerier<T> Querier<T>() where T : class, new() => new QuerierBuilder<T>()
        { PrintSQL = _printSQL, ConnectString = _connString }.Build();

    public ITypeQuerier TypeQuerier(Type type) => new TypeQuerierBuilder()
        { PrintSQL = _printSQL, ConnectString = _connString, Type = type }.Build();
}