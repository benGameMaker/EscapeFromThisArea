using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class OverPanel : BasePanel
{
    public TextMeshProUGUI txtPeroration;
    public Button btnRestart;
    public Button btnReture;

    public override void Init()
    {
        btnRestart.onClick.AddListener(() =>
        {
            string currentScene = SceneManager.GetActiveScene().name;
            UIManager.Instance.GetPanel<PlayerPanel>().gameObject.SetActive(true);
            LoadingMgr.Instance.LoadSceneAsync(currentScene, "还想再试一次吗？", false, true);
            //HideGameSceneUI();
        });
        btnReture.onClick.AddListener(() =>
        {
            LoadingMgr.Instance.LoadSceneAsync("BeginScene", "正在返回主菜单...", false, true);
            //HideGameSceneUI();
        });
    }

    public override void ShowMe()
    {
        base.ShowMe();
        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = true;
    }

    void HideGameSceneUI()
    {
        UIManager.Instance.HidePanel<VehiclePanel>();
        UIManager.Instance.HidePanel<OverPanel>();
        UIManager.Instance.HidePanel<InteractionPanel>();
    }
}
