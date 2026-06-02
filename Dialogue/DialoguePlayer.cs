using Godot;
using System;
using System.Collections.Generic;
using 你的项目.Scripts.角色;
using 你的项目.Scripts.资源;
using 你的项目.Scripts.管理器;

/// <summary>
/// Manages dialogue playback, option execution, scene transitions, and UI display for conversations.
/// </summary>
[GlobalClass]
public partial class DialoguePlayer : Node
{
	public static DialoguePlayer 实例 { get; private set; }
	public static DialogueSequence 待触发的传送后对话 = null;
	public static bool 等待传送后触发 = false;
	public static bool 禁止NPC交互 { get; set; } = false;

	private DialogueUI _界面;
	private OptionExecutor _执行器;
	private 对话状态存储 _状态存储;

	private bool _待显示场景 = false;
	private string _待显示场景路径;
	private DialogueSequence _待显示场景后续对话;
	private List<历史条目> _历史记录 = new List<历史条目>();
	private 场景展示UI _场景展示UI;
	private bool _等待场景展示结束 = false;
	private DialogueSequence _场景展示后续对话;

	private DialogueSequence 当前序列;
	private int 当前句子索引 = 0;
	private bool _对话中 = false;
	public bool 对话进行中 => _对话中;

	private Dictionary<string, bool> 已改变对话记录 = new Dictionary<string, bool>();
	private Dictionary<string, HashSet<int>> 序列已选选项记录 = new Dictionary<string, HashSet<int>>();

	private class 回到选项数据
	{
		public DialogueSequence 原始序列;
		public int 选项索引;
	}
	private 回到选项数据 _回到选项数据;

	public override void _Ready()
	{
		if (实例 != null)
		{
			QueueFree();
			return;
		}
		实例 = this;
		ProcessMode = ProcessModeEnum.Always;

		_执行器 = new OptionExecutor();
		_状态存储 = new 对话状态存储();
		_执行器.对话状态存储实例 = _状态存储;

		if (TransitionManager.实例 != null)
			TransitionManager.实例.转场完成 += 当场切换完成;
		
		_场景展示UI = GetNodeOrNull<场景展示UI>("/root/场景展示UI");
		if (_场景展示UI != null)
			_场景展示UI.场景展示结束 += 当场景展示结束;
	}

	public void 场景加载完成()
	{
		if (等待传送后触发 && 待触发的传送后对话 != null)
			CallDeferred(nameof(延迟开始传送后对话));
	}

	private void 返回到原始选项()
	{
		if (_回到选项数据 == null) return;
		var 原始序列 = _回到选项数据.原始序列;
		int 已选索引 = _回到选项数据.选项索引;
		当前序列 = 原始序列;
		var 已选索引集合 = 获取当前序列已选选项();
		var 可显示的选项 = new List<BranchOption>();
		for (int i = 0; i < 原始序列.Options.Count; i++)
		{
			var opt = 原始序列.Options[i];
			if (!opt.是否回到选项 && 已选索引集合.Contains(i))
				continue;
			可显示的选项.Add(opt);
		}
		if (可显示的选项.Count > 0)
			_界面?.显示选项(可显示的选项, 替换玩家名字, 已选索引集合);
		else
			结束对话();
		_回到选项数据 = null;
	}

	private void 当场景展示结束(DialogueSequence 后续对话)
	{
		_等待场景展示结束 = false;
		_对话中 = false;
		if (后续对话 != null)
			开始对话(后续对话);
		else
			结束对话();
	}

	private void 延迟开始传送后对话()
	{
		开始对话(待触发的传送后对话, 保持历史: true);
		等待传送后触发 = false;
		待触发的传送后对话 = null;
	}

	public void 开始对话(DialogueSequence 序列, bool 保持历史 = false, bool 保留回到选项数据 = false)
	{
		if (序列 == null || _对话中) return;
		if (!保持历史)
			_历史记录.Clear();
		DialogueSequence 实际序列 = 获取实际序列(序列);
		if (实际序列.Sentences.Count == 0) return;

		当前序列 = 实际序列;
		当前句子索引 = 0;
		_对话中 = true;
		if (!保留回到选项数据)
			_回到选项数据 = null;
		_历史记录.Clear();
		显示或创建界面();
		_界面.显示对话面板();
		EmitSignal(nameof(对话开始));
		显示当前句子();
		禁止玩家移动(true);
	}

