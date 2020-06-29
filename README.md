## Introduction

Transform Particle SystemはUnity向けのライブラリです。
添付の動画のように、パーティクルを様々な形状に変形させることができます。


## Getting started

本パーティクルシステムの使い方を解説します。


### TransformParticleSystem

`TransformParticleSystem`は本パーティクルシステムのコア機能を提供します。
Compute Shaderのセットアップからパーティクルの更新、描画までを担当します。


### ParticleTargetGroup

パーティクルターゲットグループは、後述する`ParticleTarget`クラス郡からなるターゲットをグループ化するのに用いられます。

基本的に本パーティクルシステムはグループが最小単位となっているため、ターゲットがひとつである場合でも、このグループクラスを利用する必要があります。

### ParticleTarget

メッシュモデルの頂点をターゲットとする、一番ベーシックなターゲットクラスです。

### TextureParticleTarget

設定したテクスチャのピクセルを元にパーティクルを変形させるためのターゲットクラスです。
パーティクルの色は、対象テクスチャの色に応じて変化させることができます。


### TextParticleTarget

設定されたテクスチャを文字列相当のテクスチャとして扱う、`TextureParticleTarget`の特殊版です。
テクスチャ内の透明部位を無視し、色があるピクセルに対してパーティクルが移動するようになります。


## Author Info

- Twitter: https://twitter.com/edo_m18


## License

このライブラリはApache License 2.0ライセンスの下にあります。
