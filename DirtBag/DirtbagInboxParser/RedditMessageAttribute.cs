using System;

using Microsoft.Azure.WebJobs.Description;
namespace RedditSharp.Azure {
    [AttributeUsage(AttributeTargets.Parameter)][Binding]
    public sealed class RedditMessageAttribute : Attribute {
        public RedditMessageAttribute( bool markAsRead, MessageType msgType ) {
            MarkAsRead = markAsRead;
            MessageTypes = msgType;
        }
        

        public bool MarkAsRead { get; private set; }
        public MessageType MessageTypes { get; set; }


        
    }
    [Flags]
    public enum MessageType {
        PrivateMessage = 0x01,
        CommentReply = 0x02,
        PostReply = 0x04,
        UsernameMention = 0x08,
        All = PrivateMessage | CommentReply | PostReply | UsernameMention
    }
}

