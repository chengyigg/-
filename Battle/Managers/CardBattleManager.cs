using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// Manages the battle flow, energy system, card effects, and turn transitions.
/// </summary>
public partial class CardBattleManager : Node
{
	public CardBattleUnit 玩家单位 { get; private set; }
	public CardBattleUnit 怪物单位 { get; private set; }

	private int 当前能量 = 0;
	private const int 最大能量 = 3;
	private bool _是否强化模式 = false;
	public bool 是否强化模式 => _是否强化模式;

	[Export] private Label 玩家生命值显示;
	[Export] private Label 怪物生命值显示;
	[Export] private Label 玩家护盾显示;
	[Export] private Label 怪物护盾显示;
	[Export] private Label 玩家手牌数量显示;
	[Export] private Label 怪物手牌数量显示;
	[Export] private Label 玩家卡组数量显示;
	[Export] private Label 怪物卡组数量显示;
	[Export] public 敌人HandManager 敌人HandManager;
	[Export] private ProgressBar 玩家行动条显示;
	[Export] private ProgressBar 怪物行动条显示;
	[Export] private Label 战斗日志;
	[Export] private Button 结束回合按钮;
	[Export] private Control 卡牌生成位置;
	[Export] private Control 玩家手牌位置;
	[Export] private Button 强化模式按钮;
	[Export] private HBoxContainer 能量槽容器;
	[Export] private PackedScene 能量格预制体;
	[Export] public 卡牌使用记录管理器 卡牌使用记录管理器;

	private TurnStateMachine _stateMachine;
	private 战斗UI控制器 _uiController;
	private EnemyAIController _enemyAI;

	[Signal] public delegate void 玩家回合开始EventHandler();
	[Signal] public delegate void 怪物回合开始EventHandler();
	[Signal] public delegate void 战斗结束EventHandler(bool 玩家胜利);

	public override void _Ready()
	{
		_stateMachine = new TurnStateMachine();
		_uiController = new 战斗UI控制器();
		_enemyAI = new EnemyAIController();

		AddChild(_stateMachine);
		AddChild(_uiController);
		AddChild(_enemyAI);

		_uiController.初始化(
			玩家生命值显示, 怪物生命值显示,
			玩家护盾显示, 怪物护盾显示,
			玩家手牌数量显示, 怪物手牌数量显示,
			玩家卡组数量显示, 怪物卡组数量显示,
			玩家行动条显示, 怪物行动条显示,
			战斗日志, 结束回合按钮,
			卡牌生成位置, 玩家手牌位置,
			能量槽容器, 能量格预制体, this
		);

		_enemyAI.初始化(敌人HandManager, this);
		_stateMachine.初始化(this, _uiController, _enemyAI);

		Callable.From(连接装备管理器信号).CallDeferred();

		if (结束回合按钮 != null)
			结束回合按钮.Pressed += () => _stateMachine.结束玩家回合();

		if (强化模式按钮 != null)
			强化模式按钮.Pressed += 切换强化模式;
	}
	
	/// <summary>Ends the monster's turn.</summary>
	public void 结束怪物回合()
	{
		_stateMachine.结束怪物回合();
	}
	
	/// <summary>Adds a log message to the battle log UI.</summary>
	public void 添加战斗日志(string 日志)
	{
		_uiController?.添加战斗日志(日志);
	}

	/// <summary>Updates all battle UI elements.</summary>
	public void 更新UI()
	{
		_uiController?.更新UI(玩家单位, 怪物单位);
	}
	
	private void 设置能量(int 新值)
	{
		当前能量 = Mathf.Clamp(新值, 0, 最大能量);
		_uiController.更新能量显示(当前能量, _是否强化模式);
		var 主场景 = GetParent() as 卡牌游戏主场景;
		主场景?.玩家HandManager?.刷新所有卡牌高光();
	}

	/// <summary>Increases current energy by the given amount.</summary>
	public void 增加能量(int 数量) => 设置能量(当前能量 + 数量);
	
	private void 消耗能量(int 数量)
	{
		int 旧能量 = 当前能量;
		设置能量(当前能量 - 数量);
		if (当前能量 <= 0 && _是否强化模式)
			关闭强化模式();
	}

	private void 切换强化模式()
	{
		if (!_stateMachine.战斗进行中) return;
		if (_是否强化模式)
			关闭强化模式();
		else
		{
			if (当前能量 <= 0)
			{
				_uiController.添加战斗日志("能量不足，无法启动强化模式！");
				return;
			}
			开启强化模式();
		}
	}

