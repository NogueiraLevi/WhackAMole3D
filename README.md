# WhackAMole3D

**Jogo 3D desenvolvido em Unity e C# como projeto acadêmico em equipe.**

## Sobre o projeto

O **WhackAMole3D** é uma adaptação do clássico *Whack-a-Mole*. O jogador interage com uma máquina 3D e deve acertar fantasmas que surgem de forma aleatória. O objetivo é acumular acertos e evitar erros durante a partida.

O projeto tem como finalidade aplicar conceitos de desenvolvimento de jogos, programação orientada a objetos, interação 3D, controle de estados, interface e versionamento colaborativo com Git e GitHub.

## Mecânicas implementadas

- **Aparição aleatória:** cinco posições de fantasmas são controladas pelo `MoleRandomizer`, com intervalos configuráveis e opção de evitar repetições consecutivas.
- **Interação com o mouse:** o clique esquerdo é detectado por meio do Input System e de *raycasting* da câmera.
- **Registro de acertos e erros:** o `HitManager` contabiliza acertos, marteladas fora do alvo e cliques atrasados, com suporte a efeitos sonoros.
- **Cronômetro crescente:** o tempo de partida é exibido no formato `MM:SS`.
- **Condição de derrota:** a partida termina ao atingir **10 erros**, considerando a soma de erros de mira e cliques atrasados.
- **Fluxo de cenas:** menu principal, partida e tela de Game Over, com opção de jogar novamente.

> **Estado do projeto:** MVP em desenvolvimento. As mecânicas acima estão implementadas no código; a experiência visual, o balanceamento e a validação completa da execução ainda podem evoluir.

## Tecnologias

| Tecnologia | Utilização |
| --- | --- |
| Unity **6.3.24f1** | Motor de jogo e editor |
| C# | Implementação das regras e interações |
| Unity Input System | Captura do clique do jogador |
| Unity Physics | Detecção de objetos por raycast |
| TextMeshPro | Exibição do cronômetro |
| Git e GitHub | Versionamento e colaboração |

## Como executar

**Pré-requisitos:** Unity Hub, editor **Unity 6.3.24f1** e Git.

1. Clone o repositório:
   ```bash
   git clone https://github.com/NogueiraLevi/WhackAMole3D.git
   ```
2. No **Unity Hub**, selecione **Add project from disk** (ou **Add**) e indique a pasta `WhackAMole3D` clonada.
3. Abra o projeto com a versão de Unity indicada acima. Aguarde a importação dos assets e a resolução dos pacotes.
4. Abra a cena `Assets/Scenes/MainMenu.unity` e pressione **Play**.
5. Use a opção de iniciar a partida; durante o jogo, clique com o botão esquerdo nos fantasmas quando aparecerem.

As cenas previstas na configuração do projeto são `MainMenu`, `Game` e `GameOver`, localizadas em `Assets/Scenes/`.

## Organização do código

Os scripts principais estão em `Assets/Scripts/`:

| Script | Responsabilidade |
| --- | --- |
| `MainMenu.cs` | Inicia a cena de jogo |
| `GameManager.cs` | Controla o cronômetro e o limite de erros |
| `MoleRandomizer.cs` | Sorteia e controla as aparições dos fantasmas |
| `GhostTargert.cs` | Declara a classe `GhostTarget`, que controla o estado de acerto e escape |
| `HammerInput.cs` | Interpreta o clique e detecta o alvo com raycast |
| `HitManager.cs` | Registra estatísticas de acerto/erro e reproduz efeitos sonoros |
| `GameOver.cs` | Declara o menu de Game Over, incluindo reinício e saída |

A lógica está distribuída entre componentes associados aos objetos das cenas e configurados pelo **Inspector** da Unity.

## Regras atuais da partida

1. O cronômetro começa a contar ao entrar na partida.
2. Os fantasmas aparecem em posições e intervalos controlados pelo sistema de aleatoriedade.
3. Acertos válidos aumentam o contador de acertos.
4. Cliques fora de um fantasma válido contam como erro de mira; existe também uma penalidade por clique atrasado.
5. Quando a soma dessas duas categorias alcança **10 erros**, a cena **GameOver** é carregada.

**Observação:** um fantasma desaparecer sem receber um clique não gera, por si só, um erro na regra atual.

## Contexto acadêmico

Desenvolvido de forma colaborativa para uma atividade de faculdade, com foco no aprendizado prático de **Unity, C#, desenvolvimento de jogos 3D e trabalho em equipe**.

---
