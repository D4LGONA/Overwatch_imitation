using System;
using System.Collections.Generic;
using UnityEngine;

// 효과음 데이터 테이블. 소리마다 클립과 볼륨, 피치 흔들림, 위치 소리 여부를 한 줄씩 적는다.
// 볼륨 같은 값을 코드가 아니라 여기서 고치므로, 소리 조절할 때 코드를 건드리지 않아도 된다.
[CreateAssetMenu(menuName = "Overwatch/Sound Table", fileName = "SoundTable")]
public class SoundTable : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public SoundId id;
        [Tooltip("여러 개면 그중 하나를 무작위로 튼다. 연사하는 소리가 덜 거슬린다.")]
        public AudioClip[] clips;
        [Range(0f, 1f)] public float volume = 1f;
        [Tooltip("재생할 때마다 이 범위에서 피치를 고른다. (1, 1)이면 그대로.")]
        public Vector2 pitchRange = Vector2.one;
        [Tooltip("켜면 위치가 있는 소리라 멀수록 작게 들린다. 끄면 귀에 바로 들린다(내 총소리, UI).")]
        public bool spatial;
        [Tooltip("위치 소리일 때 이 거리 밖에서는 안 들린다.")]
        public float maxDistance = 40f;
    }

    [SerializeField] private List<Entry> entries = new List<Entry>();

    private Dictionary<SoundId, Entry> lookup;

    public Entry Get(SoundId id)
    {
        if (lookup == null)
        {
            lookup = new Dictionary<SoundId, Entry>();
            foreach (Entry entry in entries)
                lookup[entry.id] = entry;
        }

        return lookup.TryGetValue(id, out Entry found) ? found : null;
    }

    // 인스펙터에서 고치면 다시 만들도록 비운다. 빠진 소리가 있으면 바로 알려준다.
    private void OnValidate()
    {
        lookup = null;
        foreach (SoundId id in Enum.GetValues(typeof(SoundId)))
            if (!entries.Exists(e => e.id == id))
                Debug.LogWarning($"[SoundTable] {id} 줄이 없다.", this);
    }
}
