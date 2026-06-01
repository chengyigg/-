using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using 你的项目.Scripts.全局;
using 你的项目.Scripts.资源;

namespace 你的项目.Scripts.角色
{
	/// <summary>
	/// 玩家角色控制器，负责网格移动、动画、能力触发、跟随者管理以及场景交互。
	/// </summary>
	public partial class PlayerController : CharacterBody2D
	{
		[Export] public float 加速倍数 = 2.0f;
		public float 当前移动速度 => _当前移动速度;
		private bool 输入加速中 => Input.IsActionPressed("sprint");
		private bool _强制移动中 = false;
		[Export] public float 移动速度 = 300.0f;
		[Export] public AnimatedSprite2D 动画精灵 { get; set; }
		[Export] public PackedScene 金币UI预制体;
		public bool 正在移动中 => _isMoving;
		private List<Vector2> _路径队列 = new List<Vector2>();
		public IReadOnlyList<Vector2> 路径队列 => _路径队列;
		private int _过场动画锁计数 = 0;
		private bool _禁止移动 = false;
		private float _当前移动速度 => Input.IsActionPressed("sprint") ? 移动速度 * 加速倍数 : 移动速度;

		[Export] public float 格子大小 = 16.0f;
		private bool _isMoving = false;
		private Vector2 _startPosition;
		private Vector2 _targetPosition;
		private float _moveTimeLeft = 0f;
		private Vector2 _pendingInput = Vector2.Zero;
		private Vector2 _lastInputDirection = Vector2.Down;

		private bool _回忆界面打开 = false;
		[Export] public bool 燃烧 { get; set; } = false;
		[Export] public bool 可以过河流 { get; set; } = false;
		public bool 可移动 { get; private set; } = true;
		private bool _过场动画播放中 = false;
		private bool _输入启用 = true;
		public Vector2 最后位置 { get; set; } = Vector2.Zero;
		public string 最后场景 { get; set; } = "";

		private TileMap 河流地图;
		private int 河流图块源ID = 0;
		private Vector2I 河流图块坐标 = new Vector2I(20, 2);
		private Vector2I 红色图块坐标 = new Vector2I(2, 2);
		private 能力选择UI 能力UI;
		private HashSet<Vector2I> 已冻结的图块 = new HashSet<Vector2I>();
		private CanvasLayer _提示图层;
		private Label _提示标签;
		private CanvasLayer _金币UI实例;
		private Label _金币数值标签;

		private Vector2[] _方向历史;
		public Vector2[] 方向历史 => _方向历史;
		public Vector2 上次输入方向 => _lastInputDirection;

		[Export] public PackedScene 跟随者预制体1;
		[Export] public PackedScene 跟随者预制体2;
		[Export] public PackedScene 跟随者预制体3;

		[Export] public int _导出历史长度 = 60;
		private Vector2[] _位置历史;
		private int _历史索引 = 0;
		public Vector2[] 位置历史 => _位置历史;
		public int 历史索引 => _历史索引;
		public int 历史长度 => _导出历史长度;

		private bool _鼠标可见 = false;

		public PlayerController()
		{
			_位置历史 = new Vector2[_导出历史长度];
			_方向历史 = new Vector2[_导出历史长度];
		}

		/// <summary>添加一个跟随者实例。</summary>
		/// <param name="预制体">跟随者场景预制体。</param>
		/// <param name="滞后格子数">跟随者滞后于玩家的格子数量。</param>
		public void 添加跟随者(PackedScene 预制体, int 滞后格子数 = 2)
		{
			if (跟随者管理器.实例 == null) return;
			跟随者管理器.实例.添加跟随者(预制体, 滞后格子数);
		}

		/// <summary>移除所有跟随者。</summary>
		public void 移除所有跟随者()
		{
			if (跟随者管理器.实例 == null) return;
			跟随者管理器.实例.移除所有跟随者();
		}

		public override void _EnterTree()
		{
			if (!IsInGroup("玩家")) AddToGroup("玩家");
			强制终止移动并重置();
		}

