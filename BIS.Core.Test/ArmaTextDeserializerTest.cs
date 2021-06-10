using BIS.Core.Serialization;
using Xunit;

namespace BIS.Core.Test.Serialization
{
    public class ArmaTextDeserializerTest
    {
        [Fact]
        public void ParseSimpleArray()
        {
            var value = ArmaTextDeserializer.ParseSimpleArray("[\"Hello \"\"world\"\" !\",123.456,null,-789.123,\"Hello \"\"world\"\" !\", true, false]");
            Assert.Equal(new object[] { "Hello \"world\" !", 123.456d, null, -789.123d, "Hello \"world\" !", true, false}, value);
        }
    }
}
