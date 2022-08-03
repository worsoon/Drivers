using ESql.Drivers.MySqlDriver.ApiImpl;
using ESql.Drivers.SQLiteDriver.ApiImpl;
using Worsoon.Core;
using Worsoon.ESql;
using Worsoon.ESql.Apis;

namespace ESql.Drivers.MySqlDriver;

public class ESqlMySqlBuilder:IESql
{
    private string _connString = string.Empty;
    private bool _tableSync = false;
    private bool _printSQL = false;
    private bool _sqlPrinter = false;
    private ICacheService? _cacheService;

    public ESqlMySqlBuilder()
    {
    }
    public ESqlMySqlBuilder(string connString, bool tableSync, bool sqlprinter, ICacheService cacheService)
    {
        _connString = connString;
        _tableSync = tableSync;
        _printSQL = sqlprinter;
        _cacheService = cacheService;
    }

    public ISelector<T> Select<T>(string expression = "*") where T : class, new() =>
        new SelectorBuilder<T>(expression)
        {
            ConnectStrings = _connString,
            TableAsync = _tableSync,
            SqlPrinter = _sqlPrinter,
            ESqlInstance = this
        }.Build();

    public IDeletor<T> Delete<T>() where T : class, new() => new DeletorBuilder<T>()
    {
        ConnectString = _connString,
        PrintSql = _sqlPrinter
    }.Build();

    public IUpdator<T> Update<T>(string updateStatement = "") where T : class, new() =>
        new UpdatorBuilder<T>(updateStatement)
        {
            ConnectStrings = _connString,
            SqlStatement = updateStatement,
            PrintSQL = _sqlPrinter
        }.Build();

    public IInsertor<T> Insert<T>(string statement) where T : class, new() =>
        new InsertorBuilder<T>(statement)
        {
            ConnectStrings = _connString,
            PrintSQL = _sqlPrinter,
            Instance = this,
            WithTableSync = _tableSync
        }.Build();

    public IQuerier<T> Query<T>(string statement) where T : class, new() => new QuerierBuilder<T>()
    {
        ConnectString = _connString,
        PrintSQL = _sqlPrinter
    }.Build();

    public IFinder<T> Find<T>() where T : class, new() => new FinderBuilder<T>()
    {
        CacheService = _cacheService,
        ESqlInstance = this
    }.Build();

    public IStructor<T> Struct<T>() where T : class, new() => new StructorBuilder<T>()
        { ConnectString = _connString, PrintSql = _sqlPrinter }.Build();

    public IQuerier<T> Querier<T>() where T : class, new()
    {
        throw new NotImplementedException();
    }

    public ITypeQuerier TypeQuerier(Type type)
    {
        throw new NotImplementedException();
    }
}