		public override void _Ready()
		{
			if (动画精灵 == null) 动画精灵 = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
			初始化河流地图引用();

			能力UI = new 能力选择UI();
			GetTree().Root.AddChild(能力UI);
			能力UI.注册能力回调("燃烧能力", 选择燃烧能力);
			能力UI.注册能力回调("冻结能力", 选择冻结能力);
			动画精灵.Play("walk_down");

			for (int i = 0; i < _导出历史长度; i++)
			{
				_位置历史[i] = GlobalPosition;
				_方向历史[i] = Vector2.Down;
			}

			_创建提示UI();
			_创建金币UI();
			_控制鼠标显示();
			GlobalPosition = 获取网格对齐位置(GlobalPosition);

			if (ConditionManager.实例 != null && ConditionManager.实例.检查条件("已获得跟随者"))
			{
				CallDeferred(nameof(自动添加跟随者));
			}
		}

		private void 自动添加跟随者()
		{
			if (跟随者管理器.实例 == null) return;
			var 跟随者组 = GetTree().GetNodesInGroup("跟随者");
			if (跟随者组.Count == 0 && 跟随者预制体1 != null)
				添加跟随者(跟随者预制体1, 2);
		}

		public override void _Process(double delta)
		{
			if (_提示标签 != null && IsInstanceValid(_提示标签))
			{
				bool 状态栏打开 = 状态栏界面.获取实例()?.界面已打开 ?? false;
				_提示标签.Visible = !状态栏打开;
			}
			if (_金币UI实例 != null && IsInstanceValid(_金币UI实例))
			{
				bool 状态栏打开 = 状态栏界面.获取实例()?.界面已打开 ?? false;
				bool 当前场景需要玩家 = GetTree().CurrentScene?.IsInGroup("需要玩家") ?? false;
				_金币UI实例.Visible = 当前场景需要玩家 && !状态栏打开;
			}
		}

		public void 设置输入启用(bool 启用)
		{
			_输入启用 = 启用;
			if (!启用) _pendingInput = Vector2.Zero;
		}

		private void _创建提示UI()
		{
			if (_提示图层 != null && IsInstanceValid(_提示图层)) _提示图层.QueueFree();
			var 当前场景 = GetTree().CurrentScene;
			if (当前场景 == null) return;
			_提示图层 = new CanvasLayer();
			_提示图层.Layer = 100;
			当前场景.AddChild(_提示图层);
			_提示标签 = new Label();
			_提示标签.Text = "按 P 打开状态栏";
			_提示标签.AddThemeFontSizeOverride("font_size", 20);
			_提示标签.AddThemeColorOverride("font_color", Colors.White);
			_提示标签.AddThemeConstantOverride("outline_size", 2);
			_提示标签.AddThemeColorOverride("font_outline_color", Colors.Black);
			_提示标签.Position = new Vector2(20, 20);
			_提示图层.AddChild(_提示标签);
		}

		private void _创建金币UI()
		{
			if (金币UI预制体 == null) return;
			var 当前场景 = GetTree().CurrentScene;
			if (当前场景 == null) return;
			_金币UI实例 = 金币UI预制体.Instantiate<CanvasLayer>();
			if (_金币UI实例 == null) return;
			当前场景.AddChild(_金币UI实例);
			_金币数值标签 = _金币UI实例.GetNodeOrNull<Label>("Label");
			if (_金币数值标签 == null)
			{
				_金币数值标签 = _金币UI实例.GetNodeOrNull<Label>("GoldLabel");
				if (_金币数值标签 == null)
					_金币数值标签 = _金币UI实例.FindChild("Label", true, false) as Label;
				if (_金币数值标签 == null) return;
			}
			if (金币管理器.实例 == null) return;
			_金币数值标签.Text = 金币管理器.实例.金币数量.ToString();
			金币管理器.实例.金币数量已变化 += _更新金币显示;
		}

