namespace WpfSmsApp.ViewModels
{
    public class ObservableModel<T> : ObservableObject
    {
        public ObservableModel(T model)
        {
            this.model = model;
        }

        public T Model { get => model; }

        protected T model;
    }
}
