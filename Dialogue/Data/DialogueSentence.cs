using Godot;
using System.Collections.Generic;

[GlobalClass]
public partial class DialogueSentence : Resource
{
	[Export] public string Text { get; set; } = "";
	[Export] public string SpeakerName { get; set; } = "";
	[Export] public Texture2D Avatar { get; set; }
	
	[Export] public Godot.Collections.Dictionary<string, Variant> StyleProperties { get; set; } 
		= new Godot.Collections.Dictionary<string, Variant>();
	
	// ----- 新增：跟随者操作 -----
	[Export] public Godot.Collections.Array<PackedScene> 添加的跟随者预制体列表 { get; set; }
	[Export] public bool 移除所有跟随者 { get; set; } = false;
	
	public DialogueSentence() {}
}
