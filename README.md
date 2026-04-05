# ClashWinUI

基于 [mihomo](https://github.com/MetaCubeX/mihomo) 内核的 Windows 原生代理客户端，WinUI 3 / Fluent Design。

## 截图

<img width="1767" height="1211" alt="屏幕截图 2026-04-05 221622" src="https://github.com/user-attachments/assets/2f6bfdfd-595e-45bb-82b0-743edb89f3e7" />

<img width="1764" height="1221" alt="屏幕截图 2026-04-05 221706" src="https://github.com/user-attachments/assets/a4c1479f-f6f6-4a33-8dc0-2c070fba739a" />

<img width="1767" height="1214" alt="屏幕截图 2026-04-05 221736" src="https://github.com/user-attachments/assets/1c9e5c27-70c2-4352-9a62-db24d0d5d9d2" />


## 功能

- 系统代理 / TUN 模式
- 订阅管理（新建订阅，自动下载配置）
- 节点选择与延迟测试
- 实时日志 / 连接监控
- 代理模式（规则 / 全局 / 直连）
- 系统托盘（代理开关、节点选择）
- 最小化到托盘，关闭时可选退出
- 明暗主题，中英双语

## 安装

从 [Releases](https://github.com/BennielAllan/ClashWinUI/releases) 下载最新版本。

1. 双击 `.cer` 证书文件 → 安装到"受信任的根证书颁发机构"
2. 双击 `.msix` 安装包
3. 如提示缺少依赖，安装 `Dependencies/` 下的 `Microsoft.WindowsAppRuntime.1.8.msix`

## 使用

1. 启动应用，点击"新建"添加订阅 URL
2. 选择订阅，点击首页"启动内核"

## 构建

```bash
git clone https://github.com/BennielAllan/ClashWinUI.git
cd ClashWinUI
dotnet build ClashWinUI/ClashWinUI.csproj
```

需要 Visual Studio 2022 或 .NET 8 SDK + Windows App SDK 1.8+

## 依赖

- [mihomo](https://github.com/MetaCubeX/mihomo)
- [Windows App SDK](https://github.com/microsoft/WindowsAppSDK) 1.8
- [CommunityToolkit.WinUI](https://github.com/CommunityToolkit/Windows) 8.2

## License

MIT
