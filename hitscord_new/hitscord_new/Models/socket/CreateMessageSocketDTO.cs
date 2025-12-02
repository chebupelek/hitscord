using hitscord.Models.other;

namespace hitscord.Models.Sockets;

public class CreateMessageSocketDTO
{
    public required string Token { get; set; }
    public required Guid ChannelId { get; set; }
    public long? ReplyToMessageId { get; set; }
    public required MessageTypeEnum MessageType { get; set; }


    public ClassicMessageSocketDTO? Classic { get; set; }
    public VoteMessageSocketDTO? Vote { get; set; }

	public void Validation()
    {
        if (ChannelId == Guid.Empty)
        {
            throw new CustomException("ChannelId cannot be empty.", "CreateMessage", "ChannelId", 400, "ChannelId не может быть пустым.", "Валидация сообщения");
        }

        if (MessageType == MessageTypeEnum.Classic)
        {
            if (Classic == null)
            {
				throw new CustomException("Classic data cannot be empty.", "CreateMessage", "Classic", 400, "Classic не может быть пустым.", "Валидация сообщения");
			}

            Classic.ValidationClassic();
        }

        if (MessageType == MessageTypeEnum.Vote)
        {
			if (Vote == null)
			{
				throw new CustomException("Vote data cannot be empty.", "CreateMessage", "Vote", 400, "Vote не может быть пустым.", "Валидация сообщения");
			}

			Vote.ValidationVote();
		}
    }
}
