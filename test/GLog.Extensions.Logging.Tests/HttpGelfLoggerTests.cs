using GLog.Extensions.Logging.Tests.Fixtures;
using Xunit;

namespace GLog.Extensions.Logging.Tests
{
    public class HttpGLogLoggerTests : GLogLoggerTests, IClassFixture<HttpGraylogFixture>
    {
        public HttpGLogLoggerTests(HttpGraylogFixture graylogFixture) : base(graylogFixture,
            new LoggerFixture(new GLoggerOptions
            {
                Host = GraylogFixture.Host,
                Port = graylogFixture.InputPort,
                Protocol = GProtocol.Http,
                LogSource = nameof(HttpGLogLoggerTests)
            }))
        {
        }
    }
}
