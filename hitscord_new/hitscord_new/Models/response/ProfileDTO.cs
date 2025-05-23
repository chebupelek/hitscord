﻿using hitscord.Models.db;

namespace hitscord.Models.response;

public class ProfileDTO
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Tag { get; set; }
    public string Mail { get; set; }
    public DateOnly AccontCreateDate { get; set; }
	public bool Notifiable { get; set; }
	public bool FriendshipApplication { get; set; }
	public bool NonFriendMessage { get; set; }
	public ProfileDTO(UserDbModel user)
    {
        Id = user.Id;
        Name = user.AccountName;
        Tag = user.AccountTag;
        Mail = user.Mail;
        AccontCreateDate = DateOnly.FromDateTime((DateTime)user.AccountCreateDate);
        Notifiable = user.Notifiable;
        FriendshipApplication = user.FriendshipApplication;
        NonFriendMessage = user.NonFriendMessage;
    }
}