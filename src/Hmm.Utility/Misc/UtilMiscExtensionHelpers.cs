using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace Hmm.Utility.Misc
{
    public static class UtilMiscExtensionHelpers
    {
        public static bool Contains(this ReturnMessage returnMsg, string message)
        {
            return returnMsg.Message.Equals(message);
        }

        public static bool Contains(this IEnumerable<ReturnMessage> returnMessages, string message)
        {
            return returnMessages.Any(msg => msg.Message.Equals(message));
        }

        public static bool HasReturnedMessage<T>(this ProcessingResult<T> result)
        {
            return result != null && (result.HasInfo || result.HasWarning || result.HasError || result.HasFatal);
        }

        public static void LogMessages<T>(this ProcessingResult<T> result, ILogger logger)
        {
            if (result == null || logger == null || !result.Messages.Any())
            {
                return;
            }

            foreach (var msg in result.Messages)
            {
                switch (msg.Type)
                {
                    case MessageType.Info:
                        logger.LogInformation(msg.Message);
                        break;

                    case MessageType.Error:
                    case MessageType.Fatal:
                        logger.LogError(msg.Message);
                        break;

                    case MessageType.Warning:
                        logger.LogWarning(msg.Message);
                        break;
                }
            }
        }
    }
}