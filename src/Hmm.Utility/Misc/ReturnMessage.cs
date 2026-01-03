namespace Hmm.Utility.Misc
{
    public class ReturnMessage
    {
       /// <summary>
        /// The message content
        /// </summary>
        public string Message { get; init; }

        /// <summary>
        /// The type/severity of the message
        /// </summary>
        public MessageType Type { get; init; }

        /// <summary>
        /// Creates a new return message
        /// </summary>
        public ReturnMessage()
        {
        }

        /// <summary>
        /// Creates a new return message with specified values
        /// </summary>
        public ReturnMessage(string message, MessageType type)
        {
            Message = message;
            Type = type;
        }
    }
}