		private void _更新金币显示(int 新数量)
		{
			if (_金币数值标签 != null && IsInstanceValid(_金币数值标签))
				_金币数值标签.Text = 新数量.ToString();
		}

		public void 设置禁止移动(bool 禁止)
		{
			_禁止移动 = 禁止;
			if (禁止)
			{
				强制终止移动并重置();
				设置输入启用(false);
			}
		}

		private void _控制鼠标显示()
		{
			bool 当前场景需要玩家 = GetTree().CurrentScene?.IsInGroup("需要玩家") ?? false;
			if (当前场景需要玩家)
			{
				Input.MouseMode = Input.MouseModeEnum.Captured;
				_鼠标可见 = false;
			}
			else
			{
				Input.MouseMode = Input.MouseModeEnum.Visible;
				_鼠标可见 = true;
			}
		}

		private void _切换鼠标显示()
		{
			if (_鼠标可见)
				Input.MouseMode = Input.MouseModeEnum.Captured;
			else
				Input.MouseMode = Input.MouseModeEnum.Visible;
			_鼠标可见 = !_鼠标可见;
		}

		public override void _ExitTree()
		{
			if (_提示图层 != null && IsInstanceValid(_提示图层)) _提示图层.QueueFree();
			if (_金币UI实例 != null && IsInstanceValid(_金币UI实例)) _金币UI实例.QueueFree();
			if (金币管理器.实例 != null) 金币管理器.实例.金币数量已变化 -= _更新金币显示;
			Input.MouseMode = Input.MouseModeEnum.Visible;
		}

		public override void _Input(InputEvent @event)
		{
			if (@event.IsActionPressed("toggle_mouse")) { _切换鼠标显示(); GetViewport().SetInputAsHandled(); }
			处理能力选择输入();
			if (@event.IsActionPressed("view_cards")) { 打开卡牌查看界面(); GetViewport().SetInputAsHandled(); }
		}

		public void 开始过场动画()
		{
			强制终止移动并重置();
			_过场动画播放中 = true;
			可移动 = false;
			_输入启用 = false;
			_pendingInput = Vector2.Zero;
			_禁止移动 = true;
		}

		public void 结束过场动画(bool 强制播放空闲动画 = true)
		{
			_过场动画播放中 = false;
			可移动 = true;
			_输入启用 = true;
			_isMoving = false;
			_pendingInput = Vector2.Zero;
			_禁止移动 = false;
			SetPhysicsProcess(true);
			if (强制播放空闲动画) 播放空闲动画(_lastInputDirection);
			记录位置(GlobalPosition);
			最后位置 = GlobalPosition;
		}

		private void 打开卡牌查看界面()
		{
			var 当前场景 = GetTree().CurrentScene;
			if (当前场景 == null || !当前场景.IsInGroup("需要玩家")) return;
			var 界面预制体 = GD.Load<PackedScene>("res://卡牌系统/非战斗场景卡牌/卡牌查看界面.tscn");
			if (界面预制体 == null) return;
			var 界面 = 界面预制体.Instantiate<卡牌查看界面>();
			当前场景.AddChild(界面);
			GetTree().Paused = true;
			界面.TreeExited += () => GetTree().Paused = false;
		}

