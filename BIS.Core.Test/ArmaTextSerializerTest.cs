using System;
using BIS.Core.Serialization;
using Xunit;

namespace BIS.Core.Test.Serialization
{
    public class ArmaTextSerializerTest
    {
        [Fact]
        public void ToSimpleArrayString()
        {
            var value = ArmaTextSerializer.ToSimpleArrayString(new object[] { "Hello \"world\" !", 123.456d, null, -789.123d, "Hello \"world\" !", true, false });
            Assert.Equal("[\"Hello \"\"world\"\" !\",123.456,null,-789.123,\"Hello \"\"world\"\" !\",true,false]", value);
        }
    }
}
