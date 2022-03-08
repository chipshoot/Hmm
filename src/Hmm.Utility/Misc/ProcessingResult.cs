using Hmm.Utility.Dal.Exceptions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Hmm.Utility.Misc
{
    /// <summary>
    /// The class is used for holding information during processing.
    /// <remarks>
    /// for class who use the <see cref="ProcessingResult"/> to hold the processing
    /// message should be careful to maintain the message before the each processing.
    /// sometime be sure to RESET the message list before processing to make sure the
    /// message is only caused by current call
    /// </remarks>
    /// </summary>
    public class ProcessingResult
    {
        public ProcessingResult(ILogger logger) : this()
        {
            Logger = logger;
        }

        public ProcessingResult()
        {
            Success = true;
            MessageList = new List<ReturnMessage>();
        }

        public bool Success { get; set; }

        public List<ReturnMessage> MessageList { get; }

        public ILogger Logger { get; }

        public bool HasInfo => HasMessageOf(MessageType.Info);

        public bool HasWarning => HasMessageOf(MessageType.Warning);

        public bool HasError => HasMessageOf(MessageType.Error) || HasMessageOf(MessageType.Fatal);

        public bool HasFatal => HasMessageOf(MessageType.Fatal);

        public void Rest()
        {
            Success = true;
            MessageList.Clear();
        }

        public void AddErrorMessage(string message, bool clearOldMessage = false, bool isFailure = true)
        {
            if (clearOldMessage)
            {
                MessageList.Clear();
            }

            Success = !isFailure;

            MessageList.Add(new ReturnMessage { Message = message, Type = MessageType.Error });
            LogMessage();
        }

        public void AddWaningMessage(string message, bool clearOldMessage = false, bool isFailure = false)
        {
            if (clearOldMessage)
            {
                MessageList.Clear();
            }

            Success = !isFailure;

            MessageList.Add(new ReturnMessage { Message = message, Type = MessageType.Warning });
            LogMessage();
        }

        public void AddInfoMessage(string message, bool clearOldMessage = false)
        {
            if (clearOldMessage)
            {
                MessageList.Clear();
            }

            MessageList.Add(new ReturnMessage { Message = message, Type = MessageType.Info });
            LogMessage();
        }

        public void PropagandaResult(ProcessingResult innerResult)
        {
            if (innerResult == null)
            {
                return;
            }

            Rest();
            Success = innerResult.Success;
            MessageList.AddRange(innerResult.MessageList);
            LogMessage();
        }

        public void WrapException(Exception ex)
        {
            Rest();
            Success = false;
            AddErrorMessage(ex.GetAllMessage());
            LogMessage();
        }

        public string GetWholeMessage()
        {
            switch (MessageList.Count)
            {
                case 0:
                    return string.Empty;

                case 1:
                    var msg = MessageList.First();
                    return $"{msg.Type}: {msg.Message}";

                default:
                    return MessageList.Aggregate(string.Empty, (current, message) => $"{current}|{message.Type}: {message.Message}");
            }
        }

        private void LogMessage()
        {
            if (Logger == null)
            {
                return;
            }

            if (!MessageList.Any())
            {
                return;
            }

            foreach (var msg in MessageList)
            {
                switch (msg.Type)
                {
                    case MessageType.Info:
                        Logger.LogInformation(msg.Message);
                        break;

                    case MessageType.Error:
                        Logger.LogError(msg.Message);
                        break;

                    case MessageType.Warning:
                        Logger.LogWarning(msg.Message);
                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        private bool HasMessageOf(MessageType msgType)
        {
            var hasMsg = MessageList != null && MessageList.Any(m => m.Type == msgType);
            return hasMsg;
        }
    }
}