		public override void _PhysicsProcess(double delta)
		{
			if (_禁止移动 || _过场动画播放中 || _回忆界面打开)
			{
				Velocity = Vector2.Zero;
				MoveAndSlide();
				if (!_回忆界面打开) 播放空闲动画(_lastInputDirection);
				return;
			}

			if (!可移动 && !_强制移动中)
			{
				Velocity = Vector2.Zero;
				MoveAndSlide();
				播放空闲动画(_lastInputDirection);
				return;
			}

			if (!_输入启用 && !_强制移动中)
			{
				Velocity = Vector2.Zero;
				MoveAndSlide();
				播放空闲动画(_lastInputDirection);
				return;
			}

			if (_isMoving)
			{
				_moveTimeLeft -= (float)delta;
				if (_moveTimeLeft <= 0f)
				{
					GlobalPosition = _targetPosition;
					if (!_强制移动中)
					{
						_路径队列.Add(GlobalPosition);
						foreach (var 跟随 in GetTree().GetNodesInGroup("跟随者"))
						{
							if (跟随 is 跟随者控制器 fc)
								fc.路径队列更新();
						}
					}
					_isMoving = false;
					Velocity = Vector2.Zero;
					if (可以过河流) 改变接触的河流图块();
					更新碰撞状态();
					最后位置 = GlobalPosition;
					记录位置(GlobalPosition);

					if (_pendingInput != Vector2.Zero)
					{
						Vector2 nextDir = _pendingInput;
						_pendingInput = Vector2.Zero;
						TryMoveOneStep(nextDir);
					}
				}
				else
				{
					float t = 1f - (_moveTimeLeft / (格子大小 / _当前移动速度));
					GlobalPosition = _startPosition.Lerp(_targetPosition, t);
				}
				记录位置(GlobalPosition);
				return;
			}

			Vector2 rawInput = Vector2.Zero;
			if (Input.IsActionPressed("右")) rawInput.X += 1f;
			if (Input.IsActionPressed("左")) rawInput.X -= 1f;
			if (Input.IsActionPressed("下")) rawInput.Y += 1f;
			if (Input.IsActionPressed("上")) rawInput.Y -= 1f;

			Vector2 moveDir = Vector2.Zero;
			if (rawInput != Vector2.Zero)
			{
				if (Math.Abs(rawInput.X) > Math.Abs(rawInput.Y))
					moveDir = new Vector2(Math.Sign(rawInput.X), 0);
				else if (Math.Abs(rawInput.Y) > 0)
					moveDir = new Vector2(0, Math.Sign(rawInput.Y));
				else moveDir = rawInput;
				_lastInputDirection = moveDir;
				播放行走动画(moveDir);
			}
			else
				播放空闲动画(_lastInputDirection);

			if (moveDir != Vector2.Zero)
			{
				TryMoveOneStep(moveDir);
			}
			else _pendingInput = Vector2.Zero;

			if (!_isMoving)
			{
				记录位置(GlobalPosition);
				最后位置 = GlobalPosition;
			}
		}

		public void 设置待处理输入(Vector2 输入) => _pendingInput = 输入;

		private bool ForceMoveOneStep(Vector2 direction)
		{
			if (_isMoving) return false;
			Vector2 newPos = GlobalPosition + direction * 格子大小;
			_startPosition = GlobalPosition;
			_targetPosition = 获取网格对齐位置(newPos);
			_isMoving = true;
			_moveTimeLeft = 格子大小 / 移动速度;
			_lastInputDirection = direction;
			播放行走动画(direction);
			return true;
		}

		public async Task<bool> 强制后退(Vector2 direction, int steps)
		{
			if (steps <= 0) return true;
			bool 原强制移动 = _强制移动中;
			_强制移动中 = true;
			for (int i = 0; i < steps; i++)
			{
				if (!ForceMoveOneStep(direction))
				{
					while (_isMoving) await ToSignal(GetTree(), "physics_frame");
					if (!ForceMoveOneStep(direction)) break;
				}
				await 等待移动结束();
			}
			_强制移动中 = false;
			return true;
		}

		public void 强制终止移动并重置()
		{
			if (_isMoving)
			{
				GlobalPosition = _targetPosition;
				_isMoving = false;
				_moveTimeLeft = 0f;
				Velocity = Vector2.Zero;
			}
			_pendingInput = Vector2.Zero;
			最后位置 = GlobalPosition;
			记录位置(GlobalPosition);
		}

		private async Task 等待移动结束()
		{
			while (_isMoving) await ToSignal(GetTree(), "physics_frame");
		}

		private bool TryMoveOneStep(Vector2 direction)
		{
			if (_isMoving) return false;
			Vector2 newPos = GlobalPosition + direction * 格子大小;
			if (!IsWalkable(newPos)) return false;
			_startPosition = GlobalPosition;
			_targetPosition = newPos;
			_isMoving = true;
			_moveTimeLeft = 格子大小 / _当前移动速度;
			_lastInputDirection = direction;
			播放行走动画(direction);
			return true;
		}

