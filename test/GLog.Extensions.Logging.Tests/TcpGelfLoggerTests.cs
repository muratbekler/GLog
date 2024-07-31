using GLog.Extensions.Logging.Tests.Fixtures;
using Xunit;

namespace GLog.Extensions.Logging.Tests
{
    public class TcpGLogLoggerTests : GLogLoggerTests, IClassFixture<TcpGraylogFixture>
    {
        public TcpGLogLoggerTests(TcpGraylogFixture graylogFixture) : base(graylogFixture,
            new LoggerFixture(new GLoggerOptions
            {
                Host = GraylogFixture.Host,
                Port = graylogFixture.InputPort,
                Protocol = GProtocol.Tcp,
                LogSource = nameof(TcpGLogLoggerTests)
            }))
        {
        }
    }
}
