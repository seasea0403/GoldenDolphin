using GameFramework;
using GameFramework.Event;
using System;
using System.Collections.Generic;
using LingBoCanteen;

namespace LingBoCanteen
{
    /// <summary>新的一天开始事件。</summary>
    public sealed class DayStartEventArgs : GameEventArgs
    {
        public static readonly int EventId = typeof(DayStartEventArgs).GetHashCode();
        public override int Id => EventId;

        public int Day { get; private set; }

        public static DayStartEventArgs Create(int day)
        {
            DayStartEventArgs e = ReferencePool.Acquire<DayStartEventArgs>();
            e.Day = day;
            return e;
        }

        public override void Clear() { Day = 0; }
    }

    /// <summary>营业阶段开始事件（顾客生成开始）。</summary>
    public sealed class BusinessStartEventArgs : GameEventArgs
    {
        public static readonly int EventId = typeof(BusinessStartEventArgs).GetHashCode();
        public override int Id => EventId;

        public int Day { get; private set; }

        public static BusinessStartEventArgs Create(int day)
        {
            BusinessStartEventArgs e = ReferencePool.Acquire<BusinessStartEventArgs>();
            e.Day = day;
            return e;
        }

        public override void Clear() { Day = 0; }
    }

    /// <summary>剧情阶段开始事件。</summary>
    public sealed class StoryPhaseStartEventArgs : GameEventArgs
    {
        public static readonly int EventId = typeof(StoryPhaseStartEventArgs).GetHashCode();
        public override int Id => EventId;

        public int Day { get; private set; }

        public static StoryPhaseStartEventArgs Create(int day)
        {
            StoryPhaseStartEventArgs e = ReferencePool.Acquire<StoryPhaseStartEventArgs>();
            e.Day = day;
            return e;
        }

        public override void Clear() { Day = 0; }
    }

    /// <summary>游戏结束事件。</summary>
    public sealed class GameOverEventArgs : GameEventArgs
    {
        public static readonly int EventId = typeof(GameOverEventArgs).GetHashCode();
        public override int Id => EventId;

        /// <summary>是否为隐藏结局（发现真相）。</summary>
        public bool IsHiddenEnding { get; private set; }

        public static GameOverEventArgs Create(bool isHiddenEnding)
        {
            GameOverEventArgs e = ReferencePool.Acquire<GameOverEventArgs>();
            e.IsHiddenEnding = isHiddenEnding;
            return e;
        }

        public override void Clear() { IsHiddenEnding = false; }
    }
}
