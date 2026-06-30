# Dust765 em PC fraco (8GB + vídeo integrado)

Este guia é para computadores com processador de entrada (ex.: Celeron), 8GB de RAM e placa de vídeo integrada.

## Objetivo

Reduzir travamentos e quedas de FPS sem depender de modo automático.

## Ajustes recomendados

Faça estes ajustes no cliente:

1. Limite o FPS em `30`.
2. Ative `Reduce FPS when inactive`.
3. Desative sombras:
   - `ShadowsEnabled = false`
   - `ShadowsStatics = false`
   - `TerrainShadowsLevel = 0`
4. Defina `MaxDynamicLights = 0`.
5. Desative efeitos visuais pesados:
   - `UseCircleOfTransparency = false`
   - `AnimatedWaterEffect = false`
   - `RenderWeather = false`
   - `UseObjectsFading = false`
   - `UseColoredLights = false`
6. Desative recursos visuais adicionais:
   - `PreviewFields = false`
   - `LTHighlightRangeOnCast = false`
   - `LTHighlightRangeOnActivated = false`
   - `TransparentHousesEnabled = false`
   - `InvisibleHousesEnabled = false`
7. Use resolução menor, preferencialmente `1024x768` ou abaixo.

## Configuração do Windows

1. Feche navegador, Discord, launchers e apps de gravação antes de abrir o jogo.
2. Em energia do Windows, use modo `Alto desempenho`.
3. Mantenha pelo menos 10GB livres no disco do sistema.
4. Atualize driver de vídeo integrado para a versão mais recente estável.

## Pasta do UO

Em máquinas fracas, pastas UO mais novas podem pesar mais.
Se houver opção no seu ambiente, teste uma pasta mais leve para comparar desempenho.

## Checklist rápido

- FPS em 30
- Resolução baixa
- Sombras e clima desligados
- Luz dinâmica em 0
- Highlight/Preview/House filters desligados
- Apps de fundo fechados

Com esse perfil, o jogo tende a ficar mais estável em hardware limitado.
