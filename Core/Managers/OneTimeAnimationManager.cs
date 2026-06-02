using Godot;
using System.Collections.Generic;

namespace 你的项目.Scripts.管理器
{
	[GlobalClass]
	public partial class OneTimeAnimationManager : Node
	{
		private static OneTimeAnimationManager _实例;
		public static OneTimeAnimationManager 实例 => _实例;

		private Dictionary<string, Dictionary<string, string>> _注册表 = new();

		public override void _Ready()
		{
			if (_实例 != null)
			{
				QueueFree();
				return;
			}
			_实例 = this;
			ProcessMode = ProcessModeEnum.Always;
		}

		public void 注册一次性动画(NodePath 节点路径, string 场景路径 = null)
		{
			if (场景路径 == null)
				场景路径 = GetTree().CurrentScene.SceneFilePath;

			string 唯一ID = $"{场景路径}|{节点路径}";
			if (!_注册表.ContainsKey(场景路径))
				_注册表[场景路径] = new Dictionary<string, string>();

			if (!_注册表[场景路径].ContainsKey(节点路径))
				_注册表[场景路径][节点路径] = 唯一ID;
		}

		public void 标记动画已触发(NodePath 节点路径, string 场景路径 = null)
		{
			if (场景路径 == null)
				场景路径 = GetTree().CurrentScene.SceneFilePath;

			string 唯一ID = $"{场景路径}|{节点路径}";

			int 当前存档位 = 卡牌数据管理器.当前存档位;
			var 存档数据 = SaveManager .实例?.获取存档数据(当前存档位);
			if (存档数据 != null)
			{
				存档数据.记录动画触发(唯一ID, true);
			}
			else
			{
				GD.PrintErr($"[OneTimeAnimationManager] 无法获取存档数据，存档位={当前存档位}");
			}

			var 节点 = GetTree().CurrentScene.GetNodeOrNull<Node2D>(节点路径);
			if (节点 != null)
			{
				节点.Visible = false;
			}
			else
			{
				GD.PrintErr($"[OneTimeAnimationManager] 未找到节点: {节点路径}");
			}
		}

		public void 恢复当前场景一次性动画()
		{
			string 当前场景路径 = GetTree().CurrentScene.SceneFilePath;
			if (!_注册表.ContainsKey(当前场景路径))
				return;

			int 当前存档位 = 卡牌数据管理器.当前存档位;
			var 存档数据 = SaveManager .实例?.获取存档数据(当前存档位);
			if (存档数据 == null)
			{
				GD.PrintErr($"[OneTimeAnimationManager] 无法获取存档数据，不能恢复动画状态");
				return;
			}

			foreach (var kvp in _注册表[当前场景路径])
			{
				string 节点路径字符串 = kvp.Key;
				string 唯一ID = kvp.Value;
				if (存档数据.获取动画触发状态(唯一ID))
				{
					var 节点 = GetTree().CurrentScene.GetNodeOrNull<Node2D>(节点路径字符串);
					if (节点 != null)
					{
						节点.Visible = false;
					}
				}
			}
		}

		public void 清理注册表()
		{
			_注册表.Clear();
		}
	}
}
