using UnityEngine;
using UnityEngine.UI;

public class HealthBar_UI : MonoBehaviour
{
    [SerializeField] private Slider slider;
    [SerializeField] private HealthSystem healthSystem;

    private void Awake()
    {
        // Добавляем проверку на null
        if (healthSystem != null)
        {
            healthSystem.OnHealthChanged += UpdateHealthBar;

            // Инициализируем полосу при старте
            slider.maxValue = healthSystem.MaxHealth;
            slider.value = healthSystem.MaxHealth;
        }
    }

    private void UpdateHealthBar(int currentHealth)
    {
        slider.value = currentHealth;
    }

    private void OnDestroy()
    {
        // Важно отписаться от события при уничтожении объекта
        if (healthSystem != null)
        {
            healthSystem.OnHealthChanged -= UpdateHealthBar;
        }
    }
}