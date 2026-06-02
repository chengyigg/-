using Godot;
using System;

/// <summary>
/// Controls monster AI: drawing cards, selecting and playing cards, and managing turn flow.
/// </summary>
public partial class EnemyAIController : Node
{
	private 敌人HandManager _敌人HandManager;
	private CardBattleManager _battleManager;
	private CardBattleUnit _怪物单位;

	/// <summary>Initializes the AI with hand manager and battle manager.</summary>
	public void 初始化(敌人HandManager HandManager, CardBattleManager battleManager)
	{
		_敌人HandManager = HandManager;
		_battleManager = battleManager;
	}

	/// <summary>Sets the monster combat unit.</summary>
	public void 设置怪物单位(CardBattleUnit 怪物)
	{
		_怪物单位 = 怪物;
	}

	/// <summary>Starts the monster's draw sequence (2 cards with delays).</summary>
	public void 开始怪物抽牌序列()
	{
		怪物抽一张牌(0);
		var 抽牌计时器1 = new Timer();
		抽牌计时器1.OneShot = true;
		抽牌计时器1.WaitTime = 0.5f;
		AddChild(抽牌计时器1);
		抽牌计时器1.Timeout += () =>
		{
			if (IsInstanceValid(抽牌计时器1)) 抽牌计时器1.QueueFree();
			怪物抽一张牌(1);
			var AI计时器 = new Timer();
			AI计时器.OneShot = true;
			AI计时器.WaitTime = 0.5f;
			AddChild(AI计时器);
			AI计时器.Timeout += () =>
			{
				if (IsInstanceValid(AI计时器)) AI计时器.QueueFree();
				if (_怪物单位.手牌.Count == 0)
				{
					_battleManager.添加战斗日志("怪物没有手牌，跳过回合");
					_battleManager.结束怪物回合();
					return;
				}
				执行怪物AI();
			};
			AI计时器.Start();
		};
		抽牌计时器1.Start();
	}

	private void 怪物抽一张牌(int 第几张)
	{
		if (_怪物单位.抽牌堆.Count == 0 && _怪物单位.弃牌堆.Count == 0)
		{
			_battleManager.添加战斗日志("怪物没有牌可抽了");
			return;
		}
		var 抽到的卡牌 = _怪物单位.抽牌();
		if (抽到的卡牌 != null)
		{
			_battleManager.添加战斗日志($"怪物抽了第{第几张 + 1}张牌");
			_敌人HandManager?.添加一张卡牌();
			_battleManager.更新UI();
		}
		else
		{
			_battleManager.添加战斗日志("怪物抽牌失败");
		}
	}

	/// <summary>Executes the monster AI: decides how many cards to play and plays them sequentially.</summary>
	public async void 执行怪物AI()
	{
		if (_怪物单位.手牌 == null || _怪物单位.手牌.Count == 0)
		{
			_battleManager.添加战斗日志("怪物没有手牌，结束回合");
			_battleManager.结束怪物回合();
			return;
		}
		int 使用卡牌数量 = _怪物单位.手牌.Count <= 2 ? _怪物单位.手牌.Count : GD.RandRange(1, Mathf.Min(3, _怪物单位.手牌.Count));
		_battleManager.添加战斗日志($"怪物将使用{使用卡牌数量}张牌");
		执行怪物卡牌链(使用卡牌数量, 0);
	}

	private async void 执行怪物卡牌链(int 总卡牌数量, int 当前索引)
	{
		if (当前索引 >= 总卡牌数量 || _怪物单位.手牌.Count == 0)
		{
			_battleManager.添加战斗日志("怪物回合结束");
			await ToSignal(GetTree().CreateTimer(1.0f), "timeout");
			_敌人HandManager?.清空手牌();
			_battleManager.结束怪物回合();
			return;
		}
		await ToSignal(GetTree().CreateTimer(1.0f), "timeout");
		if (_怪物单位.手牌.Count == 0)
		{
			执行怪物卡牌链(总卡牌数量, 当前索引 + 1);
			return;
		}
		var 当前使用的卡牌 = _怪物单位.手牌[0];
		_battleManager.添加战斗日志($"怪物使用了{当前使用的卡牌.基础数据.卡牌名称}");
		if (_敌人HandManager != null && 当前使用的卡牌 != null)
		{
			try { await _敌人HandManager.播放卡牌展示动画(当前使用的卡牌, _battleManager.卡牌使用记录管理器); }
			catch (Exception e) { }
		}
		_敌人HandManager?.移除一张卡牌();
		_battleManager.执行卡牌效果(_怪物单位, _battleManager.玩家单位, 当前使用的卡牌);
		_怪物单位.弃牌(当前使用的卡牌);
		if (_battleManager.玩家单位.生命值 <= 0)
		{
			await ToSignal(GetTree().CreateTimer(1.0f), "timeout");
			_battleManager.结束战斗(false);
			return;
		}
		_battleManager.更新UI();
		if (当前索引 < 总卡牌数量 - 1)
			await ToSignal(GetTree().CreateTimer(0.5f), "timeout");
		执行怪物卡牌链(总卡牌数量, 当前索引 + 1);
	}
}
