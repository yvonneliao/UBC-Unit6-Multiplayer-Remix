using UnityEngine;

using UnityEngine.UI;
using TMPro;
using Unity.Netcode;
using static UnityEngine.LowLevelPhysics2D.PhysicsLayers;

public class PlayerStatusUI : MonoBehaviour
{
    [SerializeField] Image healthbar;
    [SerializeField] TMP_Text healthText;
    [SerializeField] TMP_Text nameText;
    [SerializeField] TMP_Text livesText;
    [SerializeField] TMP_Text scoreText;

    private NetworkStats stats;

    public void Bind(NetworkStats statsScript)
    {
        Unbind();

        stats = statsScript;
        if (stats != null)
        {
            nameText.text = $"Player {stats.OwnerClientId}";
            stats.CurrentHealth.OnValueChanged += OnHealthChanged;      
            stats.Lives.OnValueChanged += OnLivesChanged;
            stats.Score.OnValueChanged += OnScoreChanged;
        }        

        Refresh();
    }

    public void Unbind()
    {
        if (stats != null)
        {
            nameText.text = " - ";
            stats.CurrentHealth.OnValueChanged -= OnHealthChanged;
            stats.Lives.OnValueChanged -= OnLivesChanged;
            stats.Score.OnValueChanged -= OnScoreChanged;
        }
    }

    private void OnHealthChanged(float oldValue, float newValue)
    {
        Debug.Log("Health Changed!");
        Refresh();
    }

    private void OnLivesChanged(int oldValue, int newValue)
    {
        Debug.Log("Lives Changed!");
        Refresh();
    }

    private void OnScoreChanged(int oldValue, int newValue)
    {

        Debug.Log("Score Changed!");
        Refresh();
    }

    public void Refresh()
    {
        if (stats != null)
        {
            nameText.text = $"Player {stats.OwnerClientId}";
            healthText.text = $"{stats.CurrentHealth.Value} / {stats.MaxHealth}";
            healthbar.fillAmount = stats.CurrentHealth.Value / stats.MaxHealth;

            livesText.text = $"Lives: {stats.Lives.Value}";
            scoreText.text = $"Score: {stats.Score.Value}";
        }
    }

    public void UpdateContent(string name, int health, int lives, int score)
    {
        nameText.text = $"Player {name}";
        healthText.text = health + " / 100";
        healthbar.fillAmount = (float)health / 100f;
        livesText.text = "Lives: " + lives.ToString();
        scoreText.text = "Score: " + score.ToString();
    }

    private void OnDestroy()
    {
        Unbind();
    }

}
