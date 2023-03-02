using TaiwoTech.MennoniteManners.App.Domain.Chat;
using TaiwoTech.MennoniteManners.App.Domain.User;

namespace TaiwoTech.MennoniteManners.App.Models
{
    public class ChatMessageDto
    {
        public UserName UserName { get; }
        public ChatMessage Message { get; }
        public bool IsOwnMessage { get; }
        public bool IsSystemMessage { get; }
        public DateTime TimeStamp { get; set; }

        public ChatMessageDto(
            UserName userName,
            ChatMessage message,
            int testAddSeconds = 0,
            bool isMine = false,
            bool isSystem = false
        )
        {
            UserName = userName;
            Message = message;
            IsOwnMessage = isMine;
            IsSystemMessage = isSystem;
            TimeStamp = DateTime.Now.AddSeconds(testAddSeconds);
        }
    }
}
