using UnityEngine;
using TMPro;
using System.Collections.Generic;
using Flower;

public class TextManager : MonoBehaviour
{
    public static TextManager Instance { get; private set; }
    private FlowerSystem flowerSys;

    // ★ 全部 private，不序列化，代码里动态找 Clone，彻底杜绝拖到预制体资产上的问题
    private TMP_Text dialogText;
    private TMP_Text nameText;
    private GameObject nameBox;

    [Header("Flower 设置")]
    [SerializeField] private int screenWidth = 1920;
    [SerializeField] private int screenHeight = 1080;

    // ============================================================
    #region 生命周期
    // ============================================================

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        InitFlower();
    }

    private void InitFlower()
    {
        flowerSys = FlowerManager.Instance.CreateFlowerSystem("GameDialogue", false);
        flowerSys.SetScreenReference(screenWidth, screenHeight);
        flowerSys.SetupDialog("MyDialogPrefab", false);
        FindDialogComponents();

        flowerSys.SetupUIStage("bg", "DefaultUIStagePrefab", 0);
        flowerSys.SetupUIStage("char", "DefaultUIStagePrefab", 10);

        // ★ 打字速度：每个字符间隔（秒），越大越慢
        flowerSys.textSpeed = 0.06f; // ← 调到你觉得舒服的值

        flowerSys.textUpdated += OnTextUpdated;
        flowerSys.RegisterCommand("spk", OnSetSpeaker);

        flowerSys.ReadTextFromResource("1");
    }

    /// <summary>
    /// 动态查找 Flower 生成的 MyDialogPrefab(Clone) 里的组件
    /// </summary>
    private void FindDialogComponents()
    {
        // Flower Instantiate 出来的物体默认叫 "MyDialogPrefab(Clone)"
        GameObject clone = GameObject.Find("MyDialogPrefab(Clone)");
        if (clone == null)
        {
            Debug.LogError("[TM] 找不到 MyDialogPrefab(Clone)，Flower 可能没有成功实例化预制体");
            return;
        }

        // 找正文 Text（路径必须跟预制体里的层级完全一致）
        Transform dialogTf = clone.transform.Find("DialogPanel/DialogText");
        if (dialogTf != null)
            dialogText = dialogTf.GetComponent<TMP_Text>();

        // 找名字框
        Transform nameBoxTf = clone.transform.Find("NameBox");
        if (nameBoxTf != null)
        {
            nameBox = nameBoxTf.gameObject;
            Transform nameTextTf = nameBoxTf.Find("NameText");
            if (nameTextTf != null)
                nameText = nameTextTf.GetComponent<TMP_Text>();
        }

        if (nameText == null)
            Debug.LogWarning("[TM] 没找到 NameText，名字框功能不可用");
    }

    /// <summary>
    /// Flower 逐字输出时触发，TrimStart 去掉前导换行
    /// </summary>
    private void OnTextUpdated(object sender, TextUpdateEventArgs args)
    {
        if (dialogText != null)
            dialogText.text = args.text.TrimStart('\n', '\r', ' ');
    }

    void Update()
    {
        if (flowerSys != null && flowerSys.processMode == ProcessModeType.Normal)
        {
            if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space))
                flowerSys.Next();
        }
    }
    #endregion

    // ============================================================
    #region 对外接口
    // ============================================================

    public void PlayStory(string fileName)
    {
        flowerSys.ReadTextFromResource(fileName);
        Debug.Log($"[TextManager] 播放剧情: {fileName}");
    }

    public void PlayDay(int day) => PlayStory(day.ToString());
    public void PlayEnding(string endingKey) => PlayStory(endingKey);
    public void Next() => flowerSys?.Next();
    public void Stop() => flowerSys?.Stop();
    public void Resume() => flowerSys?.Resume();

    public void RegisterCommand(string keyword, FlowerSystem.CharFunction cb)
        => flowerSys.RegisterCommand(keyword, cb);

    public void RegisterCommand(string keyword, System.Action<List<string>> cb)
        => flowerSys.RegisterCommand(keyword, new FlowerSystem.CharFunction(cb));
    #endregion

    // ============================================================
    #region 内部回调
    // ============================================================

    private void OnSetSpeaker(List<string> param)
    {
        string name = param[0].Trim();
        if (string.IsNullOrEmpty(name) || name == "__none__")
        {
            if (nameBox != null) nameBox.SetActive(false);
        }
        else
        {
            if (nameBox != null) nameBox.SetActive(true);
            if (nameText != null) nameText.text = name;
        }
    }
    #endregion
}