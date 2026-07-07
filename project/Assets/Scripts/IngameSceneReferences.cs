using UnityEngine;

public class IngameSceneReferences : MonoBehaviour
{
    public static IngameSceneReferences Instance { get; private set; }

    [Header("Player Slots")]
    public Transform player1Slot;
    public Transform player2Slot;

    [Header("UI Controllers")]
    public HPBarController player1HpBar;
    public HPBarController player2HpBar;
    public RhythmUIController rhythmUI;
    public GameObject endGamePanel;

    [Header("Background Urea")]
    public GameObject audienceParent;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
