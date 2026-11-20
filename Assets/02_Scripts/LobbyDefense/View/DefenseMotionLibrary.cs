using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace LobbyDefense.View
{
    /// <summary>로비의 두 지도에서 사용하는 동작 클립을 필요할 때 읽어 UI용 스프라이트와 반전 상태를 보관한다.</summary>
    internal sealed class DefenseMotionLibrary
    {
        private const int FrameCount = 4;

        /// <summary>로드가 끝난 클립은 첫 표시 때 프레임으로 변환한다.</summary>
        private sealed class Motion
        {
            public AnimationClip Clip;
            public readonly Sprite[] Frames = new Sprite[FrameCount];
            public bool FlipX;
        }

        private readonly Dictionary<(string, string, string), Motion> _motions = new Dictionary<(string, string, string), Motion>();
        private readonly SpriteRenderer _renderer;
        private readonly Animator _animator;

        /// <summary>두 지도를 소유한 로비 뷰 <paramref name="parent"/> 아래에 샘플링 전용 오브젝트를 만든다.</summary>
        internal DefenseMotionLibrary(Transform parent)
        {
            GameObject sampler = new GameObject("Defense Motion Sampler", typeof(SpriteRenderer), typeof(Animator));
            sampler.transform.SetParent(parent, false);
            _renderer = sampler.GetComponent<SpriteRenderer>();
            _renderer.enabled = false;
            _animator = sampler.GetComponent<Animator>();
            _animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        }

        /// <summary>캐릭터 <paramref name="unitId"/>의 동작 <paramref name="motion"/>과 방향 <paramref name="direction"/>(수비는 null)에 맞는 클립을 요청한다.
        /// <paramref name="frameIndex"/>(0~3)의 스프라이트와 가로 반전 여부 <paramref name="flipX"/>를 제공한다. 준비 전에는 null을 반환한다.</summary>
        internal Sprite FindFrame(string unitId, string motion, string direction, int frameIndex, out bool flipX)
        {
            (string, string, string) key = (unitId, motion, direction);
            if (!_motions.TryGetValue(key, out Motion entry))
            {
                entry = RequestClip(unitId, motion, direction);
                if ((motion == "Idle" || motion == "Walk") && !_motions.ContainsKey((unitId, "Attack", direction)))
                {
                    RequestClip(unitId, "Attack", direction);
                }
            }
            if (entry.Clip != null && _animator.isActiveAndEnabled)
            {
                SampleFrames(entry);
                entry.Clip = null;
            }
            flipX = entry.FlipX;
            return entry.Frames[Mathf.Clamp(frameIndex, 0, FrameCount - 1)];
        }

        /// <summary>캐릭터 <paramref name="unitId"/>의 <paramref name="motion"/>·<paramref name="direction"/> 클립을 요청하고 캐시 항목을 반환한다.</summary>
        private Motion RequestClip(string unitId, string motion, string direction)
        {
            Motion entry = new Motion();
            _motions.Add((unitId, motion, direction), entry);
            string address = $"LobbyDefense/Motions/{unitId}/{motion}";
            if (!string.IsNullOrEmpty(direction))
            {
                address += "/" + direction;
            }
            AssetManager.Instance.LoadAssetAsync(new AssetManager.AssetArguments<AnimationClip>
            {
                address = address,
                successCallback = clip => entry.Clip = clip
            });
            return entry;
        }

        /// <summary>로드된 <paramref name="entry"/>의 각 프레임 안쪽을 한 번씩 평가해 원본 스프라이트 참조와 방향을 읽는다.</summary>
        private void SampleFrames(Motion entry)
        {
            AnimationClip clip = entry.Clip;
            _renderer.flipX = false;
            PlayableGraph graph = PlayableGraph.Create(clip.name);
            try
            {
                graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                AnimationPlayableOutput output = AnimationPlayableOutput.Create(graph, "Motion", _animator);
                AnimationClipPlayable playable = AnimationClipPlayable.Create(graph, clip);
                output.SetSourcePlayable(playable);
                graph.Play();
                for (int i = 0; i < FrameCount; i++)
                {
                    playable.SetTime((i + 0.25) / clip.frameRate);
                    graph.Evaluate(0f);
                    entry.Frames[i] = _renderer.sprite;
                }
                entry.FlipX = _renderer.flipX;
            }
            finally
            {
                graph.Destroy();
            }
        }
    }
}
