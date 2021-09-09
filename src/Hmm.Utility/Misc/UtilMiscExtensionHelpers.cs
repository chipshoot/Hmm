using System.Collections.Generic;
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
            return returnMessages.Any(msg=>msg.Message.Equals(message));
        }
    }
}