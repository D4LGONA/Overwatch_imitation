using System.Collections.Generic;
using TMPro;
using UnityEngine;

// 오른쪽 위 킬로그. 누가 누구를 처치했는지 팀 색으로 띄우고 잠시 뒤 지운다.
// 내가 처치하면 화면 가운데 아래에 "ELIMINATED"도 띄운다.
public class KillFeed : MonoBehaviour
{
    [Tooltip("한 줄의 원본. 배경 이미지 아래에 글자를 둔다. 처음엔 꺼둔다.")]
    [SerializeField] private RectTransform entryTemplate;
    [Tooltip("내가 처치했을 때 뜨는 글자. 비워둬도 된다.")]
    [SerializeField] private TMP_Text eliminatedText;
    [SerializeField] private int maxEntries = 5;
    [SerializeField] private float entryDuration = 5f;
    [SerializeField] private float eliminatedDuration = 2f;
    [SerializeField] private float spacing = 6f;
    [SerializeField] private Color allyColor = new Color(0.35f, 0.7f, 1f);
    [SerializeField] private Color enemyColor = new Color(1f, 0.3f, 0.3f);

    private class Entry
    {
        public RectTransform rect;
        public TMP_Text text;
        public float endTime;
    }

    // 맨 앞이 가장 최근 줄이다.
    private readonly List<Entry> active = new List<Entry>();
    private readonly Stack<Entry> idle = new Stack<Entry>();
    private Health player;
    private float eliminatedEndTime;

    private void OnEnable() => Health.AnyKilled += OnKilled;
    private void OnDisable() => Health.AnyKilled -= OnKilled;

    private void Start()
    {
        entryTemplate.gameObject.SetActive(false);
        if (eliminatedText != null)
            eliminatedText.gameObject.SetActive(false);

        PlayerController.WhenReady(p => player = p.Health);
    }

    private void OnKilled(Health victim, Health killer)
    {
        Entry entry = idle.Count > 0 ? idle.Pop() : CreateEntry();
        entry.text.text = killer != null && killer != victim
            ? $"{Label(killer)}   »   {Label(victim)}"
            : $"»   {Label(victim)}";
        entry.endTime = Time.time + entryDuration;
        entry.rect.gameObject.SetActive(true);
        active.Insert(0, entry);

        // 넘치면 가장 오래된 줄부터 치운다.
        while (active.Count > maxEntries)
            Remove(active[active.Count - 1]);
        Layout();

        if (eliminatedText != null && killer != null && killer == player && victim != player)
        {
            eliminatedText.text = $"ELIMINATED  <color=#{ColorUtility.ToHtmlStringRGB(enemyColor)}>{Name(victim)}</color>";
            eliminatedText.gameObject.SetActive(true);
            SoundManager.Play(SoundId.KillConfirm);
            eliminatedEndTime = Time.time + eliminatedDuration;
        }
    }

    // 플레이어 쪽 팀은 파랑, 상대 팀은 빨강. 원작처럼 내 편 기준으로 칠한다.
    private string Label(Health who)
    {
        bool ally = player != null && !player.IsHostileTo(who);
        string color = ColorUtility.ToHtmlStringRGB(ally ? allyColor : enemyColor);
        return $"<color=#{color}>{Name(who)}</color>";
    }

    private string Name(Health who)
    {
        if (who == player)
            return "TRACER (YOU)";
        return who.name.ToUpperInvariant();
    }

    private Entry CreateEntry()
    {
        RectTransform rect = Instantiate(entryTemplate, entryTemplate.parent);
        return new Entry { rect = rect, text = rect.GetComponentInChildren<TMP_Text>(true) };
    }

    private void Remove(Entry entry)
    {
        entry.rect.gameObject.SetActive(false);
        active.Remove(entry);
        idle.Push(entry);
    }

    // 최근 줄이 맨 위에 오도록 아래로 쌓는다.
    private void Layout()
    {
        float y = 0f;
        foreach (Entry entry in active)
        {
            entry.rect.anchoredPosition = new Vector2(0f, -y);
            y += entry.rect.sizeDelta.y + spacing;
        }
    }

    private void Update()
    {
        bool changed = false;
        for (int i = active.Count - 1; i >= 0; i--)
        {
            if (Time.time >= active[i].endTime)
            {
                Remove(active[i]);
                changed = true;
            }
        }
        if (changed)
            Layout();

        if (eliminatedText != null && eliminatedText.gameObject.activeSelf && Time.time >= eliminatedEndTime)
            eliminatedText.gameObject.SetActive(false);
    }
}