	private void 开启强化模式()
	{
		_是否强化模式 = true;
		_uiController.添加战斗日志("★ 强化模式启动！卡牌效果已增强 ★");
		_uiController.播放强化特效(GetViewport());
		if (强化模式按钮 != null)
			强化模式按钮.Disabled = false;
		_uiController.更新能量显示(当前能量, _是否强化模式);
		var 主场景 = GetParent() as 卡牌游戏主场景;
		主场景?.玩家HandManager?.刷新所有卡牌描述(true);
		主场景?.玩家HandManager?.刷新所有卡牌高光();
	}

	/// <summary>Turns off enhanced mode.</summary>
	public void 关闭强化模式()
	{
		if (!_是否强化模式) return;
		_是否强化模式 = false;
		_uiController.添加战斗日志("强化模式已关闭。");
		if (强化模式按钮 != null)
			强化模式按钮.Disabled = false;
		_uiController.更新能量显示(当前能量, _是否强化模式);
		var 主场景 = GetParent() as 卡牌游戏主场景;
		主场景?.玩家HandManager?.刷新所有卡牌描述(false);
		主场景?.玩家HandManager?.刷新所有卡牌高光();
	}

	private void 连接装备管理器信号()
	{
		if (装备管理器.实例 == null)
		{
			return;
		}
		try
		{
			装备管理器.实例.Connect("装备触发抽牌", new Callable(this, nameof(处理装备抽牌)));
		}
		catch (Exception e)
		{
		}
	}

	private void 处理装备抽牌(CardBattleUnit 单位)
	{
		if (单位 != 玩家单位) return;
		var 抽到的卡牌 = 玩家单位.抽牌();
		if (抽到的卡牌 == null) return;
		_uiController.添加战斗日志("勇气徽章触发！抽了一张牌");
		var 主场景 = GetParent() as 卡牌游戏主场景;
		主场景?.玩家HandManager?.添加卡牌到手中排队(抽到的卡牌, true);
	}

	/// <summary>Triggers equipment effects at turn start.</summary>
	public void 处理装备回合开始效果()
	{
		装备管理器.实例?.处理回合开始(玩家单位, this);
	}

	/// <summary>Executes the effect of a played card.</summary>
	public void 执行卡牌效果(CardBattleUnit 使用者, CardBattleUnit 目标, CardInstance 卡牌)
	{
		if (卡牌效果管理器.实例 != null)
			卡牌效果管理器.实例.执行卡牌效果(卡牌, 使用者, 目标, this);
		else
		{
		}
	}

	/// <summary>Applies damage from one unit to another, respecting shields and buffs.</summary>
	public void 造成伤害(CardBattleUnit 使用者, CardBattleUnit 目标, int 伤害)
	{
		int 锋利匕首加成 = 效果_锋利匕首.获取攻击加成(使用者);
		伤害 += 锋利匕首加成;

		if (使用者 == 玩家单位 && 观察弱点状态.玩家下次攻击伤害加成 > 0)
		{
			伤害 += 观察弱点状态.玩家下次攻击伤害加成;
			观察弱点状态.玩家下次攻击伤害加成 = 0;
			_uiController.添加战斗日志($"弱点观察生效！额外造成{观察弱点状态.玩家下次攻击伤害加成}点伤害");
		}

		if (目标.护盾值 > 0)
		{
			int 剩余伤害 = 伤害 - 目标.护盾值;
			目标.护盾值 = Mathf.Max(0, 目标.护盾值 - 伤害);
			_uiController.添加战斗日志($"护盾吸收了{伤害}点伤害");
			if (剩余伤害 > 0)
			{
				目标.生命值 -= 剩余伤害;
				_uiController.添加战斗日志($"对目标造成{剩余伤害}点伤害");
			}
		}
		else
		{
			目标.生命值 -= 伤害;
			_uiController.添加战斗日志($"对目标造成{伤害}点伤害");
		}

		_uiController.更新UI(玩家单位, 怪物单位);
	}
	
	/// <summary>Checks whether a card can be played in the current turn and mode.</summary>
	public bool 卡牌是否可用(CardInstance 卡牌)
	{
		if (!_stateMachine.战斗进行中 || !_stateMachine.是玩家回合())
			return false;
		if (_是否强化模式)
			return 当前能量 > 0;
		else
			return true;
	}
	
	/// <summary>Grants shield to a unit, adding equipment bonuses.</summary>
	public void 获得护盾(CardBattleUnit 单位, int 护盾值)
	{
		int 额外护甲 = 装备管理器.实例?.获取防御牌护甲加成(单位) ?? 0;
		int 总护甲 = 护盾值 + 额外护甲;
		单位.护盾值 += 总护甲;
		_uiController.添加战斗日志(额外护甲 > 0 ? $"获得了{护盾值}点护盾（坚固盾牌额外+{额外护甲}）" : $"获得了{护盾值}点护盾");
		_uiController.更新UI(玩家单位, 怪物单位);
	}

