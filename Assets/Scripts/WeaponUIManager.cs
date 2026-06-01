using UnityEngine;
using UnityEngine.UI; // Necessário para mexer em componentes de Imagem

public class WeaponUIManager : UnityEngine.MonoBehaviour
{
    [Header("=== WEAPON ICONS ===")]
    [Tooltip("Arrasta para aqui a imagem do ícone da Pedra Normal no teu Canvas")]
    public Image normalRockIcon;

    [Tooltip("Arrasta para aqui a imagem do ícone da Shotgun no teu Canvas")]
    public Image shotgunIcon;

    [Header("=== SETTINGS ===")]
    [Tooltip("A opacidade (alpha) quando a arma não está selecionada (0.3 = 30%)")]
    public float unselectedAlpha = 0.3f;

    private void Start()
    {
        // Quando o jogo começa, a pedra normal é a ativa por defeito
        UpdateWeaponUI(false);
    }

    // Função chamada sempre que o Player clica no botão de trocar arma
    public void UpdateWeaponUI(bool isShotgunActive)
    {
        // Se a Shotgun está ativa, mete o alpha a 1 (100%), senão mete a 0.3 (30%)
        SetOpacity(shotgunIcon, isShotgunActive ? 1f : unselectedAlpha);

        // A Pedra normal faz exatamente o oposto
        SetOpacity(normalRockIcon, !isShotgunActive ? 1f : unselectedAlpha);
    }

    // Função interna para poupar código a mudar as cores
    private void SetOpacity(Image img, float alpha)
    {
        if (img != null)
        {
            Color currentColor = img.color;
            currentColor.a = alpha; // Altera apenas o canal Alpha (Opacidade)
            img.color = currentColor;
        }
    }
}