	private void 刷新选项列表(int 排除选项索引)
	{
		var 已选索引集合 = 获取当前序列已选选项();
		var 可显示的选项 = new List<BranchOption>();
		for (int i = 0; i < 当前序列.Options.Count; i++)
		{
			var opt = 当前序列.Options[i];
			可显示的选项.Add(opt);
		}
		if (可显示的选项.Count > 0)
			_界面?.显示选项(可显示的选项, 替换玩家名字, 已选索引集合);
		else
			_界面?.隐藏对话面板();
	}

	public async void 选择选项(int 选项索引)
	{
		if (!_对话中 || 当前序列 == null) return;
		if (选项索引 < 0 || 选项索引 >= 当前序列.Options.Count) return;

		BranchOption 选项 = 当前序列.Options[选项索引];

		if (选项.允许存档)
		{
			if (全局存档UI管理器.实例 == null) return;
			结束对话();
			await 全局存档UI管理器.实例.请求保存并等待();
			return;
		}

		记录已选选项(选项索引);
		刷新选项列表(选项索引);
		var (结果, 场景路径, 后续对话) = _执行器.执行(选项, 当前序列, 选项索引);

		switch (结果)
		{
			case OptionExecutor.执行结果.继续对话:
				if (后续对话 != null)
				{
					_对话中 = false;
					_界面?.隐藏对话面板();
					if (选项.是否回到选项)
						开始对话并回到选项(后续对话, 当前序列, 选项索引);
					else
						开始对话(后续对话, 保持历史: true);
				}
				else
					结束对话();
				break;
			case OptionExecutor.执行结果.结束对话:
				结束对话();
				break;
			case OptionExecutor.执行结果.切换场景:
				结束对话();
				if (TransitionManager.实例 != null)
					TransitionManager.实例.开始转场(场景路径, 选项.转场动画配置, null, false);
				else
					GetTree().ChangeSceneToFile(场景路径);
				break;
			case OptionExecutor.执行结果.显示场景UI:
				显示场景UI(场景路径, 后续对话);
				break;
		}
	}

	private void 显示或创建界面()
	{
		if (_界面 == null || !IsInstanceValid(_界面))
		{
			var 界面场景 = GD.Load<PackedScene>("res://对话系统/脚本/对话UI.tscn");
			_界面 = 界面场景.Instantiate<DialogueUI>();
			GetTree().CurrentScene.AddChild(_界面);
			_界面.请求下一句 += 推进对话;
			_界面.选项被选择 += 选择选项;
		}
		_界面.显示对话面板();
	}

	private void 显示当前句子()
	{
		var 句子 = 当前序列.Sentences[当前句子索引];
		处理句子跟随者操作(句子);
		string 显示文本 = 替换玩家名字(句子.Text);
		if (!string.IsNullOrEmpty(显示文本))
		{
			string 说话人显示 = 替换玩家名字(句子.SpeakerName);
			if (string.IsNullOrEmpty(说话人显示)) 说话人显示 = "未知";
			_历史记录.Add(new 历史条目 { 说话人 = 说话人显示, 文本 = 显示文本 });
		}
		float? 自定义速度 = null;
		if (句子.StyleProperties != null && 句子.StyleProperties.TryGetValue("text_speed", out Variant 速度))
			自定义速度 = (float)速度;
		_界面?.显示句子(显示文本, 替换玩家名字(句子.SpeakerName), 句子.Avatar, 自定义速度);
	}

	private void 推进对话()
	{
		if (!_对话中) return;
		当前句子索引++;
		if (当前句子索引 < 当前序列.Sentences.Count)
		{
			显示当前句子();
		}
		else
		{
			if (当前序列.Options != null && 当前序列.Options.Count > 0)
			{
				var 可显示的选项 = new List<BranchOption>();
				var 已选索引集合 = 获取当前序列已选选项();
				for (int i = 0; i < 当前序列.Options.Count; i++)
					可显示的选项.Add(当前序列.Options[i]);
				if (可显示的选项.Count > 0)
					_界面?.显示选项(可显示的选项, 替换玩家名字, 已选索引集合);
				else
					结束对话();
			}
			else
			{
				if (_回到选项数据 != null)
					返回到原始选项();
				else
					结束对话();
			}
		}
	}

