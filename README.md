# Orleans.Redis
Orleans plugins for Redis

## Orleans.Clustering.Redis
Orleans membership provider for Redis

[Orleans](https://github.com/dotnet/orleans) is a framework that provides a straight-forward approach to building distributed high-scale computing applications, without the need to learn and apply complex concurrency or other scaling patterns. 

[Redis](https://redis.io/) is an open source (BSD licensed), in-memory data structure store, used as a database, cache and message broker.

[StackExchange.Redis](https://stackexchange.github.io/StackExchange.Redis/) StackExchange.Redis is a high performance general purpose redis client for .NET languages (C# etc).

**Orleans.Clustering.Redis** is a package that uses Redis as a backend for cluster membership, making it easy to run Orleans clusters dynamically.
<!--
# TL;DR

If you want to quickly test it, clone this repo and go to the [Samples Directory](https://github.com/OrleansContrib/Orleans.Clustering.Redis/tree/master/samples) for instructions on how to run a sample cluster.
-->
# Overview

Redis is a straight key/value store. Membership data is stored into the key [clusterid.serviceid] as a hash.
<!--
# Installation

Installation is performed via [NuGet](https://www.nuget.org/packages?q=Orleans.Clustering.Redis)

From Package Manager:

> PS> Install-Package Orleans.Clustering.Redis

.Net CLI:

> \# dotnet add package Orleans.Clustering.Redis

# Configuration

A functional Redis database is required for this provider to work.
-->
## Silo
Tell Orleans runtime that we are going to use Redis as our Cluster Membership Provider:

```cs
var silo = new SiloHostBuilder()
        ...
        .UseRedisMembership(opt =>
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

## Client

Now that our silo is up and running, the Orleans client needs to connect to the Redis database to look for Orleans gateways.

```cs
var client = new ClientBuilder()
        ...
        .UseRedisMembership(opt =>
        {
            opt.ConnectionString = "host:port";
            opt.Database = 0;
        })
        ...
        .Build();
```

At the moment the gateway list is provided by the underlying membership provider directly.

Enjoy your Orleans application running without need to specify membership manually!
