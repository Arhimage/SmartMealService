using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using WpfSmsApp.Services;

namespace WpfSmsApp.ViewModels
{
    public class DataGridVM : ObservableObject
    {
        private readonly DataItemService _dataItemService;
        private readonly LoggerService _loggerService;
        private ObservableCollection<DataItemVM> _items = new();

        public ObservableCollection<DataItemVM> Items
        {
            get => _items;
            set
            {
                if (_items != value)
                {
                    _items = value;
                    OnPropertyChanged(nameof(Items));
                }
            }
        }

        public DataGridVM(DataItemService dataItemService, LoggerService loggerService)
        {
            _dataItemService = dataItemService;
            _loggerService = loggerService;

            _ = LoadAsync();
        }

        private async Task LoadAsync()
        {
            var items = await _dataItemService.GetAllAsync();
            Items = new ObservableCollection<DataItemVM>(items.Select(i => new DataItemVM(i, _dataItemService, _loggerService)));
            Items.CollectionChanged += OnItemsCollectionChanged;
        }

        private void OnItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    _ = HandleCreateAsync(e);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    _ = HandleRemoveAsync(e);
                    break;
                default:
                    break;
            }
        }

        private async Task HandleCreateAsync(NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems is { Count: > 0 } && e.NewItems[0] is DataItemVM newItem)
            {
                try
                {
                    if (Items.Any(i => ReferenceEquals(i, newItem)))
                        return;
                    await _dataItemService.CreateAsync(newItem.Model);
                    _loggerService.LogCreateEvent(newItem.Model);
                }
                catch (Exception ex)
                {
                    string errorMessage = $"Ошибка создания: {ex.Message}";
                    _loggerService.LogErrorEvent(errorMessage, ex);
                    await Application.Current.Dispatcher.InvokeAsync(() => MessageBox.Show(errorMessage, "Ошибка!", MessageBoxButton.OK, MessageBoxImage.Error));
                }
            }
        }

        private async Task HandleRemoveAsync(NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems is { Count: > 0 } && e.OldItems[0] is DataItemVM oldItem)
            {
                try
                {
                    await _dataItemService.DeleteAsync(oldItem.Model);
                    _loggerService.LogRemoveEvent(oldItem.Model);
                }
                catch (Exception ex)
                {
                    string errorMessage = $"Ошибка удаления: {ex.Message}";
                    await Application.Current.Dispatcher.InvokeAsync(() => MessageBox.Show(errorMessage, "Ошибка!", MessageBoxButton.OK, MessageBoxImage.Error));
                    _loggerService.LogErrorEvent(errorMessage, ex);
                }
            }
        }
    }

}
