using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private MatchManager match;
    [SerializeField] private ObjectPool hitEffects;

    public MatchManager Match => match;
    public ObjectPool HitEffects => hitEffects;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}