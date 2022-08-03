using Worsoon.ESql.Apis;

namespace ESql.Drivers.SQLiteDriver.ApiImpl;

public class QuerierBuilder<T>:IQuerier<T>
{
    public string? ConnectString { get; set; } = string.Empty;
    public bool PrintSQL { get; set; } = false;
    public IQuerier<T> Build() => this;
    public string ToSql()
    {
        throw new NotImplementedException();
    }

    public int ExecuteNoneQuery()
    {
        throw new NotImplementedException();
    }

    public Task<int> ExecuteNoneQueryAsync()
    {
        throw new NotImplementedException();
    }
}