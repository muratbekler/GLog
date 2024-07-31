using Xunit.Abstractions;

namespace GLog.Extensions.Logging.Tests.Fixtures
{
    public class HttpGraylogFixture : GraylogFixture
    {
        public HttpGraylogFixture(IMessageSink messageSink) : base(messageSink)
        {
        }

        public override int InputPort => 12202;

        protected override string InputType => "org.graylog2.inputs.GLog.http.GLogHttpInput";

        protected override string InputTitle => "GLog.Extensions.Logging.Tests.Http";
    }
}
