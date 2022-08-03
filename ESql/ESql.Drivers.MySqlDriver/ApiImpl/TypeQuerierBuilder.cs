using System.Data;
using Worsoon.ESql.Apis;

namespace ESql.Drivers.SQLiteDriver.ApiImpl;

public class TypeQuerierBuilder: ITypeQuerier
{
    public Type? Type { get; set; }
    public string? ConnectString { get; set; }
    public bool? PrintSQL { get; set; }
    public ITypeQuerier Build() => this;

    public int ExecuteNoneQuery(string sql)
    {
        throw new NotImplementedException();
    }

    public Task<int> ExecuteNoneQueryAsync(string sql)
    {
        throw new NotImplementedException();
    }

    public int ExecuteNoneQuery(string sql, IDataParameter dataParameter)
    {
        throw new NotImplementedException();
    }

    public Task<int> ExecuteNoneQueryAsync(string sql, IDataParameter dataParameter)
    {
        throw new NotImplementedException();
    }
}