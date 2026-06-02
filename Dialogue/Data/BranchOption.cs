using Godot;
using System.Collections.Generic;
using 你的项目.Scripts.资源;

[GlobalClass]
public partial class BranchOption : Resource
{
	[Export] public string 选项文本 { get; set; } = "";
	[Export] public string 选项ID { get; set; } = "";
	[Export] public DialogueSequence 目标序列 { get; set; }
	
	[Export] public bool 显示场景 { get; set; } = false;
	[Export] public string 场景路径 { get; set; } = "";
	[Export] public DialogueSequence 场景后DialogueSequence { get; set; }
	
	[Export] public bool 触发场景切换 { get; set; } = false;
	[Export] public string 目标场景路径 { get; set; } = "";
	[Export] public TransitionAnimationResource 转场动画配置 { get; set; }
	
	[Export] public bool 传送后触发对话 { get; set; } = false;
	[Export] public DialogueSequence 传送后DialogueSequence { get; set; }
	
	// 新增：对话结束后自动切换场景
	[Export] public bool 对话后触发场景切换 { get; set; } = false;
	[Export] public string 对话后目标场景路径 { get; set; } = "";
	[Export] public 生成卡组资源 对话后怪物卡组资源 { get; set; }
	
[Export] public bool 返回原地 { get; set; } = false;
	
	[Export] public bool 是否只能触发一次 { get; set; } = false;
	[Export] public bool 是否回到选项 { get; set; } = false;
	
	// 新增：怪物卡组资源（用于触发战斗场景时使用）
	[Export] public 生成卡组资源 怪物卡组资源 { get; set; }
	[Export] public int 战斗胜利金币奖励 { get; set; } = 0;
	
	[Export] public 生成卡组资源 奖励卡组 { get; set; }
[Export] public string 触发条件名称 { get; set; } = "";
[Export] public bool 允许存档 { get; set; } = false;
	public BranchOption() {}
}
