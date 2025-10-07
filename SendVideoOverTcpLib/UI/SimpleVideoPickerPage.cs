using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace SendVideoOverTCPLib.UI
{
    // A simple modal picker page with grouped sections and exact formatting.
    // Sections: Today, Yesterday, Last 7 Days, Older.
    // Items are single-line, no-wrap with a bullet prefix. Returns original index of selection.
    internal class SimpleVideoPickerPage : ContentPage
    {
        private readonly TaskCompletionSource<int?> _tcs = new();

        public Task<int?> GetSelectionAsync() => _tcs.Task;

        internal sealed class PickerItem
        {
            public string Name { get; set; } = string.Empty;
            public DateTime When { get; set; }
            public int Index { get; set; } // original index in source list
        }

        internal sealed class PickerGroup : ObservableCollection<PickerItem>
        {
            public string Title { get; set; } = string.Empty;
        }

        public SimpleVideoPickerPage(string title, IReadOnlyList<PickerItem> items)
        {
            Title = title;
            BackgroundColor = Colors.White;

            // Instruction label (optional, single spacing)
            var header = new Label
            {
                Text = "Select a Video",
                FontSize = 18,
                FontAttributes = FontAttributes.Bold,
                Margin = new Thickness(12, 12, 12, 6),
                TextColor = Colors.Purple
            };

            // Build groups
            var groups = BuildGroups(items);

            // Grouped CollectionView
            var collection = new CollectionView
            {
                SelectionMode = SelectionMode.Single,
                IsGrouped = true,
                ItemsSource = groups,
                GroupHeaderTemplate = new DataTemplate(() =>
                {
                    var titleLbl = new Label
                    {
                        FontSize = 18,
                        FontAttributes = FontAttributes.Bold | FontAttributes.Italic,
                        TextColor = Colors.Green,
                        Margin = new Thickness(12, 10, 12, 4)
                    };
                    titleLbl.SetBinding(Label.TextProperty, nameof(PickerGroup.Title));
                    return titleLbl;
                }),
                ItemTemplate = new DataTemplate(() =>
                {
                    var label = new Label
                    {
                        LineBreakMode = LineBreakMode.NoWrap,
                        MaxLines = 1,
                        FontSize = 16,
                        FontAttributes = FontAttributes.Italic,
                        TextColor = Colors.Blue,
                        VerticalTextAlignment = TextAlignment.Center,
                    };
                    label.SetBinding(Label.TextProperty, new Binding(nameof(PickerItem.Name),
                        converter: new PrefixConverter("• "))); // bullet prefix

                    return new StackLayout
                    {
                        Orientation = StackOrientation.Horizontal,
                        Spacing = 8,
                        Padding = new Thickness(12, 8),
                        Children = { label }
                    };
                })
            };

            collection.SelectionChanged += (s, e) =>
            {
                if (e.CurrentSelection?.FirstOrDefault() is PickerItem picked)
                {
                    TrySetResult(picked.Index);
                }
            };

            // Buttons row
            var cancelBtn = new Button
            {
                Text = "Cancel",
                BackgroundColor = Colors.LightGray,
                TextColor = Colors.Black,
                HorizontalOptions = LayoutOptions.FillAndExpand
            };
            cancelBtn.Clicked += (s, e) => TrySetResult(null);

            var browseBtn = new Button
            {
                Text = "Browse…",
                BackgroundColor = Color.FromRgb(33, 150, 243),
                TextColor = Colors.White,
                HorizontalOptions = LayoutOptions.FillAndExpand
            };
            browseBtn.Clicked += (s, e) => TrySetResult(-1);

            var buttonsGrid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitionCollection
                {
                    new ColumnDefinition(GridLength.Star),
                    new ColumnDefinition(GridLength.Star)
                },
                ColumnSpacing = 12,
                Padding = new Thickness(12, 8, 12, 12)
            };
            buttonsGrid.Add(cancelBtn, 0, 0);
            buttonsGrid.Add(browseBtn, 1, 0);

            var root = new Grid
            {
                RowDefinitions = new RowDefinitionCollection
                {
                    new RowDefinition(GridLength.Auto),
                    new RowDefinition(GridLength.Star),
                    new RowDefinition(GridLength.Auto)
                },
                RowSpacing = 0
            };

            Grid.SetRow(header, 0);
            Grid.SetRow(collection, 1);
            Grid.SetRow(buttonsGrid, 2);
            root.Children.Add(header);
            root.Children.Add(collection);
            root.Children.Add(buttonsGrid);

            Content = root;
        }

        protected override bool OnBackButtonPressed()
        {
            TrySetResult(null);
            return true; // consume; we'll close ourselves
        }

        private async void TrySetResult(int? result)
        {
            if (!_tcs.Task.IsCompleted)
            {
                _tcs.SetResult(result);
                // Close the modal once result is set
                if (Application.Current?.MainPage?.Navigation != null)
                {
                    await Application.Current.MainPage.Navigation.PopModalAsync();
                }
            }
        }

        private static List<PickerGroup> BuildGroups(IReadOnlyList<PickerItem> items)
        {
            var now = DateTime.Now;
            DateTime todayStart = now.Date;
            DateTime yesterdayStart = todayStart.AddDays(-1);
            DateTime last7Start = todayStart.AddDays(-7);

            var today = new PickerGroup { Title = "Today" };
            var yesterday = new PickerGroup { Title = "Yesterday" };
            var last7 = new PickerGroup { Title = "Last 7 Days" };
            var older = new PickerGroup { Title = "Older" };

            foreach (var it in items)
            {
                if (it.When >= todayStart)
                    today.Add(it);
                else if (it.When >= yesterdayStart && it.When < todayStart)
                    yesterday.Add(it);
                else if (it.When >= last7Start)
                    last7.Add(it);
                else
                    older.Add(it);
            }

            // Only include non-empty groups, in order
            var list = new List<PickerGroup>();
            if (today.Count > 0) list.Add(today);
            if (yesterday.Count > 0) list.Add(yesterday);
            if (last7.Count > 0) list.Add(last7);
            if (older.Count > 0) list.Add(older);
            return list;
        }

        private sealed class PrefixConverter : IValueConverter
        {
            private readonly string _prefix;
            public PrefixConverter(string prefix) => _prefix = prefix;
            public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
                => _prefix + (value?.ToString() ?? string.Empty);
            public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
                => throw new NotImplementedException();
        }
    }
}
