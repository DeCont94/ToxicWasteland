using UnityEngine;

namespace ToxicWasteland
{
    /// <summary>Здоровье, аптечки, лом (валюта) и прокачка героя.</summary>
    public class PlayerStats : MonoBehaviour
    {
        public const int StartMaxHp = 5;
        public const int HardMaxHp = 10;
        public const int MaxMedkits = 3;

        public int maxHp = StartMaxHp;
        public int hp = StartMaxHp;
        public int medkits = 0;
        public int medkitHeal = 1;          // после улучшения у торговца — 2
        public int scrap = 0;
        public float deviceCapacity = 100f; // ёмкость устройства (улучшается)

        // счётчики покупок у торговца
        public int hpUpgrades;
        public int batteryUpgrades;
        public bool bandageUpgrade;

        /// <summary>Значение для плавного заполнения ячеек здоровья в HUD.</summary>
        public float displayHp = StartMaxHp;

        public void Heal(int amount)
        {
            hp = Mathf.Min(maxHp, hp + amount);
        }

        void Update()
        {
            float speed = hp > displayHp ? 2.5f : 25f; // лечение заполняется плавно, урон — сразу
            displayHp = Mathf.MoveTowards(displayHp, hp, Time.deltaTime * speed);
        }
    }
}
