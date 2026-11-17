using UnityEngine;

/// <summary>
/// Coloque este script em cada TELA do jogo (painel de UI ou objeto raiz da cena).
/// Quando a tela aparece (o objeto é ativado), a música escolhida começa a tocar.
///
/// Exemplos:
///   Painel "Tela Inicial"        -> Music = Start
///   Objeto raiz da partida       -> Music = Gameplay
///   Painel "Melhores Pontuações" -> Music = HighScores
///   Painel "Game Over"           -> Music = GameOver
///
/// Funciona tanto com telas em cenas separadas quanto com painéis ligados/desligados na mesma cena.
/// </summary>
public class ScreenMusic : MonoBehaviour
{
    [SerializeField] private MusicType music = MusicType.Start;

    private void OnEnable()
    {
        if (GameAudio.Instance != null)
            GameAudio.Instance.PlayMusic(music);
        else
            Debug.LogWarning("ScreenMusic: não há um GameAudio na cena. Crie o objeto 'GameAudio' primeiro.");
    }
}
