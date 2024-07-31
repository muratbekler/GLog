using Xunit.Abstractions;

namespace GLog.Extensions.Logging.Tests.Fixtures
{
    public class TcpGraylogFixture : GraylogFixture
    {
        public TcpGraylogFixture(IMessageSink messageSink) : base(messageSink)
        {
        }
        public override int InputPort => 12201;

        protected override string InputType => "org.graylog2.inputs.GLog.tcp.GLogTCPInput";

        protected override string InputTitle => "GLog.Extensions.Logging.Tests.Tcp";
    }
}
