using Hmm.Utility.Misc;
using Xunit;

namespace Hmm.Utility.Tests
{
    public class ProcessingResultTests
    {
        [Theory]
        [InlineData("This is Info", MessageType.Info, true, false, false, false)]
        [InlineData("This is Warning", MessageType.Warning, false, true, false, false)]
        [InlineData("This is Error", MessageType.Error, false, false, true, false)]
        [InlineData("This is Fatal", MessageType.Fatal, false, false, true, true)]
        public void Can_Get_Right_Result_Flag(string message, MessageType messageType, bool hasInfo, bool hasWarning, bool hasError, bool hasFatal)
        {
            // Arrange
            var result = new ProcessingResult();

            // Act
            result.MessageList.Add(new ReturnMessage { Message = message, Type = messageType });

            //Assert
            Assert.Equal(hasInfo, result.HasInfo);
            Assert.Equal(hasWarning, result.HasWarning);
            Assert.Equal(hasError, result.HasError);
            Assert.Equal(hasFatal, result.HasFatal);
        }
    }
}