using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HealthBar_UI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private HealthSystem healthSystem;
    [SerializeField] private Image healthBarFill;
    [SerializeField] private TextMeshProUGUI healthText;

    private void Start()
    {
        // Автопоиск HealthSystem если не назначен
        if (healthSystem == null)
            healthSystem = GetComponentInParent<HealthSystem>();

        if (healthSystem == null)
        {
            Debug.LogError("HealthSystem reference missing!", this);
            return;
        }

        // Подписываемся на изменение здоровья
        healthSystem.OnHealthChanged += UpdateHealthBar;

        // Инициализируем с текущими значениями
        UpdateHealthBar(healthSystem.CurrentHealth);
    }

    private void OnDestroy()
    {
        if (healthSystem != null)
            healthSystem.OnHealthChanged -= UpdateHealthBar;
    }

    private void UpdateHealthBar(int currentHealth)
    {
        float fillAmount = (float)currentHealth / healthSystem.MaxHealth;
        healthBarFill.fillAmount = fillAmount;

        if (healthText != null)
            healthText.text = $"{currentHealth}/{healthSystem.MaxHealth}";

        // Дополнительно: визуальный эффект при низком здоровье
        if (fillAmount < 0.3f)
            healthBarFill.color = Color.red;
        else
            healthBarFill.color = Color.green;
    }
}