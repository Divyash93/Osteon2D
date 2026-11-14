<div align="center">

# Osteon2D

**Um editor gratuito de animação esquelética 2D para Windows, com código-fonte disponível.**

[English](../../README.md) · [हिन्दी](README.hi.md) · [Español](README.es.md) · [简体中文](README.zh-CN.md) · [日本語](README.ja.md) · Português (BR)

</div>

## O que é o Osteon2D?

O Osteon2D é um editor de desktop nativo em que você cria uma hierarquia de ossos sobre a sua arte, vincula imagens ou malhas aos ossos e anima personagens 2D com poses-chave ao longo do tempo. É um único executável pequeno (C++20, Direct3D 11, Dear ImGui): sem navegador, sem runtime separado e sem conta.

> **Inspirado no Spine, não uma cópia.** O Osteon2D é inspirado no [Spine](https://esotericsoftware.com), da Esoteric Software, mas **não pretende replicar o software inteiro**. É um projeto independente, com código e direção próprios. Quando faz sentido, ele lê e grava o formato JSON do Spine, documentado publicamente.

## Download

Baixe o instalador mais recente na **[página de Releases](https://github.com/Divyash93/Osteon2D/releases/latest)**: `OsteonSetup.exe` (instalador, sem direitos de administrador) ou `Osteon-0.1.0-win64.zip` (portátil). Requisitos: Windows 10/11 (x64) e uma GPU com Direct3D 11. É um software em alfa inicial (versão 0.1.0).

## Recursos

- **Rigging:** ossos, slots, skins, ordem de desenho, ferramentas Pose/Create e gizmos de Rotate/Translate/Scale/Shear.
- **Arte:** regiões, malhas, malhas vinculadas, caixas delimitadoras, recorte, caminhos; importação de **PSD**, de uma imagem ou de várias imagens.
- **Pesos:** Bind, Auto, Smooth, Prune, pincel e pintura direta.
- **Restrições:** IK, transformação, caminho e física.
- **Animação:** dopesheet, editor de gráfico, Auto Key e pré-visualização.
- **Saída:** `.ost`, JSON do Spine, sequências PNG, GIF, vídeo e atlas de texturas.
- Salvamento automático com recuperação de falhas; desfazer e refazer em todas as edições.

## Runtimes (incluídos)

Há runtimes para **Godot 4** (GDScript) e **Unity** (C#) na pasta `runtimes/`. O do Unity ainda não foi executado em um Unity real. A exportação binária `.skel` é experimental. Web e engines C/C++ continuam planejados. Guia de uso (em inglês): [docs/runtimes/USER_GUIDE.md](../runtimes/USER_GUIDE.md).

## Idiomas

Este README está disponível em seis idiomas. A interface do editor está disponível em 8 idiomas (Configurações > Geral > Idioma): inglês, espanhol, francês, alemão, português (BR), hindi, chinês simplificado e japonês. Mensagens de status e textos de ajuda longos continuam em inglês.

## Licença e aviso legal

O Osteon2D é distribuído sob a **Osteon 2D Community License (OCL) 1.0** ([LICENSE](../../LICENSE)): uso e modificação gratuitos, e você pode vender o que criar com ele (jogos, animações), mas não vender o Osteon2D em si. O código de terceiros mantém a sua própria licença ([THIRD_PARTY_NOTICES.md](../../THIRD_PARTY_NOTICES.md)). O Osteon2D é um projeto independente, sem afiliação, endosso ou patrocínio da Esoteric Software; "Spine" é marca registrada da Esoteric Software LLC.