	public void 重置传送后对话记录()
	{
		_状态存储.重置所有对话记录();
	}

	public void 设置待显示场景(string 场景路径, DialogueSequence 后续对话)
	{
		_待显示场景 = true;
		_待显示场景路径 = 场景路径;
		_待显示场景后续对话 = 后续对话;
	}

	private void 结束对话()
	{
		_对话中 = false;
		_界面?.隐藏对话面板();
		if (当前序列 != null && 当前序列.可改变对话)
		{
			string 路径 = 当前序列.ResourcePath;
			_状态存储.设置已改变(路径, true);
		}
		禁止玩家移动(false);
		EmitSignal(nameof(对话结束));

		if (_待显示场景)
		{
			_待显示场景 = false;
			显示场景UI(_待显示场景路径, _待显示场景后续对话);
		}
	}

	private void 开始对话并回到选项(DialogueSequence 目标序列, DialogueSequence 原始序列, int 选项索引)
	{
		_回到选项数据 = new 回到选项数据 { 原始序列 = 原始序列, 选项索引 = 选项索引 };
		开始对话(目标序列, 保持历史: false, 保留回到选项数据: true);
	}

	private DialogueSequence 获取实际序列(DialogueSequence 原始序列)
	{
		if (!原始序列.可改变对话) return 原始序列;
		string 路径 = 原始序列.ResourcePath;
		if (_状态存储.是否已改变过(路径) && 原始序列.改变后序列 != null)
			return 原始序列.改变后序列;
		return 原始序列;
	}

	private void 记录已选选项(int 索引)
	{
		if (当前序列 == null) return;
		string key = 当前序列.ResourcePath;
		if (!序列已选选项记录.ContainsKey(key))
			序列已选选项记录[key] = new HashSet<int>();
		序列已选选项记录[key].Add(索引);
	}

	private HashSet<int> 获取当前序列已选选项()
	{
		if (当前序列 == null) return new HashSet<int>();
		string key = 当前序列.ResourcePath;
		if (!序列已选选项记录.ContainsKey(key))
			序列已选选项记录[key] = new HashSet<int>();
		return 序列已选选项记录[key];
	}

	private string 替换玩家名字(string 原文)
	{
		if (string.IsNullOrEmpty(原文)) return 原文;
		string 名字 = 玩家数据管理器.实例?.玩家名字 ?? "冒险者";
		return 原文.Replace("{玩家名字}", 名字);
	}

	private void 处理句子跟随者操作(DialogueSentence 句子)
	{
		var 玩家组 = GetTree().GetNodesInGroup("玩家");
		if (玩家组.Count == 0) return;
		var 玩家 = 玩家组[0] as PlayerController;
		if (玩家 == null) return;

		if (句子.移除所有跟随者)
			玩家.移除所有跟随者();

		if (句子.添加的跟随者预制体列表 != null && 句子.添加的跟随者预制体列表.Count > 0)
		{
			foreach (var 预制体 in 句子.添加的跟随者预制体列表)
				if (预制体 != null)
					玩家.添加跟随者(预制体, 2);
		}
	}

	private void 禁止玩家移动(bool 禁止)
	{
		var 玩家组 = GetTree().GetNodesInGroup("玩家");
		foreach (var 节点 in 玩家组)
		{
			if (节点 is PlayerController 玩家)
			{
				if (禁止)
					玩家.开始过场动画();
				else
					玩家.结束过场动画();
			}
		}
	}

	private void 显示场景UI(string 场景路径, DialogueSequence 后续对话)
	{
		if (_场景展示UI == null)
		{
			结束对话();
			return;
		}
		_界面.隐藏对话面板();
		_对话中 = false;
		_等待场景展示结束 = true;
		_场景展示后续对话 = 后续对话;
		_场景展示UI.显示场景(场景路径, 后续对话, true);
	}

	private void 当场切换完成(string 新场景路径)
	{
		if (等待传送后触发 && 待触发的传送后对话 != null)
			CallDeferred(nameof(延迟开始传送后对话));
	}

	public List<历史条目> 获取历史记录() => new List<历史条目>(_历史记录);

	[Signal] public delegate void 对话开始EventHandler();
	[Signal] public delegate void 对话结束EventHandler();
}
