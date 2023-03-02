using TaiwoTech.MennoniteManners.App.Domain.Chat;
using TaiwoTech.MennoniteManners.App.Domain.User;
using TaiwoTech.MennoniteManners.App.Models;

namespace TaiwoTech.MennoniteManners.App.Constants
{
    internal static class ChatMessageTemplates
    {
        public static ChatMessageDto NewUser(UserName username)
        {
            var message = new ChatMessage($"{username} has joined the game");
            return new ChatMessageDto(username, message, isSystem: true);
        }

        public static ChatMessageDto RemoveUser(UserName userName)
        {
            var message = new ChatMessage($"{userName} has left the game");
            return new ChatMessageDto(userName, message, isSystem: true);
        }

        public static ChatMessageDto NewHost(UserName username)
        {
            var message = new ChatMessage($"{username} is now the host");
            return new ChatMessageDto(username, message, isSystem: true);
        }
    }
}
