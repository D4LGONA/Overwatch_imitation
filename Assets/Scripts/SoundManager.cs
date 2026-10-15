using UnityEngine;

// SoundTable을 읽어서 효과음을 튼다. 캐릭터가 스포너로 생성되고 씬도 바뀌어서,
// 컴포넌트마다 클립을 꽂는 대신 어디서든 SoundManager.Play(SoundId.Blink)로 부른다.
// 테이블은 Resources/SoundTable에 두고 처음 부를 때 불러온다.
public static class SoundManager
{
    private const int PoolSize = 32;

    private static SoundTable table;
    private static AudioSource[] pool;
    private static int nextIndex;

    // 켜면 모든 소리가 안 들린다. 이미 나고 있던 소리도 바로 끊긴다. 죽어 있는 동안 쓴다.
    // AudioListener.volume은 씬이 바뀌어도 남으므로 판·타이틀 시작할 때 다시 켠다.
    public static void SetMuted(bool muted)
    {
        AudioListener.volume = muted ? 0f : 1f;
    }

    // 귀에 바로 들리는 소리. 테이블에서 위치 소리로 정한 것도 위치 없이 부르면 여기로 온다.
    public static void Play(SoundId id)
    {
        SoundTable.Entry entry = Find(id);
        if (entry != null)
            PlayEntry(entry, Vector3.zero, false);
    }

    // 위치가 있는 소리. 테이블에서 위치 소리가 아니라고 정했으면 위치는 무시하고 그냥 들린다.
    public static void Play(SoundId id, Vector3 position)
    {
        SoundTable.Entry entry = Find(id);
        if (entry != null)
            PlayEntry(entry, position, entry.spatial);
    }

    private static SoundTable.Entry Find(SoundId id)
    {
        if (table == null)
        {
            table = Resources.Load<SoundTable>("SoundTable");
            if (table == null)
            {
                Debug.LogWarning("[SoundManager] Resources/SoundTable을 찾지 못했다.");
                return null;
            }
        }

        SoundTable.Entry entry = table.Get(id);
        if (entry == null || entry.clips == null || entry.clips.Length == 0)
        {
            Debug.LogWarning($"[SoundManager] {id}에 클립이 없다.");
            return null;
        }
        return entry;
    }

    // 매번 오브젝트를 만들지 않도록 미리 만든 소스를 돌려 쓴다. 다 쓰고 있으면 가장 오래된 걸 끊는다.
    // 소스마다 피치가 따로라서 연사할 때 피치를 흔들어도 다른 소리에 영향이 없다.
    private static void PlayEntry(SoundTable.Entry entry, Vector3 position, bool spatial)
    {
        EnsurePool();
        AudioSource source = pool[nextIndex];
        nextIndex = (nextIndex + 1) % pool.Length;

        source.transform.position = position;
        source.spatialBlend = spatial ? 1f : 0f;
        source.maxDistance = entry.maxDistance;
        source.clip = entry.clips[Random.Range(0, entry.clips.Length)];
        source.volume = entry.volume;
        source.pitch = Random.Range(entry.pitchRange.x, entry.pitchRange.y);
        source.Play();
    }

    // 씬이 바뀌어도 소리가 끊기지 않게(타이틀에서 PLAY를 누른 클릭음 등) 한 번 만들고 계속 쓴다.
    private static void EnsurePool()
    {
        if (pool != null && pool[0] != null)
            return;

        GameObject root = new GameObject("SoundManager");
        Object.DontDestroyOnLoad(root);

        pool = new AudioSource[PoolSize];
        for (int i = 0; i < PoolSize; i++)
        {
            GameObject child = new GameObject("Source");
            child.transform.SetParent(root.transform);
            AudioSource source = child.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = 2f;
            pool[i] = source;
        }
    }
}
