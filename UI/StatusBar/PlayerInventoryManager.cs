using Godot;
using System.Collections.Generic;

/// <summary>
/// 玩家背包管理器，负责道具的添加、使用、数量叠加及UI刷新通知。
/// </summary>
public partial class PlayerInventoryManager : Node
{
	private static PlayerInventoryManager _实例;
	public static PlayerInventoryManager 实例 => _实例;

	/// <summary>玩家当前持有的道具列表（引用类型，直接操作需谨慎）。</summary>
	public List<道具数据> 玩家道具列表 = new List<道具数据>();

	public override void _Ready()
	{
		if (_实例 == null)
		{
			_实例 = this;
			ProcessMode = ProcessModeEnum.Always;
			// 不再硬编码初始道具
			EmitSignal(SignalName.道具列表已更新);
		}
		else
		{
			QueueFree();
		}
	}

	/// <summary>添加道具，若已有则叠加数量。</summary>
	public void 添加道具(道具数据 新道具)
	{
		var 已有 = 玩家道具列表.Find(p => p.道具ID == 新道具.道具ID);
		if (已有 != null)
		{
			已有.数量 += 新道具.数量;
		}
		else
		{
			玩家道具列表.Add(新道具);
		}
		EmitSignal(SignalName.道具列表已更新);
	}

	/// <summary>使用一个道具（减少数量），数量归零时移除。</summary>
	public void 使用道具(道具数据 道具)
	{
		道具.数量--;
		if (道具.数量 <= 0)
		{
			玩家道具列表.Remove(道具);
		}
		EmitSignal(SignalName.道具列表已更新);
	}

	[Signal]
	public delegate void 道具列表已更新EventHandler();
}
