using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;

namespace Oxford
{
    public class Log
    {
        private TextBlock _textBlock;
        private ScrollViewer _scrollViewer;
        private CoreDispatcher _dispatcher;

        public Log(TextBlock textBlock, ScrollViewer scrollViewer, CoreDispatcher dispatcher)
        {
            _textBlock = textBlock;
            _scrollViewer = scrollViewer;
            _dispatcher = dispatcher;
        }

        public async void WriteLine(string log)
        {
            await _dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                _textBlock.Text += log;
                _textBlock.Text += Environment.NewLine;
                _scrollViewer.UpdateLayout();
                _scrollViewer.ScrollToVerticalOffset(_scrollViewer.ScrollableHeight);
            });
        }
    }
}
