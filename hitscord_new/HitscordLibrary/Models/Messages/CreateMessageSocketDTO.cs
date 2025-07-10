using HitscordLibrary.Models.other;

namespace HitscordLibrary.Models.Messages;

public class CreateMessageSocketDTO
{
    public required string Token { get; set; }
    public required Guid ChannelId { get; set; }
    public Guid? ReplyToMessageId { get; set; }
    public required MessageTypeEnum MessageType { get; set; }


    public ClassicMessageSocketDTO? Classic { get; set; }
    public VoteMessageSocketDTO? Vote { get; set; }

	public void Validation(Guid UserId)
    {
        if (ChannelId == Guid.Empty)
        {
            throw new CustomExceptionUser("ChannelId cannot be empty.", "CreateMessage", "ChannelId", 400, "ChannelId не может быть пустым.", "Валидация сообщения", UserId);
        }

        if (MessageType == MessageTypeEnum.Classic)
        {
            if (Classic == null)
            {
				throw new CustomExceptionUser("Classic data cannot be empty.", "CreateMessage", "Classic", 400, "Classic не может быть пустым.", "Валидация сообщения", UserId);
			}

            Classic.ValidationClassic(UserId);
        }

        if (MessageType == MessageTypeEnum.Vote)
        {
			if (Vote == null)
			{
				throw new CustomExceptionUser("Vote data cannot be empty.", "CreateMessage", "Vote", 400, "Vote не может быть пустым.", "Валидация сообщения", UserId);
			}

			Vote.ValidationVote(UserId);
		}
    }
}
