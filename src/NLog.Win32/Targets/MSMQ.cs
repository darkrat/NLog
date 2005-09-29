using System;
using System.Messaging;
using System.Text;

using NLog.Config;

namespace NLog.Win32.Targets
{
    /// <summary>
    /// Writes log message to the specified message queue handled by MSMQ.
    /// </summary>
    /// <example>
    /// <p>
    /// To set up the target in the <a href="config.html">configuration file</a>, 
    /// use the following syntax:
    /// </p>
    /// <xml src="examples/targets/MSMQ/MSMQTarget.nlog" />
    /// <p>
    /// You can use a single target to write to multiple queues (similar to writing to multiple files with the File target).
    /// </p>
    /// <xml src="examples/targets/MSMQ/MSMQTargetMultiple.nlog" />
    /// <p>
    /// The above examples assume just one target and a single rule. 
    /// More configuration options are described <a href="config.html">here</a>.
    /// </p>
    /// <p>
    /// To set up the log target programmatically use code like this:
    /// </p>
    /// <cs src="examples/targets/MSMQ/MSMQTarget.cs" />
    /// </example>
    [Target("MSMQ")]
	public class MSMQTarget : Target
	{
        private Layout _queue;
        private Layout _label;
        private bool _createIfNotExists = false;
        private Encoding _encoding = System.Text.Encoding.UTF8;
        private bool _useXmlEncoding = false;
        private MessagePriority _messagePriority = MessagePriority.Normal;
        private bool _recoverableMessages = false;

        /// <summary>
        /// Name of the queue to write to.
        /// </summary>
        /// <remarks>
        /// To write to a private queue on a local machine use <c>.\private$\QueueName</c>.
        /// For other available queue names, consult MSMQ documentation.
        /// </remarks>
        [RequiredParameter]
        [AcceptsLayout]
        public string Queue
        {
            get { return _queue.Text; }
            set { _queue = new Layout(value); }
        }

        /// <summary>
        /// The label to associate with each message.
        /// </summary>
        /// <remarks>
        /// By default no label is associated.
        /// </remarks>
        [AcceptsLayout]
        public string Label
        {
            get { return _label.Text; }
            set { _label = new Layout(value); }
        }

        /// <summary>
        /// Create the queue if it doesn't exists.
        /// </summary>
        [System.ComponentModel.DefaultValue(false)]
        public bool CreateQueueIfNotExists
        {
            get { return _createIfNotExists; }
            set { _createIfNotExists = value; }
        }

        /// <summary>
        /// Encoding to be used when writing text to the queue.
        /// </summary>
        public string Encoding
        {
            get { return _encoding.EncodingName; }
            set { _encoding = System.Text.Encoding.GetEncoding(value); }
        }

        /// <summary>
        /// Use the XML format when serializing message.
        /// </summary>
        [System.ComponentModel.DefaultValue(false)]
        public bool UseXmlEncoding
        {
            get { return _useXmlEncoding; }
            set { _useXmlEncoding = value; }
        }

        /// <summary>
        /// Use recoverable messages (with guaranteed delivery).
        /// </summary>
        [System.ComponentModel.DefaultValue(false)]
        public bool Recoverable
        {
            get { return _recoverableMessages; }
            set { _recoverableMessages = value; }
        }

        /// <summary>
        /// Adds all layouts used by this target to the specified collection.
        /// </summary>
        /// <param name="layouts">The collection to add layouts to.</param>
        public override void PopulateLayouts(LayoutCollection layouts)
        {
            base.PopulateLayouts (layouts);
            layouts.Add(_queue);
            layouts.Add(_label);
        }

        /// <summary>
        /// Writes the specified logging event to a queue specified in the Queue 
        /// parameter.
        /// </summary>
        /// <param name="logEvent">The logging event.</param>
        protected override void Write(LogEventInfo logEvent)
        {
            if (_queue == null)
                return;

            string queue = _queue.GetFormattedMessage(logEvent);

            if (!MessageQueue.Exists(queue))
            {
                if (CreateQueueIfNotExists)
                    MessageQueue.Create(queue);
                else
                    return;
            }

            using (MessageQueue mq = new MessageQueue(queue))
            {
                Message msg = PrepareMessage(logEvent);
                if (msg != null)
                {
                    mq.Send(msg);
                }
            }
        }

        /// <summary>
        /// Prepares a message to be sent to the message queue.
        /// </summary>
        /// <param name="logEvent">The log event to be used when calculating label and text to be written.</param>
        /// <returns>The message to be sent</returns>
        /// <remarks>
        /// You may override this method in inheriting classes
        /// to provide services like encryption or message 
        /// authentication.
        /// </remarks>
        protected virtual Message PrepareMessage(LogEventInfo logEvent)
        {
            Message msg = new Message();
            if (_label != null)
            {
                msg.Label = _label.GetFormattedMessage(logEvent);
            }
            msg.Recoverable = _recoverableMessages;
            msg.Priority = _messagePriority;

            if (_useXmlEncoding)
            {
                msg.Body = CompiledLayout.GetFormattedMessage(logEvent);
            }
            else
            {
                byte[] dataBytes = _encoding.GetBytes(CompiledLayout.GetFormattedMessage(logEvent));

                msg.BodyStream.Write(dataBytes, 0, dataBytes.Length);
            }
            return msg;
        }
	}
}