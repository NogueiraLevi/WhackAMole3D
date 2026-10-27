# Halloween Arcade

Identidade visual aplicada às cenas utilizadas pelo build: `MainMenu`, `Game` e `GameOver`.

- Máquina com pintura roxa, tampo escuro, letreiro e contornos neon, portais e emblema de abóbora.
- Ambiente tridimensional de fliperama com carpete estampado, paredes acústicas, gabinetes e iluminação roxa/verde.
- Modelos e texturas originais dos fantasmas e do martelo reaproveitados. Materiais convertidos para URP.
- O martelo é um acessório visual estático. O controle de clique original continua em `HammerInput`.
- Texturas `Enamel`, `Carpet` e `Wall` geradas proceduralmente, sem dependências externas.
- Pipeline URP e Renderer incluídos no projeto para substituir referências que apontavam para assets ausentes.

## Editar

Abra `Assets/Scenes/Game.unity`. Os objetos decorativos ficam em **Halloween Arcade • Art**; não têm colisores. Os cinco fantasmas, seus colisores e as referências dos componentes originais foram preservados. O mesh visual da máquina usa grupos de materiais separados, sem alterar seu collider.

Menus e HUD usam os componentes e eventos existentes. A mudança do enquadramento é exclusivamente visual; os tempos, regras, contadores e transições permanecem nos scripts originais.

O menu **WhackAMole > Arte > Aplicar ambientação Halloween** recria a direção visual das três cenas e sobrescreve as alterações de arte nelas. Salve seu trabalho antes de usá-lo. O gerador fica em `Assets/Editor/HalloweenArtBuilder.cs`, é exclusivo do Editor e não roda durante a partida.

As prévias renderizadas ficam em `ArtPreviews/`, na raiz do projeto. Elas mostram a cena no Editor; durante a partida o randomizador controla quais fantasmas ficam visíveis.

## Validação

O gerador verifica shaders suportados, ausência de colisores na decoração, os cinco alvos originais e os componentes centrais de jogo. Renderizações das três cenas permitem conferir enquadramento e interface. Isso não substitui testar uma partida interativa no Play Mode.
