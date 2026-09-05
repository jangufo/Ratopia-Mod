# RatopiaMod.Nyaiko.YunQing

## 开发配置：

默认构建只依赖环境变量`RATOPIA_DIR`它会被用来定位Ratopia的安装目录。

其余的目录会根据它来派生。比如BepInExCoreDir=RatopiaDir\BepInEx\core，GameManagedDir=RatopiaDir\Ratopia_Data\Managed

默认release构建后会把dll文件放在`{RATOPIA_DIR}/BepInEx/plugins/{PluginName}/`目录下。

如果要在本地构建项目，请配置环境变量`RATOPIA_DIR`，然后执行：`dotnet build -c Release`。`Debug` 构建只输出到项目的`bin/Debug`目录，不会安装到游戏。如果只需要生成产物而暂时不写入游戏目录，可执行：`dotnet build -c Release -p:DisableRatopiaModInstall=true`。如果 Steam 安装目录权限不足，请在有写入权限的终端中执行构建。

## 项目集合中的mod

### RatopiaMod.YunQing.All

鉴于该mod迁移过很多次，本次直接从版本3.0开始，重新维护，独立更新

原先版本并不会失效，但游戏更新后可能会失效，这里存的肯定是最新的

该mod会在`BepInEx/plugins/RatopiaMod.YunQing.All`文件夹下生成2个文件

1. RatopiaMod.YunQing.All.dll
1. translate-kv.json（翻译文件）

该mod会在`BepInEx/config`文件夹下生成1个文件

1. RatopiaMod.YunQing.YunQingAll.cfg

**在删除时记得清理**该mod不会修改游戏存档

#### RatopiaMod.YunQing.All功能与占用快捷键

1. F9打开控制面板。控制面板中有调节其他功能的开关
1. F3，打开调试控制台（并汉化翻译）
1. F4，打开地形控制台
1. 鱼会淹死在水里，包括鮟鱇鱼
1. 设置五种汇率模式分别生成预期汇率券。负汇率最大值，负汇率，官方正常值，正汇率，正汇率最大值
1. 银行兑换值分别按 `x1`、`x10`、`x100`、`x500` 变化。

#### RatopiaMod.YunQing.All发布历史

v3.0.0
云清把源码添加到奈娅子仓库中新发布的版本
