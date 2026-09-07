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
## GitHub Actions 构建与发布

本仓库没有提交游戏 DLL。`RATOPIA_DIR` 在 Actions 中会指向一个临时目录，目录里只放编译所需的引用 DLL：

```powershell
# 在已安装 Ratopia 的本机生成依赖包
powershell -ExecutionPolicy Bypass -File .\scripts\package-ci-dependencies.ps1
```

生成的 `RatopiaMod.Nyaiko.YunQing\artifacts\ratopia-ci-deps.zip` 包含 `BepInEx/core` 和 `Ratopia_Data/Managed` 下的必要引用 DLL。当前 CI 使用私有依赖仓库 `jangufo/ratopia-ci-deps` 的 `deps-v1` Release；不要提交或公开发布这些游戏文件。

本仓库已配置两个 Actions secrets：

- `RATOPIA_DEPS_REPO`：`jangufo/ratopia-ci-deps`
- `RATOPIA_DEPS_TOKEN`：workflow 用来读取私有依赖仓库的 token

游戏更新后，可以直接双击仓库根目录下的 `scripts\update-ratopia-ci-deps.cmd`。它会比较当前 `Assembly-CSharp.dll` 的 SHA256 与 `scripts\Assembly-CSharp.sha256`：

- 一致：不做任何更新；
- 不一致：重新收集引用 DLL，替换私有依赖仓库 `deps-v1` Release 中的 `ratopia-ci-deps.zip`，并把新的 SHA256 写回 `scripts\Assembly-CSharp.sha256`。

一键更新脚本需要本机环境变量 `GITHUB_TOKEN` 具备私有依赖仓库的写入权限。更新成功后，请提交并推送 `scripts\Assembly-CSharp.sha256` 的变化。

可手动运行 `.github/workflows/ratopia-mod-nyaiko-yunqing.yml` 获取构建产物；也可以推送 `yunqing-v*` 格式的 tag（例如 `yunqing-v3.0.0`），工作流会自动创建 GitHub Release 并上传 `RatopiaMod.YunQing.All-v版本号.zip`。该 zip 内部保留 `BepInEx/plugins/RatopiaMod.YunQing.All/` 路径，可直接解压到 Ratopia 根目录。

如果不想上传任何游戏 DLL，替代方案是安装 GitHub self-hosted runner，让 runner 所在机器已安装 Ratopia，并把 `RATOPIA_DIR` 配置为 runner 环境变量；此时可以删除 workflow 里的“Download private Ratopia reference assemblies”步骤，并把 `runs-on` 改为对应的 self-hosted label。注意外部 fork PR 默认拿不到 secrets，因此依赖私有 DLL 的 CI 只适合本仓库分支触发。
