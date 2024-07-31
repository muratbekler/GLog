using Xunit.Abstractions;

namespace GLog.Extensions.Logging.Tests.Fixtures
{
    public class UdpGraylogFixture : GraylogFixture
    {
        public UdpGraylogFixture(IMessageSink messageSink) : base(messageSink)
        {
        }

        public override int InputPort => 12201;

        protected override string InputType => "org.graylog2.inputs.GLog.udp.GLogUDPInput";

        protected override string InputTitle => "GLog.Extensions.Logging.Tests.Udp";
    }
}
