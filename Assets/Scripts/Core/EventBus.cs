using System;
using System.Collections.Generic;
using UnityEngine;

namespace MathRPG.Core
{
    /// <summary>모든 게임 이벤트가 구현하는 마커 인터페이스.</summary>
    public interface IGameEvent { }

    /// <summary>
    /// 시스템 간 느슨한 결합을 위한 정적 이벤트 버스 (CLAUDE.md 2-2).
    /// 발행자와 구독자가 서로를 직접 참조하지 않게 해준다.
    /// 예) Combat이 마나 변화를 발행하면 UI가 구독 — Combat은 UI의 존재를 모른다.
    ///
    /// 사용법:
    ///   EventBus.Subscribe&lt;ManaChangedEvent&gt;(OnManaChanged);   // 보통 OnEnable
    ///   EventBus.Unsubscribe&lt;ManaChangedEvent&gt;(OnManaChanged); // 반드시 OnDisable에서 해제
    ///   EventBus.Publish(new ManaChangedEvent { Current = 30f });
    /// </summary>
    public static class EventBus
    {
        // 도메인 리로드가 꺼진 상태(Enter Play Mode Options)에서 이전 플레이 세션의
        // 구독이 남아 파괴된 오브젝트를 호출하는 것을 막기 위한 초기화 목록.
        private static readonly List<Action> ResetActions = new List<Action>();

        private static class Channel<T> where T : IGameEvent
        {
            public static Action<T> Handlers;

            static Channel()
            {
                ResetActions.Add(() => Handlers = null);
            }
        }

        public static void Subscribe<T>(Action<T> handler) where T : IGameEvent
        {
            Channel<T>.Handlers += handler;
        }

        public static void Unsubscribe<T>(Action<T> handler) where T : IGameEvent
        {
            Channel<T>.Handlers -= handler;
        }

        public static void Publish<T>(T gameEvent) where T : IGameEvent
        {
            Channel<T>.Handlers?.Invoke(gameEvent);
        }

        /// <summary>특정 이벤트의 모든 구독을 제거한다. 씬 정리 등 예외적인 경우에만 사용.</summary>
        public static void ClearAll<T>() where T : IGameEvent
        {
            Channel<T>.Handlers = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlayModeStart()
        {
            for (int i = 0; i < ResetActions.Count; i++)
            {
                ResetActions[i].Invoke();
            }
        }
    }
}
