<p align="center">
  <img src="https://github.com/dotnet/orleans/blob/gh-pages/assets/logo.png" alt="Orleans.Redis" width="300px"> 
  <h1>Orleans Redis Providers</h1>
</p>

1.5.x branch 
[![Build status](https://ci.appveyor.com/api/projects/status/6xxnvi7rh131c9f1?svg=true)](https://ci.appveyor.com/project/OrleansContrib/orleans-storageprovider-redis)
2.x.x branch
[![Build status](https://ci.appveyor.com/api/projects/status/6xxnvi7rh131c9f1/branch/dev?svg=true)](https://ci.appveyor.com/project/OrleansContrib/orleans-storageprovider-redis/branch/dev)

[Orleans](https://github.com/dotnet/orleans) is a framework that provides a straight-forward approach to building distributed high-scale computing applications, without the need to learn and apply complex concurrency or other scaling patterns. 

[Redis](https://redis.io/) is an open source (BSD licensed), in-memory data structure store, used as a database, cache and message broker.

[StackExchange.Redis](https://stackexchange.github.io/StackExchange.Redis/) library underneath.

## Orleans.Persistence.Redis

**Attention**: Version 7.x is NOT binary compatible with previous versions.  
Configuration and serialization have changed to align with serialization changes in the Orleans framework 7.x

### Installation

> PS> Install-Package Orleans.Persistence.Redis -prerelease

### Usage

Configure your Orleans silos

```cs
var silo = new SiloHostBuilder()
    .AddRedisGrainStorage("Redis", optionsBuilder => optionsBuilder.Configure(options =>
    {
        options.DataConnectionString = "localhost:6379"; // This is the deafult
        options.DatabaseNumber = 1;
    }))
    .Build();
await silo.StartAsync();
```

Decorate your grain classes with the `StorageProvider` attribute or use DI with the `PersistentState` attribute as needed.

 ```cs
[StorageProvider(ProviderName = "Redis")]
public class SomeGrain : Grain<SomeGrainState>, ISomeGrain

public class SomeGrain2 : Grain, ISomeGrain2 {
    SomeGrain2(
        [PersistentState("state", "Redis")] IPersistentState<SomeGrain2State> state
    )
}



 ```

These settings will enable the Redis cache to act as the store for grains that have persistent state.

### Configuration

* __DataConnectionString="..."__ (required) the connection string to your redis database (i.e. `localhost:6379`, is passed directly to StackExchange.Redis)
* __DatabaseNumber=1__ (optional) the number of the redis database to connect to. Defaults to 0.

## Orleans.Clustering.Redis

Orleans clustering provider for Redis

**Orleans.Clustering.Redis** enables Orleans applications to use Redis as a backend for cluster membership.

Redis is a straight key/value store. Membership data is stored as a hash.

If you want to quickly test it, clone this repo and go to the [samples directory](https://github.com/OrleansContrib/Orleans.Redis/tree/main/samples) for instructions on how to run a sample cluster.

### Installation

Installation is performed via [NuGet](https://www.nuget.org/packages/Orleans.Clustering.Redis/)

From Package Manager:

``` powershell
Install-Package Orleans.Clustering.Redis
```

.Net CLI:

``` powershell
dotnet add package Orleans.Clustering.Redis
```

### Configuration

A functional Redis database is required for this provider to work.

#### Silo
Tell Orleans runtime that we are going to use Redis as our Cluster Membership Provider:

```cs
var silo = new SiloHostBuilder()
        ...
        .UseRedisClustering(opt =>
        {
            opt.ConnectionString = "host:port";
            opt.Database = 0;
        })
        ...
        .Build();
``` 

`ConnectionString` tells the connector where to find the Redis database.

`Database` is an integer which tells the membership table which database to get after connecting to the Redis service.

More information on connection string configuration can be found at on the StackExchange.Redis driver site (https://stackexchange.github.io/StackExchange.Redis/Configuration.html).

#### Client

Now that our silo is up and running, the Orleans client needs to connect to the Redis database to look for Orleans gateways.

```cs
var client = new ClientBuilder()
        ...
        .UseRedisClustering(opt =>
        {
            opt.ConnectionString = "host:port";
            opt.Database = 0;
        })
        ...
        .Build();
```

At the moment the gateway list is provided by the underlying membership provider directly.

## License

This project is licensed under the [MIT license](https://github.com/OrleansContrib/Orleans.Redis/blob/main/LICENSE).
