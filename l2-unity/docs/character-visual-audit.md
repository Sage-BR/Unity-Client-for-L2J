# Auditoria visual de personagens — 14/10/2026

Escopo: nomes sobre o rosto de NPCs e manchas no cabelo do humano fighter.
Preservar L2J4Team Interlude 746 com conteúdo C1; estes defeitos não justificam trocar opcodes.

## Nomes

O deslocamento padrão de -0,12 m colocava o texto dentro da cabeça. O NPC
30048 tem CharacterController de 0,75 m, centro 0,38 m, menor que a altura
derivada do npcgrp (collision_height=22). A âncora agora usa o maior topo entre
colisor e dados de colisão, mais 0,08 m. Usa a transformação do próprio colisor
quando ele pertence a um filho. Mantém altura estável, sem acompanhar os ossos
animados a cada passo. O menu tinha override serializado -0,03 m e foi atualizado.

## Cabelos

CacheHair gravava style*6+color*2, enquanto GetHair consultava style*8+color*2.
Isso sobrescrevia variantes e selecionava peças diferentes. Corrigido o stride
para quatro cores, duas peças por combinação, removendo a compensação especial
da humana fighter que dependia do índice anterior.

Teste nativo confirmou que os 406 prefabs existentes resolvem exatamente para
seu penteado/cor/peça. A comparação visual ainda mostrou manchas no penteado
zero, portanto o cache não explicava sozinho o defeito. As capturas sem cada
camada isolaram o problema na peça frontal `ah`. Troca de camadas, descarte das
faces internas e inversão vertical não resolveram.

Leitura do MFighter.utx instalado em `D:/Jogos/L2 C2 (Splendor)/systextures`
confirmou que os atlas originais têm layout diferente dos `_ori` que estavam
no projeto. Foram restauradas as 12 texturas das famílias m000/m001/m002,
quatro cores cada, preservando seus GUIDs e sem redesenhar pixels. As referências
dos materiais foram corrigidas explicitamente; havia cor apontando para outro
recurso. A camada `bh` conserva o atlas original já correto.

O FinalBlend original dessas famílias usa FrameBufferBlending=2, AlphaTest=True,
TwoSided=True e AlphaRef=5. Materiais agora usam SrcAlpha/OneMinusSrcAlpha,
cutoff 5/255 e fila transparente, mantendo a escrita de profundidade. O teste
nativo verifica as 12 referências e essas propriedades. Os hashes antes/depois
e a origem estão em `character-visual-atlas-provenance.json`.

## Reprodução

Editor: L2CharacterVisualAudit.Run. Relatório e imagens em Logs/CharacterVisual.
O teste instancia os assets reais, verifica os ossos, testa âncoras com colisor
filho deslocado/escalado e compara texturas com iluminação neutra. O material
neutro é temporário e não é salvo nos assets. A captura CorrectedActor usa o
shader real com iluminação ambiente controlada; Corrected usa material neutro
para conferir UV/atlas. Capturas exploratórias antigas sem iluminação configurada
podem estar pretas e não demonstram falha do jogo.

O download publicado do UE Viewer retornou 404; nenhum executável foi instalado.
A extração local foi feita por um leitor restrito ao pacote UE2 123/23, exportando
o primeiro mip DXT3 para PNG sem modificar o cliente C2. Preservados o leitor
exploratório e os recursos extraídos em Logs/CharacterVisual. O leitor não deve
ser tratado como extrator geral de outros pacotes/versões. Referência de formatos:
[UE Viewer, fonte original](https://github.com/gildor2/UEViewer).

O equipamento IL ausente usa o fallback já existente para itens básicos.
A blusa assimétrica do item 21 usa MFighter_m001_t02_u e não é uma textura
corrompida. Os assets das armaduras IL ausentes não foram adicionados nesta tarefa.

## Resultado da validação e build

`Logs/CharacterVisualAuditFinal2.log`: execução concluída sem exceções/erros.
`docs/character-visual/validation.txt`: testes da âncora, 406 prefabs e 12 materiais
aprovados. `docs/character-visual/after.png`: renderização conferida no shader real,
sem a mancha frontal. `before.png` é a reprodução inicial do cabelo; a roupa
do teste inicial era outra peça básica e não deve ser comparada como regressão.

O build `20261014-230021` ficou incompleto, sem BuildSummary, e não é entregável.
Rebuild solicitado em 15/10/2026 e concluído via Unity CLI:
`Builds/WindowsClean/20261015-100947/L2-Unity.exe`.
BuildSummary: Succeeded, zero erros, 139 avisos. Log: `Logs/Rebuild-20261015.log`.

Teste de abertura: mais de 85 segundos aberto/responsivo, modelos e menu carregados,
sem exceções, erro fatal, crash ou erro de shader no log separado
`Logs/RebuildSmoke-20261015.log`. Somente a instância do teste foi encerrada.
Não houve autenticação ou teste de nomes dentro da cidade nesta execução.
Os 139 avisos permanecem registrados; não foram silenciados nem declarados resolvidos.
