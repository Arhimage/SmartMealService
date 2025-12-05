using System.Windows;

namespace WpfSmsApp
{
    public static class Delay
    {
        public static void Debounce(Func<Task> execute, ref CancellationTokenSource cts, int millisecondsDelay = 500)
        {
            cts?.Cancel();
            cts = new CancellationTokenSource();
            var token = cts.Token;

            _ = Task.Delay(millisecondsDelay, token).ContinueWith(async t =>
            {
                if (!t.IsCanceled)
                {
                    try
                    {
                        await execute();
                    }
                    catch (Exception ex)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                            MessageBox.Show(ex.Message, "Ошибка!", MessageBoxButton.OK, MessageBoxImage.Error));
                    }
                }
            }, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.FromCurrentSynchronizationContext());
        }
    }
}
