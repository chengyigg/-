using Godot;
using System.Collections.Generic;
using 你的项目.Scripts.角色;

[GlobalClass]
public partial class FollowerManager : Node
{
	public static FollowerManager 实例 { get; private set; }
	
	private List<FollowerController> _所有跟随者 = new List<FollowerController>();
	
	public override void _Ready()
	{
		if (实例 != null)
		{
			QueueFree();
			return;
		}
		实例 = this;
		ProcessMode = ProcessModeEnum.Always;
	}
	
	public Godot.Collections.Array<string> 获取所有跟随者预制体路径()
	{
		var 路径列表 = new Godot.Collections.Array<string>();
		foreach (var 跟随 in _所有跟随者)
		{
			if (IsInstanceValid(跟随) && !string.IsNullOrEmpty(跟随.预制体资源路径))
			{
				路径列表.Add(跟随.预制体资源路径);
			}
		}
		return 路径列表;
	}

	public void 重建跟随者(Godot.Collections.Array<string> 路径列表, int 默认滞后格子数 = 2)
	{
		移除所有跟随者();
		
		foreach (string 路径 in 路径列表)
		{
			var 预制体 = GD.Load<PackedScene>(路径);
			if (预制体 != null)
			{
				添加跟随者(预制体, 默认滞后格子数, 路径);
			}
		}
	}

	public void 添加跟随者(PackedScene 预制体, int 滞后格子数 = 2, string 预制体路径 = null)
	{
		if (预制体 == null) return;
		var 跟随者 = 预制体.Instantiate<FollowerController>();
		跟随者.滞后格子数 = 滞后格子数;
		if (!string.IsNullOrEmpty(预制体路径))
			跟随者.预制体资源路径 = 预制体路径;
		else if (预制体.ResourcePath != null)
			跟随者.预制体资源路径 = 预制体.ResourcePath;
		
		AddChild(跟随者);
		_所有跟随者.Add(跟随者);
		
		// 初始化跟随者：跳转到玩家当前位置并清空队列
var 玩家 = 获取当前玩家() as PlayerController;
if (玩家 != null)
	跟随者.路径队列更新();
	}

	public void 重置所有跟随者()
	{
		foreach (var 跟随 in _所有跟随者)
		{
			if (IsInstanceValid(跟随))
				跟随.重新初始化();   // 清空队列并跳回玩家位置
		}
	}

	public void 移除所有跟随者()
	{
		foreach (var 跟随 in _所有跟随者)
		{
			if (IsInstanceValid(跟随))
			{
				跟随.SetPhysicsProcess(false);
				跟随.QueueFree();
			}
		}
		_所有跟随者.Clear();
	}

	public void 隐藏所有跟随者()
	{
		foreach (var 跟随 in _所有跟随者)
		{
			if (IsInstanceValid(跟随))
				跟随.Visible = false;
		}
	}

	public void 显示所有跟随者()
	{
		foreach (var 跟随 in _所有跟随者)
		{
			if (IsInstanceValid(跟随))
				跟随.Visible = true;
		}
	}

	private Node2D 获取当前玩家()
	{
		var 玩家组 = GetTree().GetNodesInGroup("玩家");
		if (玩家组.Count > 0)
			return 玩家组[0] as Node2D;
		return null;
	}
	
	public Vector2? 获取玩家当前位置()
	{
		var 玩家 = 获取当前玩家();
		return 玩家?.GlobalPosition;
	}
	
	public void 移除跟随者实例(FollowerController 跟随者)
	{
		if (_所有跟随者.Contains(跟随者))
			_所有跟随者.Remove(跟随者);
	}
}
