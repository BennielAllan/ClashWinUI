using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Storage;
using ClashWinUI.Models;

namespace ClashWinUI.Services;

/// <summary>
/// Holds the list of subscriptions/profiles and persists to local app data. Reference: clash-verge-rev subscription list.
/// </summary>
public sealed class SubscriptionService
{
    private const string SubscriptionsKey = "SubscriptionList";
    private readonly ObservableCollection<SubscriptionItem> _items = new();
    private bool _loaded;

    public ObservableCollection<SubscriptionItem> Items => _items;

    public static SubscriptionService Instance { get; } = new();

    private SubscriptionService() { }

    public async Task LoadAsync()
    {
        if (_loaded) return;
        try
        {
            var folder = ApplicationData.Current.LocalFolder;
            var file = await folder.TryGetItemAsync("subscriptions.json") as StorageFile;
            if (file != null)
            {
                var text = await FileIO.ReadTextAsync(file);
                var list = JsonSerializer.Deserialize(text, AppJsonContext.Default.ListSubscriptionItem);
                if (list != null)
                {
                    _items.Clear();
                    foreach (var item in list)
                        _items.Add(item);
                }
            }
        }
        catch { /* ignore */ }
        _loaded = true;
    }

    public async Task SaveAsync()
    {
        try
        {
            var folder = ApplicationData.Current.LocalFolder;
            var file = await folder.CreateFileAsync("subscriptions.json", CreationCollisionOption.ReplaceExisting);
            var list = _items.ToList();
            var json = JsonSerializer.Serialize(list, AppJsonContext.Default.ListSubscriptionItem);
            await FileIO.WriteTextAsync(file, json);
        }
        catch { /* ignore */ }
    }

    public void Add(SubscriptionItem item)
    {
        _items.Add(item);
        _ = SaveAsync();
    }

    public void Remove(SubscriptionItem item)
    {
        _items.Remove(item);
        _ = SaveAsync();
    }

    public void Update(SubscriptionItem item)
    {
        var idx = _items.IndexOf(item);
        if (idx >= 0)
        {
            _items[idx] = item;
            _ = SaveAsync();
        }
    }
}