		private void 连续移动(Vector2 direction)
		{
			if (_isMoving) return;
			TryMoveOneStep(direction);
		}

		private bool IsWalkable(Vector2 worldPos)
		{
			Vector2 motion = worldPos - GlobalPosition;
			if (TestMove(GlobalTransform, motion)) return false;
			return true;
		}

		public Vector2 获取网格对齐位置(Vector2 pos)
		{
			float x = Mathf.Round(pos.X / 格子大小) * 格子大小;
			float y = Mathf.Round(pos.Y / 格子大小) * 格子大小;
			return new Vector2(x, y);
		}

		private void 记录位置(Vector2 位置)
		{
			_位置历史[_历史索引] = 位置;
			_方向历史[_历史索引] = _lastInputDirection;
			_历史索引 = (_历史索引 + 1) % _导出历史长度;
		}

		public bool 能力是否激活(string 能力名称) => 能力UI != null && 能力UI.能力是否激活(能力名称);

		private void 处理能力选择输入()
		{
			if (Input.IsActionJustPressed("ability_menu"))
			{
				if (能力UI.是否正在显示()) 能力UI.隐藏();
				else 能力UI.显示();
			}
		}

		private void 选择燃烧能力()
		{
			if (可以过河流) 设置过河流能力(false);
			切换燃烧状态();
		}

		private void 选择冻结能力()
		{
			if (燃烧) 设置燃烧状态(false);
			设置过河流能力(true);
		}

		private void 初始化河流地图引用() => 河流地图 = GetNodeOrNull<TileMap>("../TileMap");

		private TileMap 获取有效的河流地图()
		{
			if (河流地图 == null || !IsInstanceValid(河流地图)) 初始化河流地图引用();
			return 河流地图;
		}

		public void 设置可移动(bool 可移动状态) => 可移动 = 可移动状态;

		public void 切换燃烧状态() => 燃烧 = !燃烧;

		public void 设置燃烧状态(bool 状态)
		{
			燃烧 = 状态;
			if (状态 && 可以过河流) 设置过河流能力(false);
		}

		private void 改变接触的河流图块()
		{
			var 当前河流地图 = 获取有效的河流地图();
			if (当前河流地图 == null) return;
			Vector2I 玩家单元格 = 当前河流地图.LocalToMap(GlobalPosition);
			for (int x = -1; x <= 1; x++)
				for (int y = -1; y <= 1; y++)
				{
					Vector2I 检测单元格 = new Vector2I(玩家单元格.X + x, 玩家单元格.Y + y);
					if (是河流图块(检测单元格) && !已冻结的图块.Contains(检测单元格))
						冻结河流图块(检测单元格);
				}
		}

		private void 冻结河流图块(Vector2I 单元格位置)
		{
			var 当前河流地图 = 获取有效的河流地图();
			if (当前河流地图 == null) return;
			当前河流地图.SetCell(0, 单元格位置, 河流图块源ID, 红色图块坐标);
			已冻结的图块.Add(单元格位置);
		}

		private bool 是河流图块(Vector2I 单元格位置)
		{
			var 当前河流地图 = 获取有效的河流地图();
			if (当前河流地图 == null) return false;
			TileData 图块数据 = 当前河流地图.GetCellTileData(0, 单元格位置);
			if (图块数据 != null)
			{
				Vector2I 当前图块坐标 = 当前河流地图.GetCellAtlasCoords(0, 单元格位置);
				return 当前图块坐标 == 河流图块坐标;
			}
			return false;
		}

