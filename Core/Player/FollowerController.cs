using Godot;
using System.Collections.Generic;
using 你的项目.Scripts.角色;

/// <summary>
/// Controls a follower that trails behind the player at a fixed grid lag distance.
/// </summary>
public partial class FollowerController : CharacterBody2D
{
	[Export] public AnimatedSprite2D 动画精灵 { get; set; }
	[Export] public int 滞后格子数 = 2;               // 最终保持落后玩家2格
	[Export] public float 正常速度 = 140f;             // 平时移动速度
	[Export] public float 追赶倍数 = 1.2f;             // 追赶时速度 = 玩家速度 × 此倍数
	[Export] public int 加速阈值 = 10;                 // 落后超过此格数开始加速
	[Export] public int 减速阈值 = 4;                  // 落后小于等于此格数恢复慢速
	[Export] public float 格子大小 = 16f;
	[Export] public string 预制体资源路径 { get; set; } = "";

	private PlayerController _玩家;
	private bool _isMoving = false;
	private Vector2 _startPosition;
	private Vector2 _targetPosition;
	private float _moveTimeLeft = 0f;
	private Vector2 _lastDirection = Vector2.Down;
	private float _当前使用速度;

	// 关键：记录下一个要走向的玩家路径索引
	private int _当前目标索引 = 0;

	public override void _Ready()
	{
		if (动画精灵 == null)
			动画精灵 = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		AddToGroup("跟随者");

		_玩家 = GetTree().GetFirstNodeInGroup("玩家") as PlayerController;
		if (_玩家 == null)
		{
			SetPhysicsProcess(false);
			return;
		}

		GlobalPosition = _玩家.GlobalPosition;
		ZIndex = 10;
		动画精灵.SpeedScale = 1.0f;
		_当前使用速度 = 正常速度;
		// 初始目标索引设为 0，表示要从玩家路径的第一个点开始走
		_当前目标索引 = 0;
	}

	/// <summary>
	/// Called by the player when its path queue has been updated. Attempts to start moving if idle.
	/// </summary>
	public void 路径队列更新()
	{
		if (_玩家 == null) return;
		// 如果空闲，尝试启动移动
		if (!_isMoving)
			TryStartMove();
	}

	/// <summary>
	/// Resets the follower to the player's position and clears movement state (used after teleport or scene reload).
	/// </summary>
	public void 重新初始化()
	{
		if (_玩家 == null) return;
		_isMoving = false;
		_moveTimeLeft = 0f;
		GlobalPosition = _玩家.GlobalPosition;
		_targetPosition = GlobalPosition;
		_当前目标索引 = _玩家.路径队列.Count - 1; // 直接指向玩家当前位置
		if (_当前目标索引 < 0) _当前目标索引 = 0;
		播放空闲动画(_lastDirection);
		_当前使用速度 = 正常速度;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_玩家 == null || !IsInstanceValid(_玩家))
		{
			_玩家 = GetTree().GetFirstNodeInGroup("玩家") as PlayerController;
			if (_玩家 == null) return;
			重新初始化();
		}

		if (_isMoving)
		{
			_moveTimeLeft -= (float)delta;
			if (_moveTimeLeft <= 0f)
			{
				GlobalPosition = _targetPosition;
				_isMoving = false;
				// 移动完成后，目标索引自动指向下一个点（但还没开始移动，等待下次 TryStartMove）
				// 注意：不需要增加索引，因为 TryStartMove 里会检查并取当前索引的目标
				// 但为了正确消费，我们移动完成时应该让索引++（已经走过了当前目标）
				_当前目标索引++;
				// 到达后尝试移动下一个点
				TryStartMove();
			}
			else
			{
				float t = 1f - (_moveTimeLeft / (格子大小 / _当前使用速度));
				GlobalPosition = _startPosition.Lerp(_targetPosition, t);
			}
			return;
		}

		// 空闲状态：播放空闲动画并尝试开始移动
		播放空闲动画(_lastDirection);
		TryStartMove();
	}

	private void TryStartMove()
	{
		var 队列 = _玩家.路径队列;
		if (队列 == null || 队列.Count == 0) return;

		// 计算允许的最大索引（玩家路径队列长度 - 滞后格子数 - 1）
		int 最大允许索引 = 队列.Count - 1 - 滞后格子数;
		if (最大允许索引 < 0) 最大允许索引 = 0;

		// 如果当前目标索引已经超过最大允许索引，说明已经跟到滞后位置，不再移动
		if (_当前目标索引 > 最大允许索引) return;

		// 确保目标索引有效
		if (_当前目标索引 >= 队列.Count) return;

		Vector2 目标 = 队列[_当前目标索引];

		// 如果目标与当前位置非常接近（理论上不会，但防御）
		if (GlobalPosition.DistanceTo(目标) < 格子大小 * 0.2f)
		{
			// 直接跳过这个点，索引+1，然后重试
			_当前目标索引++;
			TryStartMove();
			return;
		}

		// 计算落后格子数 = 玩家尾部索引 - 当前目标索引
		int 落后格子 = (队列.Count - 1) - _当前目标索引;

		// 速度决策（落后超过加速阈值且玩家加速时才用高速，否则慢速或保持）
		bool 玩家正在加速 = _玩家.当前移动速度 > _玩家.移动速度;
		if (落后格子 > 加速阈值)
		{
			_当前使用速度 = _玩家.当前移动速度 * 追赶倍数;
		}
		else if (落后格子 <= 减速阈值)
		{
			_当前使用速度 = 正常速度;
		}
		// 中间区域保持当前速度不变

		// 开始移动
		_startPosition = GlobalPosition;
		_targetPosition = 目标;
		_isMoving = true;
		_moveTimeLeft = 格子大小 / _当前使用速度;
		_lastDirection = (_targetPosition - _startPosition).Normalized();
		播放行走动画(_lastDirection);
		if (动画精灵 != null)
			动画精灵.SpeedScale = _当前使用速度 / 正常速度;
	}

	// ------------------ 动画 ------------------
	private void 播放行走动画(Vector2 方向)
	{
		if (动画精灵 == null) return;
		string 动画名 = 获取方向动画名(方向, "walk");
		if (动画精灵.SpriteFrames != null && 动画精灵.SpriteFrames.HasAnimation(动画名))
		{
			if (动画精灵.Animation != 动画名)
				动画精灵.Play(动画名);
		}
	}

	private void 播放空闲动画(Vector2 方向)
	{
		if (动画精灵 == null) return;
		string 空闲动画名 = 获取方向动画名(方向, "idle");
		if (动画精灵.SpriteFrames != null && 动画精灵.SpriteFrames.HasAnimation(空闲动画名))
		{
			if (动画精灵.Animation != 空闲动画名)
				动画精灵.Play(空闲动画名);
		}
		else
		{
			string 行走动画名 = 获取方向动画名(方向, "walk");
			if (动画精灵.SpriteFrames.HasAnimation(行走动画名))
			{
				if (动画精灵.Animation != 行走动画名)
				{
					动画精灵.Play(行走动画名);
					动画精灵.Pause();
					动画精灵.Frame = 0;
				}
			}
		}
	}

	private string 获取方向动画名(Vector2 方向, string 前缀)
	{
		if (Mathf.Abs(方向.X) > Mathf.Abs(方向.Y))
			return 方向.X > 0 ? 前缀 + "_right" : 前缀 + "_left";
		else if (Mathf.Abs(方向.Y) > 0)
			return 方向.Y > 0 ? 前缀 + "_down" : 前缀 + "_up";
		else
			return 前缀 + "_down";
	}
}
