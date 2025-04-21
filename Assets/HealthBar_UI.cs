using UnityEngine;
using UnityEngine.UI;

public class HealthBar_UI : MonoBehaviour
{
    [SerializeField] private Slider slider;
    [SerializeField] private HealthSystem healthSystem;

    private void Awake()
    {
        // ��������� �������� �� null
        if (healthSystem != null)
        {
            healthSystem.OnHealthChanged += UpdateHealthBar;

            // �������������� ������ ��� ������
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
        // ����� ���������� �� ������� ��� ����������� �������
        if (healthSystem != null)
        {
            healthSystem.OnHealthChanged -= UpdateHealthBar;
        }
    }
}