		private bool 是红色图块(Vector2I 单元格位置)
		{
			var 当前河流地图 = 获取有效的河流地图();
			if (当前河流地图 == null) return false;
			TileData 图块数据 = 当前河流地图.GetCellTileData(0, 单元格位置);
			if (图块数据 != null)
			{
				Vector2I 当前图块坐标 = 当前河流地图.GetCellAtlasCoords(0, 单元格位置);
				return 当前图块坐标 == 红色图块坐标;
			}
			return false;
		}

		private void 更新碰撞状态()
		{
			var 当前河流地图 = 获取有效的河流地图();
			if (当前河流地图 == null) { SetCollisionMaskValue(1, true); return; }
			Vector2I 玩家单元格 = 当前河流地图.LocalToMap(GlobalPosition);
			bool 玩家在红色图块上 = 是红色图块(玩家单元格);
			SetCollisionMaskValue(1, !玩家在红色图块上);
		}

		public void 设置过河流能力(bool 可以过)
		{
			可以过河流 = 可以过;
			if (可以过 && 燃烧) 设置燃烧状态(false);
		}

		public void 重置冻结状态()
		{
			var 当前河流地图 = 获取有效的河流地图();
			if (当前河流地图 != null)
				foreach (Vector2I 单元格 in 已冻结的图块)
					当前河流地图.SetCell(0, 单元格, 河流图块源ID, 河流图块坐标);
			已冻结的图块.Clear();
		}

		public Godot.Collections.Array<string> 获取跟随者预制体路径列表()
		{
			if (跟随者管理器.实例 != null) return 跟随者管理器.实例.获取所有跟随者预制体路径();
			return new Godot.Collections.Array<string>();
		}

		public void 重建跟随者(Godot.Collections.Array<string> 路径列表)
		{
			if (跟随者管理器.实例 != null) 跟随者管理器.实例.重建跟随者(路径列表);
		}

		public int 获取当前冻结数量() => 已冻结的图块.Count;

		public void 切换过河流能力() => 设置过河流能力(!可以过河流);

		/// <summary>将玩家传送到指定世界坐标，重置移动状态并重建跟随者队列。</summary>
		public void 传送到(Vector2 位置)
		{
			强制终止移动并重置();
			GlobalPosition = 位置;
			最后位置 = 位置;
			_路径队列.Clear();
			_路径队列.Add(位置);

			if (跟随者管理器.实例 != null)
				跟随者管理器.实例.重置所有跟随者();
		}

		public void 保存状态()
		{
			最后位置 = GlobalPosition;
			最后场景 = GetTree().CurrentScene.Name;
		}

		public void 准备场景切换() => 强制终止移动并重置();

		private void 播放行走动画(Vector2 方向)
		{
			if (动画精灵 == null) return;
			string 动画名 = 获取方向动画名(方向, "walk");
			if (动画精灵.SpriteFrames == null || !动画精灵.SpriteFrames.HasAnimation(动画名)) return;
			动画精灵.Play(动画名);
			float 基准速度 = 移动速度;
			float 比例 = _当前移动速度 / 基准速度;
			动画精灵.SpeedScale = 比例;
		}

		private void 播放空闲动画(Vector2 方向)
		{
			if (动画精灵 == null) return;
			string 空闲动画名 = 获取方向动画名(方向, "idle");
			if (动画精灵.SpriteFrames != null && 动画精灵.SpriteFrames.HasAnimation(空闲动画名))
				动画精灵.Play(空闲动画名);
			else
			{
				string 行走动画名 = 获取方向动画名(方向, "walk");
				if (动画精灵.SpriteFrames.HasAnimation(行走动画名))
				{
					动画精灵.Play(行走动画名);
					动画精灵.Pause();
					动画精灵.Frame = 0;
				}
			}
		}

		private string 获取方向动画名(Vector2 方向, string 前缀)
		{
			if (Math.Abs(方向.X) > Math.Abs(方向.Y))
				return 方向.X > 0 ? 前缀 + "_right" : 前缀 + "_left";
			else if (Math.Abs(方向.Y) > 0)
				return 方向.Y > 0 ? 前缀 + "_down" : 前缀 + "_up";
			else return 前缀 + "_down";
		}
	}
}
