
using System;
using System.Collections.Generic;
using System.Linq;

namespace WebSocket.Portable.Internal
{
    public delegate void LogReceiver(LogLevel level, string message);

    public sealed class LogManager
    {
        public static readonly LogManager Instance = new LogManager();

        private readonly List<LogReceiver> _receivers;

        private LogManager()
        {
            _receivers = new List<LogReceiver>();
        }

        public void LogMessage(Type type, LogLevel logLevel, string message)
        {
            IList<LogReceiver> receivers;
            lock (_receivers)
            {
                receivers = _receivers.ToList();
            }
            
            foreach (var receiver in receivers)
            {
                try
                {
                    receiver(logLevel, message);
                }
                // ReSharper disable once EmptyGeneralCatchClause
                catch (Exception)
                {
                    
                }
            }
        }

        public void AddReceiver(LogReceiver receiver)
        {
            if (receiver == null)
                throw new ArgumentNullException("receiver");

            lock (_receivers)
            {
                _receivers.Add(receiver);
            }
        }

        public void Remove(LogReceiver receiver)
        {
            lock (_receivers)
                _receivers.Remove(receiver);
        }
    }
}
