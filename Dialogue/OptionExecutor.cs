using Godot;
using System.Collections.Generic;
using 你的项目.Scripts.角色;
using 你的项目.Scripts.资源;
using 你的项目.Scripts.管理器;

public class OptionExecutor
{
	public enum 执行结果
	{
		继续对话,
		结束对话,
		切换场景,
		显示场景UI
	}

	public (执行结果 结果, string 场景路径, DialogueSequence 后续对话) 执行(BranchOption 选项, DialogueSequence 当前序列, int 选项索引)
	{
		if (选项.奖励卡组 != null && 选项.奖励卡组.卡组.Count > 0)
			玩家卡组管理器.实例?.添加卡牌列表(选项.奖励卡组.卡组);

		if (!string.IsNullOrEmpty(选项.触发条件名称))
			ConditionManager.实例?.设置条件满足(选项.触发条件名称);

		if (选项.对话后触发场景切换 && !string.IsNullOrEmpty(选项.对话后目标场景路径))
		{
			准备战斗切换(选项);
			return (执行结果.切换场景, 选项.对话后目标场景路径, null);
		}

		if (选项.显示场景 && !string.IsNullOrEmpty(选项.场景路径))
		{
			if (选项.目标序列 != null)
			{
				if (DialoguePlayer.实例 != null)
					DialoguePlayer.实例.设置待显示场景(选项.场景路径, 选项.场景后DialogueSequence);
				return (执行结果.继续对话, "", 选项.目标序列);
			}
			else
				return (执行结果.显示场景UI, 选项.场景路径, 选项.场景后DialogueSequence ?? 选项.目标序列);
		}

		if (选项.触发场景切换 && !string.IsNullOrEmpty(选项.目标场景路径))
		{
			准备普通传送(选项, 当前序列, 选项索引);
			return (执行结果.切换场景, 选项.目标场景路径, null);
		}

		if (选项.目标序列 != null)
			return (执行结果.继续对话, "", 选项.目标序列);
		else
			return (执行结果.结束对话, "", null);
	}

	private void 准备战斗切换(BranchOption 选项)
	{
		if (DialoguePlayer.实例 != null)
		{
			卡牌数据管理器.返回场景路径 = DialoguePlayer.实例.GetTree().CurrentScene.SceneFilePath;
			if (卡牌数据管理器.使用动画结束位置)
			{
				卡牌数据管理器.返回玩家位置 = 卡牌数据管理器.动画结束位置;
				卡牌数据管理器.使用动画结束位置 = false;
			}
			else
			{
				var 玩家节点 = 获取玩家节点();
				if (玩家节点 != null)
					卡牌数据管理器.返回玩家位置 = 玩家节点.GlobalPosition;
			}
		}
		卡牌数据管理器.从战斗返回 = true;
		卡牌数据管理器.战斗胜利金币奖励 = 选项.战斗胜利金币奖励;
		卡牌数据管理器.临时敌人卡组 = 选项.对话后怪物卡组资源;
	}

	private void 准备普通传送(BranchOption 选项, DialogueSequence 当前序列, int 选项索引)
	{
		if (选项.返回原地)
		{
			if (DialoguePlayer.实例 != null)
			{
				卡牌数据管理器.返回场景路径 = DialoguePlayer.实例.GetTree().CurrentScene.SceneFilePath;
				if (卡牌数据管理器.使用动画结束位置)
				{
					卡牌数据管理器.返回玩家位置 = 卡牌数据管理器.动画结束位置;
					卡牌数据管理器.使用动画结束位置 = false;
				}
				else
				{
					var 玩家节点 = 获取玩家节点();
					if (玩家节点 != null)
						卡牌数据管理器.返回玩家位置 = 玩家节点.GlobalPosition;
				}
			}
			卡牌数据管理器.从战斗返回 = true;
			卡牌数据管理器.临时敌人卡组 = 选项.怪物卡组资源;
		}
		else
		{
			卡牌数据管理器.从战斗返回 = false;
			卡牌数据管理器.返回场景路径 = "";
			卡牌数据管理器.返回玩家位置 = Vector2.Zero;
			卡牌数据管理器.临时敌人卡组 = null;
		}

		if (选项.传送后触发对话 && 选项.传送后DialogueSequence != null)
		{
			string 标识 = $"{当前序列.ResourcePath}|{选项索引}";
			bool 已触发 = 对话状态存储实例.是否已触发传送后对话(标识);
			if (!(选项.是否只能触发一次 && 已触发))
			{
				DialoguePlayer.待触发的传送后对话 = 选项.传送后DialogueSequence;
				DialoguePlayer.等待传送后触发 = true;
				if (选项.是否只能触发一次)
					对话状态存储实例.设置传送后对话已触发(标识, true);
			}
		}
	}

	private Node2D 获取玩家节点()
	{
		if (DialoguePlayer.实例 == null) return null;
		var 玩家组 = DialoguePlayer.实例.GetTree().GetNodesInGroup("玩家");
		if (玩家组.Count > 0) return 玩家组[0] as Node2D;
		return null;
	}

	public 对话状态存储 对话状态存储实例 { get; set; }
}
