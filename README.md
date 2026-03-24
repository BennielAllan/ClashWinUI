# ClashWinUI

基于 [mihomo](https://github.com/MetaCubeX/mihomo)（Clash Meta）内核的 Windows 原生代理客户端，使用 WinUI 3 构建，采用 Fluent Design 风格。

## 截图

<img width="1770" height="1215" alt="image" src="https://github.com/user-attachments/assets/e08f5cde-f328-4bfe-ab6d-889b8f62171d" />

## 功能

- 系统代理 / TUN 模式切换
- 订阅管理（远程 URL / 本地文件，自动刷新）
- 代理节点选择与延迟测试
- 实时日志流
- 实时连接监控
- 代理模式切换（规则 / 全局 / 直连）
- 亮色 / 暗色主题，跟随系统
- 中文 / 英文界面

## 系统要求

- Windows 10 1809（Build 17763）或更高版本
- x86 / x64 / ARM64

## 构建

需要：

- Visual Studio 2022（含 Windows App SDK 工作负载）或 .NET 8 SDK
- Windows App SDK 1.8+

```bash
git clone https://github.com/BennielAllan/ClashWinUI.git
cd ClashWinUI
dotnet build ClashWinUI/ClashWinUI.csproj
```

## 使用

1. 将 `mihomo-windows-amd64.exe` 放入 `ClashWinUI/Core/` 目录
2. 如需 TUN 模式，同时将 `wintun.dll` 放入 `Core/` 目录
3. 启动应用，在订阅页面添加订阅
4. 在主页启动内核（TUN 模式需要管理员权限，会弹出 UAC 提示）

## 依赖

- [mihomo](https://github.com/MetaCubeX/mihomo) — 代理内核
- [Windows App SDK](https://github.com/microsoft/WindowsAppSDK) 1.8
- [CommunityToolkit.WinUI](https://github.com/CommunityToolkit/Windows) 8.2

## License

MIT
