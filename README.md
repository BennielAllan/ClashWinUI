# ClashWinUI

基于 [mihomo](https://github.com/MetaCubeX/mihomo) 内核的 Windows 原生代理客户端，WinUI 3 / Fluent Design。

## 截图

<img width="1770" height="1215" alt="image" src="https://github.com/user-attachments/assets/e08f5cde-f328-4bfe-ab6d-889b8f62171d" />

## 功能

- 系统代理 / TUN 模式
- 订阅管理（URL / 本地文件，自动刷新）
- 节点选择与延迟测试
- 实时日志 / 连接监控
- 代理模式（规则 / 全局 / 直连）
- 系统托盘（代理开关、节点选择）
- 明暗主题，中英双语

## 使用

1. 将 `mihomo-windows-amd64.exe` 放入 `ClashWinUI/Core/`（TUN 模式需额外放入 `wintun.dll`）
2. 启动应用，添加订阅，启动内核

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