	private void 执行特殊效果(CardBattleUnit 使用者, CardBattleUnit 目标, CardInstance 卡牌)
	{
		_uiController.添加战斗日志($"发动特殊效果：{卡牌.基础数据.卡牌描述}");
	}

	/// <summary>Called when the player uses a card from hand.</summary>
	public void 玩家使用卡牌(CardInstance 卡牌)
	{
		if (!_stateMachine.战斗进行中 || !_stateMachine.是玩家回合()) return;

		_uiController.添加战斗日志($"玩家使用了{卡牌.基础数据.卡牌名称}");
		if (卡牌使用记录管理器 != null)
			卡牌使用记录管理器.添加记录(卡牌, true);

		执行卡牌效果(玩家单位, 怪物单位, 卡牌);

		if (是否强化模式)
			消耗能量(1);

		if (卡牌.基础数据.类型 == CardData.卡牌类型.装备)
			玩家单位.手牌.Remove(卡牌);
		else
		{
			玩家单位.使用卡牌(卡牌);
			if (卡牌.基础数据.类型 == CardData.卡牌类型.攻击)
				装备管理器.实例?.通知攻击使用(玩家单位);
		}

		_uiController.更新UI(玩家单位, 怪物单位);

		if (怪物单位.生命值 <= 0)
		{
			结束战斗(true);
			return;
		}

		if (玩家单位.手牌.Count == 0)
		{
			_uiController.添加战斗日志("玩家使用完所有手牌，自动结束回合");
			var 计时器 = new Timer();
			计时器.OneShot = true;
			计时器.WaitTime = 1.0f;
			AddChild(计时器);
			计时器.Timeout += () => { 计时器.QueueFree(); _stateMachine.结束玩家回合(); };
			计时器.Start();
		}
	}

	/// <summary>Sets the combat units.</summary>
	public void 设置战斗单位(CardBattleUnit 玩家, CardBattleUnit 怪物)
	{
		玩家单位 = 玩家;
		怪物单位 = 怪物;
		_enemyAI.设置怪物单位(怪物);
	}

	/// <summary>Initializes and starts the battle.</summary>
	public void 开始战斗(Godot.Collections.Array<CardData> 玩家卡组, Godot.Collections.Array<CardData> 怪物卡组)
	{
		玩家单位.初始化卡组(玩家卡组);
		怪物单位.初始化卡组(怪物卡组);
		玩家单位.生命值 = 50;
		怪物单位.生命值 = 10;
		玩家单位.抽起始手牌(5);

		_stateMachine.战斗进行中 = true;
		_stateMachine.玩家回合中 = true;

		_stateMachine.开始玩家回合(false);
		_uiController.更新UI(玩家单位, 怪物单位);
		设置能量(1);
		_是否强化模式 = false;
		_uiController.更新能量显示(当前能量, _是否强化模式);
		卡牌使用记录管理器?.清空记录();
	}

	/// <summary>Ends the battle with the given outcome.</summary>
	public void 结束战斗(bool 玩家胜利)
	{
		_stateMachine.战斗进行中 = false;
		_stateMachine.玩家回合中 = false;
		_uiController.添加战斗日志(玩家胜利 ? "战斗胜利！" : "战斗失败...");
		EmitSignal(SignalName.战斗结束, 玩家胜利);
	}

	/// <summary>Player draws three cards sequentially.</summary>
	public void 玩家抽三张牌()
	{
		玩家抽一张牌(0);
		var 抽牌计时器 = new Timer();
		抽牌计时器.Name = "抽牌计时器";
		抽牌计时器.OneShot = true;
		抽牌计时器.WaitTime = 0.2f;
		AddChild(抽牌计时器);
		抽牌计时器.Timeout += async () =>
		{
			抽牌计时器.QueueFree();
			玩家抽一张牌(1);
			await ToSignal(GetTree().CreateTimer(0.2f), "timeout");
			玩家抽一张牌(2);
			if (玩家单位.手牌.Count == 0)
			{
				_uiController.添加战斗日志("玩家抽牌后仍没有手牌，自动结束回合");
				await ToSignal(GetTree().CreateTimer(1.0f), "timeout");
				_stateMachine.结束玩家回合();
			}
		};
		抽牌计时器.Start();
	}

	private void 玩家抽一张牌(int 第几张)
	{
		var 抽到的卡牌 = 玩家单位.抽牌();
		if (抽到的卡牌 != null)
		{
			_uiController.添加战斗日志($"玩家抽了第{第几张 + 1}张牌");
			var 主场景 = GetParent() as 卡牌游戏主场景;
			主场景?.玩家HandManager?.添加卡牌到手中排队(抽到的卡牌, true);
			_uiController.更新UI(玩家单位, 怪物单位);
		}
	}
}
