﻿using HitscordLibrary.Models.other;

namespace hitscord.Models.request;

public class ChangeIconUserDTO
{
	public required IFormFile Icon { get; set; }
}