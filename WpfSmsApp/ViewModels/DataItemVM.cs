using System.Windows;
using WpfSmsApp.Models;
using WpfSmsApp.Services;

namespace WpfSmsApp.ViewModels
{
    public class DataItemVM : ObservableModel<DataItem>
    {
        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged(nameof(Name));

                    var oldModel = model with { };
                    model.Name = value.Trim();                    
                    DebouncedUpdate(oldModel, model);
                }
            }
        }
        public string Value
        {
            get => _value;
            set
            {
                if (_value != value)
                {
                    _value = value;
                    OnPropertyChanged(nameof(Value));

                    var oldModel = model with { };
                    model.Value = value.Trim();
                    DebouncedUpdate(oldModel, model);
                }
            }
        }
        public string Comment
        {
            get => _comment;
            set
            {
                if (_comment != value)
                {
                    _comment = value;
                    OnPropertyChanged(nameof(Comment));

                    var oldModel = model with { };
                    model.Comment = value.Trim();
                    DebouncedUpdate(oldModel, model);
                }
            }
        }

        private string _name;
        private string _value;
        private string _comment;
        private DataItem? _pendingOldModel;

        private readonly DataItemService _dataItemService;
        private readonly LoggerService _loggerService;
        private CancellationTokenSource _cts = new();

        public DataItemVM() : this(new DataItem() { Name = "EmptyName", Value = "EmptyValue", Comment = "EmptyComment" }, DataItemService.Instance, LoggerService.Instance) { }

        public DataItemVM(DataItem model, DataItemService dataItemService, LoggerService loggerService) : base(model)
        {
            _dataItemService = dataItemService;
            _loggerService = loggerService;

            _name = model.Name;
            _value = model.Value;
            _comment = model.Comment;
        }

        private void DebouncedUpdate(DataItem oldModel, DataItem newModel)
        {
            _pendingOldModel ??= oldModel;

            Delay.Debounce(async () =>
            {
                var capturedOldModel = _pendingOldModel;
                _pendingOldModel = null;

                try
                {
                    await _dataItemService.UpdateAsync(capturedOldModel, newModel);
                }
                catch (Exception ex)
                {
                    string errorMessage = $"Ошибка обновления: {ex.Message}";
                    await Application.Current.Dispatcher.InvokeAsync(() => MessageBox.Show(errorMessage, "Ошибка!", MessageBoxButton.OK, MessageBoxImage.Error));
                    _loggerService.LogErrorEvent(errorMessage, ex);
                }
            }, ref _cts);
        }

    }
}
