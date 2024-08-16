# GLog.Extensions.Logging [![travis](https://img.shields.io/travis/com/mattwcole/GLog-extensions-logging?style=flat-square)](https://www.travis-ci.com/github/mattwcole/GLog-extensions-logging) [![downloads](https://img.shields.io/nuget/dt/GLog.Extensions.Logging?style=flat-square)](https://www.nuget.org/packages/GLog.Extensions.Logging) [![nuget](https://img.shields.io/nuget/v/GLog.Extensions.Logging.svg?style=flat-square)](https://www.nuget.org/packages/GLog.Extensions.Logging) [![license](https://img.shields.io/github/license/mattwcole/GLog-extensions-logging.svg?style=flat-square)](https://github.com/mattwcole/GLog-extensions-logging/blob/master/LICENSE.md)

[GLog](https://docs.graylog.org/en/3.1/pages/GLog.html) provider for [Microsoft.Extensions.Logging](https://docs.microsoft.com/en-us/dotnet/core/extensions/logging) for sending logs to [Graylog](https://www.graylog.org/), [Logstash](https://www.elastic.co/products/logstash) and more from .NET Standard 2.0+ compatible components.

## Usage

Start by installing the [NuGet package](https://www.nuget.org/packages/GLog.Extensions.Logging).

```sh
dotnet add package GLog.Extensions.Logging
```

### ASP.NET Core

Use the `LoggingBuilder.AddGLog()` extension method from the `GLog.Extensions.Logging` namespace when configuring your host.

```csharp
public static IHostBuilder CreateHostBuilder(string[] args) => Host
    .CreateDefaultBuilder(args)
    .ConfigureWebHostDefaults(webBuilder =>
    {
        webBuilder
            .UseStartup<Startup>()
            .ConfigureLogging((context, builder) => builder.AddGLog(options =>
            {
                // Optional customisation applied on top of settings in Logging:GLog configuration section.
                options.LogSource = context.HostingEnvironment.ApplicationName;
                options.AdditionalFields["machine_name"] = Environment.MachineName;
                options.AdditionalFields["app_version"] = Assembly.GetEntryAssembly()
                    ?.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
            }));
    });
```

Logger options are taken from the "GLog" provider section in `appsettings.json` in the same way as other providers. These are customised further in the code above.

```json5
{
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    },
    "GLog": {
      "Host": "localhost",
      "Port": 12201,                 // Not required if using default 12201.
      "Protocol": "UDP",             // Not required if using default UDP.
      "LogSource": "My.App.Name",    // Not required if set in code as above.
      "AdditionalFields": {          // Optional fields added to all logs.
        "foo": "bar"
      },
      "LogLevel": {
        "Microsoft": "Warning",
        "Microsoft.Hosting.Lifetime": "Information"
      }
    }
  }
}
```

```json5
{
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    },
    "GLog": {
      "Host": "localhost",
      "Port": 5672,                 // Not required if using default 5672.
      "Protocol": "RabbitMQ",       // Not required if using default RabbitMQ.
	  "Exchange": "log-messages",
      "ExchangeType": "fanout",
      "RoutingKey": null,
      "LogSource": "My.App.Name",    // Not required if set in code as above.
      "AdditionalFields": {          // Optional fields added to all logs.
        "foo": "bar",
		"protocol": "AMQP"
      },
      "LogLevel": {
        "Microsoft": "Warning",
        "Microsoft.Hosting.Lifetime": "Information"
      }
    }
  }
}
```

For a full list of options e.g. UDP/TCP/HTTP(S) settings, see [`GLogLoggerOptions`](src/GLog.Extensions.Logging/GLogLoggerOptions.cs). See the [samples](/samples) directory full examples. For more information on providers and logging in general, see the aspnetcore [logging documentation](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/logging).

### Auto Reloading Config

Settings can be changed at runtime and will be applied without the need for restarting your app. In the case of invalid config (e.g. missing hostname) the change will be ignored.

### Additional Fields

By default, `logger` and `exception` fields are included on all messages (the `exception` field is only added when an exception is passed to the logger). There are a number of other ways to attach data to logs.

#### Global Fields

Global fields can be added to all logs by setting them in `GLogLoggerOptions.AdditionalFields` (`machine_name`, `app_version` and `foo` in the previous example).

#### Scoped Fields

[Log scopes](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/logging/?view=aspnetcore-3.0#log-scopes) can also be used to attach fields to a group of related logs. Create a log scope with a [`ValueTuple<string, string>`](https://blogs.msdn.microsoft.com/dotnet/2017/03/09/new-features-in-c-7-0/), `ValueTuple<string, int/double/decimal>` (or any other numeric value) or `Dictionary<string, object>` to do so. _Note that any other types passed to `BeginScope()` will be ignored, including `Dictionary<string, string>`._

```csharp
using (_logger.BeginScope(("correlation_id", correlationId)))
{
    // Field will be added to all logs within this scope (using any ILogger<T> instance).
}

using (_logger.BeginScope(new Dictionary<string, object>
{
    ["order_id"] = orderId,
    ["customer_id"] = customerId
}))
{
    // Fields will be added to all logs within this scope (using any ILogger<T> instance).
}
```

#### Structured/Semantic Logging

[Semantic logging](https://softwareengineering.stackexchange.com/questions/312197/benefits-of-structured-logging-vs-basic-logging) is also supported meaning fields can be extracted from individual log lines like so.

```csharp
_logger.LogInformation("Order {order_id} took {order_time} seconds to process", orderId, orderTime);
```

Here the message will contain `order_id` and `order_time` fields.

#### Additional Fields Factory

It is possible to derive additional fields from log data with a factory function. This is useful for adding more information than what is provided by default e.g. the Microsoft log level or exception type.

```csharp
options.AdditionalFieldsFactory = (logLevel, eventId, exception) =>
    new Dictionary<string, object>
    {
        ["log_level"] = logLevel.ToString(),
        ["exception_type"] = exception?.GetType().ToString()
    };
```

### Log Filtering

The "GLog" provider can be filtered in the same way as the default providers (details [here](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/logging/?view=aspnetcore-5.0#how-filtering-rules-are-applied)).

### Compression

By default UDP messages of 512 bytes or more are GZipped however this behaviour can be disabled or altered with `GLogLoggerOptions.CompressUdp` and `GLogLoggerOptions.UdpCompressionThreshold`.

#### Logstash GLog Plugin

The [Logstash GLog plugin](https://www.elastic.co/guide/en/logstash/current/plugins-inputs-GLog.html) requires entirely compressed UDP messages in which case the UDP compression threshold must be set to 0.

### Testing

This repository contains a Docker Compose file that can be used for creating a local Graylog stack using the [Graylog Docker image](https://hub.docker.com/r/graylog/graylog/). This can be useful for testing application logs locally. Requires [Docker](https://www.docker.com/get-docker) and Docker Compose.

- `docker-compose up`
- Navigate to [http://localhost:9000](http://localhost:9000)
- Credentials: admin/admin
- Create a UDP input on port 12201 and set `GLogLoggerOptions.Host` to `localhost`.

## Contributing

Pull requests welcome! In order to run tests, first run `docker-compose up` to create the Graylog stack. Existing tests log messages and use the Graylog API to assert that they have been sent correctly. A UDP input will be created as part of the test setup (if not already present), so there is no need to create one manually. Build and tests are run on CI in Docker, meaning it is possible to run the build locally under identical conditions using `docker-compose -f docker-compose.ci.build.yml -f docker-compose.yml up --abort-on-container-exit`.
