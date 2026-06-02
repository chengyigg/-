using Godot;
using System;

[GlobalClass]
public partial class DialogueSequence : Resource
{
	[Export] public Godot.Collections.Array<DialogueSentence> Sentences { get; set; } 
		= new Godot.Collections.Array<DialogueSentence>();
	
	[Export] public Godot.Collections.Array<BranchOption> Options { get; set; } 
		= new Godot.Collections.Array<BranchOption>();
	
	// 可改变对话相关
	[Export] public bool 可改变对话 { get; set; } = false;
	[Export] public DialogueSequence 改变后序列 { get; set; }
	
	public DialogueSequence() {}
}
